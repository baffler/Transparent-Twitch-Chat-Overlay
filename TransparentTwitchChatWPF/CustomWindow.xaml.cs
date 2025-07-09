using Jot;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using TransparentTwitchChatWPF.Utils;
using TransparentTwitchChatWPF.View.Settings;
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

        private WindowDisplayMode _currentMode;
        private bool _isOverlayInteractable = false;
        //private readonly string _customUrl = "";
        //private readonly string _hashCode = "";

        Thickness noBorderThickness = new Thickness(0);
        Thickness borderThickness = new Thickness(4);

        private Button _closeButton;
        private WebView2 webView;
        private bool hasWebView2Runtime = false;
        private bool webViewProcessFailed = false;

        // Tracked properties
        public string Url { get; set; }
        public string HashCode { get; set; }
        public string DisplayName { get; set; }
        public double ZoomLevel { get; set; }
        public byte OpacityLevel { get; set; }
        public string customCSS { get; set; }
        public string customJS { get; set; }
        public bool AllowInteraction { get; set; }

        public CustomWindow(MainWindow main, string URL, string displayName, string CustomCSS, bool allowInteraction)
        {
            this._mainWindow = main;

            Url = URL;
            ZoomLevel = 1;
            OpacityLevel = 0;
            customCSS = CustomCSS;
            customJS = "";
            AllowInteraction = allowInteraction;

            InitializeComponent();

            HashCode = Hasher.Create64BitHash(URL);
            
            if (string.IsNullOrEmpty(displayName))
                DisplayName = HashCode;
            else
                DisplayName = displayName;

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
                    cw.AllowInteraction,
                    cw.Top, cw.Width, cw.Height, cw.Left, cw.WindowState
                })
                .PersistOn(nameof(Window.Closing))
                .StopTrackingOn(nameof(Window.Closing));
            App.Settings.Tracker.Track(this);

            this.Title = DisplayName;

            // TODO: Setting this here to make sure we use the URL passed in the constructor
            // otherwise it could load a value that is invalid possibly?
            Url = URL;

            if (ZoomLevel <= 0) ZoomLevel = 1;

            menuItemCheckboxAllowInteraction.IsChecked = AllowInteraction;

            InitializeWebViewAsync();
        }

        public void Persist()
        {
            App.Settings.Tracker.Persist(this);
        }

        #region Setup And Initialization
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

            CoreWebView2Environment cwv2Environment = await WebView2EnvironmentManager.GetEnvironmentAsync();

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

            webView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;

            // Finalize setup.
            //this.jsCallbackFunctions = new JsCallbackFunctions();
            //webView.CoreWebView2.AddHostObjectToScript("jsCallbackFunctions", this.jsCallbackFunctions);

            SetupBrowser();

            // This ensures the display mode is set only after the browser is fully initialized.
            SetDisplayMode(WindowDisplayMode.Setup);
        }

        private void SetupBrowser()
        {
            webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            SetBackgroundOpacity(OpacityLevel);
            //SetInteractable(AllowInteraction);

            if (!string.IsNullOrWhiteSpace(this.Url))
            {
                NavigateToUrl(this.Url);
            }
        }
        #endregion

        #region Window/UI State Management
        public async Task RefreshState()
        {
            // Immediately apply native UI changes
            SetDisplayMode(_currentMode);

            // Then, apply the web content changes
            await ApplyWebPageStyles();
        }

        public void SetDisplayMode(WindowDisplayMode mode)
        {
            _currentMode = mode;
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return; // Exit if window isn't ready

            switch (mode)
            {
                case WindowDisplayMode.Setup:
                    // STATE: Borders are visible for configuration.
                    WindowHelper.SetWindowInteractable(hwnd); // Make frame interactable
                    if (webView != null)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            webView.IsHitTestVisible = AllowInteraction;
                            webView.Focusable = AllowInteraction;
                        });
                    }

                    this.WindowStyle = WindowStyle.None;
                    this.BorderThickness = this.borderThickness;

                    this.AppTitleBar.Visibility = Visibility.Visible;
                    this.FooterBar.Visibility = Visibility.Visible;
                    this.ResizeMode = ResizeMode.CanResizeWithGrip;
                    this.webView.SetValue(Grid.RowSpanProperty, 1);
                    this.ShowInTaskbar = true;

                    SetCloseButtonVisibility(true);
                    UpdateTitle();
                    SetBackgroundOpacity(OpacityLevel);

                    break;

                case WindowDisplayMode.Overlay:
                    // STATE: Borders are hidden for overlay/in-game use.
                    WindowHelper.SetWindowClickThrough(hwnd); // Make EVERYTHING click-through
                    _isOverlayInteractable = false; // Always reset toggle when entering overlay mode
                    if (webView != null)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            webView.IsHitTestVisible = AllowInteraction;
                            webView.Focusable = AllowInteraction;
                        });
                    }

                    this.WindowStyle = WindowStyle.None;
                    this.Background = Brushes.Transparent;
                    this.BorderThickness = this.noBorderThickness;

                    this.AppTitleBar.Visibility = Visibility.Collapsed;
                    this.FooterBar.Visibility = Visibility.Collapsed;
                    this.ResizeMode = ResizeMode.NoResize;
                    this.webView.SetValue(Grid.RowSpanProperty, 2);
                    this.ShowInTaskbar = false;
                    
                    SetCloseButtonVisibility(false);
                    UpdateTitle();
                    SetBackgroundOpacity(OpacityLevel);

                    break;
            }
        }

        private void btnHide_Click(object sender, RoutedEventArgs e)
        {
            hideBorders();
        }

        public void ToggleBorderVisibility()
        {
            if (_currentMode == WindowDisplayMode.Overlay)
            {
                SetDisplayMode(WindowDisplayMode.Setup);
            }
            else
            {
                SetDisplayMode(WindowDisplayMode.Overlay);
            }
        }

        public async void drawBorders()
        {
            SetDisplayMode(WindowDisplayMode.Setup);
            await ApplyWebPageStyles();
        }

        public async void hideBorders()
        {
            SetDisplayMode(WindowDisplayMode.Overlay);
            await ApplyWebPageStyles();
        }

        public void SetInteractable(bool interactable)
        {
            // --- Guard Clauses: Do nothing if conditions aren't met ---
            if (!AllowInteraction) return; // Rule 1: setting must allow interaction.
            if (_currentMode != WindowDisplayMode.Overlay) return; // Rule 2: Only works in Overlay mode.

            // Toggle the state
            _isOverlayInteractable = interactable;

            // Apply the change
            if (webView != null)
            {
                this.Dispatcher.Invoke(() =>
                {
                    webView.IsHitTestVisible = _isOverlayInteractable;
                    webView.Focusable = _isOverlayInteractable;
                });
            }

            var hwnd = new WindowInteropHelper(this).Handle;
            if (_isOverlayInteractable)
            {
                WindowHelper.SetWindowInteractable(hwnd);
                this.AppTitleBar.Visibility = Visibility.Visible;
                UpdateTitle();
                SetBackgroundOpacity(OpacityLevel);
            }
            else
            {
                WindowHelper.SetWindowClickThrough(hwnd);
                this.AppTitleBar.Visibility = Visibility.Collapsed;
                UpdateTitle();
                SetBackgroundOpacity(OpacityLevel);
            }
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


        private void SetBackgroundOpacity(byte opacity)
        {
            // Convert the 0-255 byte to a 0.0-1.0 double.
            double remappedOpacity = opacity / 255.0;

            // If interactable, ensure the minimum opacity is 0.01.
            // Otherwise, the value is already valid.
            bool isInteractable = (AllowInteraction && _isOverlayInteractable) ||
                                  (AllowInteraction && _currentMode == WindowDisplayMode.Setup);

            double finalOpacity = isInteractable
                ? Math.Max(0.01, remappedOpacity)
                : remappedOpacity;

            Debug.WriteLine($"Setting background opacity to: {finalOpacity}");

            this.overlay.Opacity = finalOpacity;
            this.FooterBar.Opacity = finalOpacity;
        }
        #endregion

        #region Browser Events
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

            // Apply the initial overflow style after the page has loaded
            await ApplyWebPageStyles();
        }

        private void webView_CoreWebView2ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            webViewProcessFailed = true;
            this.Close();
        }
        #endregion

        #region Browser Methods
        private async Task ApplyWebPageStyles()
        {
            // Check if we are in Overlay mode and apply the correct overflow style
            bool shouldHideOverflow = (_currentMode == WindowDisplayMode.Overlay);

            // In your code, you had a method called SetBodyOverflow - we'll assume it's InjectOverflowStyle now
            await InjectOverflowStyle(shouldHideOverflow);
        }

        private async Task InjectOverflowStyle(bool inject)
        {
            // Ensure the webView is ready
            if (webView == null || webView.CoreWebView2 == null)
            {
                return;
            }

            try
            {
                string script;
                if (inject)
                {
                    // Use !important to override the site's CSS. Apply to both html and body.
                    script = @"
                        document.documentElement.style.setProperty('overflow', 'hidden', 'important');
                        document.body.style.setProperty('overflow', 'hidden', 'important');
                    ";
                }
                else
                {
                    // Cleanly remove our override and let the page's own CSS take back control.
                    script = @"
                        document.documentElement.style.removeProperty('overflow');
                        document.body.style.removeProperty('overflow');
                    ";
                }

                // Execute the script to directly set the style on the body element
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set body overflow: {ex.Message}");
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

                try
                {
                    webView.CoreWebView2.NavigateToString($"""
                        <html>
                            <head><title>Error</title></head>
                            <body style='color: #D3D3D3; background-color: #2B2B2B; font-family: sans-serif; text-align: center; padding-top: 50px;'>
                                <h1>Something went wrong</h1>
                                <p>Could not load the requested page @: '{url}'</p>
                            </body>
                        </html>
                    """);
                    SetBackgroundOpacity(255);
                }
                catch (Exception fallbackEx)
                {
                    Debug.WriteLine($"Failed to navigate to the fallback error page: {fallbackEx.Message}");
                }
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

        private void SetZoomFactor(double zoom)
        {
            if (zoom <= 0.1) zoom = 0.1;
            if (zoom > 4) zoom = 4;

            this.webView.ZoomFactor = zoom;
            ZoomLevel = zoom;
        }
        #endregion

        #region Menu Items
        private void MenuItem_CopyUrl(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(this.Url);
        }

        private void MenuItem_SetDisplayName(object sender, RoutedEventArgs e)
        {
            var inputWindow = new InputWindow("Window Display Name");
            inputWindow.Owner = this; // Set the owner to center the dialog over the main window

            // ShowDialog() returns a nullable boolean
            if (inputWindow.ShowDialog() == true)
            {
                string name = inputWindow.ResponseText.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Display name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                DisplayName = name;
                UpdateTitle();
            }
        }

        private async void MenuItemInteraction_Checked(object sender, RoutedEventArgs e)
        {
            AllowInteraction = true;
            await RefreshState();
        }

        private async void MenuItemInteraction_Unchecked(object sender, RoutedEventArgs e)
        {
            AllowInteraction = false;
            await RefreshState();
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

        private void MenuItem_DevToolsClick(object sender, RoutedEventArgs e)
        {
            this.webView.CoreWebView2.OpenDevToolsWindow();
        }

        private void MenuItem_ReloadClick(object sender, RoutedEventArgs e)
        {
            NavigateToUrl(this.Url);
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

        private void MenuItem_MigrateSettings(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("This will migrate settings from an older version. It will overwrite the current settings for this window.", "Migrate Settings", MessageBoxButton.OKCancel, MessageBoxImage.Information)
               == MessageBoxResult.OK)
            {
                MigrateSettings();
            }
        }
        #endregion

        #region Window Events
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.IsShuttingDown || webViewProcessFailed)
            {
                e.Cancel = false; // Allow the window to close normally
                return;
            }

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
        #endregion

        public void SetTopMost(bool topMost)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }

            var hwnd = new WindowInteropHelper(this).Handle;
            WindowHelper.SetWindowPosTopMost(hwnd);
        }

        #region Helpers
        private void UpdateTitle()
        {
            var isInteractable = AllowInteraction && (_currentMode == WindowDisplayMode.Setup || _isOverlayInteractable);
            if (isInteractable)
            {
                this.Title = $"{DisplayName} [Interactable]";
                this.InteractableIcon.Visibility = Visibility.Visible;
            }
            else
            {
                this.Title = DisplayName;
                this.InteractableIcon.Visibility = Visibility.Collapsed;
            }
        }
        private void MigrateSettings()
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
        #endregion

    }

    // Class for serializing/deserializing older settings for migration
    public class OldSettingProperty
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
