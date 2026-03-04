using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF.ChatProviders;

public class CustomProvider : IChatProvider
{
    public Uri GetNavigationUri()
    {
        return new Uri(App.Settings.GeneralSettings.CustomURL);
    }

    public string GetCssToInject()
    {
        if (!string.IsNullOrEmpty(App.Settings.GeneralSettings.CustomCSS))
        {
            return App.Settings.GeneralSettings.CustomCSS;
        }

        return "";
    }

    public string GetJavascriptToExecute()
    {
        return "";
    }
}
