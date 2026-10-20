using AnyCard.Desktop.Services;
using System.Windows;
using System.Windows.Controls;


namespace AnyCard.Desktop.Views;
/// <summary>
/// Interaktionslogik für SettingsView.xaml
/// </summary>
public partial class SettingsView : UserControl
{
    private readonly ApiClient _apiClient;
    public event EventHandler? SessionExpired;
    public SettingsView(ApiClient apiClient)
    {
        InitializeComponent();
        _apiClient = apiClient;
        AgainValueBox.ItemsSource = Enumerable.Range(1, 60).ToList();
        HardValueBox.ItemsSource = Enumerable.Range(1, 60).ToList();
        GoodValueBox.ItemsSource = Enumerable.Range(1, 60).ToList();
        EasyValueBox.ItemsSource = Enumerable.Range(1, 60).ToList();
    }

    public async Task LoadAsync()
    {
        SettingsMessageText.Visibility = Visibility.Collapsed;

        var response = await _apiClient.GetUserSettingsAsync();
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
            MessageBox.Show("Fehler beim Laden der Einstellungen.");
            return;
        }
        ShowInterval(AgainValueBox, AgainUnitBox, response.Data!.Again);
        ShowInterval(HardValueBox, HardUnitBox, response.Data.Hard);
        ShowInterval(GoodValueBox, GoodUnitBox, response.Data.Good);
        ShowInterval(EasyValueBox, EasyUnitBox, response.Data.Easy);
    }

    private async void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var againInterval = ReadInterval(AgainValueBox, AgainUnitBox);
        var hardInterval = ReadInterval(HardValueBox, HardUnitBox);
        var goodInterval = ReadInterval(GoodValueBox, GoodUnitBox);
        var easyInterval = ReadInterval(EasyValueBox, EasyUnitBox);

        var response = await _apiClient.UpdateUserSettingsAsync(againInterval, hardInterval, goodInterval, easyInterval);
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
        if (response.Error == ApiError.InvalidInput)
        {
            MessageBox.Show("Erlaubt: 1 Minute bis 30 Tage.");
            return;
        }
        if (response.Error != ApiError.None)
        {
            MessageBox.Show("Fehler beim Speichern der Einstellungen.");
            return;
        }

        SettingsMessageText.Text = "Gespeichert";
        SettingsMessageText.Visibility = Visibility.Visible;
    }
    private void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowInterval(AgainValueBox, AgainUnitBox, TimeSpan.FromMinutes(1));
        ShowInterval(HardValueBox, HardUnitBox, TimeSpan.FromHours(2));
        ShowInterval(GoodValueBox, GoodUnitBox, TimeSpan.FromDays(2));
        ShowInterval(EasyValueBox, EasyUnitBox, TimeSpan.FromDays(6));
    }

    private static (int Value, int UnitIndex) SplitInterval(TimeSpan interval)
    {
        if (interval.TotalDays % 1 == 0)
        {
            return ((int)interval.TotalDays, 2);
        }

        if (interval.TotalHours % 1 == 0)
        {
            return ((int)interval.TotalHours, 1);
        }

        return ((int)interval.TotalMinutes, 0);
    }

    private static void ShowInterval(ComboBox valueBox, ComboBox unitBox, TimeSpan interval)
    {
        var (value, unitIndex) = SplitInterval(interval);
        valueBox.SelectedItem = value;
        unitBox.SelectedIndex = unitIndex;
    }

    private static TimeSpan ReadInterval(ComboBox valueBox, ComboBox unitBox)
    {
        var value = (int)valueBox.SelectedItem;

        return unitBox.SelectedIndex switch
        {
            0 => TimeSpan.FromMinutes(value),
            1 => TimeSpan.FromHours(value),
            2 => TimeSpan.FromDays(value),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
