using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Color = System.Windows.Media.Color;

namespace TransparentTwitchChatWPF.ChatProviders;

public class KapChatProvider : IChatProvider
{
    public Uri GetNavigationUri()
    {
        string username = App.Settings.GeneralSettings.Username;
        if (username.Contains("/"))
            username = username.Split('/').Last();

        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty or whitespace.", nameof(username));

        string fade = App.Settings.GeneralSettings.FadeTime;
        if (!App.Settings.GeneralSettings.FadeChat) { fade = "false"; }

        string theme = string.Empty;
        if ((App.Settings.GeneralSettings.ThemeIndex >= 0) && (App.Settings.GeneralSettings.ThemeIndex < KapChat.Themes.Count))
            theme = KapChat.Themes[App.Settings.GeneralSettings.ThemeIndex];

        string url = @"https://nightdev.com/hosted/obschat/?";
        url += @"theme=" + theme;
        url += @"&channel=" + username;
        url += @"&fade=" + fade;
        url += @"&bot_activity=" + (!App.Settings.GeneralSettings.BlockBotActivity).ToString();
        url += @"&prevent_clipping=false";

        return new Uri(url);
    }

    public async Task ConfigureAsync(CoreWebView2 coreWebView2)
    {
        string browserPath = Path.Combine(AppContext.BaseDirectory, "browser");
        if (Directory.Exists(browserPath))
        {
            string emoteBundlePath = Path.Combine(browserPath, "emote-bundle.js");
            if (File.Exists(emoteBundlePath))
            {
                string emoteBundleScript = File.ReadAllText(emoteBundlePath);
                //_logger.LogInformation(emoteBundleScript);

                await coreWebView2.ExecuteScriptAsync(emoteBundleScript);

                //await this.webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(emoteBundleScript);
            }
            else
            {
                //_logger.LogWarning("Emote bundle script not found at: " + emoteBundlePath);
            }
        }
        else
        {
            //_logger.LogWarning("Browser path does not exist: " + browserPath);
        }
    }

    public string GetCssToInject()
    {
        // If the user has provided their own CSS, use that and bypass our generation.
        if (!string.IsNullOrEmpty(App.Settings.GeneralSettings.CustomCSS))
        {
            return App.Settings.GeneralSettings.CustomCSS;
        }

        // Prepare the dynamic color values first.
        Color highlightColor = App.Settings.GeneralSettings.ChatHighlightColor;
        string rgbaHighlight = $"rgba({highlightColor.R},{highlightColor.G},{highlightColor.B},{highlightColor.A / 255f:0.00})";

        Color modsColor = App.Settings.GeneralSettings.ChatHighlightModsColor;
        string rgbaMods = $"rgba({modsColor.R},{modsColor.G},{modsColor.B},{modsColor.A / 255f:0.00})";

        Color vipsColor = App.Settings.GeneralSettings.ChatHighlightVIPsColor;
        string rgbaVIPs = $"rgba({vipsColor.R},{vipsColor.G},{vipsColor.B},{vipsColor.A / 255f:0.00})";

        // Use a raw string literal to build the final CSS string
        string finalCss = $$"""
        .chat_line {
            line-height: 28px;
        }

        .emote {
            max-height: 28px;
            margin: -4px -2px;
        }

        .emoticon {
            max-height: 28px;
            margin: -4px -2px;
        }

        /* Ensure badges are also aligned correctly. */
        .badges img {
            width: 20px;
            height: 20px;
            vertical-align: middle;
            margin-bottom: 8px;
        }

        /* Ensure username and message flow as a single text block. */
        .username, .message {
            display: inline !important;
            vertical-align: middle;
        }

        /* User-defined highlight colors */
        .highlight { background-color: {{rgbaHighlight}} !important; }
        .highlightMod { background-color: {{rgbaMods}} !important; }
        .highlightVIP { background-color: {{rgbaVIPs}} !important; }
    """;

        return finalCss;
    }

