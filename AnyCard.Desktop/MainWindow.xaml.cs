using AnyCard.Desktop.Models;
using AnyCard.Desktop.Services;
using AnyCard.Desktop.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace AnyCard.Desktop;

public partial class MainWindow : Window
{
    private readonly ApiClient _apiClient = new();
    private List<CardDto> _dueCards = new();
    private int _currentCardIndex = 0;
    private List<CategoryDto> _categories = new();
    private readonly CategoryDto _allCategoriesOption = new(0, "Kategorien");
    private int? _selectedCategoryId = null;
    private bool _isUpdatingCategoryList;
    private readonly SettingsView _settingsView;
    private readonly OverviewView _overviewView;
    private readonly CreateCardView _createCardView;
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
    }

    private string GetLoginPassword()
    {
        return _passwordVisible ? UserPasswordVisible.Text : UserPassword.Password;
    }
    private string GetRegisterPassword()
    {
        return _registerPasswordVisible ? RegisterUserPasswordVisible.Text : RegisterUserPassword.Password;
    }
    private async Task<bool> LoadDueCardsAsync()
    {
        var apiResult = await _apiClient.GetDueCardsAsync(_selectedCategoryId);
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
            MessageBox.Show("Fehler beim Laden der Karten.");
            return false;
        }
        _dueCards = apiResult.Data ?? new List<CardDto>();
        _currentCardIndex = 0;
        ShowCurrentCard();
        return true;
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
            CardCategoryBox.Visibility = Visibility.Visible;
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
        if (apiResult.Error == ApiError.Unauthorized)
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
        if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || string.IsNullOrWhiteSpace(GetLoginPassword()))
        {
            MessageBox.Show("Bitte geben Sie E-Mail und Passwort ein.");
            return;
        }
        string email = EmailTextBox.Text;
        string password = GetLoginPassword();

        LoginButton.IsEnabled = false;
        try
        {
            var result = await _apiClient.LoginAsync(email, password);
            switch (result.Error)
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
                case ApiError.TooManyRequests:
                    MessageBox.Show("Zu viele Versuche. Bitte warten Sie einen Moment.");
                    break;
                default:
                    MessageBox.Show("Ein Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.");
                    break;
            }
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RegisterEmailTextBox.Text) || string.IsNullOrWhiteSpace(GetRegisterPassword()))
        {
            MessageBox.Show("Bitte geben Sie E-Mail und Passwort ein.");
            return;
        }

        string email = RegisterEmailTextBox.Text.Trim();
        string password = GetRegisterPassword();

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            MessageBox.Show("Bitte geben Sie eine gültige E-Mail-Adresse ein.");
            return;
        }
        if (password.Length < 8 || password.Length > 100)
        {
            MessageBox.Show("Das Passwort muss zwischen 8 und 100 Zeichen lang sein.");
            return;
        }

        RegisterButton.IsEnabled = false;
        try
        {
            var result = await _apiClient.RegisterAsync(email, password);
            switch (result.Error)
            {
                case ApiError.None:
                    ShowCardsScreen();
                    await LoadCategoriesAsync();
                    await LoadDueCardsAsync();
                    break;
                case ApiError.Conflict:
                    MessageBox.Show("Diese E-Mail-Adresse ist bereits vergeben");
                    break;
                case ApiError.NetworkUnavailable:
                    MessageBox.Show("Server nicht erreichbar.");
                    break;
                case ApiError.InvalidInput:
                    MessageBox.Show("Bitte prüfen Sie E-Mail-Adresse und Passwort (mindestens 8 Zeichen).");
                    break;
                case ApiError.TooManyRequests:
                    MessageBox.Show("Zu viele Versuche. Bitte warten Sie einen Moment.");
                    break;
                default:
                    MessageBox.Show("Ein Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.");
                    break;
            }
        }
        finally
        {
            RegisterButton.IsEnabled = true;
        }
    }

    private async void CreateNewCardButton_Click(object sender, RoutedEventArgs e)
    {
        HideAllScreens();
        BackToCardsButton.Visibility = Visibility.Visible;
        ShowAllCardsButton.Visibility = Visibility.Visible;
        ScreenHost.Content = _createCardView;
        await _createCardView.LoadAsync();
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
    private async void BackToCardsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await LoadDueCardsAsync())
        {
            return;
        }
        HideAllScreens();
        CardPanel.Visibility = Visibility.Visible;
        CreateNewCardButton.Visibility = Visibility.Visible;
        CardCategoryBox.Visibility = Visibility.Visible;
        ShowAllCardsButton.Visibility = Visibility.Visible;
        SettingsButton.Visibility = Visibility.Visible;
    }

    private void ShowLoginScreen()
    {
        HideAllScreens();
        UserPassword.Visibility = Visibility.Visible;
        RegisterUserPassword.Visibility = Visibility.Visible;
        LoginPanel.Visibility = Visibility.Visible;
        LogoutButton.Visibility = Visibility.Collapsed;
        _selectedCategoryId = null;
        CardCategoryBox.ItemsSource = null;
        _dueCards = new();
        _categories = new();
        _currentCardIndex = 0;
        _passwordVisible = false;
        _registerPasswordVisible = false;
        QuestionText.Text = string.Empty;
        RegisterEmailTextBox.Clear();
        RegisterUserPassword.Clear();
        RegisterUserPasswordVisible.Clear();
        UserPasswordVisible.Clear();
        EmailTextBox.Clear();
        UserPassword.Clear();
        ForgotPasswordEmailTextBox.Clear();
        ResetPasswordEmailTextBox.Clear();
        ResetPasswordCodeTextBox.Clear();
        NewUserPassword.Clear();
        RepeatNewUserPassword.Clear();
    }

    private void ShowCardsScreen()
    {
        HideAllScreens();
        CardPanel.Visibility = Visibility.Visible;
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
        if (_isUpdatingCategoryList || CardCategoryBox.SelectedItem is not CategoryDto selected)
        {
            return;
        }

        _selectedCategoryId = selected.Id != 0 ? selected.Id : null;
        await LoadDueCardsAsync();
    }

    private void GoToForgotPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        ForgotPasswordEmailTextBox.Text = EmailTextBox.Text.Trim();
        LoginPanel.Visibility = Visibility.Collapsed;
        ForgotPasswordPanel.Visibility = Visibility.Visible;
    }

    private async void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        ForgotPasswordButton.IsEnabled = false;
        try
        {
            var email = ForgotPasswordEmailTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                MessageBox.Show("Bitte geben Sie eine gültige E-Mail-Adresse ein.");
                return;
            }

            var apiResult = await _apiClient.ForgotPasswordAsync(email);
            if (apiResult.Error == ApiError.None)
            {
                MessageBox.Show("Wenn die E-Mail-Adresse existiert, erhalten Sie eine E-Mail mit Anweisungen zum Zurücksetzen des Passworts.");
                ResetPasswordEmailTextBox.Text = email;
                ForgotPasswordPanel.Visibility = Visibility.Collapsed;
                ResetPasswordPanel.Visibility = Visibility.Visible;
            }
            else if (apiResult.Error == ApiError.NetworkUnavailable)
            {
                MessageBox.Show("Server nicht erreichbar.");
            }
            else if (apiResult.Error == ApiError.InvalidInput)
            {
                MessageBox.Show("Bitte geben Sie eine gültige E-Mail-Adresse ein.");
            }
            else if (apiResult.Error == ApiError.TooManyRequests)
            {
                MessageBox.Show("Zu viele Versuche. Bitte warten Sie einen Moment.");
            }
            else
            {
                MessageBox.Show("Ein Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.");
            }
        }
        finally { ForgotPasswordButton.IsEnabled = true; }
    }

    private void BackToLoginButton_Click(object sender, RoutedEventArgs e)
    {
        ShowLoginScreen();
    }

    private async void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ResetPasswordEmailTextBox.Text) || string.IsNullOrWhiteSpace(ResetPasswordCodeTextBox.Text)
            || string.IsNullOrWhiteSpace(NewUserPassword.Password))
        {
            MessageBox.Show("Bitte geben Sie Ihre E-Mail-Adresse, den Reset-Code und Ihr neues Passwort ein.");
            return;
        }
        if (NewUserPassword.Password.Length < 8 || NewUserPassword.Password.Length > 100
            || NewUserPassword.Password != RepeatNewUserPassword.Password)
        {
            MessageBox.Show("Das neue Passwort entspricht nicht den Anforderungen oder stimmt nicht mit dem Bestätigungspasswort überein.");
            return;
        }

        var email = ResetPasswordEmailTextBox.Text.Trim();

        ResetPasswordButton.IsEnabled = false;
        try
        {
            var apiResult = await _apiClient.ResetPasswordAsync(email, ResetPasswordCodeTextBox.Text.Trim(), NewUserPassword.Password);
            switch (apiResult.Error)
            {
                case ApiError.None:
                    MessageBox.Show("Passwort erfolgreich zurückgesetzt. Sie können sich jetzt anmelden.");
                    ShowLoginScreen();
                    break;
                case ApiError.InvalidInput:
                    MessageBox.Show("Code ungültig oder abgelaufen. Fordern Sie bei Bedarf einen neuen Code an.");
                    break;
                case ApiError.NetworkUnavailable:
                    MessageBox.Show("Server nicht erreichbar.");
                    break;
                case ApiError.TooManyRequests:
                    MessageBox.Show("Zu viele Versuche. Bitte warten Sie einen Moment.");
                    break;
                default:
                    MessageBox.Show("Ein Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.");
                    break;
            }
        }
        finally
        {
            ResetPasswordButton.IsEnabled = true;
        }
    }

    private void GoToResetPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        ResetPasswordEmailTextBox.Text = ForgotPasswordEmailTextBox.Text.Trim();
        ForgotPasswordPanel.Visibility = Visibility.Collapsed;
        ResetPasswordPanel.Visibility = Visibility.Visible;
    }

    private async void ShowAllCardsButton_Click(object sender, RoutedEventArgs e)
    {
        HideAllScreens();
        BackToCardsButton.Visibility = Visibility.Visible;
        SettingsButton.Visibility = Visibility.Visible;
        ScreenHost.Content = _overviewView;
        await _overviewView.LoadAsync();
    }

    private void HideAllScreens()
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        RegisterPanel.Visibility = Visibility.Collapsed;
        ForgotPasswordPanel.Visibility = Visibility.Collapsed;
        ResetPasswordPanel.Visibility = Visibility.Collapsed;
        CardCategoryBox.Visibility = Visibility.Collapsed;
        ShowAllCardsButton.Visibility = Visibility.Collapsed;
        CreateNewCardButton.Visibility = Visibility.Collapsed;
        BackToCardsButton.Visibility = Visibility.Collapsed;
        CardPanel.Visibility = Visibility.Collapsed;
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

}

