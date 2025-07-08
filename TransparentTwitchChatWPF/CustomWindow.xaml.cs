using Jot;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using TransparentTwitchChatWPF.Utils;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using File = System.IO.File;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using Point = System.Windows.Point;

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
        //private readonly string _customUrl = "";
        //private readonly string _hashCode = "";

        private Button _closeButton;
        private WebView2 webView;
        private bool hasWebView2Runtime = false;

        // Tracked properties
        public string Url { get; set; }
        public string HashCode { get; set; }
        public string DisplayName { get; set; }
        public double ZoomLevel { get; set; }
        public byte OpacityLevel { get; set; }
        public string customCSS { get; set; }
        public string customJS { get; set; }

        public CustomWindow(MainWindow main, string URL, string CustomCSS)
        {
            this._mainWindow = main;

            Url = URL;
            ZoomLevel = 1;
            OpacityLevel = 0;
            customCSS = CustomCSS;
            customJS = "";

            InitializeComponent();

            HashCode = Hasher.Create64BitHash(URL);
            // TODO: Let user set display name
            DisplayName = HashCode;

            App.Settings.Tracker.Configure<CustomWindow>()
                .Id(w => w.HashCode, null, false)
                .Properties(cw => new
                {
                    cw.Url,
                    cw.HashCode,
                    cw.DisplayName,
                    cw.ZoomLevel,
                    cw.OpacityLevel,
                    cw.customCSS,
                    cw.customJS,
                    cw.Top, cw.Width, cw.Height, cw.Left, cw.WindowState
                })
                .PersistOn(nameof(Window.Closing))
                .StopTrackingOn(nameof(Window.Closing));
            App.Settings.Tracker.Track(this);

            // TODO: Setting this here to make sure we use the URL passed in the constructor
            // otherwise it could load a value that is invalid possibly?
            Url = URL;

            if (ZoomLevel <= 0) ZoomLevel = 1;

            InitializeWebViewAsync();
        }

        public void Persist()
        {
            App.Settings.Tracker.Persist(this);
        }

        private async void InitializeWebViewAsync()
        {
            try
            {
                string version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                if (string.IsNullOrEmpty(version))
                {
                    hasWebView2Runtime = false;
                    PlaceholderOverlay.Visibility = Visibility.Visible;
                    return;
                }
            }
            catch (Exception)
            {
                hasWebView2Runtime = false;
                PlaceholderOverlay.Visibility = Visibility.Visible;
                return;
            }

            hasWebView2Runtime = true;

            // Make sure the placeholder overlay is hidden
            PlaceholderOverlay.Visibility = Visibility.Collapsed;

            await SetupWebViewAsync();
        }

        private async Task SetupWebViewAsync()
        {
            // Create and configure.
            webView = new WebView2
            {
                DefaultBackgroundColor = System.Drawing.Color.Transparent
            };

            var options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required")
            {
                AdditionalBrowserArguments = "--disable-background-timer-throttling"
            };
            string userDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TransparentTwitchChatWPF");
            CoreWebView2Environment cwv2Environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);

            // Add to visual tree.
            Grid.SetRow(webView, 1);
            Grid.SetRowSpan(webView, 1);
            Grid.SetZIndex(webView, 0);
            this.mainWindowGrid.Children.Add(webView);

            // Initialize and subscribe to events.
            await webView.EnsureCoreWebView2Async(cwv2Environment);

            //webView.CoreWebView2InitializationCompleted += webView_CoreWebView2InitializationCompleted;
            webView.NavigationCompleted += webView_NavigationCompleted;
            webView.CoreWebView2.ProcessFailed += webView_CoreWebView2ProcessFailed;

            // Finalize setup.
            //this.jsCallbackFunctions = new JsCallbackFunctions();
            //webView.CoreWebView2.AddHostObjectToScript("jsCallbackFunctions", this.jsCallbackFunctions);

            SetupBrowser();
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
                    //webView.CoreWebView2InitializationCompleted -= webView_CoreWebView2InitializationCompleted;
                    webView.NavigationCompleted -= webView_NavigationCompleted;

                    // The CoreWebView2 might be null if it failed very early
                    if (webView.CoreWebView2 != null)
                    {
                        webView.CoreWebView2.ProcessFailed -= webView_CoreWebView2ProcessFailed;
                    }

                    // Remove the failed control from the UI and dispose it
                    this.mainWindowGrid.Children.Remove(webView);
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
                    MessageBox.Show($"Fatal error: Could not recover the WebView2 component. {ex.Message} The window will now close.", "Recovery Failed", MessageBoxButton.OK, MessageBoxImage.Stop);
                    this.Close();
                }
            });
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
            
            if (!string.IsNullOrWhiteSpace(this.Url))
            {
                NavigateToUrl(this.Url);
            }
        }

        private void NavigateToUrl(string url)
        {
            try
            {
                this.webView.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                string urlStatus = string.IsNullOrEmpty(url) ? "<Empty>" : url;
                Debug.WriteLine($"Failed to navigate to custom chat URL: {urlStatus}\n{ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Failed to navigate to the custom chat URL. Please check the URL and try again.\n\nUrl: '{urlStatus}'", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (this.Url.ToLower().Contains("webcaptioner.com"))
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
                this._mainWindow.RemoveCustomWindow(this.Url);

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

        private void MenuItem_MigrateSettings(object sender, RoutedEventArgs e)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppContext.BaseDirectory, "LegacyHasher.exe"),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            string DataFolder = (App.Settings.Tracker.Store as Jot.Storage.JsonFileStore).FolderPath;

            using (var process = Process.Start(startInfo))
            {
                // Send the single URL to the legacy hasher
                process.StandardInput.WriteLine(this.Url);
                process.StandardInput.Close(); // Signal that we're done writing

                // Read the single line of output, which is the old hash code
                string oldHashCode = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                // Now you have everything you need to perform the migration
                if (!string.IsNullOrEmpty(oldHashCode))
                {
                    // Construct the old and new filenames
                    string oldFileName = $"CustomWindow_{oldHashCode}.json";
                    string oldFileNameMigrated = $"CustomWindow_{oldHashCode}.json.migrated";
                    string newFileName = $"{this.HashCode}.json";

                    // Construct the full paths
                    string oldSettingsPath = Path.Combine(DataFolder, oldFileName);
                    string oldSettingsMigratedPath = Path.Combine(DataFolder, oldFileNameMigrated);
                    string newSettingsPath = Path.Combine(DataFolder, newFileName);

                    if (File.Exists(oldSettingsPath))
                    {
                        try
                        {
                            string json = File.ReadAllText(oldSettingsPath);

                            // Deserialize into a list of the generic properties
                            var oldProperties = JsonConvert.DeserializeObject<List<OldSettingProperty>>(json);

                            if (oldProperties != null)
                            {
                                // Helper function to find a value by name and convert it
                                T GetValue<T>(string name)
                                {
                                    var prop = oldProperties.FirstOrDefault(p => p.Name == name);
                                    // Check if the property was found and its value is not null
                                    if (prop != null && prop.Value != null)
                                    {
                                        // Use Convert for safe type conversion from object
                                        return (T)Convert.ChangeType(prop.Value, typeof(T));
                                    }
                                    return default(T); // Return the default value (0, null, etc.) if not found
                                }

                                // Manually assign each value from the parsed list
                                this.Height = GetValue<double>("Height");
                                this.Width = GetValue<double>("Width");
                                this.Top = GetValue<double>("Top");
                                this.Left = GetValue<double>("Left");
                                this.ZoomLevel = GetValue<double>("ZoomLevel");
                                this.customCSS = GetValue<string>("customCSS");
                                this.customJS = GetValue<string>("customJS");

                                // Handle the WindowState enum separately
                                var windowStateProp = oldProperties.FirstOrDefault(p => p.Name == "WindowState");
                                if (windowStateProp != null && windowStateProp.Value != null)
                                {
                                    // First, convert the object (containing an Int64) to an int.
                                    int stateValue = Convert.ToInt32(windowStateProp.Value);
                                    // Then, cast the int to the WindowState enum.
                                    this.WindowState = (System.Windows.WindowState)stateValue;
                                }

                                this.Persist(); // Save the migrated settings

                                // Rename the old file to indicate it has been migrated
                                File.Move(oldSettingsPath, oldSettingsPath + ".migrated");
                                // Show a success message to the user
                                MessageBox.Show("Settings migrated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show("Old settings file was empty or invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Show an error message if the migration fails
                            MessageBox.Show($"Failed to migrate settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else if (File.Exists(oldSettingsMigratedPath))
                    {
                        // Show a warning message if already migrated
                        MessageBox.Show($"Already migrated settings. File = '{oldFileNameMigrated}'", "Already migrated", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        // Show an error message if the old file wasn't found
                        MessageBox.Show("Could not find the old settings file to migrate.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }

    // Class for serializing/deserializing older settings for migration
    public class OldSettingProperty
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
    /*public class CustomWindowConfig
    {
        public double ZoomLevel { get; set; }
        public byte OpacityLevel { get; set; }
        public string customCSS { get; set; }
        public string customJS { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Left { get; set; }
        public WindowState WindowState { get; set; }
    }*/
}
