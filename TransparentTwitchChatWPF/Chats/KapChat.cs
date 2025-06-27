using System.Text.Json;
using System.Web;
using Color = System.Windows.Media.Color;

namespace TransparentTwitchChatWPF.Chats
{
    public class KapChat : Chat
    {
        public KapChat() : base(ChatTypes.KapChat)
        {
        }

        public override string PushNewChatMessage(string message = "", string nick = "", string color = "")
        {
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

                // Step 1: Escape ONLY the user-provided message content.
                // JavaScriptStringEncode will handle quotes, backslashes, etc., inside the message.
                // Importantly, it will leave the \x01 characters alone if they were in the message.
                string escapedMessage = HttpUtility.JavaScriptStringEncode(message);

                // Step 2: Manually construct the final JavaScript string literal.
                // We add the \x01 characters and the outer quotes. Because the content
                // is already escaped, this is now safe.
                string finalActionArgument = $"\"\\x01ACTION {escapedMessage}\\x01\"";

                js = $"var ttags = {{ color: {safeColorForJs} }};\n" +
                     $"Chat.insert({safeNickForJs}, ttags, {finalActionArgument});";
            }

            return js;
        }

        public override string PushNewMessage(string message = "")
        {
            // Serialize the message to make it a valid JavaScript string literal.
            // This handles quotes, backslashes, newlines, etc.
            string safeJsMessage = JsonSerializer.Serialize(message);

            return $"Chat.insert(null, null, {safeJsMessage});";
        }

        public override string SetupJavascript()
        {
            string[] blockList = new string[App.Settings.GeneralSettings.BlockedUsersList.Count];
            App.Settings.GeneralSettings.BlockedUsersList.CopyTo(blockList, 0);

            string js = @"const jsCallback = chrome.webview.hostObjects.jsCallbackFunctions;";

            if (App.Settings.GeneralSettings.HighlightUsersChat)
            {
                string[] vipList = new string[App.Settings.GeneralSettings.AllowedUsersList.Count];
                App.Settings.GeneralSettings.AllowedUsersList.CopyTo(vipList, 0);

                js += @"var oldChatInsert = Chat.insert;
                            Chat.insert = function(nick, tags, message) {
                                var nick = nick || 'Chat';
                                var tags2 = tags ? Chat.parseTags(nick, tags) : {};

                                var vips = ['";
                js += string.Join(",", vipList).Replace(",", "','").ToLower();
                js += @"'];
                                var allowOther = false;
                                var highlightSuffix = '';

                                var blockUsers = ['";
                js += string.Join(",", blockList).Replace(",", "','").ToLower();
                js += @"'];
                                if (blockUsers.includes(nick.toLowerCase())) {
                                    return;
                                }";

                if (App.Settings.GeneralSettings.FilterAllowAllVIPs)
                    js += CustomJS_Defaults.VIP_Check;

                if (App.Settings.GeneralSettings.FilterAllowAllMods)
                    js += CustomJS_Defaults.Mod_Check;

                js += @"if (vips.includes(nick.toLowerCase()) || allowOther) {";

                if (App.Settings.GeneralSettings.ChatNotificationSound.ToLower() != "none")
                    js += CustomJS_Defaults.Callback_PlaySound;

                js += @"
                                Chat.vars.queue.push('<div class=""highlight' + highlightSuffix + '"">');
                                oldChatInsert.apply(oldChatInsert, arguments);
                                Chat.vars.queue.push('</div>');
                                return;
                            }
                            else
                            {
                                return oldChatInsert.apply(oldChatInsert, arguments);
                            }
                        }";

                return js;
            }
            else if (App.Settings.GeneralSettings.AllowedUsersOnlyChat)
            {
                string[] vipList = new string[App.Settings.GeneralSettings.AllowedUsersList.Count];
                App.Settings.GeneralSettings.AllowedUsersList.CopyTo(vipList, 0);

                js += @"var oldChatInsert = Chat.insert;
                            Chat.insert = function(nick, tags, message) {
                                var nick = nick || 'Chat';
                                var tags2 = tags ? Chat.parseTags(nick, tags) : {};

                                var vips = ['";
                js += string.Join(",", vipList).Replace(",", "','").ToLower();
                js += @"'];
                                var allowOther = false;";

                if (App.Settings.GeneralSettings.FilterAllowAllVIPs)
                    js += CustomJS_Defaults.VIP_Check;

                if (App.Settings.GeneralSettings.FilterAllowAllMods)
                    js += CustomJS_Defaults.Mod_Check;

                js += @"if (vips.includes(nick.toLowerCase()) || (nick == 'Chat') || allowOther) {";

                if (App.Settings.GeneralSettings.ChatNotificationSound.ToLower() != "none")
                    js += CustomJS_Defaults.Callback_PlaySound;

                js += @"
                            return oldChatInsert.apply(oldChatInsert, arguments);
                        }
                    }";

                return js;
            }
            else if (App.Settings.GeneralSettings.ChatNotificationSound.ToLower() != "none")
            {
                // Insert JS to play a sound on each chat message
                js += @"var oldChatInsert = Chat.insert;
                            Chat.insert = function(nick, tags, message) {
                                var nick = nick || 'Chat';
                                var tags2 = tags ? Chat.parseTags(nick, tags) : {};

                                var blockUsers = ['";
                js += string.Join(",", blockList).Replace(",", "','").ToLower();
                js += @"'];
                                if (blockUsers.includes(nick.toLowerCase())) {
                                    return;
                                }

                                (async function() {
                                    await jsCallback.playSound();
                                })();
                                return oldChatInsert.apply(oldChatInsert, arguments);
                            }";

                return js;
            }
            else if ((App.Settings.GeneralSettings.BlockedUsersList != null) &&
                    (App.Settings.GeneralSettings.BlockedUsersList.Count > 0))
            {
                // No other options were selected, we're just gonna check the block list only here

                js += @"var oldChatInsert = Chat.insert;
                            Chat.insert = function(nick, tags, message) {
                                var nick = nick || 'Chat';
                                var tags2 = tags ? Chat.parseTags(nick, tags) : {};

                                var blockUsers = ['";
                js += string.Join(",", blockList).Replace(",", "','").ToLower();
                js += @"'];
                                if (blockUsers.includes(nick.toLowerCase())) {
                                    return;
                                }
                                return oldChatInsert.apply(oldChatInsert, arguments);
                            }";

                return js;
            }

            return String.Empty;
        }

        public override string SetupCustomCSS()
        {
            string css = string.Empty;

            if (string.IsNullOrEmpty(App.Settings.GeneralSettings.CustomCSS))
            {
                // Fix for KapChat so a long chat message doesn't wrap to a new line
                css = @".message { display: inline !important; }";

                // Highlight
                Color c = App.Settings.GeneralSettings.ChatHighlightColor;
                float a = (c.A / 255f);
                string rgba = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, a);
                css += "\n .highlight { background-color: " + rgba + " !important; }";

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
            else
                css = App.Settings.GeneralSettings.CustomCSS;

            return css;
        }
    }
}