    public string GetJavascriptToExecute()
    {
        var settings = App.Settings.GeneralSettings;

        var vipList = settings.AllowedUsersList?.Cast<string>().Select(u => u.ToLowerInvariant()).ToList() ?? new List<string>();
        var blockList = settings.BlockedUsersList?.Cast<string>().Select(u => u.ToLowerInvariant()).ToList() ?? new List<string>();

        string vipListJson = JsonSerializer.Serialize(vipList);
        string blockListJson = JsonSerializer.Serialize(blockList);

        string username = App.Settings.GeneralSettings.Username;

        string finalScript = $$"""
(function() {
    'use strict';

    const MAX_RETRIES = 20;
    let currentRetry = 0;
    const SCRIPT_ID = 'KapChat_Wrapper_v2.3_Robust';

    // Simplified logger for maximum reliability
    function logToWebViewConsole(level, message) {
        const logMessage = `[${SCRIPT_ID} - ${level.toUpperCase()}]: ${message}`;
        console.log(logMessage); // Always log to the dev console as a fallback
        try {
            if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects &&
                window.chrome.webview.hostObjects.jsCallbackFunctions &&
                typeof window.chrome.webview.hostObjects.jsCallbackFunctions.logMessage === 'function') {
                window.chrome.webview.hostObjects.jsCallbackFunctions.logMessage(logMessage);
            }
        } catch (e) { /* silent fail */ }
    }

    logToWebViewConsole('info', 'KapChat wrapper injection script started.');

    function performChatInsertModification() {
        if (typeof Chat === 'undefined' || typeof Chat.insert !== 'function' || typeof window.emoteManager === 'undefined') {
            currentRetry++;
            if (currentRetry < MAX_RETRIES) {
                setTimeout(performChatInsertModification, 500);
            } else {
                logToWebViewConsole('error', `Failed to find Chat.insert or window.emoteManager after max retries. Chat ready: ${typeof Chat !== 'undefined'}, EmoteManager ready: ${typeof window.emoteManager !== 'undefined'}`);
            }
            return;
        }

        if (Chat.insert.isWrappedByMyScript) {
            return;
        }

        try {
            emoteManager.init('{{username}}');
            logToWebViewConsole('info', 'Emote manager initialized for channel: {{username}}');
        } catch (e) {
            logToWebViewConsole('error', `Failed to initialize emote manager: ${e.message}`);
            return; // Stop if emotes can't be initialized
        }

        const CSHARP_SETTINGS = {
            highlightUsers: {{settings.HighlightUsersChat.ToString().ToLower()}},
            allowedUsersOnly: {{settings.AllowedUsersOnlyChat.ToString().ToLower()}},
            playSound: {{(settings.ChatNotificationSound?.ToLower() != "none").ToString().ToLower()}},
            filterAllowAllVIPs: {{settings.FilterAllowAllVIPs.ToString().ToLower()}},
            filterAllowAllMods: {{settings.FilterAllowAllMods.ToString().ToLower()}},
            vips: JSON.parse('{{vipListJson}}'),
            blockList: JSON.parse('{{blockListJson}}'),
            jsCallback: (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects) ? window.chrome.webview.hostObjects.jsCallbackFunctions : null
        };

        const originalChatInsert = Chat.insert;
        Chat.insert = function(nick, tags, message) {
            const lowerNick = (nick || '').toLowerCase();
            if (CSHARP_SETTINGS.blockList.includes(lowerNick)) return;

            const parsedTags = tags ? Chat.parseTags(nick, tags) : {};

            let allowOtherBasedOnTags = false;
            let highlightSuffix = '';
            let isVip = false;
            let isMod = false;

            // We need to loop through the badges array instead of checking for a property.
            if (parsedTags.badges && Array.isArray(parsedTags.badges)) {
                parsedTags.badges.forEach(badge => {
                    if (badge.type === 'vip') {
                        isVip = true;
                    } else if (badge.type === 'moderator') {
                        isMod = true;
                    }
                });
            }

            if (CSHARP_SETTINGS.filterAllowAllVIPs && isVip) {
                highlightSuffix = 'VIP';
                allowOtherBasedOnTags = true;
            }
            if (CSHARP_SETTINGS.filterAllowAllMods && isMod) {
                highlightSuffix = 'Mod';
                allowOtherBasedOnTags = true;
            }

            if (CSHARP_SETTINGS.allowedUsersOnly) {
                const isExplicitlyAllowed = CSHARP_SETTINGS.vips.includes(lowerNick);
                if (!isExplicitlyAllowed && !allowOtherBasedOnTags && nick) return;
            }

            const isManuallyHighlightedUser = CSHARP_SETTINGS.vips.includes(lowerNick);
            const shouldHighlight = CSHARP_SETTINGS.highlightUsers && (isManuallyHighlightedUser || allowOtherBasedOnTags);

            if (CSHARP_SETTINGS.playSound) {
                 let shouldPlaySound = !CSHARP_SETTINGS.highlightUsers && !CSHARP_SETTINGS.allowedUsersOnly ||
                                      (CSHARP_SETTINGS.highlightUsers && shouldHighlight) ||
                                      (CSHARP_SETTINGS.allowedUsersOnly && (CSHARP_SETTINGS.vips.includes(lowerNick) || allowOtherBasedOnTags));
                if (shouldPlaySound && CSHARP_SETTINGS.jsCallback) {
                    CSHARP_SETTINGS.jsCallback.playSound().catch(err => logToWebViewConsole('error', 'Error playing sound: ' + (err.message || err)));
                }
            }

            const originalQueuePush = Chat.vars.queue.push;
            let capturedHtml = '';
            Chat.vars.queue.push = (html) => { capturedHtml += html; };

            try {
                originalChatInsert.apply(this, arguments);
            } finally {
                Chat.vars.queue.push = originalQueuePush;
            }

            if (capturedHtml) {
                const tempDiv = document.createElement('div');
                tempDiv.innerHTML = capturedHtml;
                const chatLine = tempDiv.querySelector('.chat_line');
                const messageSpan = tempDiv.querySelector('.chat_line .message'); 

                if (messageSpan) {
                    try {
                        const emoteMaps = emoteManager.getEmoteMaps();
                        const allEmotes = new Map([...emoteMaps.sevenTV, ...emoteMaps.bttv, ...emoteMaps.ffz]);
                        // Replace the inner HTML of the message span, not the whole message string
                        messageSpan.innerHTML = emoteManager.replace(messageSpan.innerHTML, allEmotes);
                    } catch (e) {
                         logToWebViewConsole('error', `Failed to replace emotes in DOM: ${e.message}`);
                    }
                }

                if (chatLine && shouldHighlight) {
                    chatLine.classList.add(`highlight${highlightSuffix}`);
                }

                // Push the fully modified HTML
                Chat.vars.queue.push.call(Chat.vars.queue, tempDiv.innerHTML);

            } else {
                 // Fallback if HTML capture fails
                originalChatInsert.apply(this, arguments);
            }
        };
        Chat.insert.isWrappedByMyScript = true;
        logToWebViewConsole('info', 'SUCCESS: Chat.insert fully modified for emotes and highlighting.');
    }
    performChatInsertModification();
})();
""";

        return finalScript;
    }
}
