using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Windows.Media;
using TwitchLib.Api.Helix;
using Color = System.Windows.Media.Color;

namespace TransparentTwitchChatWPF.Chats
{
    public class jCyan : Chat
    {
        public jCyan() : base(ChatTypes.jCyan)
        {
        }

        public override string PushNewChatMessage(string message = "", string nick = "", string color = "") {
            string safeNickForJs = string.IsNullOrEmpty(nick) ? "null" : JsonSerializer.Serialize(nick);

            string js;

            if (string.IsNullOrEmpty(color))
            {
                string safeMessageForJs = JsonSerializer.Serialize(message);
                js = $"Chat.write({safeNickForJs}, null, {safeMessageForJs});";
            }
            else
            {
                string safeColorForJs = JsonSerializer.Serialize(color);

                // Escape ONLY the user-provided message content.
                // JavaScriptStringEncode will handle quotes, backslashes, etc., inside the message.
                string escapedMessage = HttpUtility.JavaScriptStringEncode(message);

                // Manually construct the final JavaScript string literal.
                // We add the \x01 characters and the outer quotes. Because the content
                // is already escaped, this is now safe.
                string finalActionArgument = $"\"\\x01ACTION {escapedMessage}\\x01\"";

                js = $"var ttags = {{ color: {safeColorForJs} }};\n" +
                     $"Chat.write({safeNickForJs}, ttags, {finalActionArgument});";
            }

            return js;
        }

        public override string PushNewMessage(string message = "") {
            // Encode the message to be safe for insertion into HTML.
            // This turns characters like '<' and '>' into '&lt;' and '&gt;'.
            string safeHtmlMessage = HttpUtility.HtmlEncode(message);

            // Construct the full HTML string
            string htmlString = $"<div class=\"chat_line\" data-time=\"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}\">{safeHtmlMessage}</div>";

            // Serialize the entire HTML string to make it a valid JavaScript string literal.
            // This handles all quotes and backslashes, and wraps it in quotes for JavaScript.
            string finalJsArgument = JsonSerializer.Serialize(htmlString);

            // Push the safe, serialized string.
            return $"Chat.info.lines.push({finalJsArgument});";
        }
        
