using AnyCard.Desktop.Models;
using AnyCard.Desktop.Services;
using System.Windows;
using System.Windows.Controls;

namespace AnyCard.Desktop.Views;
/// <summary>
/// Interaktionslogik für OverviewView.xaml
/// </summary>
public partial class OverviewView : UserControl
{
    private readonly ApiClient _apiClient;
    public event EventHandler? SessionExpired;
    private int _overviewPage = 1;
    private bool _isInitializing = true;
    private readonly CategoryDto _allCategoriesOption = new(0, "Kategorien");
    public OverviewView(ApiClient apiClient)
    {
        InitializeComponent();
        _apiClient = apiClient;
    }

    public async Task LoadAsync()
    {
        var response = await _apiClient.GetCategoriesAsync();
        if (response.Error == ApiError.Unauthorized)
        {
            SessionExpired?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (response.Error == ApiError.NetworkUnavailable)
        {
            MessageBox.Show("Server nicht erreichbar.");
            return;
        }
        if (response.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Laden der Kategorien.");
            return;
        }

        var categories = response.Data ?? new List<CategoryDto>();
        _isInitializing = true;
        try
        {
            var filterItems = new List<CategoryDto> { _allCategoriesOption }.Concat(categories).ToList();

            OverviewCategoryBox.ItemsSource = filterItems;
            OverviewCategoryBox.SelectedItem = _allCategoriesOption;
            OverviewRatingBox.SelectedIndex = 0;
            _overviewPage = 1;
        }
        finally
        {
            _isInitializing = false;
        }

        await LoadSummaryAsync();
        await LoadOverviewAsync();
    }

    private async Task LoadOverviewAsync()
    {
        UserRating? userRating = OverviewRatingBox.SelectedIndex == 0
            ? null
            : (UserRating)(OverviewRatingBox.SelectedIndex - 1);

        var selected = OverviewCategoryBox.SelectedItem as CategoryDto;
        int? categoryId = selected == null || selected.Id == 0 ? null : selected.Id;

        var response = await _apiClient.GetCardsOverviewAsync(categoryId, userRating, _overviewPage);
        if (response.Error == ApiError.Unauthorized)
        {
            SessionExpired?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (response.Error == ApiError.NetworkUnavailable)
        {
            MessageBox.Show("Server nicht erreichbar.");
            return;
        }
        if (response.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Laden der Karten.");
            return;
        }

        var result = response.Data!.Items.Select(ct => ct with { NextShowtime = ct.NextShowtime.ToLocalTime() }).ToList();
        OverviewCardsList.ItemsSource = result;
        OverviewEmptyText.Visibility = result.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        PageInfoText.Text = $"Seite {response.Data.PageNumber} von {response.Data.TotalPages}";
        PreviousPageButton.IsEnabled = response.Data.PageNumber > 1;
        NextPageButton.IsEnabled = response.Data.TotalPages > response.Data.PageNumber;
    }

    private async Task LoadSummaryAsync()
    {
        var response = await _apiClient.GetProgressSummaryAsync();
        if (response.Error == ApiError.Unauthorized)
        {
            SessionExpired?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (response.Error == ApiError.NetworkUnavailable)
        {
            MessageBox.Show("Server nicht erreichbar.");
            return;
        }
        if (response.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Laden der Statistik.");
            return;
        }

        SummaryCardsText.Text = $"Karten: {response.Data!.CardsCount}";
        SummaryCategoriesText.Text = $"Kategorien: {response.Data.CategoriesCount}";
        SummaryReviewText.Text = $"Fällig: {response.Data.CardsToReviewCount}";
    }
    private async void OverviewFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }
        _overviewPage = 1;
        await LoadOverviewAsync();
    }

    private async void PreviousPageButton_Click(object sender, RoutedEventArgs e)
    {
        _overviewPage--;
        await LoadOverviewAsync();
    }

    private async void NextPageButton_Click(object sender, RoutedEventArgs e)
    {
        _overviewPage++;
        await LoadOverviewAsync();
    }
}
