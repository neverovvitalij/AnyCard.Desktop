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
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        string username = UsernameTextBox.Text;
        string password = UserPassword.Password;
       
        var authResponse = await _apiClient.LoginAsync(username, password);
        if(authResponse != null)
        {
            MessageBox.Show("Login erfolgreich!");
        }
        else
        {
            MessageBox.Show("Benutzername oder Passwort falsch.");
        }
    }
}

