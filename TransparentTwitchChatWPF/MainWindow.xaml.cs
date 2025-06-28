using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TransparentTwitchChatWPF.Helpers;
using Velopack;
using Velopack.Sources;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;

/*
 * v1.1.1
 * - Fixed widget settings not being saved
 * - Added BTTV, FFZ, and 7TV emotes to KapChat
 * - Fixed alert sounds not showing up in the settings
 * - Widget inclusion in Bring to top Hotkey
 * 
 * v1.1.0
 * - Updated to .net8.0 and updated many dependencies
 * - Get velopack working to replace Squirrel
 * - Fix twitch integration
 * 
 * v1.0.5
 * - Replaced jChat with jCyan
 * 
 * v1.0.4
 * - Remove that little x
 * - Channel point redemptions working
 * - Background opacity fixes
 * 
 * v1.0.3
 * - Fix for KapChat not working
 * 
 * v1.0.0
 * - Added Squirrel for installing and updating the app
 * > Option to disable update checks from the message
 * 
 * v0.10.0
 * > Lock down app if no runtime installed
 * >> Display a panel with link or auto-download and install
 * > Allow interaction fix (when off)
 * > Redo the startup image (for first time launch)
 * > BTTV and FFZ get unset when switching chats
 * > KapChat customCSS gets unset when switching chats
 * > Popout CSS Editors
 * > Custom Javascript
 * > Global hotkey to force app to be topmost
 * > Update Zoom level values to match with webView2
 * > Deal with popup windows
 * > Reset settings button
 * > Tips and hints in the chat window, turn off in settings
 * 
 * v0.9.5
 * - The uninstaller will now remove stored settings/data
 * - Audio for browser enabled by default
 * - Audio device selection (for sound clips)
 * - Volume control for sounds clips
 * - Can now change the folder for sound clips
 * - Added an entry in the context menu to open Dev Tools
 * - CefSharp (Chromium) updated to 117.2.20
 * 
 * v0.9.4
 * - Filtering: can block users now
 * - Automatically checks for updates on start (Can be disabled in settings)
 * 
 * v0.9.3
 * - Added a setting to allow multiple instances
 * - Fix for crash when adding a widget with a long URL
 * - Updated CEFsharp (Chromium) and Newtsonsoft.Json to latest versions
 * ~ Known Issues:
 * ~ Jump lists no longer work if you allow multiple instances (it just launches another instance)
 * ~ Can't login to the popout Twitch, this is caused by Twitch blocking login for unsupported browsers
 * 
 * v0.9.2
 * - jChat support (this allows BetterTTV, FrankerFaceZ and 7TV emotes)
 * - CefSharp (Chromium) updated to 99.2.9
 * - Twitch popout with BTTV and FFZ emotes support - PR by: github.com/r-o-b-o-t-o
 * - Twitch popout will keep your login session - PR by: github.com/r-o-b-o-t-o
 * 
 * v0.9.1
 * - TwitchLib support for points redemption
 * - Filter settings will let you highlight certain usernames/mods/vip
 * - Possible bug fix for startup crash Load() issue.
 * - System tray icon will always be enabled for now (to prevent no interaction with app)
 * 
 * v0.9.0
 * - Chat filter settings for KapChat version
 * - Filter by allowed usernames, all mods, or all VIPs
 * - You can configure the filter settings under Chat settings and click the Open Chat Filter Settings button
 *
 * 
 * TODO:
 * > Custom CSS for widgets
 * > Memory leak with settings window?
 * > Link to sample CSS for custom CSS for the different chat types
 * Add opacity and zoom levels into the General settings
 * Save and load different css files
 * Fade out the chat when there's not been a message for awhile (useful if opacity is set high)
 * Cascade the windows when creating a new one
 * Cascade the windows when resetting their positions
 * Easier/better size grip, kinda hard to see it, or click on it right now
 * Hotkey and/or menu item to hide the chat and make it visible again
 * Allowing you to chat from the app, quickly (hotkey to enable it?)
 * Custom javascript
 *
 * 
 */

namespace TransparentTwitchChatWPF;

