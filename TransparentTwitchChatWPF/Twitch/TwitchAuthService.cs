using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;
using TransparentTwitchChatWPF.Utils;
using TwitchLib.Api.Helix;

namespace TransparentTwitchChatWPF.Twitch;

public class TwitchAuthService : ITwitchAuthService
{
    public event EventHandler<string> AccessTokenReceived;

    private HttpListener _listener;
    private string _state = string.Empty;
    private bool _isConnecting = false;
    private readonly string _prefix = "http://localhost:8981/";

    // --- Public Interface Method ---
    public async Task ConnectAsync()
    {
        // Prevent a new connection attempt while one is already in progress.
        if (_isConnecting)
        {
            Debug.WriteLine("Connection attempt already in progress. Skipping new connection request.");
            return;
        }
        _isConnecting = true;

        try
        {
            _state = GenerateStateToken();
            LaunchBrowser(_state);
            string accessToken = await StartWebListenerWithTimeoutAsync();

            if (!string.IsNullOrEmpty(accessToken))
            {
                AccessTokenReceived?.Invoke(this, accessToken);
            }
        }
        catch (HttpListenerException ex)
        {
            // Handle cases where the listener can't start (e.g., permissions).
            Debug.WriteLine($"HTTP Listener Error: {ex.Message}. Try running as admin or registering the URL prefix.");
        }
        catch (TimeoutException)
        {
            Debug.WriteLine("Twitch connection timed out after 2 minutes.");
        }
        catch (Exception ex)
        {
            // Catch any other unexpected errors.
            Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
        finally
        {
            // Ensure the state flag is always reset.
            _isConnecting = false;
        }
    }

    // --- Private Helper Methods ---

    private void LaunchBrowser(string state)
    {
        string clientId = "yv4bdnndvd4gwsfw7jnfixp0mnofn7";
        string scopes = HttpUtility.UrlEncode("user_read user:read:broadcast user:read:chat bits:read channel:read:redemptions channel:read:subscriptions");
        string redirectUri = _prefix + "auth?";
        string url = $"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={clientId}&redirect_uri={redirectUri}&force_verify=true&state={state}&scope={scopes}";

        ShellHelper.OpenUrl(url);
    }

    private async Task<string> StartWebListenerWithTimeoutAsync()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(_prefix);

        // Use a CancellationTokenSource for the timeout.
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        cts.Token.Register(() => _listener?.Abort()); // Abort listener on timeout

        string accessToken = string.Empty;
        try
        {
            _listener.Start();
            accessToken = await ListenAsync(cts.Token);
        }
        // Guarantee the listener is always closed, even if ListenAsync fails.
        finally
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
            _listener.Close();
        }
        return accessToken;
    }

    private async Task<string> ListenAsync(CancellationToken cancellationToken)
    {
        string accessToken = string.Empty;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                HttpListenerContext context = await _listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                // --- Handle the two different requests ---

                // Request #1: The initial redirect from Twitch.
                // The URL will not contain the token, so we serve the HTML page.
                if (request.Url.AbsolutePath == "/auth")
                {
                    // Respond with the HTML for the page,
                    // this will also have the javascript to send us the token in Request #2.
                    byte[] buffer = Encoding.UTF8.GetBytes(AUTH_PAGE);
                    response.ContentType = "text/html";
                    response.ContentEncoding = Encoding.UTF8;
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.Close();
                    // Continue listening for Request #2.
                }
                // Request #2: The callback from our JavaScript containing the token.
                else if (request.Url.AbsolutePath == "/callback")
                {
                    // safely parse the query string.
                    var query = HttpUtility.ParseQueryString(request.Url.Query);
                    string receivedToken = query["access_token"];
                    string receivedState = query["state"];

                    // Validate the state to prevent CSRF attacks.
                    if (_state.Equals(receivedState))
                    {
                        accessToken = receivedToken;
                    }
                    // Respond with a simple OK and stop listening.
                    response.StatusCode = 200;
                    response.Close();
                    break; // Exit the while loop
                }
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        return accessToken;
    }

    private string GenerateStateToken()
    {
        return Guid.NewGuid().ToString();
    }

    private const string AUTH_PAGE = @"
<!DOCTYPE html>
<html>
<head>
    <title>Authorization Success</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;
            background-color: #ebf5fc;
            color: #444;
            margin: 0;
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            text-align: center;
        }
        * {
          margin: 0;
          padding: 0;
          box-sizing: border-box;
        }
        .container {
            max-width: 450px;
        }
        
        /* --- Stacked Text Title --- */
        .title-container {
          color: #444;
          font-size: 1.5rem;
          display: flex;
          flex-direction: column;
        }

        .left {
          text-align: left;
          width: 100%;
        }

        .right {
          text-align: right;
          width: 100%;
        }

        .stack {
          display: grid;
          grid-template-columns: 1fr;
        }

        .stack span {
          font-weight: bold;
          grid-row-start: 1;
          grid-column-start: 1;
          font-size: 4rem;
        }
        /* --- End of Stacked Text Title --- */

        .logo {
            margin: 0 auto 50px auto; 
            width: 150px;
            height: 150px;
            border-radius: 50%;
            background: linear-gradient(to bottom left, #007fe8 15%, #00cdfc);
            position: relative;
        }
        .logo::before {
            content: """";
            position: absolute;
            /* Positioned relative to the .logo container */
            bottom: -40px; 
            left: -40px;
            width: 150px;
            height: 150px;
            background: linear-gradient(
              to bottom left,
              rgba(249, 180, 70, 1) 15%,
              rgba(234, 47, 152, 0.8)
            );
            border-radius: 60px;
            border: 12px solid #ebf5fc; /* Uses body background for cutout effect */
        }
        h1 {
            font-size: 22px;
            color: #111;
            margin: 0 0 8px 0;
        }
        p {
            font-size: 16px;
            line-height: 1.5;
            margin: 0 0 25px 0;
        }
        hr {
            border: none;
            border-top: 1px solid #e0e0e0;
            margin-bottom: 25px;
        }
        .footer a {
            font-size: 14px;
            color: #555;
            text-decoration: none;
            margin: 0 10px;
        }
        .footer a:hover {
            text-decoration: underline;
        }
    </style>
    <script src=""https://ajax.googleapis.com/ajax/libs/jquery/3.6.4/jquery.min.js""></script>
    <script type=""text/javascript"" language=""javascript"">
        function isEmpty(str) {
            return (!str || str.length === 0);
        }
        $(document).ready(function() {
            if(!isEmpty(location.hash.substr(1))) {
                var xreq = new XMLHttpRequest();
                xreq.responseType = 'text';
                xreq.open('GET', '/callback?' + location.hash.substr(1), true);
                xreq.send();
            }
        });
    </script>
</head>
<body>
    <div class=""container"">
        <div class=""title-container"">
            <span class=""left"">Transparent</span>
            <div class=""stack"">
                <span>Twitch Chat</span>
            </div>
            <span class=""right"">Overlay</span>
        </div>

        <div class=""logo""></div>
        <h1>Successfully connected to Twitch!</h1>
        <p>
            You may now close this tab and return to the application.
            <br>
            You can manage access for this app at any time in your settings.
        </p>
        <hr>
        <div class=""footer"">
            <a href=""https://github.com/baffler/Transparent-Twitch-Chat-Overlay"">github repository</a>
        </div>
    </div>
</body>
</html>";
}
