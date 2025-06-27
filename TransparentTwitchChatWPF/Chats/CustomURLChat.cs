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
                if (!string.IsNullOrEmpty(App.Settings.GeneralSettings.TwitchPopoutCSS))
                    css = App.Settings.GeneralSettings.TwitchPopoutCSS;
            }
            else
            {
                if (!string.IsNullOrEmpty(App.Settings.GeneralSettings.CustomCSS))
                    css = App.Settings.GeneralSettings.CustomCSS;
            }

            return css;
        }
    }
}
