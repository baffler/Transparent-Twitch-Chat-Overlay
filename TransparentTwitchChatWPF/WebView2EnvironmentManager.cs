using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace TransparentTwitchChatWPF;

public static class WebView2EnvironmentManager
{
    private static Task<CoreWebView2Environment> _environmentTask;

    public static Task<CoreWebView2Environment> GetEnvironmentAsync()
    {
        // If the task is null, create it. This ensures the environment is only created once.
        if (_environmentTask == null)
        {
            var options = new CoreWebView2EnvironmentOptions()
            {
                AdditionalBrowserArguments = "--autoplay-policy=no-user-gesture-required --disable-background-timer-throttling --msWebView2CancelInitialNavigation"
            };

            string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TransparentTwitchChatWPF");
            _environmentTask = CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
        }

        return _environmentTask;
    }
}