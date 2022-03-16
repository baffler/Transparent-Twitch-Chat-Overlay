using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF.Chats
{
    public class CustomURLChat : Chat
    {
        public override string SetupJavascript()
        {
            return String.Empty;
        }

        public override string SetupCustomCSS()
        {
            string css = string.Empty;

            if (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.CustomCSS))
                css = SettingsSingleton.Instance.genSettings.CustomCSS;

            return css;
        }
    }
}
