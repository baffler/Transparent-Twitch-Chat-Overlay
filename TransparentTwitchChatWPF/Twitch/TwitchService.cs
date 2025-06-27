using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Client;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;
using TransparentTwitchChatWPF.Utils;

namespace TransparentTwitchChatWPF.Twitch;

public class TwitchService : IHostedService, IDisposable
{
    // DI
    private readonly ILogger<TwitchService> _logger;

    private readonly TwitchAPI _api;
    private readonly EventSubWebsocketClient _eventSubWebsocketClient;
    private string _userId;

    public event EventHandler<AccessTokenValidatedEventArgs> AccessTokenValidated;
    public event EventHandler<TwitchUserDataEventArgs> UserDataFetched;

    public event EventHandler<ChannelPointsCustomRewardRedemptionArgs> ChannelPointsRewardRedeemed;

    public string AuthTokenExpiration { get; private set; } = "...";
    public string TwitchConnectionStatus { get; private set; } = "Not Connected";
    public BitmapImage ProfileImage { get; private set; }

    private bool _isEventSubInit = false;

    private readonly Random _random = new Random();

    // Reconnection strategy parameters
    private const int MaxReconnectAttempts = 7;  // Maximum number of times to try reconnecting
    private const int BaseDelayMilliseconds = 1000; // Initial delay: 1 second
    private const int MaxDelayMilliseconds = 60000; // Maximum delay: 1 minute

    private bool _disposedValue; // To detect redundant calls

    public TwitchService(ILogger<TwitchService> logger,
        EventSubWebsocketClient eventSubWebsocketClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("TwitchService as IHostedService constructed!");

        _eventSubWebsocketClient = eventSubWebsocketClient ?? throw new ArgumentNullException(nameof(eventSubWebsocketClient));

        _api = new TwitchAPI();
        // Initialize Twitch API settings (Client ID and Access Token)
        _api.Settings.ClientId = "yv4bdnndvd4gwsfw7jnfixp0mnofn7";

        TwitchConnection.AccessTokenResponse += TwitchConnection_AccessTokenResponse;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_isEventSubInit)
        {
            _logger.LogInformation("EventSub wasn't initialized yet, skipping StartAsync.");
            return;
        }