        public override string SetupJavascript()
        {
            var settings = App.Settings.GeneralSettings;

            bool modifyChatWrite = settings.HighlightUsersChat ||
                                     settings.AllowedUsersOnlyChat ||
                                     (settings.ChatNotificationSound?.ToLower() != "none") ||
                                     (settings.BlockedUsersList != null && settings.BlockedUsersList.Count > 0);

            if (!modifyChatWrite)
            {
                return string.Empty; // No script needed
            }

            // Prepare settings values for injection
            //var vipList = settings.AllowedUsersList ?? new List<string>();
            //var blockList = settings.BlockedUsersList ?? new List<string>();
            List<string> vipList = new List<string>();
            if (settings.AllowedUsersList != null)
            {
                foreach (string item in settings.AllowedUsersList)
                {
                    vipList.Add(item.ToLowerInvariant());
                }
            }

            List<string> blockList = new List<string>();
            if (settings.BlockedUsersList != null)
            {
                foreach (string item in settings.BlockedUsersList)
                {
                    blockList.Add(item.ToLowerInvariant());
                }
            }

            // Serialize lists to JSON strings. Lowercasing is handled in JS for consistency.
            string vipListJson = JsonSerializer.Serialize(vipList.ConvertAll(u => u.ToLowerInvariant()));
            string blockListJson = JsonSerializer.Serialize(blockList.ConvertAll(u => u.ToLowerInvariant()));

            // Use a Raw String Literal ($""") to build the final script
            string finalScript = $$"""
        (function() {
            'use strict';

            const MAX_RETRIES = 20;
            let currentRetry = 0;
            const SCRIPT_ID = 'ChatWrapper_Main_v1.5_Robust';

            function logToWebViewConsole(level, message) {
                const logMessage = `[${SCRIPT_ID} - ${level.toUpperCase()}]: ${message}`;
                if (console[level.toLowerCase()]) {
                    console[level.toLowerCase()](logMessage);
                } else {
                    console.log(logMessage);
                }
                try {
                    if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects &&
                        window.chrome.webview.hostObjects.jsCallbackFunctions &&
                        typeof window.chrome.webview.hostObjects.jsCallbackFunctions.logMessage === 'function') {
                        window.chrome.webview.hostObjects.jsCallbackFunctions.logMessage(logMessage);
                    }
                } catch (e) { /* silent fail */ }
            }

            logToWebViewConsole('info', 'Chat wrapper injection script started.');

            function performChatWriteModification() {
                if (typeof Chat === 'undefined' || typeof Chat.write !== 'function' || typeof Chat.info === 'undefined' || typeof Chat.info.lines === 'undefined') {
                    currentRetry++;
                    if (currentRetry < MAX_RETRIES) {
                        setTimeout(performChatWriteModification, 500);
                    } else {
                        logToWebViewConsole('error', 'Failed to modify Chat.write after max retries.');
                    }
                    return;
                }

                if (Chat.write.isWrappedByMyScript) {
                    return;
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

                const originalChatWrite = Chat.write;
                Chat.write = function(nick, tags, message, platform) {
                    const lowerNick = nick.toLowerCase();
                    if (CSHARP_SETTINGS.blockList.includes(lowerNick)) return;
                    
                    let allowOtherBasedOnTags = false;
                    let highlightSuffix = '';
                    let isVip = false;
                    let isMod = false;

                    if (tags && typeof(tags.badges) === 'string') {
                        tags.badges.split(',').forEach(badgeStr => {
                            const badge = badgeStr.split('/')[0].toLowerCase();
                            if (badge === 'vip') isVip = true;
                            else if (badge === 'moderator') isMod = true;
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
                        if (!isExplicitlyAllowed && !allowOtherBasedOnTags) return;
                    }
                    
                    const isManuallyHighlightedUser = CSHARP_SETTINGS.vips.includes(lowerNick);
                    const shouldHighlight = CSHARP_SETTINGS.highlightUsers && (isManuallyHighlightedUser || allowOtherBasedOnTags);

                    // Sound logic...
                    if (CSHARP_SETTINGS.playSound && CSHARP_SETTINGS.jsCallback && typeof CSHARP_SETTINGS.jsCallback.playSound === 'function') {
                         let shouldPlaySound = !CSHARP_SETTINGS.highlightUsers && !CSHARP_SETTINGS.allowedUsersOnly ||
                                     (CSHARP_SETTINGS.highlightUsers && shouldHighlight) ||
                                     (CSHARP_SETTINGS.allowedUsersOnly && (CSHARP_SETTINGS.vips.includes(lowerNick) || allowOtherBasedOnTags));
                        if (shouldPlaySound) {
                            CSHARP_SETTINGS.jsCallback.playSound().catch(err => logToWebViewConsole('error', 'Error playing sound: ' + (err.message || err)));
                        }
                    }
                    
                    if (shouldHighlight) {
                        const originalLinesPush = Chat.info.lines.push;
                        let capturedChatLineHtml = '';
                        Chat.info.lines.push = (html) => { capturedChatLineHtml = html; };
                        try {
                            originalChatWrite.apply(this, arguments);
                        } finally {
                            Chat.info.lines.push = originalLinesPush;
                        }
                        if (capturedChatLineHtml) {
                            const tempDiv = document.createElement('div');
                            tempDiv.innerHTML = capturedChatLineHtml;
                            const chatLine = tempDiv.firstElementChild;

                            if (chatLine) {
                                chatLine.classList.add(`highlight${highlightSuffix}`);
                                const modifiedHtml = chatLine.outerHTML;
                                Chat.info.lines.push.call(Chat.info.lines, modifiedHtml);
                            } else {
                                // if for some reason we couldn't parse the html, fall back to the original
                                // html so the message is not lost
                                Chat.info.lines.push.call(Chat.info.lines, capturedChatLineHtml);
                            }
                        } else {
                            originalChatWrite.apply(this, arguments);
                        }
                    } else {
                        originalChatWrite.apply(this, arguments);
                    }
                };
                Chat.write.isWrappedByMyScript = true;
                logToWebViewConsole('info', 'SUCCESS: Chat.write has been modified.');
            }
            performChatWriteModification();
        })();
        """;

            return finalScript;
        }

        public override string SetupCustomCSS() {
            string css = string.Empty;

            if (!string.IsNullOrEmpty(App.Settings.GeneralSettings.CustomCSS))
                css = App.Settings.GeneralSettings.CustomCSS;
            else
            {
                // Highlight
                Color c = App.Settings.GeneralSettings.ChatHighlightColor;
                float a = (c.A / 255f);
                string rgba = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, a);
                css = ".highlight { background-color: " + rgba + " !important; }";

                // Mods Highlight
                c = App.Settings.GeneralSettings.ChatHighlightModsColor;
                a = (c.A / 255f);
                rgba = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, a);
                css += "\n .highlightMod { background-color: " + rgba + " !important; }";

                // VIPs Highlight
                c = App.Settings.GeneralSettings.ChatHighlightVIPsColor;
                a = (c.A / 255f);
                rgba = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, a);
                css += "\n .highlightVIP { background-color: " + rgba + " !important; }";
            }

            return css;
        }
    }
}