using Chats;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using ModernWpf.Controls;
using NAudio.Wave;
using NHotkey;
using NHotkey.Wpf;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Threading;
using TransparentTwitchChatWPF.Twitch;
using TransparentTwitchChatWPF.Utils;
using Velopack;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, BrowserWindow
{
    private readonly ILogger<MainWindow> _logger;
    private readonly TwitchService _twitchService;

    private WebView2 webView;
    private bool hasWebView2Runtime = false;

    private DispatcherTimer _timerCheckForegroundFocus;
    private DispatcherTimer _timerCheckWebView2Install;
    private int _timerTick = 0;

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    Thickness noBorderThickness = new Thickness(0);
    Thickness borderThickness = new Thickness(4);
    int cOpacity = 0;

    private bool _hiddenBorders = false;
    private bool _interactable = true;

    JsCallbackFunctions jsCallbackFunctions;
    List<BrowserWindow> windows = new List<BrowserWindow>();
    private Chat _currentChat;
    private Button _closeButton;

    public MainWindow(ILogger<MainWindow> logger, TwitchService twitchService)
    {
        InitializeComponent();
        DataContext = this;

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _twitchService = twitchService ?? throw new ArgumentNullException(nameof(twitchService));
        _twitchService.ChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;

        _currentChat = new CustomURLChat(ChatTypes.CustomURL); // TODO: initializing here needed?

        Growl.GrowlMessageRequested += HandleGrowlMessage;

        App.Settings.Tracker.Configure<MainWindow>()
            .Id(w => w.GetType().Name + "_State", null, false)
            .Properties(w => new { w.Top, w.Width, w.Height, w.Left, w.WindowState })
            .PersistOn(nameof(Window.Closing))
            .StopTrackingOn(nameof(Window.Closing));
        App.Settings.Tracker.Track(this);

        _timerTick = 0;
        _timerCheckForegroundFocus = new DispatcherTimer();
        _timerCheckForegroundFocus.Interval = TimeSpan.FromSeconds(1);
        _timerCheckForegroundFocus.Tick += _timer_Tick;
        _timerCheckForegroundFocus.Start();

        SetupOrReplaceHotkeys();
        SettingsWindow.SettingsWindowActive += OnSettingsWindowActive;

        LocalHtmlHelper.EnsureLocalBrowserFiles();
        InitializeWebViewAsync();
    }

    private void OnChannelPointsRewardRedeemed(object sender, TwitchLib.EventSub.Websockets.Core.EventArgs.Channel.ChannelPointsCustomRewardRedemptionArgs e)
    {
        _logger.LogInformation("Channel Points Reward Redeemed");

        var payloadEvent = e.Notification.Payload.Event;

        _logger.LogInformation($"{payloadEvent.UserName} redeemed '{payloadEvent.Reward.Title}' ({payloadEvent.Reward.Cost} points)");
        PushNewChatMessageDispatcherInvoke(
            $"redeemed '{payloadEvent.Reward.Title}' ({payloadEvent.Reward.Cost} points)",
            payloadEvent.UserName, "#a1b3c4");

        if (!string.IsNullOrEmpty(payloadEvent.UserInput))
        {
            PushNewChatMessageDispatcherInvoke(
                $"{payloadEvent.UserInput}", 
                payloadEvent.UserName, "#a1b3c4");
        }
    }

    private void OnSettingsWindowActive(bool isActive)
    {
        HotkeyManager.Current.IsEnabled = !isActive;
    }

    private void SetupOrReplaceHotkeys()
    {
        HotkeyManager.Current.Remove("ToggleBorders");
        HotkeyManager.Current.Remove("ToggleInteraction");
        HotkeyManager.Current.Remove("BringToTopTimer");

        menuItemToggleBorders.Header = "Toggle Borders";
        menuItemToggleInteractable.Header = "Toggle Interactable";
        menuItemBringToTop.Header = "Bring to Top";

        if (App.Settings.GeneralSettings.ToggleBordersHotkey != null)
        {
            if (App.Settings.GeneralSettings.ToggleBordersHotkey.Key != Key.None)
            {
                try
                {
                    HotkeyManager.Current.AddOrReplace(
                        "ToggleBorders",
                        App.Settings.GeneralSettings.ToggleBordersHotkey.Key,
                        App.Settings.GeneralSettings.ToggleBordersHotkey.Modifiers,
                        OnHotKeyToggleBorders);

                    menuItemToggleBorders.Header = $"Toggle Borders ({App.Settings.GeneralSettings.ToggleBordersHotkey.ToString()})";
                }
                catch (Exception)
                { }
            }
        }

        if (App.Settings.GeneralSettings.ToggleInteractableHotkey != null)
        {
            if (App.Settings.GeneralSettings.ToggleInteractableHotkey.Key != Key.None)
            {
                try
                {
                    HotkeyManager.Current.AddOrReplace(
                        "ToggleInteraction",
                        App.Settings.GeneralSettings.ToggleInteractableHotkey.Key,
                        App.Settings.GeneralSettings.ToggleInteractableHotkey.Modifiers,
                        OnHotKeyToggleInteraction);

                    menuItemToggleInteractable.Header = $"Toggle Interactable ({App.Settings.GeneralSettings.ToggleInteractableHotkey.ToString()})";
                }
                catch (Exception)
                { }
            }
        }

        if (App.Settings.GeneralSettings.BringToTopHotkey != null)
        {
            if (App.Settings.GeneralSettings.BringToTopHotkey.Key != Key.None)
            {
                try
                {
                    HotkeyManager.Current.AddOrReplace(
                        "BringToTopTimer",
                        App.Settings.GeneralSettings.BringToTopHotkey.Key,
                        App.Settings.GeneralSettings.BringToTopHotkey.Modifiers,
                        OnHotKeyBringToTopTimer);
                    menuItemBringToTop.Header = $"Bring to Top ({App.Settings.GeneralSettings.BringToTopHotkey.ToString()})";
                }
                catch (Exception)
                { }
            }
        }
    }

    private void OnHotKeyToggleInteraction(object sender, HotkeyEventArgs e)
    {
        SetInteractable(!this.webView.Focusable);
        e.Handled = true;
    }

    private void OnHotKeyBringToTopTimer(object sender, HotkeyEventArgs e)
    {
        StartCheckForegroundWindowTimer();
        e.Handled = true;
    }

    private void OnHotKeyToggleBorders(object sender, HotkeyEventArgs e)
    {
        ToggleBorderVisibility();
        e.Handled = true;
    }


    private void CheckWebView2Timer_Tick(object sender, EventArgs e)
    {
        string version = "";
        try
        {
            version = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch
        {
            return;
        }

        if (string.IsNullOrEmpty(version))
            return;

        InitializeWebViewAsync();
    }

    private void ShowWebViewDownloadLink()
    {
        hasWebView2Runtime = false;

        if (_timerCheckWebView2Install == null)
        {
            _timerCheckWebView2Install = new System.Windows.Threading.DispatcherTimer();
            _timerCheckWebView2Install.Interval = TimeSpan.FromSeconds(2.5);
            _timerCheckWebView2Install.Tick += CheckWebView2Timer_Tick;
            _timerCheckWebView2Install.Start();
        }

        this.overlay.Opacity = 1;
        TextBlock textBlock = new TextBlock
        {
            Text = "Please download and install the WebView2 Runtime to use this app.\nThe app will refresh after install.\n\n",
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            Background = Brushes.Black,
            Padding = new Thickness(20),
            Margin = new Thickness(20),
        };

        Hyperlink link = new Hyperlink
        {
            NavigateUri = new Uri("https://go.microsoft.com/fwlink/p/?LinkId=2124703"),
            Foreground = Brushes.White,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
        };
        link.Inlines.Add("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
        link.RequestNavigate += Hyperlink_RequestNavigate;

        textBlock.Inlines.Add(link);
        overlay.Child = textBlock;
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
        e.Handled = true;
    }

    async void InitializeWebViewAsync()
    {
        string version = "";

        try
        {
            version = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch (Exception ex)
        {
            ShowWebViewDownloadLink();
            return;
        }

        if (string.IsNullOrEmpty(version))
        {
            ShowWebViewDownloadLink();
            return;
        }

        // setup webview
        webView = new WebView2
        {
            DefaultBackgroundColor = System.Drawing.Color.Transparent
        };

        hasWebView2Runtime = true;
        if (_timerCheckWebView2Install != null)
        {
            _timerCheckWebView2Install.Stop();
            _timerCheckWebView2Install.Tick -= CheckWebView2Timer_Tick;
            _timerCheckWebView2Install = null;
        }

        this.overlay.Child = null;

        webView.CoreWebView2InitializationCompleted += webView_CoreWebView2InitializationCompleted;
        webView.ContentLoading += webView_ContentLoading;
        webView.NavigationStarting += webView_NavigationStarting;
        webView.NavigationCompleted += webView_NavigationCompleted;
        webView.WebMessageReceived += webView_WebMessageReceived;

        Grid.SetRow(webView, 1);
        Grid.SetRowSpan(webView, 1);
        Grid.SetZIndex(webView, 0);

        this.mainWindowGrid.Children.Add(webView);

        var options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");
        options.AdditionalBrowserArguments = "--disable-background-timer-throttling";
        string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TransparentTwitchChatWPF");
        CoreWebView2Environment cwv2Environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
        await webView.EnsureCoreWebView2Async(cwv2Environment);

        this.jsCallbackFunctions = new JsCallbackFunctions();
        webView.CoreWebView2.AddHostObjectToScript("jsCallbackFunctions", this.jsCallbackFunctions);

        SetupBrowser();
    }

    private void webView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            MessageBox.Show(GetWindow(this), "Failed to initialize WebView2:\n" + e.InitializationException.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void _timer_Tick(object sender, EventArgs e)
    {
        CheckForegroundWindow();

        _timerTick += 1;
        if (_timerTick >= 3)
        {
            _timerCheckForegroundFocus.Stop();
        }
    }

    private void StartCheckForegroundWindowTimer()
    {
        _timerCheckForegroundFocus.Stop();
        _timerTick = 0;
        _timerCheckForegroundFocus.Start();
    }

    private void CheckForegroundWindow()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        var hwnd = new WindowInteropHelper(this).Handle;

        foreach (BrowserWindow win in this.windows)
            win.SetTopMost(true);

        if (foregroundWindow != hwnd)
            WindowHelper.SetWindowPosTopMost(hwnd);
    }

    public void ProcessCommandLineArgs(string[] args)
    {
        // Check if the command is our special "show window" command
        if (args.Length > 0 && args[0] == IpcManager.ShowWindowCommand)
        {
            CheckForegroundWindow();
            return;
        }

        foreach (var arg in args)
        {
            switch (arg.ToLower())
            {
                case "/toggleborders":
                    ToggleBorderVisibility();
                    break;
                case "/settings":
                    ShowSettingsWindow();
                    break;
                case "/resetwindow":
                    ResetWindowPosition();

                    if (MessageBox.Show("Show settings folder?", "Settings Folder", MessageBoxButton.YesNo, MessageBoxImage.Question)
                        == MessageBoxResult.Yes)
                    {
                        OpenSettingsFolder();
                    }
                    break;
            }
        }
    }

    public void SetInteractable(bool interactable)
    {
        this.Dispatcher.Invoke(() =>
        {
            this.webView.Focusable = interactable;
        });

        _interactable = interactable;
        var hwnd = new WindowInteropHelper(this).Handle;

        if (interactable)
        {
            WindowHelper.SetWindowExDefault(hwnd);
            this.AppTitleBar.Visibility = Visibility.Visible;

            this.Topmost = false;
            this.Activate();
            this.Topmost = true;

            if (this.cOpacity <= 0)
                this.overlay.Opacity = 0.01;
        }
        else
        {
            WindowHelper.SetWindowExTransparent(hwnd);
            this.AppTitleBar.Visibility = Visibility.Collapsed;

            if (this.cOpacity <= 0)
                this.overlay.Opacity = 0;
        }

        CheckForegroundWindow();
        StartCheckForegroundWindowTimer();
    }

    public void drawBorders()
    {
        this.ShowInTaskbar = true;
        SetInteractable(App.Settings.GeneralSettings.AllowInteraction);

        var hwnd = new WindowInteropHelper(this).Handle;
        WindowHelper.SetWindowExDefault(hwnd);

        // show minimize, maximize, and close buttons
        //btnHide.Visibility = Visibility.Visible;
        //btnSettings.Visibility = Visibility.Visible;
        SetCloseButtonVisibility(true);

        this.AppTitleBar.Visibility = Visibility.Visible;
        this.FooterBar.Visibility = Visibility.Visible;
        this.webView.SetValue(Grid.RowSpanProperty, 1);
        this.BorderBrush = Brushes.Black;
        this.BorderThickness = this.borderThickness;
        this.ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;

        _hiddenBorders = false;

        this.Topmost = false;
        this.Activate();
        this.Topmost = true;

        CheckForegroundWindow();
    }

    public void hideBorders()
    {
        if (App.Settings.GeneralSettings.HideTaskbarIcon)
            this.ShowInTaskbar = false;

        // Prevent interaction with the browser
        SetInteractable(false);

        // hide minimize, maximize, and close buttons
        //btnHide.Visibility = Visibility.Hidden;
        //btnSettings.Visibility = Visibility.Hidden;
        SetCloseButtonVisibility(false);

        this.AppTitleBar.Visibility = Visibility.Collapsed;
        this.FooterBar.Visibility = Visibility.Collapsed;
        this.webView.SetValue(Grid.RowSpanProperty, 2);
        this.BorderBrush = Brushes.Transparent;
        this.BorderThickness = this.noBorderThickness;
        this.ResizeMode = System.Windows.ResizeMode.NoResize;

        this.WindowStyle = WindowStyle.None;
        this.Background = Brushes.Transparent;

        _hiddenBorders = true;

        this.Topmost = false;
        this.Activate();
        this.Topmost = true;

        CheckForegroundWindow();
    }

    public void ToggleBorderVisibility()
    {
        if (!hasWebView2Runtime) return;

        if (_hiddenBorders)
            DrawBordersForAllWindows();
        else
            HideBordersForAllWindows();
    }

    public void DrawBordersForAllWindows()
    {
        drawBorders();
        foreach (BrowserWindow win in this.windows)
            win.drawBorders();
    }

    public void HideBordersForAllWindows()
    {
        hideBorders();
        foreach (BrowserWindow win in this.windows)
            win.hideBorders();
    }

    public void ResetWindowPosition()
    {
        this.ResetWindowState();
        foreach (BrowserWindow win in this.windows)
            win.ResetWindowState();
    }

    public void ResetWindowState()
    {
        drawBorders();
        this.WindowState = WindowState.Normal;
        this.Left = 10;
        this.Top = 10;
        this.Height = 500;
        this.Width = 320;
    }

    private void SetCustomChatAddress(string url)
    {
        this.webView.CoreWebView2.Navigate(url);
    }

    private void SetChatAddress(string chatChannel)
    {
        string username = chatChannel;
        if (chatChannel.Contains("/"))
            username = chatChannel.Split('/').Last();

        string fade = App.Settings.GeneralSettings.FadeTime;
        if (!App.Settings.GeneralSettings.FadeChat) { fade = "false"; }

        string theme = string.Empty;
        if ((App.Settings.GeneralSettings.ThemeIndex >= 0) && (App.Settings.GeneralSettings.ThemeIndex < KapChat.Themes.Count))
            theme = KapChat.Themes[App.Settings.GeneralSettings.ThemeIndex];

        string url = @"https://nightdev.com/hosted/obschat/?";
        url += @"theme=" + theme;
        url += @"&channel=" + username;
        url += @"&fade=" + fade;
        url += @"&bot_activity=" + (!App.Settings.GeneralSettings.BlockBotActivity).ToString();
        url += @"&prevent_clipping=false";
        this.webView.CoreWebView2.Navigate(url);
    }

    public void ShowInputFadeDialogBox()
    {
        Input_Fade inputDialog = new Input_Fade();
        if (inputDialog.ShowDialog() == true)
        {
            string fadeTime = inputDialog.Channel;
            int fadeTimeInt = 0;

            int.TryParse(fadeTime, out fadeTimeInt);

            App.Settings.GeneralSettings.FadeChat = (fadeTimeInt > 0);
            App.Settings.GeneralSettings.FadeTime = fadeTime;
        }
    }

    public void ExitApplication()
    {
        App.IsShuttingDown = true;
        Application.Current.Shutdown();

        // Removing the 'are you sure' for now
        /*if (App.Settings.GeneralSettings.ConfirmClose)
        {
            var msgBoxResult = MessageBox.Show("Sure you want to exit the application?", "Exit",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Question,
                                                MessageBoxResult.No,
                                                MessageBoxOptions.DefaultDesktopOnly);
            if (msgBoxResult == MessageBoxResult.Yes)
            {
                System.Windows.Application.Current.Shutdown();
            }
        }
        else
        {
            System.Windows.Application.Current.Shutdown();
        }*/
        //SystemCommands.CloseWindow(this);
    }

    private void MenuItem_ToggleBorderVisible(object sender, RoutedEventArgs e)
    {
        if (!hasWebView2Runtime) return;

        foreach (BrowserWindow window in this.windows)
        {
            if (window != null)
            {
                if (this._hiddenBorders)
                    window.drawBorders();
                else
                    window.hideBorders();
            }
        }
        ToggleBorderVisibility();
    }

    private void MenuItem_ShowSettings(object sender, RoutedEventArgs e)
    {
        if (!hasWebView2Runtime) return;
        ShowSettingsWindow();
    }

    private void MenuItem_VisitWebsite(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/baffler/Transparent-Twitch-Chat-Overlay/releases/latest",
            UseShellExecute = true
        });
    }

    private void btnHide_Click(object sender, RoutedEventArgs e)
    {
        if (!hasWebView2Runtime) return;
        hideBorders();
    }

    private void MenuItem_ZoomIn(object sender, RoutedEventArgs e)
    {
        if (!hasWebView2Runtime) return;

        if (App.Settings.GeneralSettings.ZoomLevel < 4.0)
        {
            SetZoomFactor(App.Settings.GeneralSettings.ZoomLevel + 0.1);
            App.Settings.GeneralSettings.ZoomLevel = this.webView.ZoomFactor;
        }
    }

    private void MenuItem_ZoomOut(object sender, RoutedEventArgs e)
    {
        if (!hasWebView2Runtime) return;

        if (App.Settings.GeneralSettings.ZoomLevel > 0.1)
        {
            SetZoomFactor(App.Settings.GeneralSettings.ZoomLevel - 0.1);
            App.Settings.GeneralSettings.ZoomLevel = this.webView.ZoomFactor;
        }
    }

    private void MenuItem_ZoomReset(object sender, RoutedEventArgs e)
    {
        if (!hasWebView2Runtime) return;
        SetZoomFactor(1);
        App.Settings.GeneralSettings.ZoomLevel = 1;
    }

    private void SetZoomFactor(double zoom)
    {
        if (zoom <= 0.1) zoom = 0.1;
        if (zoom > 4) zoom = 4;

        this.webView.ZoomFactor = zoom;
        App.Settings.GeneralSettings.ZoomLevel = zoom;
    }

    private void webView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
    {
        Console.WriteLine("webView_NavigationStarting");
    }

    private async void webView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            Console.WriteLine("webView_NavigationCompleted: (Unsuccessful) " + e.WebErrorStatus.ToString());
            return;
        }

        Console.WriteLine("Navigation completed: " + e.HttpStatusCode);

        this.webView.Dispatcher.Invoke(new Action(() =>
        {
            SetZoomFactor(App.Settings.GeneralSettings.ZoomLevel);
        }));

        if (App.Settings.GeneralSettings.ChatType == (int)ChatTypes.TwitchPopout)
            TwitchPopoutSetup();

        _logger.LogInformation("_currentChat.ChatType = " + _currentChat.ChatType.ToString());
        if (_currentChat.ChatType == ChatTypes.KapChat)
        {
            _logger.LogInformation("Setting up KapChat emotes and scripts.");
            string browserPath = Path.Combine(AppContext.BaseDirectory, "browser");
            if (Directory.Exists(browserPath))
            {
                string emoteBundlePath = Path.Combine(browserPath, "emote-bundle.js");
                if (File.Exists(emoteBundlePath))
                {
                    _logger.LogInformation("Loading emote bundle script from: " + emoteBundlePath);
                    string emoteBundleScript = File.ReadAllText(emoteBundlePath);
                    //_logger.LogInformation(emoteBundleScript);

                    await this.webView.ExecuteScriptAsync(emoteBundleScript);

                    //await this.webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(emoteBundleScript);
                }
                else
                {
                    _logger.LogWarning("Emote bundle script not found at: " + emoteBundlePath);
                }
            }
            else
            {
                _logger.LogWarning("Browser path does not exist: " + browserPath);
            }
        }

        string js = this._currentChat.SetupJavascript();
        if (!string.IsNullOrEmpty(js))
            await this.webView.ExecuteScriptAsync(js);
        //await this.webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(js);

        // Custom CSS
        string script = string.Empty;
        string css = this._currentChat.SetupCustomCSS();

        if (!string.IsNullOrEmpty(css))
            script = InsertCustomCSS2(css);

        if (!string.IsNullOrEmpty(script))
            await this.webView.ExecuteScriptAsync(script);

        if (this._currentChat != null)
            this.PushNewMessage(this._currentChat.ChatType.ToString() + " Loaded.");

        // Pub Sub
        if (App.Settings.GeneralSettings.RedemptionsEnabled)
        {
            _ = _twitchService.InitializeAsync();
        }
    }

    private void TwitchPopoutSetup()
    {
        if (App.Settings.GeneralSettings.BetterTtv)
        {
            InsertCustomJavaScriptFromUrl("https://cdn.betterttv.net/betterttv.js");
        }
        if (App.Settings.GeneralSettings.FrankerFaceZ)
        {
            // Observe for FrankerFaceZ's reskin stylesheet
            // that breaks the transparency and remove it
            InsertCustomJavaScript(@"
(function() {
    const head = document.getElementsByTagName(""head"")[0];
    const observer = new MutationObserver((mutations, observer) => {
        for (const mut of mutations) {
            if (mut.type === ""childList"") {
                for (const node of mut.addedNodes) {
                    if (node.tagName.toLowerCase() === ""link"" && node.href.includes(""color_normalizer"")) {
                        node.remove();
                    }
                }
            }
        }
    });
    observer.observe(head, {
        attributes: false,
        childList: true,
        subtree: false,
    });
})();
                        ");

            InsertCustomJavaScriptFromUrl("https://cdn.frankerfacez.com/static/script.min.js");
        }
    }

    private void webView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (App.Settings.GeneralSettings.ChatType == (int)ChatTypes.TwitchPopout)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                Debug.WriteLine(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        if (App.Settings.GeneralSettings.ChatType == (int)ChatTypes.jCyan)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                PushNewMessage(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }

    //private void InsertCustomCSS_old(string CSS)
    //{
    //    string base64CSS = Utilities.Base64Encode(CSS.Replace("\r\n", "").Replace("\t", ""));

    //    string href = "data:text/css;charset=utf-8;base64," + base64CSS;

    //    string script = "var link = document.createElement('link');";
    //    script += "link.setAttribute('rel', 'stylesheet');";
    //    script += "link.setAttribute('type', 'text/css');";
    //    script += "link.setAttribute('href', '" + href + "');";
    //    script += "document.getElementsByTagName('head')[0].appendChild(link);";

    //    this.Browser1.ExecuteScriptAsync(script);
    //}

    private string InsertCustomCSS2(string CSS)
    {
        string uriEncodedCSS = Uri.EscapeDataString(CSS);
        string script = "const ttcCSS = document.createElement('style');";
        script += "ttcCSS.innerHTML = decodeURIComponent(\"" + uriEncodedCSS + "\");";
        script += "document.querySelector('head').appendChild(ttcCSS);";
        return script;
    }

    private string InsertCustomJS2(string js)
    {
        string uriEncodedJS = Uri.EscapeDataString(js);
        string script = "const ttcJS = document.createElement('script');";
        script += "ttcJS.innerHTML = decodeURIComponent(\"" + uriEncodedJS + "\");";
        script += "document.querySelector('head').appendChild(ttcJS);";
        return script;
    }

    private async void InsertCustomJavaScript(string JS)
    {
        try
        {
            await this.webView.ExecuteScriptAsync(JS);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InsertCustomJavaScriptFromUrl(string scriptUrl)
    {
        InsertCustomJavaScript(@"
(function() {
    const script = document.createElement(""script"");
    script.src = """ + scriptUrl + @""";
    document.getElementsByTagName(""head"")[0].appendChild(script);
})();
            ");
    }

    public void OpenSettingsFolder()
    {
        string folderPath = (App.Settings.Tracker.Store as Jot.Storage.JsonFileStore).FolderPath;
        try
        {
            if (Directory.Exists(folderPath))
                Process.Start("explorer.exe", folderPath);
            else
                MessageBox.Show($"'{folderPath}' does not exist!");
        }
        catch (Win32Exception winEx)
        {
            // This specifically catches "Access is denied" and other OS-level errors.
            MessageBox.Show($"Windows could not open the folder. Please check your permissions for this location.\n\nPath: {folderPath}\n\nError: {winEx.Message}",
                            "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            // Catch any other unexpected errors
            MessageBox.Show($"An unexpected error occurred while trying to open the folder.\n\nError: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void CreateNewWindow(string URL, string CustomCSS)
    {
        if (App.Settings.GeneralSettings.CustomWindows.Contains(URL))
        {
            MessageBox.Show("That URL already exists as a window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        else
        {
            App.Settings.GeneralSettings.CustomWindows.Add(URL);
            OpenNewCustomWindow(URL, CustomCSS);
        }
    }

    private void CreateNewWindowDialog()
    {
        if (!hasWebView2Runtime) return;
        Input_Custom inputDialog = new Input_Custom();
        if (inputDialog.ShowDialog() == true)
        {
            CreateNewWindow(inputDialog.Url, inputDialog.CustomCSS);
        }
    }

    private void MenuItem_ClickNewWindow(object sender, RoutedEventArgs e)
    {
        if (!hasWebView2Runtime) return;
        CreateNewWindowDialog();
    }

    private string GetSoundClipsFolder()
    {
        string path = App.Settings.GeneralSettings.SoundClipsFolder;

        string baseDirectory = AppContext.BaseDirectory;
        string defaultAssetsPath = Path.Combine(baseDirectory, "assets");

        if (path == "Default" || !Directory.Exists(path))
        {
            path = defaultAssetsPath;
        }

        if (!Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create directory '{path}': {ex.Message}");
                // Handle the error appropriately, maybe fall back to a known good path or show a message
            }
        }

        Debug.WriteLine($"Sound Clips Folder: '{path}'");

        return path;
    }

    private void ShowSettingsWindow()
    {
        if (!hasWebView2Runtime)
        {
            if (
            MessageBox.Show(
                            "Please download and install the WebView2 Runtime to use this app.\nRestart this app after you install.",
                            "WebView2 Runtime Required",
                            MessageBoxButton.OK, MessageBoxImage.Error
                            )
                == MessageBoxResult.OK)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
                    UseShellExecute = true
                });
            }

            return;
        }

        WindowSettings config = new WindowSettings
        {
            Title = "Main Window",
            ChatType = App.Settings.GeneralSettings.ChatType,
            URL = App.Settings.GeneralSettings.CustomURL,
            jChatURL = App.Settings.GeneralSettings.jChatURL,
            Username = App.Settings.GeneralSettings.Username,
            ChatFade = App.Settings.GeneralSettings.FadeChat,
            FadeTime = App.Settings.GeneralSettings.FadeTime,
            //ShowBotActivity = App.Settings.GeneralSettings.ShowBotActivity,
            ChatNotificationSound = App.Settings.GeneralSettings.ChatNotificationSound,
            Theme = App.Settings.GeneralSettings.ThemeIndex,
            CustomCSS = App.Settings.GeneralSettings.CustomCSS,
            AutoHideBorders = App.Settings.GeneralSettings.AutoHideBorders,
            ConfirmClose = App.Settings.GeneralSettings.ConfirmClose,
            EnableTrayIcon = App.Settings.GeneralSettings.EnableTrayIcon,
            HideTaskbarIcon = App.Settings.GeneralSettings.HideTaskbarIcon,
            AllowInteraction = App.Settings.GeneralSettings.AllowInteraction,
            RedemptionsEnabled = App.Settings.GeneralSettings.RedemptionsEnabled,
            ChannelID = App.Settings.GeneralSettings.ChannelID,
            BetterTtv = App.Settings.GeneralSettings.BetterTtv,
            FrankerFaceZ = App.Settings.GeneralSettings.FrankerFaceZ,
        };

        SettingsWindow settingsWindow = new SettingsWindow(this, config);

        if (settingsWindow.ShowDialog() == true)
        {
            App.Settings.GeneralSettings.ChatType = config.ChatType;

            if (config.ChatType == (int)ChatTypes.CustomURL)
            {
                this._currentChat = new CustomURLChat(ChatTypes.CustomURL);
                App.Settings.GeneralSettings.CustomURL = config.URL;
                App.Settings.GeneralSettings.CustomCSS = config.CustomCSS;

                if (!string.IsNullOrEmpty(App.Settings.GeneralSettings.CustomURL))
                    SetCustomChatAddress(App.Settings.GeneralSettings.CustomURL);
            }
            else if (config.ChatType == (int)ChatTypes.TwitchPopout)
            {
                this._currentChat = new CustomURLChat(ChatTypes.TwitchPopout);
                App.Settings.GeneralSettings.Username = config.Username;

                if (!string.IsNullOrEmpty(App.Settings.GeneralSettings.Username))
                    SetCustomChatAddress("https://www.twitch.tv/popout/" + App.Settings.GeneralSettings.Username + "/chat?popout=");

                App.Settings.GeneralSettings.BetterTtv = config.BetterTtv;
                App.Settings.GeneralSettings.FrankerFaceZ = config.FrankerFaceZ;
            }
            else if (config.ChatType == (int)ChatTypes.KapChat)
            {
                this._currentChat = new Chats.KapChat();
                App.Settings.GeneralSettings.Username = config.Username;
                App.Settings.GeneralSettings.FadeChat = config.ChatFade;
                App.Settings.GeneralSettings.FadeTime = config.FadeTime;
                //App.Settings.GeneralSettings.ShowBotActivity = config.ShowBotActivity;
                App.Settings.GeneralSettings.ChatNotificationSound = config.ChatNotificationSound;
                App.Settings.GeneralSettings.ThemeIndex = config.Theme;

                if (App.Settings.GeneralSettings.ThemeIndex == 0)
                    App.Settings.GeneralSettings.CustomCSS = config.CustomCSS;
                else
                    App.Settings.GeneralSettings.CustomCSS = string.Empty;


                if (!string.IsNullOrEmpty(App.Settings.GeneralSettings.Username))
                {
                    if (!config.RedemptionsEnabled)
                        _twitchService.DisableEventSub();

                    SetChatAddress(App.Settings.GeneralSettings.Username);
                }


                if (App.Settings.GeneralSettings.ChatNotificationSound.ToLower() == "none")
                    this.jsCallbackFunctions.MediaFile = string.Empty;
                else
                {
                    string file = Path.Combine(GetSoundClipsFolder(), App.Settings.GeneralSettings.ChatNotificationSound);
                    if (System.IO.File.Exists(file))
                    {
                        this.jsCallbackFunctions.MediaFile = file;
                    }
                    else
                    {
                        this.jsCallbackFunctions.MediaFile = string.Empty;
                    }
                }
            }

            else if (config.ChatType == (int)ChatTypes.jCyan)
            {
                this._currentChat = new jCyan();
                App.Settings.GeneralSettings.jChatURL = config.jChatURL;
                App.Settings.GeneralSettings.ChatNotificationSound = config.ChatNotificationSound;

                if (!string.IsNullOrEmpty(App.Settings.GeneralSettings.Username))
                {
                    if (!config.RedemptionsEnabled)
                        _twitchService.DisableEventSub();

                    if (string.IsNullOrEmpty(App.Settings.GeneralSettings.jChatURL))
                    {
                        string localIndex = LocalHtmlHelper.GetIndexHtmlPath();
                        webView.CoreWebView2.Navigate(new Uri(localIndex).AbsoluteUri);
                    }
                    else
                        SetCustomChatAddress(App.Settings.GeneralSettings.jChatURL);
                }
                else
                {
                    if (string.IsNullOrEmpty(App.Settings.GeneralSettings.jChatURL))
                    {
                        string localIndex = LocalHtmlHelper.GetIndexHtmlPath();
                        webView.CoreWebView2.Navigate(new Uri(localIndex).AbsoluteUri);
                    }
                    else
                        SetCustomChatAddress(App.Settings.GeneralSettings.jChatURL);
                }


                if (App.Settings.GeneralSettings.ChatNotificationSound.ToLower() == "none")
                    this.jsCallbackFunctions.MediaFile = string.Empty;
                else
                {
                    string file = Path.Combine(GetSoundClipsFolder(), App.Settings.GeneralSettings.ChatNotificationSound);
                    if (System.IO.File.Exists(file))
                    {
                        this.jsCallbackFunctions.MediaFile = file;
                    }
                    else
                    {
                        this.jsCallbackFunctions.MediaFile = string.Empty;
                    }
                }
            }

            App.Settings.GeneralSettings.AutoHideBorders = config.AutoHideBorders;
            App.Settings.GeneralSettings.ConfirmClose = config.ConfirmClose;
            App.Settings.GeneralSettings.EnableTrayIcon = config.EnableTrayIcon;
            App.Settings.GeneralSettings.HideTaskbarIcon = config.HideTaskbarIcon;
            App.Settings.GeneralSettings.AllowInteraction = config.AllowInteraction;
            App.Settings.GeneralSettings.RedemptionsEnabled = config.RedemptionsEnabled;

            this.taskbarControl.Visibility = Visibility.Visible; //config.EnableTrayIcon ? Visibility.Visible : Visibility.Hidden;
            this.ShowInTaskbar = !config.HideTaskbarIcon;

            if (!this._hiddenBorders)
            {
                this.webView.Focusable = true;
                if (config.AllowInteraction)
                    SetInteractable(true);
            }

            SetupOrReplaceHotkeys();

            // Save the new changes for the General Settings
            App.Settings.Persist();
        }
    }

    private void OpenNewCustomWindow(string url, string CustomCSS, bool hideBorder = false)
    {
        CustomWindow newWindow = new CustomWindow(this, url, CustomCSS);
        windows.Add(newWindow);
        newWindow.Show();

        if (hideBorder)
            newWindow.hideBorders();
    }

    public void RemoveCustomWindow(string url)
    {
        if (App.Settings.GeneralSettings.CustomWindows.Contains(url))
        {
            App.Settings.GeneralSettings.CustomWindows.Remove(url);
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        Window window = (Window)sender;
        window.Topmost = true;
    }

    private void Window_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        Window window = (Window)sender;
        window.Topmost = true;
    }

    private void btnSettings_Click(object sender, RoutedEventArgs e)
    {
        Point screenPos = btnSettings.PointToScreen(new Point(0, btnSettings.ActualHeight));
        settingsBtnContextMenu.IsOpen = false;
        //settingsBtnContextMenu.PlacementTarget = this.btnSettings;
        settingsBtnContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Absolute;
        settingsBtnContextMenu.HorizontalOffset = screenPos.X;
        settingsBtnContextMenu.VerticalOffset = screenPos.Y;
        settingsBtnContextMenu.IsOpen = true;
    }

    private void btnSettings_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        ShowSettingsWindow();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (App.Settings.GeneralSettings.VersionTracker <= 0.95)
        {
            App.Settings.GeneralSettings.ZoomLevel = 1;
            App.Settings.GeneralSettings.VersionTracker = 0.96;
        }

        this.taskbarControl.Visibility = Visibility.Visible;

        try
        {
            var mainWindow = Application.Current.MainWindow;

            if (mainWindow != null)
            {
                var titleBarControl = mainWindow.FindChildByType<DependencyObject>("ModernWpf.Controls.Primitives.TitleBarControl");
                if (titleBarControl != null)
                {
                    _closeButton = titleBarControl.FindChild<Button>("CloseButton");
                }
            }
        }
        catch //(Exception ex)
        {
            //MessageBox.Show($"Failed to hide close button: {ex.Message}");
        }

        if (App.Settings.GeneralSettings.CheckForUpdates
            && (DateTime.Now.Date > App.Settings.GeneralSettings.LastUpdateCheck.Date))
        {
            _ = CheckForUpdateAsync();
        }
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

    private async Task CheckForUpdateAsync()
    {
#if !DEBUG
        _logger.LogInformation("Checking for updates...");

        var mgr = new UpdateManager(new GithubSource("https://github.com/baffler/Transparent-Twitch-Chat-Overlay", null, true));

        try
        {
            var newVersion = await mgr.CheckForUpdatesAsync();

            App.Settings.GeneralSettings.LastUpdateCheck = DateTime.Now;
            App.Settings.Persist(); // Save the last update check time

            if (newVersion == null)
            {
                _logger.LogInformation("No updates available.");
                return; // no update available
            }

            if (MessageBox.Show($"New Version [v{newVersion.TargetFullRelease.Version}] is available.\n(Currently on [v{newVersion.BaseRelease.Version}])\n\nWould you like to update now?",
                        "New Version Available",
                        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
            {
                _logger.LogInformation($"Downloading and applying update to version {newVersion.TargetFullRelease.Version}...");
                await mgr.DownloadUpdatesAsync(newVersion);
                mgr.ApplyUpdatesAndRestart(newVersion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            MessageBox.Show("Error checking for updates:\n" + ex.Message, "Error while Checking for Update", MessageBoxButton.OK, MessageBoxImage.Error);
        }
#endif
    }

    private void webView_ContentLoading(object sender, Microsoft.Web.WebView2.Core.CoreWebView2ContentLoadingEventArgs e)
    {

    }

    private void SetupBrowser()
    {
        if (App.Settings.GeneralSettings.ZoomLevel <= 0)
            App.Settings.GeneralSettings.ZoomLevel = 1;

        webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
        //this.bgColor = new SolidColorBrush(Color.FromArgb(App.Settings.GeneralSettings.OpacityLevel, 0, 0, 0));
        this.cOpacity = App.Settings.GeneralSettings.OpacityLevel;
        SetBackgroundOpacity(this.cOpacity);

        if (App.Settings.GeneralSettings.AutoHideBorders)
            hideBorders();
        else
            drawBorders();

        if (App.Settings.GeneralSettings.ChatNotificationSound.ToLower() != "none")
        {
            string file = Path.Combine(GetSoundClipsFolder(), App.Settings.GeneralSettings.ChatNotificationSound);
            if (System.IO.File.Exists(file))
            {
                this.jsCallbackFunctions.MediaFile = file;
            }
            else
            {
                this.jsCallbackFunctions.MediaFile = string.Empty;
            }
        }

        if (App.Settings.GeneralSettings.CustomWindows != null)
        {
            foreach (string url in App.Settings.GeneralSettings.CustomWindows)
                OpenNewCustomWindow(url, "", App.Settings.GeneralSettings.AutoHideBorders);
        }

        if (App.Settings.GeneralSettings.jChatURL.ToLower().Contains("giambaj.it"))
        {
            App.Settings.GeneralSettings.jChatURL = string.Empty;
        }

        if ((App.Settings.GeneralSettings.ChatType == (int)ChatTypes.CustomURL) && (!string.IsNullOrWhiteSpace(App.Settings.GeneralSettings.CustomURL)))
        {
            this._currentChat = new CustomURLChat(ChatTypes.CustomURL);
            SetCustomChatAddress(App.Settings.GeneralSettings.CustomURL);
        }
        else if ((App.Settings.GeneralSettings.ChatType == (int)ChatTypes.TwitchPopout) && (!string.IsNullOrEmpty(App.Settings.GeneralSettings.Username)))
        {
            this._currentChat = new CustomURLChat(ChatTypes.TwitchPopout);
            SetCustomChatAddress("https://www.twitch.tv/popout/" + App.Settings.GeneralSettings.Username + "/chat?popout=");
        }
        else if (!string.IsNullOrWhiteSpace(App.Settings.GeneralSettings.Username))
        { // TODO: need to clean this up to determine which type of chat to load better
            if (App.Settings.GeneralSettings.ChatType == (int)ChatTypes.KapChat)
            {
                this._currentChat = new Chats.KapChat();
                SetChatAddress(App.Settings.GeneralSettings.Username);
            }
            else if (App.Settings.GeneralSettings.ChatType == (int)ChatTypes.jCyan)
            {
                if (string.IsNullOrEmpty(App.Settings.GeneralSettings.jChatURL))
                {
                    string localIndex = LocalHtmlHelper.GetJChatIndexPath();
                    webView.CoreWebView2.Navigate(new Uri(localIndex).AbsoluteUri);
                }
                else
                {
                    this._currentChat = new jCyan();
                    SetCustomChatAddress(App.Settings.GeneralSettings.jChatURL);
                }
            }
            else
            {
                string localIndex = LocalHtmlHelper.GetIndexHtmlPath();
                webView.CoreWebView2.Navigate(new Uri(localIndex).AbsoluteUri);
            }
        }
        else if (App.Settings.GeneralSettings.ChatType == (int)ChatTypes.jCyan)
        { // TODO: need to clean this up to determine which type of chat to load better
            if (string.IsNullOrEmpty(App.Settings.GeneralSettings.jChatURL))
            {
                string localIndex = LocalHtmlHelper.GetJChatIndexPath();
                webView.CoreWebView2.Navigate(new Uri(localIndex).AbsoluteUri);
            }
            else
            {
                this._currentChat = new jCyan();
                SetCustomChatAddress(App.Settings.GeneralSettings.jChatURL);
            }
        }
        else
        {
            string localIndex = LocalHtmlHelper.GetIndexHtmlPath();
            webView.CoreWebView2.Navigate(new Uri(localIndex).AbsoluteUri);

            //CefSharp.WebBrowserExtensions.LoadHtml(Browser1,
            //"<html><body style=\"font-size: x-large; color: white; text-shadow: -1px -1px 0 #000, 1px -1px 0 #000, -1px 1px 0 #000, 1px 1px 0 #000; \">Load a channel to connect to by right-clicking the tray icon.<br /><br />You can move and resize the window, then press the [o] button to hide borders, or use the tray icon menu.</body></html>");
        }
    }

    private void MenuItem_SettingsClick(object sender, RoutedEventArgs e)
    {
        ShowSettingsWindow();
    }

    private void MenuItem_Exit(object sender, RoutedEventArgs e)
    {
        ExitApplication();
    }

    private void MenuItem_ResetWindowClick(object sender, RoutedEventArgs e)
    {
        ResetWindowPosition();

        if (MessageBox.Show("Show settings folder?", "Settings Folder", MessageBoxButton.YesNo, MessageBoxImage.Question)
            == MessageBoxResult.Yes)
        {
            OpenSettingsFolder();
        }
    }

    private void SetBackgroundOpacity(int Opacity)
    {
        double Remap(int inputValue)
        {
            double outputValue = (inputValue - 0) * (1.0 - 0.0) / (255 - 0) + 0.0;
            return outputValue;
        }

        double remapped = Remap(Opacity);
        if (remapped <= 0)
        {
            if (_interactable)
                remapped = 0.01;
            else
                remapped = 0;
        }
        else if (remapped >= 1) remapped = 1;
        this.overlay.Opacity = remapped;
        this.FooterBar.Opacity = remapped;
    }

    private void MenuItem_IncOpacity(object sender, RoutedEventArgs e)
    {
        if (!hasWebView2Runtime) return;

        this.cOpacity += 15;
        if (this.cOpacity > 255) this.cOpacity = 255;
        App.Settings.GeneralSettings.OpacityLevel = (byte)this.cOpacity;
        //this.bgColor.Color = Color.FromArgb((byte)this.cOpacity, 0, 0, 0);
        SetBackgroundOpacity(this.cOpacity);
    }

    private void MenuItem_DecOpacity(object sender, RoutedEventArgs e)
    {
        if (!hasWebView2Runtime) return;

        this.cOpacity -= 15;
        if (this.cOpacity < 0) this.cOpacity = 0;
        App.Settings.GeneralSettings.OpacityLevel = (byte)this.cOpacity;
        //this.bgColor.Color = Color.FromArgb((byte)this.cOpacity, 0, 0, 0);
        SetBackgroundOpacity(this.cOpacity);
    }

    private void MenuItem_ResetOpacity(object sender, RoutedEventArgs e)
    {
        if (!hasWebView2Runtime) return;

        this.cOpacity = 0;
        App.Settings.GeneralSettings.OpacityLevel = 0;
        //this.bgColor.Color = Color.FromArgb(0, 0, 0, 0);
        SetBackgroundOpacity(this.cOpacity);
    }

    private void PushNewMessage(string message = "")
    {
        string js = this._currentChat.PushNewMessage(message);

        if (!string.IsNullOrEmpty(js))
        {
            this.webView.ExecuteScriptAsync(js);
        }
    }

    private void PushNewChatMessage(string message = "", string nick = "", string color = "")
    {
        //this.Browser1.Dispatcher.Invoke(new Action(() => { });

        string js = this._currentChat.PushNewChatMessage(message, nick, color);
        if (!string.IsNullOrEmpty(js))
            this.webView.ExecuteScriptAsync(js);
    }

    private void PushNewMessageDispatcherInvoke(string message = "")
    {
        string js = this._currentChat.PushNewMessage(message);

        if (!string.IsNullOrEmpty(js))
        {
            this.webView.Dispatcher.Invoke(() =>
            {
                this.webView.ExecuteScriptAsync(js);
            });
        }
    }

    private void PushNewChatMessageDispatcherInvoke(string message = "", string nick = "", string color = "")
    {
        string js = this._currentChat.PushNewChatMessage(message, nick, color);
        if (!string.IsNullOrEmpty(js))
        {
            this.webView.Dispatcher.Invoke(() =>
            {
                this.webView.ExecuteScriptAsync(js);
            });
        }
    }

    /*private void EventSubConnectedInit()
    {
        try
        {
            _isPubSubConnected = true;
            if (!string.IsNullOrEmpty(App.Settings.GeneralSettings.OAuthToken))
            {
                PushNewMessage("PubSub Service Connected");
                Debug.WriteLine("PubSub Service: SendTopics()");
                _pubSub.SendTopics(App.Settings.GeneralSettings.OAuthToken);
            }
            else
            {
                PushNewMessage("PubSub Service Connected, but no OAuth Token found. Try reconnecting your Twitch account in the settings.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PubSubConnectedInit() Error: {ex.Message}");
        }
    }

    private void _pubSub_OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
    {
        PushNewMessageDispatcherInvoke($"PubSub Service Error: {e.Exception.Message}");
    }

    private void _pubSub_OnPubSubServiceClosed(object sender, EventArgs e)
    {
        _isPubSubConnected = false;
        PushNewMessageDispatcherInvoke("PubSub Service Closed");
    }

    private void _pubSub_OnListenResponse(object sender, OnListenResponseArgs e)
    {
        if (!e.Successful)
        {
            PushNewMessageDispatcherInvoke($"Failed to listen! Response: {e.Response.Error}");
        }
        else
            PushNewMessageDispatcherInvoke($"Success! Listening to topic: {e.Topic}");
    }

    private void _pubSub_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
    {
        if (App.Settings.GeneralSettings.RedemptionsEnabled)
        {
            var redeem = e.RewardRedeemed.Redemption;

            PushNewChatMessageDispatcherInvoke(
                $"redeemed '{redeem.Reward.Title}' ({redeem.Reward.Cost} points)", // ~ {e.ChannelId}",
                redeem.User.DisplayName, "#a1b3c4");

            if (!string.IsNullOrEmpty(redeem.UserInput) && !string.IsNullOrWhiteSpace(redeem.UserInput))
            {
                PushNewChatMessageDispatcherInvoke($"\"{redeem.UserInput}\"", redeem.User.DisplayName, "#a1b3c4");
            }
    }*/

    private void MenuItem_DevToolsClick(object sender, RoutedEventArgs e)
    {
        if (!hasWebView2Runtime) return;

        this.webView.CoreWebView2.OpenDevToolsWindow();
    }

    private void MenuItem_BringToTopTimer(object sender, RoutedEventArgs e)
    {
        StartCheckForegroundWindowTimer();
    }

    private void MenuItem_ToggleInteractable(object sender, RoutedEventArgs e)
    {
        if (!hasWebView2Runtime) return;
        SetInteractable(!this.webView.Focusable);
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        Growl.GrowlMessageRequested -= HandleGrowlMessage;

        if (!App.IsShuttingDown)
            ExitApplication();
    }

    private void HandleGrowlMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (!hasWebView2Runtime) return;
        if (_currentChat == null) return;
        if (this.webView == null) return;

        string js = _currentChat.PushNewMessage(message);

        if (!string.IsNullOrEmpty(js))
        {
            this.webView.Dispatcher.Invoke(() =>
            {
                this.webView.ExecuteScriptAsync(js);
            });
        }
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

[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
public class JsCallbackFunctions
{
    private string _mediaFile;
    private AudioFileReader _audioFileReader;
    private WaveOutEvent _waveOutDevice;

    public string MediaFile
    {
        get { return _mediaFile; }
        set
        {
            _mediaFile = value;
            InitAudio();
        }
    }

    public JsCallbackFunctions()
    {
        this._mediaFile = "";
    }

    private void VerifyOutputDevice()
    {
        var deviceId = App.Settings.GeneralSettings.DeviceID;

        if (deviceId < 0)
        {
            App.Settings.GeneralSettings.DeviceName = "Default";
            return;
        }

        if (deviceId >= WaveOut.DeviceCount)
        {
            App.Settings.GeneralSettings.DeviceID = -1;
            App.Settings.GeneralSettings.DeviceName = "Default";
            return;
        }

        var capabilities = WaveOut.GetCapabilities(deviceId);
        if (!App.Settings.GeneralSettings.DeviceName.StartsWith(capabilities.ProductName))
        {
            App.Settings.GeneralSettings.DeviceID = -1;
            App.Settings.GeneralSettings.DeviceName = "Default";
        }
    }

    private void InitAudio()
    {
        if (_waveOutDevice != null)
        {
            _waveOutDevice.Stop();
            _waveOutDevice = null;
        }

        if (_audioFileReader != null)
        {
            _audioFileReader.Dispose();
            _audioFileReader = null;
        }

        if (string.IsNullOrEmpty(_mediaFile) || string.Equals(_mediaFile.ToLower(), "none")) return;

        _audioFileReader = new AudioFileReader(_mediaFile);
        _audioFileReader.Volume = App.Settings.GeneralSettings.OutputVolume;
        _waveOutDevice = new WaveOutEvent();

        VerifyOutputDevice();
        if (App.Settings.GeneralSettings.DeviceID >= 0)
            _waveOutDevice.DeviceNumber = App.Settings.GeneralSettings.DeviceID;

        _waveOutDevice.Init(_audioFileReader);
    }

    public void playSound()
    {
        try
        {
            if (!string.IsNullOrEmpty(_mediaFile))
            {
                if ((_waveOutDevice != null) && (_audioFileReader != null))
                {
                    _audioFileReader.Position = 0;
                    _waveOutDevice.Play();
                }
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    public async Task playSoundAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_mediaFile))
            {
                if ((_waveOutDevice != null) && (_audioFileReader != null))
                {
                    _audioFileReader.Position = 0;
                    _waveOutDevice.Play();
                }
            }
            Console.WriteLine("[C# Host] PlaySoundAsync called.");
            // throw new InvalidOperationException("Test sound error from C#");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[C# Host ERROR] Exception in PlaySoundAsync: {ex.Message}\n{ex.StackTrace}");
            // Re-throw to make JS see it as a rejected promise,
            throw;
        }
    }

    public void logMessage(string msg)
    {
        Console.WriteLine($"[JS] {msg}");
    }

    public void showMessageBox(string msg)
    {
        try
        {
            MessageBox.Show(msg);
        }
        catch { }
    }
}