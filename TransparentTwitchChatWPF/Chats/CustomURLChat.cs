using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF.Chats
{
    public class CustomURLChat : Chat
    {
        public CustomURLChat(ChatTypes chatType) : base(chatType)
        {
        }

        public override string SetupJavascript()
        {
            return String.Empty;
        }

        public override string SetupCustomCSS()
        {
            string css = string.Empty;

            if (this.ChatType == ChatTypes.TwitchPopout)
            {
                if (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.TwitchPopoutCSS))
                    css = SettingsSingleton.Instance.genSettings.TwitchPopoutCSS;
            }
            else
            {
                if (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.CustomCSS))
                    css = SettingsSingleton.Instance.genSettings.CustomCSS;
            }

            return css;
        }
    }
}
