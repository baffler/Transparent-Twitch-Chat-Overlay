using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Media;

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
            var settings = SettingsSingleton.Instance.genSettings;
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
            sb.AppendLine("    const MAX_RETRIES = 20;");
            sb.AppendLine("    let currentRetry = 0;");
            sb.AppendLine("    const SCRIPT_ID = 'ChatWrapper_Main_v1.1';");
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
                // console.warn(`[${SCRIPT_ID} - WARN]: Error calling hostObject logMessage: ${e.message}`);
                // Avoid logging within logToWebViewConsole about logging itself to prevent loops if console is also broken
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
            sb.AppendLine("        CSHARP_SETTINGS.vips = JSON.parse(CSHARP_SETTINGS.vipListJSON);");
            sb.AppendLine("        CSHARP_SETTINGS.blockList = JSON.parse(CSHARP_SETTINGS.blockListJSON);");
            sb.AppendLine("        logToWebViewConsole('info', 'CSHARP_SETTINGS loaded: ' + JSON.stringify({ ...CSHARP_SETTINGS, vips: '...', blockList: '...' }));");


            // --- Original Chat.write and the New Wrapped Function ---
            sb.AppendLine("        const originalChatWrite = Chat.write;");
            sb.AppendLine("        Chat.write = function(nick, tags, message, platform) {");
            sb.AppendLine("            const lowerNick = nick.toLowerCase();");
            sb.AppendLine("            let allowOtherBasedOnTags = false;");
            sb.AppendLine("            let highlightSuffix = '';");

            // 1. Block List Check (Universal)
            sb.AppendLine("            if (CSHARP_SETTINGS.blockList.includes(lowerNick)) {");
            sb.AppendLine("                //logToWebViewConsole('info', `User '${nick}' is blocked. Message suppressed.`);");
            sb.AppendLine("                return;"); // Stop processing
            sb.AppendLine("            }");

            // 2. VIP/Mod Tag Checks (Used for multiple features)
            sb.AppendLine("            if (CSHARP_SETTINGS.filterAllowAllVIPs) {");
            sb.Append("                "); // Indentation for snippet
            sb.AppendLine(CustomJS_Defaults.jCyan_VIP_Check.Trim().Replace("\n", "\n                "));
            sb.AppendLine("            }");
            sb.AppendLine("            if (CSHARP_SETTINGS.filterAllowAllMods) {");
            sb.Append("                "); // Indentation for snippet
            sb.AppendLine(CustomJS_Defaults.jCyan_Mod_Check.Trim().Replace("\n", "\n                "));
            sb.AppendLine("            }");

            // 3. Allowed Users Only Filter
            sb.AppendLine("            if (CSHARP_SETTINGS.allowedUsersOnly) {");
            sb.AppendLine("                const isExplicitlyAllowed = CSHARP_SETTINGS.vips.includes(lowerNick);");
            sb.AppendLine("                if (!isExplicitlyAllowed && !allowOtherBasedOnTags) {");
            sb.AppendLine("                    //logToWebViewConsole('info', `User '${nick}' not in allowed list (AllowedUsersOnly). Message suppressed.`);");
            sb.AppendLine("                    return;"); // Stop processing
            sb.AppendLine("                }");
            sb.AppendLine("            }");

            // 4. Determine if Highlighting is Needed
            sb.AppendLine("            let applyHighlighting = false;");
            sb.AppendLine("            if (CSHARP_SETTINGS.highlightUsers) {");
            sb.AppendLine("                if (CSHARP_SETTINGS.vips.includes(lowerNick) || allowOtherBasedOnTags) {");
            sb.AppendLine("                    applyHighlighting = true;");
            sb.AppendLine("                    if (highlightSuffix === '' && CSHARP_SETTINGS.vips.includes(lowerNick)) highlightSuffix = ' vip-list-highlight';");
            // Generic highlight suffix if only allowOtherBasedOnTags was true and no specific one was set by VIP/Mod checks
            sb.AppendLine("                    if (highlightSuffix === '' && allowOtherBasedOnTags) highlightSuffix = ' general-tag-highlight';");
            sb.AppendLine("                    if (highlightSuffix === '') highlightSuffix = ' default-highlight';"); // Fallback if needed
            sb.AppendLine("                }");
            sb.AppendLine("            }");

            // 5. Play Notification Sound
            sb.AppendLine("            if (CSHARP_SETTINGS.playSound && CSHARP_SETTINGS.jsCallback && typeof CSHARP_SETTINGS.jsCallback.playSound === 'function') {");
            sb.AppendLine("                let shouldPlaySound = false;");
            sb.AppendLine("                if (CSHARP_SETTINGS.highlightUsers) {"); // If highlighting is on, sound only for highlighted users
            sb.AppendLine("                    if (applyHighlighting) shouldPlaySound = true;");
            sb.AppendLine("                } else if (CSHARP_SETTINGS.allowedUsersOnly) {"); // If allowed-only is on (and highlighting is off), sound for allowed users
            sb.AppendLine("                    if (CSHARP_SETTINGS.vips.includes(lowerNick) || allowOtherBasedOnTags) shouldPlaySound = true;");
            sb.AppendLine("                } else {"); // Neither specific filter is on, so sound for everyone (not blocked)
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
            sb.AppendLine("            if (applyHighlighting) {");
            sb.AppendLine("                //logToWebViewConsole('info', `Applying highlight (class: 'highlight${highlightSuffix}') for user '${nick}'.`);");
            sb.AppendLine("                const originalLinesPush = Chat.info.lines.push;");
            sb.AppendLine("                let capturedChatLineHtml = '';");
            sb.AppendLine("                Chat.info.lines.push = function(htmlOutput) { capturedChatLineHtml = htmlOutput; };");
            sb.AppendLine("                try { originalChatWrite.apply(this, arguments); } catch (e) { logToWebViewConsole('error', 'Error in originalChatWrite (highlight path): ' + e); }");
            sb.AppendLine("                Chat.info.lines.push = originalLinesPush;");
            sb.AppendLine("                if (capturedChatLineHtml) {");
            sb.AppendLine("                    const wrappedHtml = `<div class=\"highlight${highlightSuffix}\">${capturedChatLineHtml}</div>`;");
            sb.AppendLine("                    Chat.info.lines.push.call(Chat.info.lines, wrappedHtml);"); // Ensure correct 'this' for push
            sb.AppendLine("                } else { logToWebViewConsole('warn', `No HTML captured for highlight for nick: ${nick}`); }");
            sb.AppendLine("            } else {");
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

        public override string SetupCustomCSS() {
            string css = string.Empty;

            if (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.CustomCSS))
                css = SettingsSingleton.Instance.genSettings.CustomCSS;
            else
            {
                // Highlight
                Color c = SettingsSingleton.Instance.genSettings.ChatHighlightColor;
                float a = (c.A / 255f);
                string rgba = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, a);
                css = ".highlight { background-color: " + rgba + " !important; }";

                // Mods Highlight
                c = SettingsSingleton.Instance.genSettings.ChatHighlightModsColor;
                a = (c.A / 255f);
                rgba = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, a);
                css += "\n .highlightMod { background-color: " + rgba + " !important; }";

                // VIPs Highlight
                c = SettingsSingleton.Instance.genSettings.ChatHighlightVIPsColor;
                a = (c.A / 255f);
                rgba = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, a);
                css += "\n .highlightVIP { background-color: " + rgba + " !important; }";
            }

            return css;
        }
    }
}