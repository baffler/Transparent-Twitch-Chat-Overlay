using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace TransparentTwitchChatWPF.ChatProviders;

public class TwitchPopoutProvider : IChatProvider
{
    public Uri GetNavigationUri()
    {
        string username = App.Settings.GeneralSettings.Username;
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty or whitespace.", nameof(username));

        string url = $"https://www.twitch.tv/popout/{username}/chat?popout=";
        return new Uri(url);
    }

    public async Task ConfigureAsync(CoreWebView2 coreWebView2)
    {
        if (App.Settings.GeneralSettings.BetterTtv)
        {
            // Define the JavaScript to update the emotes setting using a bitwise operation.
            // The value '16' corresponds to the flag for enabling 7TV emotes.
            if (App.Settings.GeneralSettings.BetterTtv_7tv || App.Settings.GeneralSettings.BetterTtv_AdvEmoteMenu)
            {
                // Perform a bitwise OR with 16 to enable the 7TV flag
                // This adds the 7TV setting without disabling others.
                string enable7tvFlag = @"settings.emotes[0] = settings.emotes[0] | 16;";
                string disable7tvFlag = @"settings.emotes[0] = settings.emotes[0] & ~16;";
                string enableAdvEmoteMenuFlag = "settings.emoteMenu = 2;";
                string disableAdvEmoteMenuFlag = "settings.emoteMenu = 0;";

                var bttvSettingsScript = $$"""
                (function() {
                    try {
                        const settingsKey = 'bttv_settings';
                        let settings = JSON.parse(localStorage.getItem(settingsKey) || '{}');

                        // Enable 7TV Emotes (Bitwise Flag)
                        if (settings.emotes && Array.isArray(settings.emotes)) {
                        {{(App.Settings.GeneralSettings.BetterTtv_7tv ? enable7tvFlag : disable7tvFlag)}}
                        }

                        // --- Enable BTTV Emote Menu ---
                        // 0 = Off, 1 = Legacy, 2 = Modern
                        {{(App.Settings.GeneralSettings.BetterTtv_AdvEmoteMenu ? enableAdvEmoteMenuFlag : disableAdvEmoteMenuFlag)}}

                        // --- Save all changes back to localStorage ---
                        localStorage.setItem(settingsKey, JSON.stringify(settings));
                        console.log('BTTV settings updated to enable 7TV and the BTTV Emote Menu.');
                    } catch (e) {
                        console.error('Failed to pre-configure BTTV settings', e);
                    }
                })();
            """;

                await coreWebView2.ExecuteScriptAsync(bttvSettingsScript);
            }

            // Inject the main BTTV script.
            InsertCustomJavaScriptFromUrl(coreWebView2, "https://cdn.betterttv.net/betterttv.js");
        }
    
        if (App.Settings.GeneralSettings.FrankerFaceZ)
        {
            // Observe for FrankerFaceZ's reskin stylesheet
            // that breaks the transparency and remove it
            string ffzScript = @"
(function() {
    const head = document.getElementsByTagName(""head"")[0];
    const observer = new MutationObserver((mutations, observer) => {
        for (const mut of mutations) {
            if (mut.type === ""childList"") {
                for (const node of mut.addedNodes) {
                    if (node.tagName.toLowerCase() === ""link"" && node.href.includes(""color_normalizer"")) {
                        node.remove();
                    }
                }
            }
        }
    });
    observer.observe(head, {
        attributes: false,
        childList: true,
        subtree: false,
    });
})();
                        ";

            await coreWebView2.ExecuteScriptAsync(ffzScript);
            InsertCustomJavaScriptFromUrl(coreWebView2, "https://cdn.frankerfacez.com/static/script.min.js");
        }
    }

    public string GetCssToInject()
    {
        return "";
    }

    public string GetJavascriptToExecute()
    {
        return "";
    }

    // -- Helper methods --------------------------
    private async Task ExecuteScriptAsync(CoreWebView2 coreWebView2, string script)
    {
        if (string.IsNullOrEmpty(script)) return;
        await coreWebView2.ExecuteScriptAsync(script);
    }

    private async void InsertCustomJavaScriptFromUrl(CoreWebView2 coreWebView2, string scriptUrl)
    {
        await ExecuteScriptAsync(coreWebView2, @"
(function() {
    const script = document.createElement(""script"");
    script.src = """ + scriptUrl + @""";
    document.getElementsByTagName(""head"")[0].appendChild(script);
})();
            ");
    }
}
