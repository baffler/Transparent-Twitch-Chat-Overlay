using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TransparentTwitchChatWPF.Helpers;

namespace TransparentTwitchChatWPF.ChatProviders;

public class NativeChatProvider : IChatProvider
{
    public Uri GetNavigationUri()
    {
        string hostName = OverlayPathHelper.GetNativeChatHostname();
        return new Uri($"https://{hostName}/v2/index.html");
    }

    public Task ConfigureAsync(CoreWebView2 coreWebView2)
    {
        SyncChannelSettings();

        PostWebMessage(coreWebView2, "config", App.Settings.jChatSettings);
        PostWebMessage(coreWebView2, "credentials", new
        {
            ClientId = "yv4bdnndvd4gwsfw7jnfixp0mnofn7",
            Token = App.Settings.GeneralSettings.OAuthToken
        });

        return Task.CompletedTask;
    }

    // --- Helper methods --------------------------
    private void SyncChannelSettings()
    {
        if (string.IsNullOrEmpty(App.Settings.jChatSettings.Channel))
        {
            App.Settings.jChatSettings.Channel = !string.IsNullOrEmpty(App.Settings.GeneralSettings.Username) ? App.Settings.GeneralSettings.Username : "baffler";
        }
    }

    private void PostWebMessage(CoreWebView2 coreWebView2, string type, object payload)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var message = new { Type = type, Payload = payload };
        coreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message, options));
    }
}
