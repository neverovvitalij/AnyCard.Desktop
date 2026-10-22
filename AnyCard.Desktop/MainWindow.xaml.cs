using AnyCard.Desktop.Models;
using AnyCard.Desktop.Services;
using AnyCard.Desktop.Views;
using System.Windows;

namespace AnyCard.Desktop;

public partial class MainWindow : Window
{
    private readonly ApiClient _apiClient = new();
    private List<CategoryDto> _categories = new();
    private readonly CategoryDto _allCategoriesOption = new(0, "Kategorien");
    private int? _selectedCategoryId = null;
    private bool _isUpdatingCategoryList;
    private readonly SettingsView _settingsView;
    private readonly OverviewView _overviewView;
    private readonly CreateCardView _createCardView;
    private readonly ReviewView _reviewView;
    private readonly AuthView _authView;
    public MainWindow()
    {
        InitializeComponent();
        _settingsView = new SettingsView(_apiClient);
        _settingsView.SessionExpired += (s, e) => ShowLoginScreen();

        _overviewView = new OverviewView(_apiClient);
        _overviewView.SessionExpired += (s, e) => ShowLoginScreen();
        

        _createCardView = new CreateCardView(_apiClient);
        _createCardView.SessionExpired += (s, e) => ShowLoginScreen();
        _createCardView.CardCreated += async (s, e) => await LoadDueCardsAsync();

        _reviewView = new ReviewView(_apiClient);
        _reviewView.SessionExpired += (s, e) => ShowLoginScreen();
        _reviewView.SingleCardReviewed += async (s, e) => 
        {
            ShowOverviewScreen();
            await _overviewView.RefreshAsync();
        };
        _overviewView.CardSelected += async (s, cardId) =>
        {
            ShowCardsScreen();
            await _reviewView.LoadCardAsync(cardId);
        };

        _authView = new AuthView(_apiClient);
        _authView.LoggedIn += async (s, e) =>
        {
            ShowCardsScreen();
            await LoadCategoriesAsync();
            await LoadDueCardsAsync();
        };
        ShowLoginScreen();
    }



    private async Task<bool> LoadCategoriesAsync()
    {
        var apiResult = await _apiClient.GetCategoriesAsync();
        if (apiResult.Error == ApiError.Unauthorized)
        {
            ShowLoginScreen();
            return false;
        }
        if (apiResult.Error == ApiError.NetworkUnavailable)
        {
            MessageBox.Show("Server nicht erreichbar.");
            return false;
        }
        if (apiResult.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Laden der Kategorien.");
            return false;
        }

        _categories = apiResult.Data ?? new List<CategoryDto>();

        var filterItems = new List<CategoryDto> { _allCategoriesOption }.Concat(_categories).ToList();
        var current = filterItems.FirstOrDefault(c => c.Id == (_selectedCategoryId ?? 0));

        _isUpdatingCategoryList = true;
        try
        {
            CardCategoryBox.ItemsSource = filterItems;
            CardCategoryBox.SelectedItem = current ?? _allCategoriesOption;
        }
        finally
        {
            _isUpdatingCategoryList = false;
        }

        if (current == null)
        {
            _selectedCategoryId = null;
        }

        return true;
    }

    private async Task<bool> LoadDueCardsAsync()
    {
        return await _reviewView.LoadAsync(_selectedCategoryId);
    }

    private async void CreateNewCardButton_Click(object sender, RoutedEventArgs e)
    {
        HideAllScreens();
        BackToCardsButton.Visibility = Visibility.Visible;
        ShowAllCardsButton.Visibility = Visibility.Visible;
        ScreenHost.Content = _createCardView;
        await _createCardView.LoadAsync();
    }

    private async void BackToCardsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await LoadDueCardsAsync())
        {
            return;
        }
        HideAllScreens();
        ScreenHost.Content = _reviewView;
        CreateNewCardButton.Visibility = Visibility.Visible;
        CardCategoryBox.Visibility = Visibility.Visible;
        ShowAllCardsButton.Visibility = Visibility.Visible;
        SettingsButton.Visibility = Visibility.Visible;
    }

    private void ShowLoginScreen()
    {
        HideAllScreens();
        LogoutButton.Visibility = Visibility.Collapsed;
        _selectedCategoryId = null;
        CardCategoryBox.ItemsSource = null;
        _categories = new();
        _authView.Reset();
        ScreenHost.Content = _authView;
    }

    private void ShowCardsScreen()
    {
        HideAllScreens();
        ScreenHost.Content = _reviewView;
        CreateNewCardButton.Visibility = Visibility.Visible;
        LogoutButton.Visibility = Visibility.Visible;
        CardCategoryBox.Visibility = Visibility.Visible;
        ShowAllCardsButton.Visibility = Visibility.Visible;
        SettingsButton.Visibility = Visibility.Visible;
    }
    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        await _apiClient.LogoutAsync();
        ShowLoginScreen();
    }

    private async void CardCategoryBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isUpdatingCategoryList || CardCategoryBox.SelectedItem is not CategoryDto selected)
        {
            return;
        }

        _selectedCategoryId = selected.Id != 0 ? selected.Id : null;
        await LoadDueCardsAsync();
    }

    private async void ShowAllCardsButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowOverviewAsync();
    }

    private async Task ShowOverviewAsync()
    {
        ShowOverviewScreen();
        await _overviewView.LoadAsync();
    }

    private void HideAllScreens()
    {
        CardCategoryBox.Visibility = Visibility.Collapsed;
        ShowAllCardsButton.Visibility = Visibility.Collapsed;
        CreateNewCardButton.Visibility = Visibility.Collapsed;
        BackToCardsButton.Visibility = Visibility.Collapsed;
        ScreenHost.Content = null;
    }

    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        HideAllScreens();
        SettingsButton.Visibility = Visibility.Collapsed;
        BackToCardsButton.Visibility = Visibility.Visible;
        ShowAllCardsButton.Visibility = Visibility.Visible;
        ScreenHost.Content = _settingsView;
        await _settingsView.LoadAsync();
    }

    private void ShowOverviewScreen()
    {
        HideAllScreens();
        BackToCardsButton.Visibility = Visibility.Visible;
        SettingsButton.Visibility = Visibility.Visible;
        ScreenHost.Content = _overviewView;
    }
}

