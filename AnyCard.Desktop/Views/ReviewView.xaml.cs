using AnyCard.Desktop.Models;
using AnyCard.Desktop.Services;
using System.Windows;
using System.Windows.Controls;

namespace AnyCard.Desktop.Views;

/// <summary>
/// Interaktionslogik für ReviewView.xaml
/// </summary>
public partial class ReviewView : UserControl
{
    private readonly ApiClient _apiClient;
    public event EventHandler? SessionExpired;
    private List<CardDto> _dueCards = new();
    private int _currentCardIndex = 0;
    private bool _isSingleCardMode = false;
    public event EventHandler? SingleCardReviewed;
    public ReviewView(ApiClient apiClient)
    {
        InitializeComponent();
        _apiClient = apiClient;
        
    }

    public async Task<bool> LoadAsync(int? categoryId)
    {

        var apiResult = await _apiClient.GetDueCardsAsync(categoryId);
        if (apiResult.Error == ApiError.Unauthorized)
        {
            SessionExpired?.Invoke(this, EventArgs.Empty);
            return false;
        }
        if (apiResult.Error == ApiError.NetworkUnavailable)
        {
            MessageBox.Show("Server nicht erreichbar.");
            return false;
        }
        if (apiResult.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Laden der Karten.");
            return false;
        }
        _dueCards = apiResult.Data ?? new List<CardDto>();
        _currentCardIndex = 0;
        _isSingleCardMode = false;
        ShowCurrentCard();
        return true;
    }
    private void ShowCurrentCard()
    {
        if (_dueCards.Count > 0 && _currentCardIndex < _dueCards.Count)
        {
            var currentCard = _dueCards[_currentCardIndex];
            QuestionText.Text = currentCard.Question;
            AnswerText.Text = currentCard.Answer;
            ShowAnswerButton.Visibility = Visibility.Visible;
            DeleteCardButton.Visibility = Visibility.Visible;
            HideAnswerButton.Visibility = Visibility.Collapsed;
            RatingButtonsPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            QuestionText.Text = "Keine Karten verfügbar.";
            AnswerText.Text = string.Empty;
            AnswerText.Visibility = Visibility.Collapsed;
            ShowAnswerButton.Visibility = Visibility.Collapsed;
            HideAnswerButton.Visibility = Visibility.Collapsed;
            RatingButtonsPanel.Visibility = Visibility.Collapsed;
            DeleteCardButton.Visibility = Visibility.Collapsed;
        }
    }
    private async Task LoadNextCard(int cardId, UserRating userRating)
    {
        var apiResult = await _apiClient.ReviewCardAsync(cardId, userRating);
        if (apiResult.Error == ApiError.Unauthorized)
        {
            SessionExpired?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (apiResult.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Bewerten der Karte.");
        }
        if (_isSingleCardMode)
        {
            _isSingleCardMode = false;
            SingleCardReviewed?.Invoke(this, EventArgs.Empty);
            return;
        }
        _currentCardIndex++;
        ShowCurrentCard();
    }

    public async Task<bool> LoadCardByIdAsync(int cardId)
    {
        var response = await _apiClient.GetCardByIdAsync(cardId);
        if (response.Error == ApiError.Unauthorized)
        {
            SessionExpired?.Invoke(this, EventArgs.Empty);
            return false;
        }
        if (response.Error == ApiError.NetworkUnavailable)
        {
            MessageBox.Show("Server nicht erreichbar.");
            return false;
        }
        if (response.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Laden der Karte.");
            return false;
        }

        _dueCards = new List<CardDto> { response.Data! };
        _currentCardIndex = 0;
        _isSingleCardMode = true;
        ShowCurrentCard();
        return true;
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

    private async void DeleteCardButton_Click(object sender, RoutedEventArgs e)
    {
        var data = _dueCards[_currentCardIndex];
        var result = MessageBox.Show(
            $"Karte \"{data.Question}\" wirklich löschen?",
            "Karte löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }
        var response = await _apiClient.DeleteCardAsync(data.Id);
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
            MessageBox.Show("Fehler beim Löschen der Karte.");
            return;
        }
        if (_isSingleCardMode)
        {
            _isSingleCardMode = false;
            SingleCardReviewed?.Invoke(this, EventArgs.Empty);
            return;
        }
        _dueCards.RemoveAt(_currentCardIndex);
        ShowCurrentCard();
    }
}

