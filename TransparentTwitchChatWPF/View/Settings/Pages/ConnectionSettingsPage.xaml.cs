using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TransparentTwitchChatWPF.Helpers;
using TransparentTwitchChatWPF.Twitch;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace TransparentTwitchChatWPF.View.Settings;

/// <summary>
/// Interaction logic for ConnectionSettingsPage.xaml
/// </summary>
public partial class ConnectionSettingsPage : UserControl
{
    public event Action<TwitchConnectionStatus> TwitchConnectionStatusChanged;

    private readonly ITwitchAuthService _twitchAuthService;
    private TwitchAPI _api;

    public ConnectionSettingsPage(ITwitchAuthService twitchAuthService)
    {
        InitializeComponent();
        _twitchAuthService = twitchAuthService;
        // Subscribe to the instance event.
        _twitchAuthService.AccessTokenReceived += OnAccessTokenReceived;

        // Unsubscribe to prevent memory leaks
        this.Unloaded += (s, e) => {
            _twitchAuthService.AccessTokenReceived -= OnAccessTokenReceived;
        };

        _api = new TwitchAPI();
        _api.Settings.ClientId = "yv4bdnndvd4gwsfw7jnfixp0mnofn7";
    }

    public void SetupValues()
    {
        ValidateTwitchConnection();
    }

    private async void btConnect_Click(object sender, RoutedEventArgs e)
    {
        // Give the user initial info and disable the button to prevent re-clicks.
        MessageBox.Show("Please check your default browser to authorize the application.", "Twitch Connection", MessageBoxButton.OK, MessageBoxImage.Information);
        btConnect.IsEnabled = false;

        try
        {
            // Await the async operation. The UI will remain responsive.
            await _twitchAuthService.ConnectAsync();

            // Show a success message if it completes without errors.
            //MessageBox.Show("Connection successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (TimeoutException)
        {
            // Catch specific, expected errors.
            MessageBox.Show("The connection timed out. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            // Catch any other unexpected errors.
            MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // ALWAYS re-enable the button, even if an error occurred.
            btConnect.IsEnabled = true;
        }
    }

    private void btDisconnect_Click(object sender, RoutedEventArgs e)
    {
        lblTwitch.Content = "Not Connected";
        lblTwitchStatus.Content = "";
        imgTwitch.Source = null;

        TwitchConnectionStatusChanged?.Invoke(new TwitchConnectionStatus(TwitchConnectionStatusState.NotConnected, "Not Connected"));

        App.Settings.GeneralSettings.ChannelID = string.Empty;
        App.Settings.GeneralSettings.OAuthToken = string.Empty;
        _api.Settings.AccessToken = string.Empty;
    }

    private void ValidateTwitchConnection()
    {
        if (!string.IsNullOrEmpty(App.Settings.GeneralSettings.OAuthToken))
        {
            _api.Settings.AccessToken = App.Settings.GeneralSettings.OAuthToken;
            _ = FetchUserDataAsync();
            _ = ValidateAuthToken(App.Settings.GeneralSettings.OAuthToken);
        }
    }

    private void OnAccessTokenReceived(object sender, string e)
    {
        App.Settings.GeneralSettings.OAuthToken = e;
        _api.Settings.AccessToken = e;

        _ = FetchUserDataAsync();
        _ = ValidateAuthToken(e);
    }

    private async Task FetchUserDataAsync()
    {
        try
        {
            var getUser = await _api.Helix.Users.GetUsersAsync();
            GetUsersResponseCallback(getUser);
        }
        catch (Exception e)
        {
            //MessageBox.Show(e.Message, "Fetch User Data Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GetUsersResponseCallback(GetUsersResponse e)
    {
        string userName = e.Users[0].DisplayName;
        string userID = e.Users[0].Id;
        string profileImageUrl = e.Users[0].ProfileImageUrl;

        lblTwitch.Content = $"{userName} ({userID})";

        App.Settings.GeneralSettings.ChannelID = userID;

        var bitmap = ImageHelpers.LoadFromUrl(profileImageUrl);
        imgTwitch.Source = bitmap;
    }

    private async Task ValidateAuthToken(string accessToken)
    {
        try
        {
            var t = await _api.Auth.ValidateAccessTokenAsync(accessToken);
            if (t == null)
            {
                TwitchConnectionStatusChanged?.Invoke(new TwitchConnectionStatus(TwitchConnectionStatusState.Error, "Auth Token Error"));
            }
            else
                AuthTokenValidatedCallback(t);
        }
        catch (Exception e)
        {
            TwitchConnectionStatusChanged?.Invoke(new TwitchConnectionStatus(TwitchConnectionStatusState.Error, "Auth Token Error"));
            Debug.WriteLine($"{e.Message}\n{e.InnerException}");
            //MessageBox.Show(e.Message, "Validate Auth Token Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AuthTokenValidatedCallback(ValidateAccessTokenResponse e)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(e.ExpiresIn);
        string expires = string.Format("{0} Days {1} Hours", timeSpan.Days, timeSpan.Hours);

        string status = "Connected";
        var statusState = TwitchConnectionStatusState.Active;

        if (App.Settings.GeneralSettings.RedemptionsEnabled)
        {
            statusState = TwitchConnectionStatusState.Active;
            status += " (Active)";
        }
        else
        {
            statusState = TwitchConnectionStatusState.Inactive;
            status += " (Inactive)";
        }

        TwitchConnectionStatusChanged?.Invoke(new TwitchConnectionStatus(statusState, status));

        lblTwitchStatus.Content = $"Expires: {expires}";
    }
}

public enum TwitchConnectionStatusState
{
    NotConnected,
    Active,
    Inactive,
    Error
}
public record class TwitchConnectionStatus(TwitchConnectionStatusState StatusState, string Message);