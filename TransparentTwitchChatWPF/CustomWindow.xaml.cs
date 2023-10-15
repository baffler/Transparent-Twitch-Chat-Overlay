using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using Jot.DefaultInitializer;
using CefSharp;

namespace TransparentTwitchChatWPF
{
    /// <summary>
    /// Interaction logic for CustomWindow.xaml
    /// </summary>
    public partial class CustomWindow : Window, BrowserWindow
    {
        MainWindow mainWindow;

        bool hiddenBorders = false;
        string customURL = "";

        [Trackable]
        public double ZoomLevel { get; set; }

        public CustomWindow(MainWindow main, string Url = "")
        {
            this.mainWindow = main;
            this.customURL = Url;
            ZoomLevel = 1;

            InitializeComponent();

            //Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            //string hashCode = rgx.Replace(this.customURL, "");
            string hashCode = String.Format("{0:X}", this.customURL.GetHashCode());
            Services.Tracker.Configure(this).IdentifyAs(hashCode).Apply();

            InitializeWebViewAsync();
        }

        async void InitializeWebViewAsync()
        {
            await webView.EnsureCoreWebView2Async(null);

            //this.jsCallbackFunctions = new JsCallbackFunctions();
            //webView.CoreWebView2.AddHostObjectToScript("jsCallbackFunctions", this.jsCallbackFunctions);

            SetupBrowser();
        }

        private void headerBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (e.ClickCount == 1)
                this.DragMove();
            else if (e.ClickCount == 2)
            {
                if (this.WindowState == WindowState.Maximized)
                    this.WindowState = WindowState.Normal;
            }
        }

        private void btnHide_Click(object sender, RoutedEventArgs e)
        {
            hideBorders();
        }

        public void drawBorders()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowHelper.SetWindowExDefault(hwnd);

            btnClose.Visibility = Visibility.Visible;
            btnMin.Visibility = Visibility.Visible;
            btnMax.Visibility = Visibility.Visible;
            btnHide.Visibility = Visibility.Visible;
            //btnSettings.Visibility = System.Windows.Visibility.Visible;

            headerBorder.Background = Brushes.LightSlateGray;
            this.BorderBrush = Brushes.LightSlateGray;
            this.ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;

            hiddenBorders = false;

            this.Topmost = false;
            this.Activate();
            this.Topmost = true;

            this.webView.IsEnabled = true;
        }

        public void hideBorders()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowHelper.SetWindowExTransparent(hwnd);

            btnClose.Visibility = Visibility.Hidden;
            btnMin.Visibility = Visibility.Hidden;
            btnMax.Visibility = Visibility.Hidden;
            btnHide.Visibility = Visibility.Hidden;
            btnSettings.Visibility = Visibility.Hidden;

            headerBorder.Background = Brushes.Transparent;
            this.BorderBrush = Brushes.Transparent;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;

            hiddenBorders = true;

            this.Topmost = false;
            this.Activate();
            this.Topmost = true;

            this.webView.IsEnabled = false;
        }

        public void ToggleBorderVisibility()
        {
            if (hiddenBorders)
                drawBorders();
            else
                hideBorders();
        }

        public void ResetWindowState()
        {
            drawBorders();
            this.WindowState = WindowState.Normal;
            this.Left = 10;
            this.Top = 10;
            this.Height = 450;
            this.Width = 300;
        }

        private void CommandBinding_CanExecute_1(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            if (MessageBox.Show("This will delete the settings for this window. Are you sure?", "Remove Window", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                this.mainWindow.RemoveCustomWindow(this.customURL);
                // TODO: delete window Jot settings
                SystemCommands.CloseWindow(this);
            }
        }

        private void CommandBinding_Executed_3(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void SetupBrowser()
        {
            if (!string.IsNullOrWhiteSpace(this.customURL))
            {
                SetCustomAddress(this.customURL);
            }
        }

        private void SetCustomAddress(string url)
        {
            webView.CoreWebView2.Navigate(url);
        }

        private void ShowSettingsWindow(WindowSettings config)
        {
            /*SettingsWindow settingsWindow = new SettingsWindow(config);

            if (settingsWindow.ShowDialog() == true)
            {
                // update the AppSettings
                MessageBox.Show(config.URL);
            }*/
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            //WindowSettings cfg = new WindowSettings { isCustomURL = true, URL = this.customURL };
            //ShowSettingsWindow(cfg);
        }

        private void MenuItem_VisitWebsite(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/baffler/Transparent-Twitch-Chat-Overlay/releases");
        }

        private void MenuItem_ZoomIn(object sender, RoutedEventArgs e)
        {
            if (ZoomLevel < 4.0)
            {
                this.webView.ZoomFactor = ZoomLevel + 0.1;
                ZoomLevel = this.webView.ZoomFactor;
            }
        }

        private void MenuItem_ZoomOut(object sender, RoutedEventArgs e)
        {
            if (ZoomLevel > 0.1)
            {
                this.webView.ZoomFactor = ZoomLevel - 0.1;
                ZoomLevel = this.webView.ZoomFactor;
            }
        }

        private void MenuItem_ZoomReset(object sender, RoutedEventArgs e)
        {
            this.webView.ZoomFactor = 1;
            ZoomLevel = 1;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void btnMax_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private async void webView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            this.webView.Dispatcher.Invoke(new Action(() => { this.webView.ZoomFactor = ZoomLevel; }));

            // Insert some custom CSS for webcaptioner.com domain
            if (this.customURL.ToLower().Contains("webcaptioner.com"))
            {
                string base64CSS = Utilities.Base64Encode(CustomCSS_Defaults.WebCaptioner.Replace("\r\n", "").Replace("\t", ""));

                string href = "data:text/css;charset=utf-8;base64," + base64CSS;

                string script = "var link = document.createElement('link');";
                script += "link.setAttribute('rel', 'stylesheet');";
                script += "link.setAttribute('type', 'text/css');";
                script += "link.setAttribute('href', '" + href + "');";
                script += "document.getElementsByTagName('head')[0].appendChild(link);";

                await this.webView.ExecuteScriptAsync(script);
            }
        }

        private void MenuItem_DevToolsClick(object sender, RoutedEventArgs e)
        {
            this.webView.CoreWebView2.OpenDevToolsWindow();
        }
    }
}
