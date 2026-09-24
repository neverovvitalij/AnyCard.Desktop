using System.Windows;
using AnyCard.Desktop.Models;
using AnyCard.Desktop.Services;

namespace AnyCard.Desktop;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ApiClient _apiClient = new();
    private List<CardDto> _dueCards = new();
    private int _currentCardIndex = 0;
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
            var categories = apiResult.Data ?? new List<CategoryDto>();
            CategoryComboBox.ItemsSource = categories;
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
        }
        else
        {
            QuestionText.Text = "Keine Karten verfügbar.!";
            ShowAnswerButton.Visibility = Visibility.Collapsed; 
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
        var selectedCategory = CategoryComboBox.SelectedItem as CategoryDto;
        if (selectedCategory == null)
        {
            MessageBox.Show("Bitte wählen Sie eine Kategorie aus.");
            return;
        }
        int categoryId = selectedCategory.Id;

        var apiResult = await _apiClient.CreateCardAsync(NewQuestionTextBox.Text, NewAnswerTextBox.Text,  categoryId);
        if(apiResult.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Erstellen der Karte.");
        }
        else
        {
            MessageBox.Show("Karte erfolgreich erstellt.");
            await LoadDueCardsAsync();
            NewQuestionTextBox.Clear();
            NewAnswerTextBox.Clear();
        }
    }
    private void BackToCardsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowCurrentCard();
        CreateCardPanel.Visibility = Visibility.Collapsed;
        CardPanel.Visibility = Visibility.Visible;
        CreateNewCardButton.Visibility = Visibility.Visible;
        BackToCardsButton.Visibility = Visibility.Collapsed;
    }
}

