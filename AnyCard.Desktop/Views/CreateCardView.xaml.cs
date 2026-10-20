using AnyCard.Desktop.Models;
using AnyCard.Desktop.Services;
using System.Windows;
using System.Windows.Controls;

namespace AnyCard.Desktop.Views;

/// <summary>
/// Interaktionslogik für CreateCardView.xaml
/// </summary>
public partial class CreateCardView : UserControl
{
    private readonly ApiClient _apiClient;
    public event EventHandler? SessionExpired;
    public event EventHandler? CardCreated;
    private List<CategoryDto> _categories = new();

    public CreateCardView(ApiClient apiClient)
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

        _categories = response.Data ?? new List<CategoryDto>();
        CategoryComboBox.ItemsSource = _categories;
    }

    private async void CreateCardButton_Click(object sender, RoutedEventArgs e)
    {
        var categoryName = CategoryComboBox.Text.Trim();
        var question = NewQuestionTextBox.Text.Trim();
        var answer = NewAnswerTextBox.Text.Trim();

        if (string.IsNullOrEmpty(question) || string.IsNullOrEmpty(answer))
        {
            MessageBox.Show("Bitte füllen Sie Frage und Antwort aus.");
            return;
        }
        if (string.IsNullOrEmpty(categoryName))
        {
            MessageBox.Show("Bitte geben Sie einen Kategorienamen ein.");
            return;
        }

        var category = _categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        if (category == null)
        {
            var apiResult = await _apiClient.CreateCategoryAsync(categoryName);
            if (apiResult.Error == ApiError.Unauthorized)
            {
                SessionExpired?.Invoke(this, EventArgs.Empty);
                return;
            }
            if (apiResult.Error == ApiError.None && apiResult.Data != null)
            {
                category = apiResult.Data;
            }
            else if (apiResult.Error == ApiError.Conflict)
            {
                await LoadAsync();
                category = _categories.FirstOrDefault(c =>
                c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                MessageBox.Show("Fehler beim Erstellen der Kategorie.");
                return;
            }
        }
        if (category == null)
        {
            MessageBox.Show("Kategorie konnte nicht ermittelt werden.");
            return;
        }
        var cardResult = await _apiClient.CreateCardAsync(
        question, answer, category.Id);
        if (cardResult.Error == ApiError.Unauthorized)
        {
            SessionExpired?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (cardResult.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Erstellen der Karte.");
            return;
        }

        await LoadAsync();
        CardCreated?.Invoke(this, EventArgs.Empty);
        NewQuestionTextBox.Clear();
        NewAnswerTextBox.Clear();
        CategoryComboBox.Text = string.Empty;
        MessageBox.Show("Karte erfolgreich erstellt.");
    }
}

