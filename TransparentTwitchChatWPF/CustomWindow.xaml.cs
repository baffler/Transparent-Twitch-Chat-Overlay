using Jot;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Controls;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using TransparentTwitchChatWPF.Utils;

namespace TransparentTwitchChatWPF
{
    /// <summary>
    /// Interaction logic for CustomWindow.xaml
    /// </summary>
    public partial class CustomWindow : Window, BrowserWindow
    {

        private readonly MainWindow _mainWindow;
        private readonly Thickness _noBorderThickness = new Thickness(0);
        private readonly Thickness _borderThickness = new Thickness(4);
        private readonly Color _borderColor = (Color)ColorConverter.ConvertFromString("#5D077F");

        private bool _hiddenBorders = false;
        private readonly string _customUrl = "";
        private readonly string _hashCode = "";

        private Button _closeButton;

        // Tracked properties
        public double ZoomLevel { get; set; }
        public byte OpacityLevel { get; set; }
        public string customCSS { get; set; }
        public string customJS { get; set; }

        public CustomWindow(MainWindow main, string Url, string CustomCSS)
        {
            this._mainWindow = main;
            this._customUrl = Url;
            ZoomLevel = 1;
            OpacityLevel = 0;
            customCSS = CustomCSS;
            customJS = "";

            InitializeComponent();

            _hashCode = Hasher.Create64BitHash(this._customUrl);
            App.Settings.Tracker.Configure<CustomWindow>()
                .Id(w => w._hashCode, null, false)
                .Properties(cw => new
                {
                    cw.ZoomLevel,
                    cw.OpacityLevel,
                    cw.customCSS,
                    cw.customJS,
                    cw.Top, cw.Width, cw.Height, cw.Left, cw.WindowState
                })
                .PersistOn(nameof(Window.Closing))
                .StopTrackingOn(nameof(Window.Closing));
            App.Settings.Tracker.Track(this);

            if (ZoomLevel <= 0) ZoomLevel = 1;

            _ = InitializeWebViewAsync();
        }

        public void Persist()
        {
            App.Settings.Tracker.Persist(this);
        }

        private async Task InitializeWebViewAsync()
        {
            var options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");
            options.AdditionalBrowserArguments = "--disable-background-timer-throttling";
            string userDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TransparentTwitchChatWPF");

            CoreWebView2Environment cwv2Environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
            await webView.EnsureCoreWebView2Async(cwv2Environment);

            //this.jsCallbackFunctions = new JsCallbackFunctions();
            //webView.CoreWebView2.AddHostObjectToScript("jsCallbackFunctions", this.jsCallbackFunctions);

            SetupBrowser();
        }

        private void btnHide_Click(object sender, RoutedEventArgs e)
        {
            hideBorders();
        }

        public void drawBorders()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowHelper.SetWindowExDefault(hwnd);

            // TODO: check if setting for interactable is enabled?
            this.overlay.Opacity = 0.01;

            SetCloseButtonVisibility(true);

            this.AppTitleBar.Visibility = Visibility.Visible;
            this.FooterBar.Visibility = Visibility.Visible;
            this.webView.SetValue(Grid.RowSpanProperty, 1);
            this.BorderBrush = new SolidColorBrush(_borderColor);
            this.BorderThickness = this._borderThickness;
            this.ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;

            _hiddenBorders = false;

            this.Topmost = false;
            this.Activate();
            this.Topmost = true;

            this.webView.Focusable = true;
        }

        public void hideBorders()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowHelper.SetWindowExTransparent(hwnd);

            this.overlay.Opacity = 0;

            SetCloseButtonVisibility(false);

            this.AppTitleBar.Visibility = Visibility.Collapsed;
            this.FooterBar.Visibility = Visibility.Collapsed;
            this.webView.SetValue(Grid.RowSpanProperty, 2);
            this.BorderBrush = Brushes.Transparent;
            this.BorderThickness = this._noBorderThickness;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;

            _hiddenBorders = true;

            this.Topmost = false;
            this.Activate();
            this.Topmost = true;

