using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using TransparentTwitchChatWPF.Helpers;
using TransparentTwitchChatWPF.Utils;

namespace TransparentTwitchChatWPF.View.Settings;

/// <summary>
/// Interaction logic for AppearanceSettings.xaml
/// </summary>
public partial class AppearanceSettingsPage : UserControl
{
    WebView2 webView;

    public AppearanceSettingsPage()
    {
        InitializeComponent();
        _ = SetupWebViewAsync();
    }

    private async Task SetupWebViewAsync()
    {
        webView = new WebView2();
        var options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required")
        {
            AdditionalBrowserArguments = "--disable-background-timer-throttling"
        };
        string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TransparentTwitchChatWPF");
        CoreWebView2Environment cwv2Environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);

        // Add to visual tree.
        Grid.SetRow(webView, 1);
        Grid.SetRowSpan(webView, 1);
        Grid.SetZIndex(webView, 0);
        this.mainGrid.Children.Add(webView);

        await webView.EnsureCoreWebView2Async(cwv2Environment);

        webView.CoreWebView2InitializationCompleted += webView_CoreWebView2InitializationCompleted;
        webView.NavigationCompleted += webView_NavigationCompleted;
        webView.WebMessageReceived += webView_WebMessageReceived;
        webView.CoreWebView2.ProcessFailed += webView_CoreWebView2ProcessFailed;

        webView.CoreWebView2.SetVirtualHostNameToFolderMapping("nativechat.overlay",
            OverlayPathHelper.GetNativeChatPath(), CoreWebView2HostResourceAccessKind.DenyCors);

        string jsonSettings = JsonSerializer.Serialize(App.Settings.jChatSettings);
        var script = $"window.appSettings = {jsonSettings};";
        await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);

        await NavigateToUrl(new Uri("https://nativechat.overlay/index.html").AbsoluteUri);
    }

    private async Task NavigateToUrl(string url)
    {
        try
        {
            //await webView.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.clearBrowserCache", "{}");
            await ClearWebViewCache();
            webView.CoreWebView2.OpenDevToolsWindow();
            webView.CoreWebView2.Navigate(url);
        }
        catch (Exception ex)
        {
            string urlStatus = string.IsNullOrEmpty(url) ? "<Empty>" : url;
            //_logger.LogError(ex, "Failed to navigate to custom chat URL: " + urlStatus);
            MessageBox.Show($"Cannot navigate to that URL.\nError:{ex.Message}\nUrl: '{urlStatus}'", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task ClearWebViewCache()
    {
        // Ensure the CoreWebView2 has been initialized
        if (this.webView != null && this.webView.CoreWebView2 != null)
        {
            // Clear the browser cache. You can add other data types to clear if needed.
            await this.webView.CoreWebView2.Profile.ClearBrowsingDataAsync();
        }
    }

    public async Task SaveValues()
    {
        if (webView != null && webView.CoreWebView2 != null)
        {
            // Execute the JavaScript function and get the JSON string
            string jsonResult = await webView.CoreWebView2.ExecuteScriptAsync("getSettingsData()");
            string unescapedJson = JsonSerializer.Deserialize<string>(jsonResult);

            if (!string.IsNullOrEmpty(unescapedJson))
            {
                try
                {
                    App.Settings.UpdateJChatConfig(unescapedJson);
                    MessageBox.Show("Settings saved successfully!");
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Error parsing settings: {ex.Message}");
                }
            }
        }
    }

    // --- WebView2 Event Handlers ------------------------------------------------------
    private void webView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            // Handle initialization failure, e.g., show an error message
            MessageBox.Show($"WebView2 initialization failed. Error: {e.InitializationException.Message}");
            return;
        }
    }

    private void webView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            Debug.WriteLine("webView_NavigationCompleted: (Unsuccessful) " + e.WebErrorStatus.ToString());
            return;
        }

        Debug.WriteLine("Navigation completed: " + e.HttpStatusCode);

        // Set UI-specific properties
        //this.webView.Dispatcher.Invoke(() => SetZoomFactor(App.Settings.GeneralSettings.ZoomLevel));
        // Configuration for the current chat type
        //await _webViewConfigurator.ConfigureAsync(webView.CoreWebView2);
    }

    private void webView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (App.Settings.GeneralSettings.ChatType == (int)ChatTypes.NativeChat)
        {
            string json = e.WebMessageAsJson;

            if (!string.IsNullOrEmpty(json))
            {
                Debug.WriteLine("Received configuration from WebView2: " + json);
                App.Settings.UpdateJChatConfig(json);
            }
        }
    }

    private async void webView_CoreWebView2ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
    {
        // It's safest to perform UI updates and re-initialization on the UI thread.
        await Dispatcher.InvokeAsync(async () =>
        {
            MessageBox.Show("The web component crashed and will be reloaded.", "WebView2 Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // --- Clean up old instance ---
            if (webView != null)
            {
                // Unsubscribe from every event to prevent memory leaks
                webView.CoreWebView2InitializationCompleted -= webView_CoreWebView2InitializationCompleted;
                webView.NavigationCompleted -= webView_NavigationCompleted;
                webView.WebMessageReceived -= webView_WebMessageReceived;

                // The CoreWebView2 might be null if it failed very early
                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.ProcessFailed -= webView_CoreWebView2ProcessFailed;
                }

                // Remove the failed control from the UI and dispose it
                this.mainGrid.Children.Remove(webView);
                webView.Dispose();
                webView = null;
            }

            // --- Re-create using the helper function ---
            try
            {
                await SetupWebViewAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal error: Could not recover the WebView2 component. {ex.Message}", "Recovery Failed", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        });
    }
}
