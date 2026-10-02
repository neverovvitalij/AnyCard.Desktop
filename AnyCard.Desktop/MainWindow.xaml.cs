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
    private readonly CategoryDto _allCategoriesOption = new(0, "Alle");
    private int? _selectedCategoryId = null;
    public MainWindow()
    {
        InitializeComponent();
    }

    private string GetLoginPassword()
    {
        return _passwordVisible ? UserPasswordVisible.Text : UserPassword.Password;
    }
    private string GetRegisterPassword()
    {
        return _registerPasswordVisible ? RegisterUserPasswordVisible.Text : RegisterUserPassword.Password;
    }
    private async Task LoadDueCardsAsync()
    {
        var apiResult = await _apiClient.GetDueCardsAsync(_selectedCategoryId);
        if(apiResult.Error == ApiError.Unauthorized)
        {
            ShowLoginScreen();
            return;
        }
        if (apiResult.Error != ApiError.None)
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
    private async Task LoadCategoriesAsync()
    {
        var apiResult = await _apiClient.GetCategoriesAsync();
        if(apiResult.Error == ApiError.Unauthorized)
        {
            ShowLoginScreen();
            return;
        }
        if (apiResult.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Laden der Kategorien.");
        }
        else
        {
            _categories = apiResult.Data ?? new List<CategoryDto>();
            CategoryComboBox.ItemsSource = _categories;
            CardCategoryBox.ItemsSource = new List<CategoryDto> { _allCategoriesOption }.Concat(_categories).ToList();
            CardCategoryBox.SelectedIndex = 0;
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
            AnswerText.Text = string.Empty;
            AnswerText.Visibility = Visibility.Collapsed;
            ShowAnswerButton.Visibility = Visibility.Collapsed; 
            HideAnswerButton.Visibility = Visibility.Collapsed;
            RatingButtonsPanel.Visibility = Visibility.Collapsed;
        }
    }
    private async Task LoadNextCard(int cardId, UserRating userRating)
    {
        var apiResult = await _apiClient.ReviewCardAsync(cardId, userRating);
            if(apiResult.Error == ApiError.Unauthorized)
        {
            ShowLoginScreen();
            return;
        }
            if (apiResult.Error != ApiError.None)
            {
            MessageBox.Show("Fehler beim Bewerten der Karte.");
        }
            _currentCardIndex++;
            ShowCurrentCard();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if(string.IsNullOrWhiteSpace(EmailTextBox.Text) || string.IsNullOrWhiteSpace(GetLoginPassword()))
        {
            MessageBox.Show("Bitte geben Sie E-Mail und Passwort ein.");
            return;
        }
        string email = EmailTextBox.Text;
        string password = GetLoginPassword();
       
        var result = await _apiClient.LoginAsync(email, password);
        switch(result.Error)
        {
            case ApiError.None:
                ShowCardsScreen();
                await LoadCategoriesAsync();
                await LoadDueCardsAsync();
                break;
            case ApiError.Unauthorized:
                MessageBox.Show("E-Mail oder Passwort falsch.");
                break;
            case ApiError.NetworkUnavailable:
                MessageBox.Show("Server nicht erreichbar.");
                break;
            default:
                MessageBox.Show("Ein Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.");
                break;
        }
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RegisterEmailTextBox.Text) || string.IsNullOrWhiteSpace(GetRegisterPassword()))
        {
            MessageBox.Show("Bitte geben Sie E-Mail und Passwort ein.");
            return;
        }
        string email = RegisterEmailTextBox.Text;
        string password = GetRegisterPassword();

        var result = await _apiClient.RegisterAsync(email, password);
        switch (result.Error)
        {
            case ApiError.None:
                ShowCardsScreen();
                await LoadDueCardsAsync();
                break;
            case ApiError.Conflict:
                MessageBox.Show("Diese E-Mail-Adresse ist bereits vergeben");
                break;
            case ApiError.NetworkUnavailable:
                MessageBox.Show("Server nicht erreichbar.");
                break;
            default:
                MessageBox.Show("Ein Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.");
                break;
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
            if(apiResult.Error == ApiError.Unauthorized)
            {
                ShowLoginScreen();
                return;
            }
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
        if(cardResult.Error == ApiError.Unauthorized)
        {
            ShowLoginScreen();
            return;
        }

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
        if(LoginPanel.Visibility == Visibility.Visible)
        {
            return;
        }
        CreateCardPanel.Visibility = Visibility.Collapsed;
        CardPanel.Visibility = Visibility.Visible;
        CreateNewCardButton.Visibility = Visibility.Visible;
        BackToCardsButton.Visibility = Visibility.Collapsed;
        AnswerText.Visibility = Visibility.Collapsed;
    }

    private void ShowLoginScreen()
    {
        RegisterPanel.Visibility = Visibility.Collapsed;
        LoginPanel.Visibility = Visibility.Visible;
        CardPanel.Visibility = Visibility.Collapsed;
        CreateNewCardButton.Visibility = Visibility.Collapsed;
        BackToCardsButton.Visibility = Visibility.Collapsed;
        LogoutButton.Visibility = Visibility.Collapsed;
        CreateCardPanel.Visibility = Visibility.Collapsed;
        CardCategoryBox.Visibility = Visibility.Collapsed;
        _selectedCategoryId = null;
        RegisterEmailTextBox.Clear();
        RegisterUserPassword.Clear();
        RegisterUserPasswordVisible.Clear();
        UserPasswordVisible.Clear();
        EmailTextBox.Clear();
        UserPassword.Clear();
    }

    private void ShowCardsScreen()
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        CardPanel.Visibility = Visibility.Visible;
        CreateNewCardButton.Visibility = Visibility.Visible;
        LogoutButton.Visibility = Visibility.Visible;
        RegisterPanel.Visibility = Visibility.Collapsed;
        CardCategoryBox.Visibility = Visibility.Visible;
    }
    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        await _apiClient.LogoutAsync();
        ShowLoginScreen();
    }

    private bool _passwordVisible = false;

    private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_passwordVisible)
        {
            UserPassword.Password = UserPasswordVisible.Text;
            UserPasswordVisible.Visibility = Visibility.Collapsed;
            UserPassword.Visibility = Visibility.Visible;
        }
        else
        {
            UserPasswordVisible.Text = UserPassword.Password;
            UserPassword.Visibility = Visibility.Collapsed;
            UserPasswordVisible.Visibility = Visibility.Visible;
        }
        _passwordVisible = !_passwordVisible;
    }

    private bool _registerPasswordVisible = false;

    private void ToggleRegisterLoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (_registerPasswordVisible)
        {
            RegisterUserPassword.Password = RegisterUserPasswordVisible.Text;
            RegisterUserPasswordVisible.Visibility = Visibility.Collapsed;
            RegisterUserPassword.Visibility = Visibility.Visible;
        }
        else
        {
            RegisterUserPasswordVisible.Text = RegisterUserPassword.Password;
            RegisterUserPassword.Visibility = Visibility.Collapsed;
            RegisterUserPasswordVisible.Visibility = Visibility.Visible;
        }
        _registerPasswordVisible = !_registerPasswordVisible;
    }

    private void SwitchRegisterLoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (LoginPanel.Visibility == Visibility.Visible)
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Visible;
        }
        else
        {
            LoginPanel.Visibility = Visibility.Visible;
            RegisterPanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void CardCategoryBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var selected = CardCategoryBox.SelectedItem as CategoryDto;
        _selectedCategoryId = selected != null && selected.Id != 0 ? selected.Id : null;
        await LoadDueCardsAsync();
    }
}