            this.webView.Focusable = false;
        }

        public void ToggleBorderVisibility()
        {
            if (_hiddenBorders)
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

        private void SetCloseButtonVisibility(bool isVisible)
        {
            if (_closeButton != null)
            {
                if (isVisible)
                    _closeButton.Visibility = Visibility.Visible;
                else
                    _closeButton.Visibility = Visibility.Collapsed;
            }
        }

        private void SetupBrowser()
        {
            webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            SetBackgroundOpacity(OpacityLevel);
            
            if (!string.IsNullOrWhiteSpace(this._customUrl))
            {
                SetCustomAddress(this._customUrl);
            }
        }

        private void SetCustomAddress(string url)
        {
            try
            {
                webView.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\nThis likely means the URL is invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void SetBackgroundOpacity(int opacity)
        {
            double Remap(int inputValue)
            {
                double outputValue = (inputValue - 0) * (1.0 - 0.0) / (255 - 0) + 0.0;
                return outputValue;
            }

            double remapped = Remap(opacity);
            if (remapped <= 0)
            {
                //if (_interactable)  remapped = 0.01;
                //else
                    remapped = 0;
            }
            else if (remapped >= 1) remapped = 1;
            this.overlay.Opacity = remapped;
            this.FooterBar.Opacity = remapped;
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            //WindowSettings cfg = new WindowSettings { isCustomURL = true, URL = this.customURL };
            //ShowSettingsWindow(cfg);

            Point screenPos = btnSettings.PointToScreen(new Point(0, btnSettings.ActualHeight));
            settingsBtnContextMenu.IsOpen = false;
            //settingsBtnContextMenu.PlacementTarget = this.btnSettings;
            settingsBtnContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Absolute;
            settingsBtnContextMenu.HorizontalOffset = screenPos.X;
            settingsBtnContextMenu.VerticalOffset = screenPos.Y;
            settingsBtnContextMenu.IsOpen = true;
        }
        
        private void MenuItem_IncOpacity(object sender, RoutedEventArgs e)
        {
            OpacityLevel = (byte)Math.Clamp(OpacityLevel + 15, 0, 255);
            SetBackgroundOpacity(OpacityLevel);
        }

        private void MenuItem_DecOpacity(object sender, RoutedEventArgs e)
        {
            OpacityLevel = (byte)Math.Clamp(OpacityLevel - 15, 0, 255);
            SetBackgroundOpacity(OpacityLevel);
        }

        private void MenuItem_ResetOpacity(object sender, RoutedEventArgs e)
        {
            OpacityLevel = 0;
            SetBackgroundOpacity(OpacityLevel);
        }

        private void MenuItem_VisitWebsite(object sender, RoutedEventArgs e)
        {
            ShellHelper.OpenUrl("https://github.com/baffler/Transparent-Twitch-Chat-Overlay/releases");
            e.Handled = true;
        }

        private void MenuItem_ZoomIn(object sender, RoutedEventArgs e)
        {
            if (ZoomLevel < 4.0)
            {
                SetZoomFactor(ZoomLevel + 0.1);
                ZoomLevel = this.webView.ZoomFactor;
            }
        }

        private void MenuItem_ZoomOut(object sender, RoutedEventArgs e)
        {
            if (ZoomLevel > 0.1)
            {
                SetZoomFactor(ZoomLevel - 0.1);
                ZoomLevel = this.webView.ZoomFactor;
            }
        }

        private void MenuItem_ZoomReset(object sender, RoutedEventArgs e)
        {
            SetZoomFactor(1);
            ZoomLevel = 1;
        }

        private void SetZoomFactor(double zoom)
        {
            if (zoom <= 0.1) zoom = 0.1;
            if (zoom > 4) zoom = 4;

            this.webView.ZoomFactor = zoom;
            ZoomLevel = zoom;
        }

        private async void webView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            this.webView.Dispatcher.Invoke(new Action(() => { SetZoomFactor(ZoomLevel); }));

            // Insert some custom CSS for webcaptioner.com domain
            if (this._customUrl.ToLower().Contains("webcaptioner.com"))
            {
                await this.webView.ExecuteScriptAsync(InsertCustomCSS(CustomCSS_Defaults.WebCaptioner));
            }

            if (!string.IsNullOrEmpty(this.customCSS))
            {
                await this.webView.ExecuteScriptAsync(InsertCustomCSS(this.customCSS));
            }
        }

        private string InsertCustomCSS(string CSS)
        {
            string uriEncodedCSS = Uri.EscapeDataString(CSS);
            string script = "const ttcCSS = document.createElement('style');";
            script += "ttcCSS.innerHTML = decodeURIComponent(\"" + uriEncodedCSS + "\");";
            script += "document.querySelector('head').appendChild(ttcCSS);";
            return script;
        }

        private void MenuItem_DevToolsClick(object sender, RoutedEventArgs e)
        {
            this.webView.CoreWebView2.OpenDevToolsWindow();
        }

        private void MenuItem_EditCSSClick(object sender, RoutedEventArgs e)
        {
            TextEditorWindow textEditorWindow = new TextEditorWindow(TextEditorType.CSS, this.customCSS);
            textEditorWindow.TextEdited += TextEditorWindow_TextEdited;
            textEditorWindow.Show();
        }

        private void TextEditorWindow_TextEdited(object sender, TextEditedEventArgs e)
        {
            this.customCSS = e.EditedText;
            this.Persist(); // Save the changes
            this.webView.Reload();
        }

        private void MenuItemExitApp_Click(object sender, RoutedEventArgs e)
        {
            App.IsShuttingDown = true;
            Application.Current.Shutdown();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.IsShuttingDown) return;

            if (MessageBox.Show("This will delete the settings for this window. Are you sure?", "Remove Window", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                this._mainWindow.RemoveCustomWindow(this._customUrl);

                // TODO: Remove the tracking configuration for this window
                /*string path = (Services.Tracker.StoreFactory as Jot.Storage.JsonFileStoreFactory).StoreFolderPath;
                string jsonFile = Path.Combine(path, "CustomWindow_" + this.hashCode + ".json");
                Debug.WriteLine(jsonFile);

                //trackingConfig.AutoPersistEnabled = false;

                if (File.Exists(jsonFile))
                {
                    try
                    {
                        File.Delete(jsonFile);
                    }
                    catch { }
                }*/

                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var thisWindow = sender as Window;

                if (thisWindow != null)
                {
                    var titleBarControl = thisWindow.FindChildByType<DependencyObject>("ModernWpf.Controls.Primitives.TitleBarControl");
                    if (titleBarControl != null)
                    {
                        _closeButton = titleBarControl.FindChild<Button>("CloseButton");
                    }
                }
            }
            catch {}
        }

        public void SetTopMost(bool topMost)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }

            var hwnd = new WindowInteropHelper(this).Handle;
            WindowHelper.SetWindowPosTopMost(hwnd);
        }
    }
}
