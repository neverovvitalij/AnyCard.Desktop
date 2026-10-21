using AnyCard.Desktop.Services;
using System.Windows;
using System.Windows.Controls;

namespace AnyCard.Desktop.Views;
/// <summary>
/// Interaktionslogik für AuthView.xaml
/// </summary>
public partial class AuthView : UserControl
{
    private readonly ApiClient _apiClient;
    public event EventHandler? LoggedIn;
    private bool _passwordVisible = false;
    private bool _registerPasswordVisible = false;
    public AuthView(ApiClient apiClient)
    {
        InitializeComponent();
        _apiClient = apiClient;
    }

    private string GetLoginPassword()
    {
        return _passwordVisible ? UserPasswordVisible.Text : UserPassword.Password;
    }

    private string GetRegisterPassword()
    {
        return _registerPasswordVisible ? RegisterUserPasswordVisible.Text : RegisterUserPassword.Password;
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
                    LoggedIn?.Invoke(this, EventArgs.Empty);
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
                    LoggedIn?.Invoke(this, EventArgs.Empty);
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

    private void GoToForgotPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        ForgotPasswordEmailTextBox.Text = EmailTextBox.Text.Trim();
        LoginPanel.Visibility = Visibility.Collapsed;
        ForgotPasswordPanel.Visibility = Visibility.Visible;
    }

    private void BackToLoginButton_Click(object sender, RoutedEventArgs e)
    {
        Reset();
    }

    private void GoToResetPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        ResetPasswordEmailTextBox.Text = ForgotPasswordEmailTextBox.Text.Trim();
        ForgotPasswordPanel.Visibility = Visibility.Collapsed;
        ResetPasswordPanel.Visibility = Visibility.Visible;
    }

    public void Reset()
    {
        RegisterPanel.Visibility = Visibility.Collapsed;
        ForgotPasswordPanel.Visibility = Visibility.Collapsed;
        ResetPasswordPanel.Visibility = Visibility.Collapsed;
        UserPassword.Visibility = Visibility.Visible;
        RegisterUserPassword.Visibility = Visibility.Visible;
        LoginPanel.Visibility = Visibility.Visible;
        _passwordVisible = false;
        _registerPasswordVisible = false;
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
                    LoggedIn?.Invoke(this, EventArgs.Empty);
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
}
