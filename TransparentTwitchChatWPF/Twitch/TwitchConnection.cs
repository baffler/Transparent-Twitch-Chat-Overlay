using System;
using System.Web;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TransparentTwitchChatWPF.Utils;

namespace TransparentTwitchChatWPF.Twitch;

public static class TwitchConnectionUtils
{
    public static readonly string AUTH_PAGE = @"<!DOCTYPE html>
<html>
<head>
<script src=""https://ajax.googleapis.com/ajax/libs/jquery/3.6.4/jquery.min.js""></script>
<script type=""text/javascript"" language=""javascript"">
function isEmpty(str) {
  return (!str || str.length === 0);
}
$(document).ready(function() {
	if(!isEmpty(location.hash.substr(1))) {
		var xreq = new XMLHttpRequest();
		xreq.open('GET', '/auth?' + location.hash.substr(1), true);
		xreq.send();
	}
});

</script>
<body>Success! You can close this tab and return to the app.</body></html>";

    public static string GenerateStateToken()
    {
        return Guid.NewGuid().ToString();
    }

    public static BitmapImage LoadImageFromUrl(string url)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(url, UriKind.Absolute);
        bitmap.EndInit();

        return bitmap;
    }
}

public class TwitchConnection
{
    HttpListener listener;
    static volatile bool isListening = false;
    static volatile string state = string.Empty;
    readonly string prefix = "http://localhost:8981/";

    public static event EventHandler<string> AccessTokenResponse;

    public async void ConnectTwitchAccount()
    {
        await LaunchAuthorizationWebPage();
        await Task.Yield();
    }

    private async Task LaunchAuthorizationWebPage()
    {
        state = TwitchConnectionUtils.GenerateStateToken();

        string clientId = "yv4bdnndvd4gwsfw7jnfixp0mnofn7";
        string redirectUri = prefix + "auth?";
        string scopes = HttpUtility.UrlEncode("user_read user:read:broadcast user:read:chat bits:read chat:read channel:read:redemptions channel:read:subscriptions");
        string url = $"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={clientId}&redirect_uri={redirectUri}&force_verify=true&state={state}&scope={scopes}";

        ShellHelper.OpenUrl(url);

        string accessToken = await StartWebListener();

        if (!string.IsNullOrEmpty(accessToken))
        {
            EventHandler<string> handler = AccessTokenResponse;
            handler?.Invoke(this, accessToken);
        }
    }

    private async Task<string> StartWebListener()
    {
        if ((listener != null) && isListening)
            listener.Stop();

        isListening = false;
        listener = new HttpListener();
        listener.Prefixes.Add(this.prefix);
        listener.Start();
        string accessToken = await Listen();
        listener.Close();
        return accessToken;
    }

    private async Task<string> Listen()
    {
        isListening = true;
        string accessToken = string.Empty;
        while (isListening)
        {
            HttpListenerContext listenerContext = null;
            try
            {
                listenerContext = await listener.GetContextAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            if (listenerContext != null)
            {
                HttpListenerRequest request = listenerContext.Request;
                HttpListenerResponse response = listenerContext.Response;

                bool doSendAuthPage = false;

                if (request.HttpMethod == "GET" && request.Url.AbsolutePath.Contains("/auth"))
                {
                    doSendAuthPage = true;
                    if (!string.IsNullOrEmpty(request.RawUrl))
                    {
                        string fragment = request.RawUrl.Replace("/auth?", "");
                        string[] tokens = fragment.Split('&');

                        string tmpaccessToken = tokens.SingleOrDefault(t => t.StartsWith("access_token="));
                        string stateToken = tokens.SingleOrDefault(t => t.StartsWith("state="));

                        if ((!string.IsNullOrEmpty(tmpaccessToken)) && (!string.IsNullOrEmpty(stateToken)))
                        {
                            stateToken = stateToken.Replace("state=", "").Trim();
                            if ((!string.IsNullOrEmpty(stateToken)) && (state.Equals(stateToken, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                accessToken = tmpaccessToken.Replace("access_token=", "").Trim();
                                isListening = false;
                                doSendAuthPage = false;
                            }
                        }
                    }
                }

                if (doSendAuthPage)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(TwitchConnectionUtils.AUTH_PAGE);
                    response.ContentType = "text/html";
                    response.ContentEncoding = Encoding.UTF8;
                    response.ContentLength64 = bytes.LongLength;
                    await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                    response.Close();
                }
                else
                {
                    response.StatusCode = 200;
                    response.Close();
                }
            }
        }

        return accessToken;
    }
}
