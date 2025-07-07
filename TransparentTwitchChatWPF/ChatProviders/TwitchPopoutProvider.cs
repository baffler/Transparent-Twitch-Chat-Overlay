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
            string bttvScriptUrl = "https://cdn.betterttv.net/betterttv.js";
            await ExecuteScriptAsync(coreWebView2, @"
                (function() {
                    const script = document.createElement(""script"");
                    script.src = """ + bttvScriptUrl + @""";
                    document.getElementsByTagName(""head"")[0].appendChild(script);
                })();
            ");
        }
        if (App.Settings.GeneralSettings.FrankerFaceZ)
        {
            // Observe for FrankerFaceZ's reskin stylesheet
            // that breaks the transparency and remove it
            await ExecuteScriptAsync(coreWebView2, @"
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
                        ");

            string ffzScriptUrl = "https://cdn.frankerfacez.com/static/script.min.js";

            await ExecuteScriptAsync(coreWebView2, @"
                (function() {
                    const script = document.createElement(""script"");
                    script.src = """ + ffzScriptUrl + @""";
                    document.getElementsByTagName(""head"")[0].appendChild(script);
                })();
            ");
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
}
