using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Interop;
using System.Text.RegularExpressions;
using Jot;
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
            ZoomLevel = 0;

            InitializeComponent();

            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            Services.Tracker.Configure(this).IdentifyAs(rgx.Replace(this.customURL, "")).Apply();
        }

        private void Browser2_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                //if ((ZoomLevel >= -4.0) && (ZoomLevel <= 4.0))
                this.Browser2.Dispatcher.Invoke(new Action(() => { this.Browser2.ZoomLevel = ZoomLevel; }));

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

                    this.Browser2.ExecuteScriptAsync(script);
                }
            }
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

            this.Browser2.IsEnabled = true;
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

            this.Browser2.IsEnabled = false;
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
            if (!this.Browser2.IsInitialized)
            {
                MessageBox.Show(
                  "Error setting up source for custom window. The component was not initialized",
                  "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK
                );
                return;
            }

            this.Browser2.ZoomLevelIncrement = 0.25;

            if (!string.IsNullOrWhiteSpace(this.customURL))
            {
                SetCustomAddress(this.customURL);
            }
        }

        private void SetCustomAddress(string url)
        {
            Browser2.Load(url);
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
            if (this.Browser2.ZoomInCommand.CanExecute(null))
            {
                if (ZoomLevel < 4.0)
                {
                    this.Browser2.ZoomInCommand.Execute(null);
                    ZoomLevel = this.Browser2.ZoomLevel;
                }
            }
        }

        private void MenuItem_ZoomOut(object sender, RoutedEventArgs e)
        {
            if (this.Browser2.ZoomOutCommand.CanExecute(null))
            {
                if (ZoomLevel > -4.0)
                {
                    this.Browser2.ZoomOutCommand.Execute(null);
                    ZoomLevel = this.Browser2.ZoomLevel;
                }
            }
        }

        private void MenuItem_ZoomReset(object sender, RoutedEventArgs e)
        {
            if (this.Browser2.ZoomResetCommand.CanExecute(null))
            {
                this.Browser2.ZoomResetCommand.Execute(null);
                ZoomLevel = this.Browser2.ZoomLevel;
            }
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

        private void Browser2_IsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Browser2.IsBrowserInitialized)
            {
                SetupBrowser();
            }
        }
    }
}
