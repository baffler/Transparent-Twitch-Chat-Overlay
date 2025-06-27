using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
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
            return "";
        }

        public override string PushNewMessage(string message = "") {
            return $"Chat.info.lines.push(\"<div class=\\\"chat_line\\\" data-time=\\\"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}\\\">{message}</div>\");";
        }
        
        public override string SetupJavascript() {
            var settings = App.Settings.GeneralSettings;
            // Determine if any Chat.write modification is needed at all.
            bool modifyChatWrite = settings.HighlightUsersChat ||
                                   settings.AllowedUsersOnlyChat ||
                                   (settings.ChatNotificationSound.ToLower() != "none") ||
                                   (settings.BlockedUsersList != null && settings.BlockedUsersList.Count > 0);

            if (!modifyChatWrite)
            {
                return string.Empty; // No JavaScript injection needed if no features are active
            }
            
            var sb = new StringBuilder();
            // --- Start of the self-invoking wrapper function ---
            sb.AppendLine("(function() {"); // Encapsulate to avoid global scope pollution as much as possible
            sb.AppendLine("    'use strict';"); // Enable strict mode for better error handling
            sb.AppendLine("    const MAX_RETRIES = 20;");
            sb.AppendLine("    let currentRetry = 0;");
            sb.AppendLine("    const SCRIPT_ID = 'ChatWrapper_Main_v1.2';");
            sb.AppendLine(@"
        function logToWebViewConsole(level, message) {
            const logMessage = `[${SCRIPT_ID} - ${level.toUpperCase()}]: ${message}`;
            if (console[level.toLowerCase()]) {
                console[level.toLowerCase()](logMessage);
            } else {
                console.log(logMessage);
            }
            
            // host object logging
            try {
                if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects &&
                    window.chrome.webview.hostObjects.jsCallbackFunctions &&
                    typeof window.chrome.webview.hostObjects.jsCallbackFunctions.logMessage === 'function')
                {
                    window.chrome.webview.hostObjects.jsCallbackFunctions.logMessage(logMessage);
                }
            } catch (e) {
                // Avoid logging errors about logging, which can cause infinite loops.
            }
        }");

            sb.AppendLine("    logToWebViewConsole('info', 'Chat wrapper injection script started.');");

            sb.AppendLine("    function performChatWriteModification() {");
            sb.AppendLine("        logToWebViewConsole('info', `Attempting Chat.write modification (Attempt ${currentRetry + 1}/${MAX_RETRIES}).`);");

            sb.AppendLine("        if (typeof Chat === 'undefined' || typeof Chat.write !== 'function' || typeof Chat.info === 'undefined' || typeof Chat.info.lines === 'undefined') {");
            sb.AppendLine("            currentRetry++;");
            sb.AppendLine("            let reason = (typeof Chat === 'undefined') ? 'Chat is undefined.' : (typeof Chat.write !== 'function') ? 'Chat.write is not a function.' : 'Chat.info.lines is undefined.';");
            sb.AppendLine("            logToWebViewConsole('warn', `Prerequisites not met: ${reason}`);");
            sb.AppendLine("            if (currentRetry < MAX_RETRIES) {");
            sb.AppendLine("                setTimeout(performChatWriteModification, 500);"); // Retry
            sb.AppendLine("            } else {");
            sb.AppendLine("                logToWebViewConsole('error', 'Failed to modify Chat.write after max retries.');");
            sb.AppendLine("            }");
            sb.AppendLine("            return;");
            sb.AppendLine("        }");

            sb.AppendLine("        if (Chat.write.isWrappedByMyScript) {");
            sb.AppendLine("            logToWebViewConsole('warn', 'Chat.write is already wrapped. Skipping.');");
            sb.AppendLine("            return;");
            sb.AppendLine("        }");

            // --- C# Settings Object Injection ---
            sb.AppendLine("        const CSHARP_SETTINGS = {");
            sb.AppendLine($"            highlightUsers: {settings.HighlightUsersChat.ToString().ToLower()},");
            sb.AppendLine($"            allowedUsersOnly: {settings.AllowedUsersOnlyChat.ToString().ToLower()},");
            sb.AppendLine($"            playSound: {(settings.ChatNotificationSound.ToLower() != "none").ToString().ToLower()},");
            sb.AppendLine($"            filterAllowAllVIPs: {settings.FilterAllowAllVIPs.ToString().ToLower()},");
            sb.AppendLine($"            filterAllowAllMods: {settings.FilterAllowAllMods.ToString().ToLower()},");

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
            
            sb.AppendLine($"            vipListJSON: '{JsonSerializer.Serialize(vipList)}',"); // Pass as string, parse in JS
            sb.AppendLine($"            blockListJSON: '{JsonSerializer.Serialize(blockList)}',");
            sb.AppendLine("            jsCallback: (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects) ? window.chrome.webview.hostObjects.jsCallbackFunctions : null");
            sb.AppendLine("        };");
            // Parse JSON strings in JS
            sb.AppendLine("        try {");
            sb.AppendLine("             CSHARP_SETTINGS.vips = JSON.parse(CSHARP_SETTINGS.vipListJSON);");
            sb.AppendLine("             CSHARP_SETTINGS.blockList = JSON.parse(CSHARP_SETTINGS.blockListJSON);");
            sb.AppendLine("        } catch (e) {");
            sb.AppendLine("             logToWebViewConsole('error', `Failed to parse settings JSON: ${e.message}`);");
            sb.AppendLine("             CSHARP_SETTINGS.vips = [];"); // Default to empty array on error
            sb.AppendLine("             CSHARP_SETTINGS.blockList = [];"); // Default to empty array on error
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        logToWebViewConsole('info', 'CSHARP_SETTINGS loaded: ' + JSON.stringify({ ...CSHARP_SETTINGS }));");


            // --- Original Chat.write and the New Wrapped Function ---
            sb.AppendLine("        const originalChatWrite = Chat.write;");
            sb.AppendLine("        Chat.write = function(nick, tags, message, platform) {");
            sb.AppendLine("            const lowerNick = nick.toLowerCase();");
            sb.AppendLine("            let allowOtherBasedOnTags = false;");
            sb.AppendLine("            let highlightSuffix = '';");
            sb.AppendLine("            let isVip = false;");
            sb.AppendLine("            let isMod = false;");

            // 1. Block List Check (Universal)
            sb.AppendLine("            if (CSHARP_SETTINGS.blockList.includes(lowerNick)) {");
            sb.AppendLine("                //logToWebViewConsole('info', `User '${nick}' is blocked. Message suppressed.`);");
            sb.AppendLine("                return;"); // Suppress message
            sb.AppendLine("            }");

            // 2. VIP/Mod Tag Checks
            sb.AppendLine(@"            if (tags && typeof(tags.badges) === 'string')
            {
                tags.badges.split(',').forEach(badgeStr => {
                    const badge = badgeStr.split('/')[0].toLowerCase();
                    if (badge === 'vip')
                    {
                        isVip = true;
                    }
                    else if (badge === 'moderator')
                    {
                        isMod = true;
                    }
                });
            }");

            sb.AppendLine("            if (CSHARP_SETTINGS.filterAllowAllVIPs && isVip) {");
            sb.AppendLine("                 highlightSuffix = 'VIP';");
            sb.AppendLine("                 allowOtherBasedOnTags = true;"); // Allow other based on VIP tag
            sb.AppendLine("            }");
            sb.AppendLine("            if (CSHARP_SETTINGS.filterAllowAllMods && isMod) {");
            sb.AppendLine("                 highlightSuffix = 'Mod';");
            sb.AppendLine("                 allowOtherBasedOnTags = true;"); // Allow other based on Mod tag
            sb.AppendLine("            }");

            // 3. Allowed Users Only Filter
            sb.AppendLine("            if (CSHARP_SETTINGS.allowedUsersOnly) {");
            sb.AppendLine("                const isExplicitlyAllowed = CSHARP_SETTINGS.vips.includes(lowerNick);");
            sb.AppendLine("                if (!isExplicitlyAllowed && !allowOtherBasedOnTags) {");
            sb.AppendLine("                    //logToWebViewConsole('info', `User '${nick}' not in allowed list (AllowedUsersOnly). Message suppressed.`);");
            sb.AppendLine("                    return;"); // Suppress message
            sb.AppendLine("                }");
            sb.AppendLine("            }");

            // 4. Determine if highlighting should be applied BEFORE using it. ***
            sb.AppendLine("            const isManuallyHighlightedUser = CSHARP_SETTINGS.vips.includes(lowerNick);");
            sb.AppendLine("            const shouldHighlight = CSHARP_SETTINGS.highlightUsers && (isManuallyHighlightedUser || allowOtherBasedOnTags);");

            // 5. Play Notification Sound
            sb.AppendLine("            if (CSHARP_SETTINGS.playSound && CSHARP_SETTINGS.jsCallback && typeof CSHARP_SETTINGS.jsCallback.playSound === 'function') {");
            sb.AppendLine("                let shouldPlaySound = false;");
            sb.AppendLine("                if (CSHARP_SETTINGS.highlightUsers) {"); // If highlighting is on, sound only for highlighted users
            sb.AppendLine("                    if (shouldHighlight) shouldPlaySound = true;");
            sb.AppendLine("                } else if (CSHARP_SETTINGS.allowedUsersOnly) {"); // If allowed-only is on (and highlighting is off), sound for allowed users
            sb.AppendLine("                    if (CSHARP_SETTINGS.vips.includes(lowerNick) || allowOtherBasedOnTags) shouldPlaySound = true;");
            sb.AppendLine("                } else {"); // If not highlighting or filtering, play sound for every message
            sb.AppendLine("                    shouldPlaySound = true;");
            sb.AppendLine("                }");
            sb.AppendLine("                if (shouldPlaySound) {");
            sb.AppendLine("                    CSHARP_SETTINGS.jsCallback.playSound().then(");
            sb.AppendLine(@"                        // empty success handler (onFulfilled)
                                                    () => {
                                                        // Sound played successfully.
                                                        // logToWebViewConsole('debug', 'playSound() resolved successfully.');
                                                    },
                                                    // error handler (onRejected)
                                                    err => {
                                                        logToWebViewConsole('error', 'Error playing sound: ' + (err.message || err));
                                                    }
                                                );");
            sb.AppendLine("                }");
            sb.AppendLine("            }");

            // 6. Execute Chat Write (Original or Wrapped for Highlighting)
            sb.AppendLine("            if (shouldHighlight) {");
            sb.AppendLine("                //logToWebViewConsole('info', `Applying highlight (class: 'highlight${highlightSuffix}') for user '${nick}'.`);");
            sb.AppendLine("                const originalLinesPush = Chat.info.lines.push;");
            sb.AppendLine("                let capturedChatLineHtml = '';");
            sb.AppendLine("                Chat.info.lines.push = function(htmlOutput) { capturedChatLineHtml = htmlOutput; };");
            sb.AppendLine("                try { originalChatWrite.apply(this, arguments); } catch (e) { logToWebViewConsole('error', 'Error in originalChatWrite (highlight path): ' + e); }");
            sb.AppendLine("                Chat.info.lines.push = originalLinesPush;");
            sb.AppendLine("                if (capturedChatLineHtml) {");
            sb.AppendLine("                    const wrappedHtml = `<div class=\"highlight${highlightSuffix}\">${capturedChatLineHtml}</div>`;");
            // Push the modified HTML to the chat display
            sb.AppendLine("                    Chat.info.lines.push.call(Chat.info.lines, wrappedHtml);"); // Ensure correct 'this' for push
            sb.AppendLine("                }");
            // Fallback in case capture failed, just write the message normally.
            sb.AppendLine("                else {");
            sb.AppendLine("                    logToWebViewConsole('warn', `No HTML captured for highlight for nick: ${nick}`);");
            sb.AppendLine("                    try { originalChatWrite.apply(this, arguments); } catch (e) { logToWebViewConsole('error', 'Error in originalChatWrite (fallback path): ' + e); }");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            // Normal path for users who are not highlighted
            sb.AppendLine("            else {");
            sb.AppendLine("                try { originalChatWrite.apply(this, arguments); } catch (e) { logToWebViewConsole('error', 'Error in originalChatWrite (normal path): ' + e); }");
            sb.AppendLine("            }");
            sb.AppendLine("            return;"); // End of wrapped Chat.write
            sb.AppendLine("        };"); // End of Chat.write = function

            sb.AppendLine("        Chat.write.isWrappedByMyScript = true;"); // Mark as wrapped
            sb.AppendLine("        logToWebViewConsole('info', 'SUCCESS: Chat.write has been modified.');");

            sb.AppendLine("    }"); // End of performChatWriteModification

            sb.AppendLine("    performChatWriteModification();"); // Initial call
            sb.AppendLine("})();"); // End of self-invoking wrapper function

            return sb.ToString();
        }

        public string SetupJavascript2()
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

            // 1. Prepare settings values for injection
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

            // 2. Use a Raw String Literal ($""") to build the final script
            string finalScript = $$"""
        (function() {
            'use strict';

            const MAX_RETRIES = 20;
            let currentRetry = 0;
            const SCRIPT_ID = 'ChatWrapper_Main_v1.4_Raw';

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
                            const wrappedHtml = `<div class="highlight${highlightSuffix}">${capturedChatLineHtml}</div>`;
                            Chat.info.lines.push.call(Chat.info.lines, wrappedHtml);
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