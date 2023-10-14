using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace TransparentTwitchChatWPF.Chats
{
    public class KapChat : Chat
    {
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
            string[] blockList = new string[SettingsSingleton.Instance.genSettings.BlockedUsersList.Count];
            SettingsSingleton.Instance.genSettings.BlockedUsersList.CopyTo(blockList, 0);

            if (SettingsSingleton.Instance.genSettings.HighlightUsersChat)
            {
                string[] vipList = new string[SettingsSingleton.Instance.genSettings.AllowedUsersList.Count];
                SettingsSingleton.Instance.genSettings.AllowedUsersList.CopyTo(vipList, 0);

                string js = @"var oldChatInsert = Chat.insert;
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

                if (SettingsSingleton.Instance.genSettings.FilterAllowAllVIPs)
                    js += CustomJS_Defaults.VIP_Check;

                if (SettingsSingleton.Instance.genSettings.FilterAllowAllMods)
                    js += CustomJS_Defaults.Mod_Check;

                js += @"if (vips.includes(nick.toLowerCase()) || allowOther) {";

                if (SettingsSingleton.Instance.genSettings.ChatNotificationSound.ToLower() != "none")
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
            else if (SettingsSingleton.Instance.genSettings.AllowedUsersOnlyChat)
            {
                string[] vipList = new string[SettingsSingleton.Instance.genSettings.AllowedUsersList.Count];
                SettingsSingleton.Instance.genSettings.AllowedUsersList.CopyTo(vipList, 0);

                string js = @"var oldChatInsert = Chat.insert;
                            Chat.insert = function(nick, tags, message) {
                                var nick = nick || 'Chat';
                                var tags2 = tags ? Chat.parseTags(nick, tags) : {};

                                var vips = ['";
                js += string.Join(",", vipList).Replace(",", "','").ToLower();
                js += @"'];
                                var allowOther = false;";

                if (SettingsSingleton.Instance.genSettings.FilterAllowAllVIPs)
                    js += CustomJS_Defaults.VIP_Check;

                if (SettingsSingleton.Instance.genSettings.FilterAllowAllMods)
                    js += CustomJS_Defaults.Mod_Check;

                js += @"if (vips.includes(nick.toLowerCase()) || (nick == 'Chat') || allowOther) {";

                if (SettingsSingleton.Instance.genSettings.ChatNotificationSound.ToLower() != "none")
                    js += CustomJS_Defaults.Callback_PlaySound;

                js += @"
                            return oldChatInsert.apply(oldChatInsert, arguments);
                        }
                    }";

                return js;
            }
            else if (SettingsSingleton.Instance.genSettings.ChatNotificationSound.ToLower() != "none")
            {
                // Insert JS to play a sound on each chat message
                string js = @"var oldChatInsert = Chat.insert;
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
	                                await CefSharp.BindObjectAsync('jsCallback');
                                    jsCallback.playSound();
                                })();
                                return oldChatInsert.apply(oldChatInsert, arguments);
                            }";

                return js;
            }
            else if ((SettingsSingleton.Instance.genSettings.BlockedUsersList != null) &&
                    (SettingsSingleton.Instance.genSettings.BlockedUsersList.Count > 0))
            {
                // No other options were selected, we're just gonna check the block list only here

                string js = @"var oldChatInsert = Chat.insert;
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

            if (string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.CustomCSS))
            {
                // Fix for KapChat so a long chat message doesn't wrap to a new line
                css = @".message { display: inline !important; }";

                // Highlight
                Color c = SettingsSingleton.Instance.genSettings.ChatHighlightColor;
                float a = (c.A / 255f);
                string rgba = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, a);
                css += "\n .highlight { background-color: " + rgba + " !important; }";

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
            else
                css = SettingsSingleton.Instance.genSettings.CustomCSS;

            return css;
        }
    }
}