        _logger.LogInformation("Starting _eventSubWebsocketClient...");
        var token = App.Settings.GeneralSettings.OAuthToken;
        var userId = App.Settings.GeneralSettings.ChannelID;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Twitch credentials are not set in App.SettingsObject");
            return;
        }

        // Get Application Token with Client credentials grant flow.
        // https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/#client-credentials-grant-flow
        _api.Settings.AccessToken = token;
        //_logger.LogInformation("_eventSubWebsocketClient AccessToken set to: " + token);

        _userId = userId;
        _logger.LogInformation("_eventSubWebsocketClient _userId set to: " + userId);

        await _eventSubWebsocketClient.ConnectAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping _eventSubWebsocketClient...");
        await _eventSubWebsocketClient.DisconnectAsync();
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("InitializeAsync()");
        await ValidateTwitchConnectionAsync();
    }

    private async Task ValidateTwitchConnectionAsync()
    {
        var oAuth = App.Settings.GeneralSettings.OAuthToken;
        if (!string.IsNullOrEmpty(oAuth))
        {
            _api.Settings.AccessToken = oAuth;
            await FetchUserDataAsync();
            await ValidateAuthTokenAsync(oAuth);
            InitEventSub();
        }
    }

    private async void TwitchConnection_AccessTokenResponse(object? sender, string e)
    {
        App.Settings.GeneralSettings.OAuthToken = e;
        App.Settings.Persist();

        _api.Settings.AccessToken = e;
        await FetchUserDataAsync();
        await ValidateAuthTokenAsync(e);
        InitEventSub();
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
            //Growl.Error("Error returned: " + e.Message);
            Growl.Warning("The Twitch connection may be invalid or expired, try reconnecting in settings.");
        }
    }

    private void GetUsersResponseCallback(GetUsersResponse e)
    {
        _logger.LogInformation("GetUsersResponseCallback");

        string userName = e.Users[0].DisplayName;
        string userID = e.Users[0].Id;
        string profileImageUrl = e.Users[0].ProfileImageUrl;

        TwitchConnectionStatus = $"Connected as {userName} ({userID})";
        ProfileImage = TwitchConnectionUtils.LoadImageFromUrl(profileImageUrl);

        App.Settings.GeneralSettings.ChannelID = userID;

        UserDataFetched?.Invoke(this, new TwitchUserDataEventArgs
        {
            DisplayName = userName,
            UserId = userID,
            ProfileImageUrl = profileImageUrl,
            ProfileImage = ProfileImage
        });
    }

    private async Task ValidateAuthTokenAsync(string accessToken)
    {
        try
        {
            var t = await _api.Auth.ValidateAccessTokenAsync(accessToken);
            if (t != null)
            {
                AuthTokenValidatedCallback(t);
            }
            else
            {
                Growl.Warning("Could not validate the access token. The Twitch connection may need to be reconnected in settings.");
            }
        }
        catch (Exception e)
        {
            //Growl.Error("Error returned: " + e.Message);
            Growl.Warning("Auth token invalid. The Twitch connection may be expired, try reconnecting in settings.");
        }
    }

    private void AuthTokenValidatedCallback(ValidateAccessTokenResponse e)
    {
        _logger.LogInformation("AuthTokenValidatedCallback");

        TimeSpan timeSpan = TimeSpan.FromSeconds(e.ExpiresIn);
        string expires = string.Format("{0} Days {1} Hours", timeSpan.Days, timeSpan.Hours);
        AuthTokenExpiration = "Expires in: " + expires;

        AccessTokenValidated?.Invoke(this, new AccessTokenValidatedEventArgs
        {
            Expires = expires,
            Login = e.Login,
            UserId = e.UserId,
            ClientId = e.ClientId,
        });
    }

    private void InitEventSub()
    {
        if (_isEventSubInit) return; // Already initialized
        _logger.LogInformation("Initializing Twitch EventSub WebSocket client for channel: " + App.Settings.GeneralSettings.ChannelID);

        // ensure no old subscriptions are hanging around
        UnsubscribeFromEvents();

        _eventSubWebsocketClient.WebsocketConnected += OnWebsocketConnected;
        _eventSubWebsocketClient.WebsocketDisconnected += OnWebsocketDisconnected;
        _eventSubWebsocketClient.WebsocketReconnected += OnWebsocketReconnected;
        _eventSubWebsocketClient.ErrorOccurred += OnErrorOccurred;
        _eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd += OnChannelPointsCustomRewardRedemptionAdd;
        _eventSubWebsocketClient.ChannelChatMessage += _eventSubWebsocketClient_ChannelChatMessage;

        _isEventSubInit = true;

        _logger.LogInformation("Connecting to EventSub...");
        _ = StartAsync(CancellationToken.None);
    }

    private async Task _eventSubWebsocketClient_ChannelChatMessage(object sender, ChannelChatMessageArgs args)
    {
        var userName = args.Notification.Payload.Event.ChatterUserName;
        var message = args.Notification.Payload.Event.Message.Text;
        var fragments = args.Notification.Payload.Event.Message.Fragments;

        //foreach (var fragment in fragments) {
        //_logger.LogInformation($"Chat Fragment: {fragment.Text} (Type: {fragment.Type}) (Emote: {fragment.Emote.})");
        //}

        var evt = args.Notification.Payload.Event;

        //_logger.LogInformation($"Chat message from {userName}: {message}");
    }

    private async Task OnChannelPointsCustomRewardRedemptionAdd(object sender, ChannelPointsCustomRewardRedemptionArgs e)
    {
        var eventData = e.Notification.Payload.Event;
        _logger.LogInformation($"ChannelPointsCustomRewardRedemptionAdd: {eventData.Reward.Title} redeemed by {eventData.UserName} ({eventData.Reward.Cost})");
        if (!string.IsNullOrWhiteSpace(eventData.UserInput))
            _logger.LogInformation($"User Input: {eventData.UserInput}");

        ChannelPointsRewardRedeemed?.Invoke(this, e);
    }


    // --- WebSocket Events -------------------------------------------------------------
    private async Task OnWebsocketConnected(object? sender, WebsocketConnectedArgs e)
    {
        _logger.LogInformation($"Websocket {_eventSubWebsocketClient.SessionId} connected!");
        Growl.Info("Twitch EventSub connection established successfully.");

        if (!e.IsRequestedReconnect)
        {
            // subscribe to topics
            // create condition Dictionary
            // need BOTH broadcaster and moderator values or EventSub returns an Error!
            var condition = new Dictionary<string, string> { { "broadcaster_user_id", _userId }, { "moderator_user_id", _userId } };
            // Create and send EventSubscription
            await _api.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.channel_points_custom_reward_redemption.add", "1", condition, EventSubTransportMethod.Websocket,
            _eventSubWebsocketClient.SessionId, accessToken: App.Settings.GeneralSettings.OAuthToken);

            var conditionChatMessage = new Dictionary<string, string> {
                { "broadcaster_user_id", "128440061" },
                { "user_id", _userId }
            };
            await _api.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.chat.message", "1", conditionChatMessage, EventSubTransportMethod.Websocket,
                _eventSubWebsocketClient.SessionId, accessToken: App.Settings.GeneralSettings.OAuthToken);

            //await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.channel_points_automatic_reward_redemption.add", "2", condition, EventSubTransportMethod.Websocket,
            //_eventSubWebsocketClient.SessionId, accessToken: App.SettingsObject.GeneralSettings.OAuthToken);
            // for special Events you need to additionally add the AccessToken of the ChannelOwner to the request.
            // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/
        }
    }

    private async Task OnWebsocketDisconnected(object? sender, EventArgs e)
    {
        _logger.LogWarning($"[Twitch EventSub] Websocket {_eventSubWebsocketClient.SessionId} disconnected!");

        for (int attempt = 0; attempt < MaxReconnectAttempts; attempt++)
        {
            // Calculate exponential backoff delay
            // Formula: BaseDelay * (2^attempt)
            double exponentialDelay = BaseDelayMilliseconds * Math.Pow(2, attempt);

            // Add jitter: a random small duration (e.g., 0 to 1 second) to prevent thundering herd
            int jitter = _random.Next(0, 1000);

            // Calculate total delay, ensuring it doesn't exceed MaxDelayMilliseconds
            int delayMilliseconds = (int)Math.Min(exponentialDelay + jitter, MaxDelayMilliseconds);

            _logger.LogInformation($"[Twitch EventSub] Reconnect attempt {attempt + 1}/{MaxReconnectAttempts}. Waiting {delayMilliseconds}ms before next attempt...");
            await Task.Delay(delayMilliseconds);

            try
            {
                _logger.LogInformation($"[Twitch EventSub] Attempting to reconnect (Attempt {attempt + 1})...");
                if (await _eventSubWebsocketClient.ReconnectAsync())
                {
                    _logger.LogInformation($"[Twitch EventSub] Websocket {_eventSubWebsocketClient.SessionId} reconnected successfully on attempt {attempt + 1}!");
                    return; // Successfully reconnected, exit the method.
                }
                else
                {
                    _logger.LogInformation($"[Twitch EventSub] Websocket {_eventSubWebsocketClient.SessionId} reconnect attempt {attempt + 1} failed.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception details. Consider specific exception types for different handling.
                _logger.LogInformation($"[Twitch EventSub] Websocket {_eventSubWebsocketClient.SessionId} reconnect attempt {attempt + 1} threw an exception: {ex.Message}");
                // Depending on the exception, might want to break the loop earlier
                // (e.g., for authentication failures or other non-recoverable errors).
            }
        }

        _logger.LogWarning($"[Twitch EventSub] Websocket {_eventSubWebsocketClient.SessionId} failed to reconnect after {MaxReconnectAttempts} attempts. Stopping retry efforts for this disconnection event.");
        // TODO: At this point, notify the user, log a more critical error, or transition to an offline state.

        Growl.Error("Twitch EventSub connection lost. Please check your internet connection or Twitch settings.");
    }

    private async Task OnWebsocketReconnected(object? sender, EventArgs e)
    {
        _logger.LogInformation($"Websocket {_eventSubWebsocketClient.SessionId} reconnected");
        Growl.Info("Twitch EventSub connection re-established successfully.");
    }

    private async Task OnErrorOccurred(object? sender, ErrorOccuredArgs e)
    {
        //LogService.Instance.ChannelChatStatus.IsConnected = true;

        _logger.LogInformation($"Websocket {_eventSubWebsocketClient.SessionId} - Error occurred!\n{e.Message}\n{e.Exception}");
        Growl.Error($"Twitch EventSub Error: {e.Message}");
    }

    // --- Cleanup Methods -------------------------------------------------------------

    public async Task DisconnectTwitchConnectionAsync()
    {
        _logger.LogInformation("User requested Twitch disconnection.");

        // Stop the active service (which disconnects the websocket)
        await StopAsync(CancellationToken.None);

        // Clear sensitive data and settings
        App.Settings.GeneralSettings.ChannelID = string.Empty;
        App.Settings.GeneralSettings.OAuthToken = string.Empty;
        App.Settings.Persist();

        // Update the UI state
        TwitchConnectionStatus = "Not Connected";
        AuthTokenExpiration = "...";
        ProfileImage = null; // Or a default image

        // Reset internal state flags
        _isEventSubInit = false;
        _userId = null;

        //    Unsubscribe from events now that we are disconnected
        //    This prevents handlers from running in a disconnected state.
        UnsubscribeFromEvents();
    }

    private void UnsubscribeFromEvents()
    {
        _logger.LogTrace("Unsubscribing from TwitchService events.");
        TwitchConnection.AccessTokenResponse -= TwitchConnection_AccessTokenResponse;

        if (_eventSubWebsocketClient != null)
        {
            _eventSubWebsocketClient.WebsocketConnected -= OnWebsocketConnected;
            _eventSubWebsocketClient.WebsocketDisconnected -= OnWebsocketDisconnected;
            _eventSubWebsocketClient.WebsocketReconnected -= OnWebsocketReconnected;
            _eventSubWebsocketClient.ErrorOccurred -= OnErrorOccurred;
            _eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd -= OnChannelPointsCustomRewardRedemptionAdd;
            _eventSubWebsocketClient.ChannelChatMessage -= _eventSubWebsocketClient_ChannelChatMessage;
        }
    }

    public virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects).
                _logger.LogInformation("Disposing TwitchService managed resources.");

                // Unsubscribe from all events to prevent memory leaks
                UnsubscribeFromEvents();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // finalizer only called by the GC if Dispose() is not called
    // It's a safety net if there's unmanaged resources
    // ~TwitchService()
    // {
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Don't change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public class AccessTokenValidatedEventArgs : EventArgs
{
    public string Expires { get; set; }
    public string Login { get; set; }
    public string UserId { get; set; }
    public string ClientId { get; set; }
}

public class TwitchUserDataEventArgs : EventArgs
{
    public string DisplayName { get; set; }
    public string UserId { get; set; }
    public string ProfileImageUrl { get; set; }
    public BitmapImage ProfileImage { get; set; }
}