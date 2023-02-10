using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF.Chats
{
    public class jChat : Chat
    {
        public override string PushNewChatMessage(string message = "", string nick = "", string color = "")
        {
            if (string.IsNullOrEmpty(nick))
                nick = "null";
            else
                nick = $"\"{nick}\"";

            string js = $"Chat.write({nick}, null, \"{message}\");";

            if (!string.IsNullOrEmpty(color))
            {
                js = "var ttags = { color : \"" + color + "\", };\n";
                js += $"Chat.write({nick}, ttags, \"\\x01ACTION {message}\\x01\");";
            }

            return js;
        }

        public override string PushNewMessage(string message = "")
        {
            return $"Chat.info.lines.push(\"<div>{message}</div>\");";
        }

        public override string SetupJavascript()
        {
            PushNewMessage("jChat: Loaded...");

            string[] blockList = new string[SettingsSingleton.Instance.genSettings.BlockedUsersList.Count];
            SettingsSingleton.Instance.genSettings.BlockedUsersList.CopyTo(blockList, 0);

            if (SettingsSingleton.Instance.genSettings.HighlightUsersChat)
            {
                string[] vipList = new string[SettingsSingleton.Instance.genSettings.AllowedUsersList.Count];
                SettingsSingleton.Instance.genSettings.AllowedUsersList.CopyTo(vipList, 0);

                string js = @"var oldChatWrite = Chat.write;
                            Chat.write = function(nick, info, message) {

                                var vips = ['";
                js += string.Join(",", vipList).Replace(",", "','").ToLower();
                js += @"'];
                                var blockUsers = ['";
                js += string.Join(",", blockList).Replace(",", "','").ToLower();
                js += @"'];
                                var allowOther = false;";

                if (SettingsSingleton.Instance.genSettings.FilterAllowAllVIPs)
                    js += CustomJS_Defaults.jChat_VIP_Check;

                if (SettingsSingleton.Instance.genSettings.FilterAllowAllMods)
                    js += CustomJS_Defaults.jChat_Mod_Check;

                js += @"if (blockUsers.includes(nick.toLowerCase())) {
                            return;
                        }";

                js += @"if (vips.includes(nick.toLowerCase()) || allowOther) {";

                if (SettingsSingleton.Instance.genSettings.ChatNotificationSound.ToLower() != "none")
                    js += CustomJS_Defaults.Callback_PlaySound;

                js += @"
                                Chat.info.lines.push('<div class=""highlight"">');
                                oldChatWrite.apply(oldChatWrite, arguments);
                                Chat.info.lines.push('</div>');
                                return;
                            }
                            else
                            {
                                return oldChatWrite.apply(oldChatWrite, arguments);
                            }
                        }";

                return js;
            }
            else if (SettingsSingleton.Instance.genSettings.AllowedUsersOnlyChat)
            {
                string[] vipList = new string[SettingsSingleton.Instance.genSettings.AllowedUsersList.Count];
                SettingsSingleton.Instance.genSettings.AllowedUsersList.CopyTo(vipList, 0);

                string js = @"var oldChatWrite = Chat.write;
                            Chat.write = function(nick, info, message) {

                                var vips = ['";
                js += string.Join(",", vipList).Replace(",", "','").ToLower();
                js += @"'];
                                var allowOther = false;";

                if (SettingsSingleton.Instance.genSettings.FilterAllowAllVIPs)
                    js += CustomJS_Defaults.jChat_VIP_Check;

                if (SettingsSingleton.Instance.genSettings.FilterAllowAllMods)
                    js += CustomJS_Defaults.jChat_Mod_Check;

                js += @"if (vips.includes(nick.toLowerCase()) || allowOther) {";

                if (SettingsSingleton.Instance.genSettings.ChatNotificationSound.ToLower() != "none")
                    js += CustomJS_Defaults.Callback_PlaySound;

                js += @"
                            return oldChatWrite.apply(oldChatWrite, arguments);
                        }
                    }";

                return js;
            }
            else if (SettingsSingleton.Instance.genSettings.ChatNotificationSound.ToLower() != "none")
            {
                // Insert JS to play a sound on each chat message, and check the block list

                string js = @"var oldChatWrite = Chat.write;
                            Chat.write = function(nick, info, message) {
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
                                return oldChatWrite.apply(oldChatWrite, arguments);
                            }";

                return js;
            }
            else if ((SettingsSingleton.Instance.genSettings.BlockedUsersList != null) &&
                    (SettingsSingleton.Instance.genSettings.BlockedUsersList.Count > 0))
            {
                // No other options were selected, we're just gonna check the block list only here

                string js = @"var oldChatWrite = Chat.write;
                            Chat.write = function(nick, info, message) {
                                var blockUsers = ['";
                js += string.Join(",", blockList).Replace(",", "','").ToLower();
                js += @"'];
                                if (blockUsers.includes(nick.toLowerCase())) {
                                    return;
                                }
                                return oldChatWrite.apply(oldChatWrite, arguments);
                            }";

                return js;
            }

            return String.Empty;
        }

        public override string SetupCustomCSS()
        {
            string css = string.Empty;

            if (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.CustomCSS))
                css = SettingsSingleton.Instance.genSettings.CustomCSS;
            else
                css = @".highlight { background-color: rgba(255,255,0,0.5) !important; }";

            return css;
        }
    }
}
