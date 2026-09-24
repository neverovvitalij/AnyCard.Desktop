using System.Windows;
using AnyCard.Desktop.Models;
using AnyCard.Desktop.Services;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            MessageBox.Show("Fehler beim Laden der Karten.");
            _dueCards = new List<CardDto>();
        }
        else
        {
            _dueCards = apiResult.Data ?? new List<CardDto>();
            _currentCardIndex = 0;
        }
           ShowCurrentCard();
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
            QuestionText.Text = "Keine Karten verfügbar.";
            ShowAnswerButton.Visibility = Visibility.Collapsed; 
        }
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

    private void CreateNewCardButton_Click(object sender, RoutedEventArgs e)
    {
        CardPanel.Visibility = Visibility.Collapsed;
        CreateNewCardButton.Visibility = Visibility.Collapsed;
        RatingButtonsPanel.Visibility = Visibility.Collapsed;
        CreateCardPanel.Visibility = Visibility.Visible;
        BackToCardsButton.Visibility = Visibility.Visible;
    }

    private void ShowAnswerButton_Click(object sender, RoutedEventArgs e)
    {
        RatingButtonsPanel.Visibility = Visibility.Visible;
        AnswerText.Visibility = Visibility.Visible;
    }
    private async void AgainButton_Click(object sender, RoutedEventArgs e)
    {
        // Handle the "Again" button click
    }
    private async void EasyButton_Click(object sender, RoutedEventArgs e)
    {
        // Handle the "Easy" button click
    }
    private async void GoodButton_Click(object sender, RoutedEventArgs e)
    {
        // Handle the "Good" button click
    }
    private async void HardButton_Click(object sender, RoutedEventArgs e)
    {
        // Handle the "Hard" button click
    }
    private async void CreateCardButton_Click(object sender, RoutedEventArgs e)
    {
       
    }
    private void BackToCardsButton_Click(object sender, RoutedEventArgs e)
    {
        CreateCardPanel.Visibility = Visibility.Collapsed;
        CardPanel.Visibility = Visibility.Visible;
        CreateNewCardButton.Visibility = Visibility.Visible;
        BackToCardsButton.Visibility = Visibility.Collapsed;
        RatingButtonsPanel.Visibility = Visibility.Collapsed;
    }
}

