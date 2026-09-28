using System.Windows;
using AnyCard.Desktop.Models;
using AnyCard.Desktop.Services;

namespace AnyCard.Desktop;
public partial class MainWindow : Window
{
    private readonly ApiClient _apiClient = new();
    private List<CardDto> _dueCards = new();
    private int _currentCardIndex = 0;
    private List<CategoryDto> _categories = new();
    public MainWindow()
    {
        InitializeComponent();
    }

    private async Task LoadDueCardsAsync()
    {
        var apiResult = await _apiClient.GetDueCardsAsync();
        if(apiResult.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Laden der Karten.!");
            _dueCards = new List<CardDto>();
        }
        else
        {
            _dueCards = apiResult.Data ?? new List<CardDto>();
            _currentCardIndex = 0;
        }
           ShowCurrentCard();
    }
    private async Task LoadCategoriesAsync()
    {
        var apiResult = await _apiClient.GetCategoriesAsync();
        if (apiResult.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Laden der Kategorien.");
        }
        else
        {
            _categories = apiResult.Data ?? new List<CategoryDto>();
            CategoryComboBox.ItemsSource = _categories;
        }
    }
    private void ShowCurrentCard()
    {
        if (_dueCards.Count > 0 && _currentCardIndex < _dueCards.Count)
        {
            var currentCard = _dueCards[_currentCardIndex];
            QuestionText.Text = currentCard.Question;
            AnswerText.Text = currentCard.Answer;
            ShowAnswerButton.Visibility = Visibility.Visible;
            HideAnswerButton.Visibility = Visibility.Collapsed;
            RatingButtonsPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            QuestionText.Text = "Keine Karten verfügbar.";
            ShowAnswerButton.Visibility = Visibility.Collapsed; 
            HideAnswerButton.Visibility = Visibility.Collapsed;
        }
    }
    private async Task LoadNextCard(int cardId, UserRating userRating)
    {
            var apiResult = await _apiClient.ReviewCardAsync(cardId, userRating);
            if(apiResult.Error != ApiError.None)
            {
            MessageBox.Show("Fehler beim Bewerten der Karte.");
        }
            _currentCardIndex++;
            ShowCurrentCard();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        string username = UsernameTextBox.Text;
        string password = UserPassword.Password;
       
        var authResponse = await _apiClient.LoginAsync(username, password);
        if(authResponse != null)
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            CardPanel.Visibility = Visibility.Visible;
            CreateNewCardButton.Visibility = Visibility.Visible;
            await LoadDueCardsAsync();
        }
        else
        {
            MessageBox.Show("Benutzername oder Passwort falsch.");
        }
    }

    private async void CreateNewCardButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadCategoriesAsync();
        CardPanel.Visibility = Visibility.Collapsed;
        CreateNewCardButton.Visibility = Visibility.Collapsed;
        CreateCardPanel.Visibility = Visibility.Visible;
        BackToCardsButton.Visibility = Visibility.Visible;
    }

    private void ShowAnswerButton_Click(object sender, RoutedEventArgs e)
    {
        AnswerText.Visibility = Visibility.Visible;
        RatingButtonsPanel.Visibility = Visibility.Visible;
        HideAnswerButton.Visibility = Visibility.Visible;
        ShowAnswerButton.Visibility = Visibility.Collapsed;
    }
    private void HideAnswerButton_Click(object sender, RoutedEventArgs e)
    {
        AnswerText.Visibility = Visibility.Collapsed;
        RatingButtonsPanel.Visibility = Visibility.Collapsed;
        HideAnswerButton.Visibility = Visibility.Collapsed;
        ShowAnswerButton.Visibility = Visibility.Visible;
    }
    private async void AgainButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadNextCard(_dueCards[_currentCardIndex].Id, UserRating.Again);
        AnswerText.Visibility = Visibility.Collapsed;
        RatingButtonsPanel.Visibility = Visibility.Collapsed;

    }
    private async void EasyButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadNextCard(_dueCards[_currentCardIndex].Id, UserRating.Easy);
        AnswerText.Visibility = Visibility.Collapsed;
        RatingButtonsPanel.Visibility = Visibility.Collapsed;
    }
    private async void GoodButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadNextCard(_dueCards[_currentCardIndex].Id, UserRating.Good);
        AnswerText.Visibility = Visibility.Collapsed;
        RatingButtonsPanel.Visibility = Visibility.Collapsed;
    }
    private async void HardButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadNextCard(_dueCards[_currentCardIndex].Id, UserRating.Hard);
        AnswerText.Visibility = Visibility.Collapsed;
        RatingButtonsPanel.Visibility = Visibility.Collapsed;
    }
    private async void CreateCardButton_Click(object sender, RoutedEventArgs e)
    {
        var categoryName = CategoryComboBox.Text.Trim();
        var qestion = NewQuestionTextBox.Text.Trim();
        var answer = NewAnswerTextBox.Text.Trim();
        if (string.IsNullOrEmpty(qestion) || string.IsNullOrEmpty(answer))
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
            if (apiResult.Error == ApiError.None && apiResult.Data != null)
            {
                category = apiResult.Data;
            }
            else if (apiResult.Error == ApiError.Conflict)
            {
                await LoadCategoriesAsync();
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
        NewQuestionTextBox.Text, NewAnswerTextBox.Text, category.Id);

        if (cardResult.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Erstellen der Karte.");
            return;
        }

        await LoadCategoriesAsync();
        await LoadDueCardsAsync();
        NewQuestionTextBox.Clear();
        NewAnswerTextBox.Clear();
        CategoryComboBox.Text = string.Empty;
        MessageBox.Show("Karte erfolgreich erstellt.");
    }
    private async void BackToCardsButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadDueCardsAsync();
        CreateCardPanel.Visibility = Visibility.Collapsed;
        CardPanel.Visibility = Visibility.Visible;
        CreateNewCardButton.Visibility = Visibility.Visible;
        BackToCardsButton.Visibility = Visibility.Collapsed;
        AnswerText.Visibility = Visibility.Collapsed;
    }
}

