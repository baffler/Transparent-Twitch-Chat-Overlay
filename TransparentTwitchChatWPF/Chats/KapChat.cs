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
            if (string.IsNullOrEmpty(nick))
                nick = "null";
            else
                nick = $"\"{nick}\"";

            string js = $"Chat.insert({nick}, null, \"{message}\");";

            if (!string.IsNullOrEmpty(color))
            {
                js = "var ttags = { color : \"" + color + "\", };\n";
                js += $"Chat.insert({nick}, ttags, \"\\x01ACTION {message}\\x01\");";
            }

            return js;
        }

        public override string PushNewMessage(string message = "")
        {
            return $"Chat.insert(null, null, \"{message}\");";
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
