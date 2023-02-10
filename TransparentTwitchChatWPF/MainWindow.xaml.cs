﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CefSharp;
using CefSharp.Wpf;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Interop;
using System.Collections.Specialized;
using Jot;
using Jot.DefaultInitializer;
using System.Media;
using TwitchLib;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using Mayerch1.GithubUpdateCheck;

/*
 * v0.94
 * - Filtering: can block users now
 * - Automatically checks for updates on start (Can be disabled in settings)
 * 
 * v0.93
 * - Added a setting to allow multiple instances
 * - Fix for crash when adding a widget with a long URL
 * - Updated CEFsharp (Chromium) and Newtsonsoft.Json to latest versions
 * ~ Known Issues:
 * ~ Jump lists no longer work if you allow multiple instances (it just launches another instance)
 * ~ Can't login to the popout Twitch, this is caused by Twitch blocking login for unsupported browsers
 * 
 * v0.92
 * - jChat support (this allows BetterTTV, FrankerFaceZ and 7TV emotes)
 * - CefSharp (Chromium) updated to 99.2.9
 * - Twitch popout with BTTV and FFZ emotes support - PR by: github.com/r-o-b-o-t-o
 * - Twitch popout will keep your login session - PR by: github.com/r-o-b-o-t-o
 * 
 * v0.91
 * - TwitchLib support for points redemption
 * - Filter settings will let you highlight certain usernames/mods/vip
 * - Possible bug fix for startup crash Load() issue.
 * - System tray icon will always be enabled for now (to prevent no interaction with app)
 * 
 * v0.9
 * - Chat filter settings for KapChat version
 * - Filter by allowed usernames, all mods, or all VIPs
 * - You can configure the filter settings under Chat settings and click the Open Chat Filter Settings button
 *
 * 
 * v0.81
 * - Updated CefSharp to 86.0.241
 * - Fix for how custom CSS was added
 * 
 * v0.8
 * 
 * - More chat sound alerts. Also a lower volume option for each, volume slider soon.
 * - New setting to let you add the Twitch Popout chat
 * - Fixed issue with settings not being saved if app was forcibly closed
 * - New setting to allow interaction with the main window source
 * - Can change opacity by right-clicking on the top border
 * - New setting in General to hide the taskbar icon
 * - When loading a webcaptioner link as a new widget, it will load default custom css
 * 
 * v0.7
 * - Added a setting to show/hide the system tray icon control
 * - Added a setting to auto-hide borders when the application launches
 * - Added a setting to enable/disable the confirmation box when closing the application
 * - Added a setting to allow for a notification sound for a new chat message
 * - Can now resize the window on any part of the border
 * - Added a maximize button
 * - Widget windows are a different color from the main window
 * - Updated Chromium Embedded Framework (CEF)
 * - (will require you to install Microsoft Visual C++ 2015-2019 Redistributable x86)
 * - (available at https://aka.ms/vs/16/release/vc_redist.x86.exe)
 * 
 * v0.6
 * - Removed the system tray icon control
 * - Settings reworked under the hood (you'll lose settings from previous versions, sorry!)
 * - You can right-click the icon in the taskbar now to toggle borders/show settings
 * - Context menu added to the border (right-click on the black border at top)
 * - Settings window added (click cogwheel button on border, right click border and choose "Settings", or
 *      right-click on the application in the taskbar and choose "Show Settings" from there)
 * - Can change themes for chat now
 * - Also custom css (for theme "None" or use Custom URL)
 * - Zoom increase/decrease buttons won't close menu now
 * - Added "New Window" under context menu. You can add alerts widgets now (like from streamlabs)
 * 
 * TODO:
 * > Custom CSS for widgets
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
 * Done:
 x Notification sound for new chat messages
 * 
 */

namespace TransparentTwitchChatWPF
{
    using Chats;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, BrowserWindow
    {
        SolidColorBrush bgColor;
        int cOpacity = 0;
        bool hiddenBorders = false;
        //GeneralSettings genSettings;
        TrackingConfiguration genSettingsTrackingConfig;
        JsCallbackFunctions jsCallbackFunctions;

        //StringCollection custom_windows = new StringCollection();
        List<BrowserWindow> windows = new List<BrowserWindow>();

        private TwitchPubSub _pubSub;

        private Chat currentChat;

        /*public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }*/

        public MainWindow()
        {
            CefSettings settings = new CefSettings()
            {
                PersistSessionCookies = true,
                PersistUserPreferences = true,
                RootCachePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TransparentTwitchChatWPF", "cef"),
                CachePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TransparentTwitchChatWPF", "cef", "cache")
            };
            //settings.CefCommandLineArgs.Add("enable-media-stream", "1"); //Enable WebRTC
            Cef.Initialize(settings);

            InitializeComponent();
            DataContext = this;

            this.currentChat = new CustomURLChat(); // TODO: initializing here needed?

            Services.Tracker.Configure(this).IdentifyAs("State").Apply();
            this.genSettingsTrackingConfig = Services.Tracker.Configure(SettingsSingleton.Instance.genSettings);
            this.genSettingsTrackingConfig.IdentifyAs("MainWindow").Apply();

            var browserSettings = new BrowserSettings
            {
                //ApplicationCache = CefState.Enabled,
                LocalStorage = CefState.Enabled,
                //FileAccessFromFileUrls = CefState.Enabled,
                //UniversalAccessFromFileUrls = CefState.Enabled,
            };
            Browser1.BrowserSettings = browserSettings;
            Browser1.RequestContext = new RequestContext(new RequestContextSettings()
            {
                CachePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TransparentTwitchChatWPF", "cef", "cache"),
                PersistSessionCookies = true,
                PersistUserPreferences = true,
            });
            //this.Browser1.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;

            //this.Browser1.RegisterAsyncJsObject("jsCallback", new JsCallbackFunctions());
            this.jsCallbackFunctions = new JsCallbackFunctions();
            Browser1.JavascriptObjectRepository.Register("jsCallback", this.jsCallbackFunctions, isAsync: true, options: BindingOptions.DefaultBinder);
        }

        public bool ProcessCommandLineArgs(IList<string> args)
        {
            if (args == null || args.Count == 0)
                return true;
            if ((args.Count > 1))
            {
                //the first index always contains the location of the exe so we need to check the second index
                if ((args[1].ToLowerInvariant() == "/toggleborders"))
                {
                    ToggleBorderVisibility();
                }
                else if ((args[1].ToLowerInvariant() == "/settings"))
                {
                    ShowSettingsWindow();
                }
                else if ((args[1].ToLowerInvariant() == "/resetwindow"))
                {
                    ResetWindowPosition();

                    if (MessageBox.Show("Show settings folder?", "Settings Folder", MessageBoxButton.YesNo, MessageBoxImage.Question)
                        == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start((Services.Tracker.StoreFactory as Jot.Storage.JsonFileStoreFactory).StoreFolderPath);
                    }
                }
            }

            return true;
        }

        public void drawBorders()
        {
            this.ShowInTaskbar = true;

            var hwnd = new WindowInteropHelper(this).Handle;
            WindowHelper.SetWindowExDefault(hwnd);

            btnClose.Visibility = Visibility.Visible;
            btnMin.Visibility = Visibility.Visible;
            btnMax.Visibility = Visibility.Visible;
            btnHide.Visibility = Visibility.Visible;
            btnSettings.Visibility = Visibility.Visible;

            headerBorder.Background = Brushes.Black;
            this.BorderBrush = Brushes.Black;
            this.ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;

            hiddenBorders = false;

            this.Topmost = false;
            this.Activate();
            this.Topmost = true;

            this.Browser1.IsEnabled = SettingsSingleton.Instance.genSettings.AllowInteraction;
        }

        public void hideBorders()
        {
            if (SettingsSingleton.Instance.genSettings.HideTaskbarIcon)
                this.ShowInTaskbar = false;

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

            this.Browser1.IsEnabled = false;
        }

        public void ToggleBorderVisibility()
        {
            if (hiddenBorders)
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

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F9)
            {
                ToggleBorderVisibility();
            }
            else if (e.Key == Key.F8)
            {
                ShowSettingsWindow();
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

        private void CommandBinding_CanExecute_1(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            ExitApplication();
        }

        private void CommandBinding_Executed_3(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void SetCustomChatAddress(string url)
        {
            Browser1.Load(url);
        }

        private void SetChatAddress(string chatChannel)
        {
            string username = chatChannel;
            if (chatChannel.Contains("/"))
                username = chatChannel.Split('/').Last();

            string fade = SettingsSingleton.Instance.genSettings.FadeTime;
            if (!SettingsSingleton.Instance.genSettings.FadeChat) { fade = "false"; }

            string theme = string.Empty;
            if ((SettingsSingleton.Instance.genSettings.ThemeIndex >= 0) && (SettingsSingleton.Instance.genSettings.ThemeIndex < KapChat.Themes.Count))
                theme = KapChat.Themes[SettingsSingleton.Instance.genSettings.ThemeIndex];

            string url = @"https://www.nightdev.com/hosted/obschat/?";
            url += @"theme=" + theme;
            url += @"&channel=" + username;
            url += @"&fade=" + fade;
            url += @"&bot_activity=" + (!SettingsSingleton.Instance.genSettings.BlockBotActivity).ToString();
            url += @"&prevent_clipping=false";
            Browser1.Load(url);
        }

        public void ShowInputFadeDialogBox()
        {
            Input_Fade inputDialog = new Input_Fade();
            if (inputDialog.ShowDialog() == true)
            {
                string fadeTime = inputDialog.Channel;
                int fadeTimeInt = 0;

                int.TryParse(fadeTime, out fadeTimeInt);

                SettingsSingleton.Instance.genSettings.FadeChat = (fadeTimeInt > 0);
                SettingsSingleton.Instance.genSettings.FadeTime = fadeTime;
            }
        }

        public void ExitApplication()
        {
            if (SettingsSingleton.Instance.genSettings.ConfirmClose)
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
            }
            //SystemCommands.CloseWindow(this);
        }

        //public void ToggleBotActivitySetting()
        //{
        //    SettingsSingleton.Instance.genSettings.ShowBotActivity = !SettingsSingleton.Instance.genSettings.ShowBotActivity;
        //    SetChatAddress(SettingsSingleton.Instance.genSettings.Username);
        //}

        private void MenuItem_ToggleBorderVisible(object sender, RoutedEventArgs e)
        {
            foreach (BrowserWindow window in this.windows)
            {
                if (window != null)
                {
                    if (this.hiddenBorders)
                        window.drawBorders();
                    else
                        window.hideBorders();
                }
            }
            ToggleBorderVisibility();
        }

        private void MenuItem_ShowSettings(object sender, RoutedEventArgs e)
        {
            ShowSettingsWindow();
        }

        private void MenuItem_VisitWebsite(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/baffler/Transparent-Twitch-Chat-Overlay/releases");
        }

        private void btnHide_Click(object sender, RoutedEventArgs e)
        {
            hideBorders();
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //this.contextMenu.IsOpen = false;
            //this.contextMenu.Placement = PlacementMode.MousePoint;
            //this.contextMenu.HorizontalOffset = 0;
            //this.contextMenu.VerticalContentAlignment = 0;
            //this.contextMenu.IsOpen = true;
        }

        private void MenuItem_ZoomIn(object sender, RoutedEventArgs e)
        {
            if (this.Browser1.ZoomInCommand.CanExecute(null))
            {
                if (SettingsSingleton.Instance.genSettings.ZoomLevel < 4.0)
                {
                    this.Browser1.ZoomInCommand.Execute(null);
                    SettingsSingleton.Instance.genSettings.ZoomLevel = this.Browser1.ZoomLevel;
                }
            }
        }

        private void MenuItem_ZoomOut(object sender, RoutedEventArgs e)
        {
            if (this.Browser1.ZoomOutCommand.CanExecute(null))
            {
                if (SettingsSingleton.Instance.genSettings.ZoomLevel > -4.0)
                {
                    this.Browser1.ZoomOutCommand.Execute(null);
                    SettingsSingleton.Instance.genSettings.ZoomLevel = this.Browser1.ZoomLevel;
                }
            }
        }

        private void MenuItem_ZoomReset(object sender, RoutedEventArgs e)
        {
            if (this.Browser1.ZoomResetCommand.CanExecute(null))
            {
                this.Browser1.ZoomResetCommand.Execute(null);
                SettingsSingleton.Instance.genSettings.ZoomLevel = this.Browser1.ZoomLevel;
            }
        }

        private void Browser1_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                this.Browser1.Dispatcher.Invoke(new Action(() => { this.Browser1.ZoomLevel = SettingsSingleton.Instance.genSettings.ZoomLevel; }));
                
                string js = this.currentChat.SetupJavascript();
                if (!string.IsNullOrEmpty(js))
                    e.Frame.ExecuteJavaScriptAsync(js, "", 0);

                // Custom CSS
                string script = string.Empty;
                string css = this.currentChat.SetupCustomCSS();

                if (!string.IsNullOrEmpty(css))
                    script = InsertCustomCSS2(css);

                if (!string.IsNullOrEmpty(script))
                    e.Frame.ExecuteJavaScriptAsync(script, "", 0);

                this.PushNewMessage("Loading...");
            }
        }
        
        private void Browser1_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                if (SettingsSingleton.Instance.genSettings.ChatType == (int)ChatTypes.TwitchPopout)
                {
                    if (SettingsSingleton.Instance.genSettings.BetterTtv)
                    {
                        InsertCustomJavaScriptFromUrl("https://cdn.betterttv.net/betterttv.js");
                    }
                    if (SettingsSingleton.Instance.genSettings.FrankerFaceZ)
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
            }
        }

        private void Browser1_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            if (SettingsSingleton.Instance.genSettings.ChatType == (int)ChatTypes.jChat)
            {
                if (e.Level == LogSeverity.Info)
                    PushNewMessage(e.Message);
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

        private void InsertCustomJavaScript(string JS)
        {
            try
            {
                this.Browser1.ExecuteScriptAsync(JS);
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

        public void CreateNewWindow(string URL)
        {
            if (SettingsSingleton.Instance.genSettings.CustomWindows.Contains(URL))
            {
                MessageBox.Show("That URL already exists as a window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                SettingsSingleton.Instance.genSettings.CustomWindows.Add(URL);
                OpenNewCustomWindow(URL);
            }
        }

        private void CreateNewWindowDialog()
        {
            Input_Custom inputDialog = new Input_Custom();
            if (inputDialog.ShowDialog() == true)
            {
                CreateNewWindow(inputDialog.Url);
            }
        }

        private void MenuItem_ClickNewWindow(object sender, RoutedEventArgs e)
        {
            CreateNewWindowDialog();
        }

        private void ShowSettingsWindow()
        {
            WindowSettings config = new WindowSettings
            {
                Title = "Main Window",
                ChatType = SettingsSingleton.Instance.genSettings.ChatType,
                URL = SettingsSingleton.Instance.genSettings.CustomURL,
                jChatURL = SettingsSingleton.Instance.genSettings.jChatURL,
                Username = SettingsSingleton.Instance.genSettings.Username,
                ChatFade = SettingsSingleton.Instance.genSettings.FadeChat,
                FadeTime = SettingsSingleton.Instance.genSettings.FadeTime,
                //ShowBotActivity = SettingsSingleton.Instance.genSettings.ShowBotActivity,
                ChatNotificationSound = SettingsSingleton.Instance.genSettings.ChatNotificationSound,
                Theme = SettingsSingleton.Instance.genSettings.ThemeIndex,
                CustomCSS = SettingsSingleton.Instance.genSettings.CustomCSS,
                AutoHideBorders = SettingsSingleton.Instance.genSettings.AutoHideBorders,
                ConfirmClose = SettingsSingleton.Instance.genSettings.ConfirmClose,
                EnableTrayIcon = SettingsSingleton.Instance.genSettings.EnableTrayIcon,
                HideTaskbarIcon = SettingsSingleton.Instance.genSettings.HideTaskbarIcon,
                AllowInteraction = SettingsSingleton.Instance.genSettings.AllowInteraction,
                RedemptionsEnabled = SettingsSingleton.Instance.genSettings.RedemptionsEnabled,
                ChannelID = SettingsSingleton.Instance.genSettings.ChannelID,
                BetterTtv = SettingsSingleton.Instance.genSettings.BetterTtv,
                FrankerFaceZ = SettingsSingleton.Instance.genSettings.FrankerFaceZ,
            };

            SettingsWindow settingsWindow = new SettingsWindow(this, config);

            if (settingsWindow.ShowDialog() == true)
            {
                SettingsSingleton.Instance.genSettings.ChatType = config.ChatType;

                if (config.ChatType == (int)ChatTypes.CustomURL)
                {
                    this.currentChat = new CustomURLChat();
                    SettingsSingleton.Instance.genSettings.CustomURL = config.URL;
                    SettingsSingleton.Instance.genSettings.CustomCSS = config.CustomCSS;

                    if (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.CustomURL))
                        SetCustomChatAddress(SettingsSingleton.Instance.genSettings.CustomURL);
                }
                else if (config.ChatType == (int)ChatTypes.TwitchPopout)
                {
                    this.currentChat = new CustomURLChat();
                    SettingsSingleton.Instance.genSettings.Username = config.Username;
                    SettingsSingleton.Instance.genSettings.CustomCSS = config.TwitchPopoutCSS;

                    if (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.Username))
                        SetCustomChatAddress("https://www.twitch.tv/popout/" + SettingsSingleton.Instance.genSettings.Username + "/chat?popout=");

                    SettingsSingleton.Instance.genSettings.BetterTtv = config.BetterTtv;
                    SettingsSingleton.Instance.genSettings.FrankerFaceZ = config.FrankerFaceZ;
                }
                else if (config.ChatType == (int)ChatTypes.KapChat)
                {
                    this.currentChat = new Chats.KapChat();
                    SettingsSingleton.Instance.genSettings.Username = config.Username;
                    SettingsSingleton.Instance.genSettings.FadeChat = config.ChatFade;
                    SettingsSingleton.Instance.genSettings.FadeTime = config.FadeTime;
                    //SettingsSingleton.Instance.genSettings.ShowBotActivity = config.ShowBotActivity;
                    SettingsSingleton.Instance.genSettings.ChatNotificationSound = config.ChatNotificationSound;
                    SettingsSingleton.Instance.genSettings.ThemeIndex = config.Theme;

                    if (SettingsSingleton.Instance.genSettings.ThemeIndex == 0)
                        SettingsSingleton.Instance.genSettings.CustomCSS = config.CustomCSS;
                    else
                        SettingsSingleton.Instance.genSettings.CustomCSS = string.Empty;


                    if (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.Username))
                    {
                        SetChatAddress(SettingsSingleton.Instance.genSettings.Username);

                        if (config.RedemptionsEnabled)
                            SetupPubSubRedemptions();
                        else
                            DisablePubSubRedemptions();
                    }


                    if (SettingsSingleton.Instance.genSettings.ChatNotificationSound.ToLower() == "none")
                        this.jsCallbackFunctions.MediaFile = string.Empty;
                    else
                    {
                        Uri startupPath = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
                        string file = System.IO.Path.GetDirectoryName(startupPath.LocalPath) + "\\assets\\" + SettingsSingleton.Instance.genSettings.ChatNotificationSound + ".wav";
                        if (System.IO.File.Exists(file))
                        {
                            this.jsCallbackFunctions.MediaFile = file;
                        }
                    }
                }

                else if (config.ChatType == (int)ChatTypes.jChat)
                {
                    this.currentChat = new jChat();
                    SettingsSingleton.Instance.genSettings.jChatURL = config.jChatURL;
                    SettingsSingleton.Instance.genSettings.ChatNotificationSound = config.ChatNotificationSound;

                    if (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.Username))
                    {
                        SetCustomChatAddress(SettingsSingleton.Instance.genSettings.jChatURL);

                        if (config.RedemptionsEnabled)
                            SetupPubSubRedemptions();
                        else
                            DisablePubSubRedemptions();
                    }
                    else
                        SetCustomChatAddress(SettingsSingleton.Instance.genSettings.jChatURL);


                    if (SettingsSingleton.Instance.genSettings.ChatNotificationSound.ToLower() == "none")
                        this.jsCallbackFunctions.MediaFile = string.Empty;
                    else
                    {
                        Uri startupPath = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
                        string file = System.IO.Path.GetDirectoryName(startupPath.LocalPath) + "\\assets\\" + SettingsSingleton.Instance.genSettings.ChatNotificationSound + ".wav";
                        if (System.IO.File.Exists(file))
                        {
                            this.jsCallbackFunctions.MediaFile = file;
                        }
                    }
                }

                SettingsSingleton.Instance.genSettings.AutoHideBorders = config.AutoHideBorders;
                SettingsSingleton.Instance.genSettings.ConfirmClose = config.ConfirmClose;
                SettingsSingleton.Instance.genSettings.EnableTrayIcon = config.EnableTrayIcon;
                SettingsSingleton.Instance.genSettings.HideTaskbarIcon = config.HideTaskbarIcon;
                SettingsSingleton.Instance.genSettings.AllowInteraction = config.AllowInteraction;
                SettingsSingleton.Instance.genSettings.RedemptionsEnabled = config.RedemptionsEnabled;
                SettingsSingleton.Instance.genSettings.ChannelID = config.ChannelID;

                this.taskbarControl.Visibility = config.EnableTrayIcon ? Visibility.Visible : Visibility.Hidden;
                this.ShowInTaskbar = !config.HideTaskbarIcon;

                if (!this.hiddenBorders) this.Browser1.IsEnabled = config.AllowInteraction;

                // Save the new changes for settings
                this.genSettingsTrackingConfig.Persist();
            }
        }

        private void OpenNewCustomWindow(string url, bool hideBorder = false)
        {
            CustomWindow newWindow = new CustomWindow(this, url);
            windows.Add(newWindow);
            newWindow.Show();

            if (hideBorder)
                newWindow.hideBorders();
        }

        public void RemoveCustomWindow(string url)
        {
            if (SettingsSingleton.Instance.genSettings.CustomWindows.Contains(url))
            {
                SettingsSingleton.Instance.genSettings.CustomWindows.Remove(url);
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
            ShowSettingsWindow();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // fix to reset setting for HideTaskbarIcon
            if (SettingsSingleton.Instance.genSettings.VersionTracker <= 0.8)
            {
                SettingsSingleton.Instance.genSettings.HideTaskbarIcon = false;
                SettingsSingleton.Instance.genSettings.EnableTrayIcon = true;
                SettingsSingleton.Instance.genSettings.VersionTracker = 0.9;
            }

            if (!SettingsSingleton.Instance.genSettings.EnableTrayIcon)
                this.taskbarControl.Visibility = Visibility.Hidden;

            GithubUpdateCheck update = new GithubUpdateCheck("baffler", "Transparent-Twitch-Chat-Overlay");
            bool isUpdate = update.IsUpdateAvailable(SettingsSingleton.Version, VersionChange.Build);

            if (isUpdate)
            {
                if (MessageBox.Show($"Version {update.Version()} is available. Would you like to download it now? (Opens in your default browser)",
                    "New Version Available", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("https://github.com/baffler/Transparent-Twitch-Chat-Overlay/releases");
                }
            }
        }

        private void Browser1_IsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Browser1.IsBrowserInitialized)
            {
                SetupBrowser();
            }
        }

        private void SetupBrowser()
        {
            if (!this.Browser1.IsInitialized)
            {
                MessageBox.Show(
                  "Error setting up source. The component was not initialized",
                  "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK
                );
                return;
            }

            this.bgColor = new SolidColorBrush(Color.FromArgb(SettingsSingleton.Instance.genSettings.OpacityLevel, 0, 0, 0));
            this.Background = this.bgColor;
            this.cOpacity = SettingsSingleton.Instance.genSettings.OpacityLevel;

            if (SettingsSingleton.Instance.genSettings.AutoHideBorders)
                hideBorders();
            else
                drawBorders();

            if (SettingsSingleton.Instance.genSettings.ChatNotificationSound.ToLower() != "none")
            {
                Uri startupPath = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
                string file = System.IO.Path.GetDirectoryName(startupPath.LocalPath) + "\\assets\\" + SettingsSingleton.Instance.genSettings.ChatNotificationSound + ".wav";
                if (System.IO.File.Exists(file))
                {
                    this.jsCallbackFunctions.MediaFile = file;
                }
            }

            if (SettingsSingleton.Instance.genSettings.CustomWindows != null)
            {
                foreach (string url in SettingsSingleton.Instance.genSettings.CustomWindows)
                    OpenNewCustomWindow(url, SettingsSingleton.Instance.genSettings.AutoHideBorders);
            }

            this.Browser1.ZoomLevelIncrement = 0.25;

            if ((SettingsSingleton.Instance.genSettings.ChatType == (int)ChatTypes.CustomURL) && (!string.IsNullOrWhiteSpace(SettingsSingleton.Instance.genSettings.CustomURL)))
            {
                this.currentChat = new CustomURLChat();
                SetCustomChatAddress(SettingsSingleton.Instance.genSettings.CustomURL);
            }
            else if ((SettingsSingleton.Instance.genSettings.ChatType == (int)ChatTypes.TwitchPopout) && (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.Username)))
            {
                this.currentChat = new CustomURLChat();
                SetCustomChatAddress("https://www.twitch.tv/popout/" + SettingsSingleton.Instance.genSettings.Username + "/chat?popout=");
            }
            else if (!string.IsNullOrWhiteSpace(SettingsSingleton.Instance.genSettings.Username))
            { // TODO: need to clean this up to determine which type of chat to load better
                if (SettingsSingleton.Instance.genSettings.ChatType == (int)ChatTypes.KapChat)
                {
                    this.currentChat = new Chats.KapChat();
                    SetChatAddress(SettingsSingleton.Instance.genSettings.Username);
                }
                else if (SettingsSingleton.Instance.genSettings.ChatType == (int)ChatTypes.jChat)
                {
                    this.currentChat = new jChat();
                    SetCustomChatAddress(SettingsSingleton.Instance.genSettings.jChatURL);
                }
                else
                {
                    Uri startupPath = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
                    string address = System.IO.Path.GetDirectoryName(startupPath.LocalPath) + "\\index.html";
                    Browser1.Load(address);
                }

                if (SettingsSingleton.Instance.genSettings.RedemptionsEnabled)
                {
                    SetupPubSubRedemptions();
                }
            }
            else if (SettingsSingleton.Instance.genSettings.ChatType == (int)ChatTypes.jChat)
            { // TODO: need to clean this up to determine which type of chat to load better
                this.currentChat = new jChat();
                SetCustomChatAddress(SettingsSingleton.Instance.genSettings.jChatURL);
            }
            else
            {
                Uri startupPath = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
                string address = System.IO.Path.GetDirectoryName(startupPath.LocalPath) + "\\index.html";
                Browser1.Load(address);

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
                System.Diagnostics.Process.Start((Services.Tracker.StoreFactory as Jot.Storage.JsonFileStoreFactory).StoreFolderPath);
            }
        }

        private void btnMax_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void MenuItem_IncOpacity(object sender, RoutedEventArgs e)
        {
            this.cOpacity += 15;
            if (this.cOpacity > 255) this.cOpacity = 255;
            SettingsSingleton.Instance.genSettings.OpacityLevel = (byte)this.cOpacity;
            this.bgColor.Color = Color.FromArgb((byte)this.cOpacity, 0, 0, 0);
        }

        private void MenuItem_DecOpacity(object sender, RoutedEventArgs e)
        {
            this.cOpacity -= 15;
            if (this.cOpacity < 0) this.cOpacity = 0;
            SettingsSingleton.Instance.genSettings.OpacityLevel = (byte)this.cOpacity;
            this.bgColor.Color = Color.FromArgb((byte)this.cOpacity, 0, 0, 0);
        }

        private void MenuItem_ResetOpacity(object sender, RoutedEventArgs e)
        {
            this.cOpacity = 0;
            SettingsSingleton.Instance.genSettings.OpacityLevel = 0;
            this.bgColor.Color = Color.FromArgb(0, 0, 0, 0);
        }

        private void PushNewMessage(string message = "")
        {
            string js = this.currentChat.PushNewMessage(message);

            if (!string.IsNullOrEmpty(js))
            {
                if (this.Browser1.CanExecuteJavascriptInMainFrame)
                    this.Browser1.ExecuteScriptAsync(js);
                else
                    this.Browser1.ExecuteScriptAsyncWhenPageLoaded(js);
            }
        }

        private void PushNewChatMessage(string message = "", string nick = "", string color = "")
        {
            //this.Browser1.Dispatcher.Invoke(new Action(() => { });

            string js = this.currentChat.PushNewChatMessage(message, nick, color);

            if (this.Browser1.CanExecuteJavascriptInMainFrame)
            {
                if (!string.IsNullOrEmpty(js))
                    this.Browser1.ExecuteScriptAsync(js);
            }
            else
            {
                js = this.currentChat.PushNewMessage(message);
                if (!string.IsNullOrEmpty(js))
                    this.Browser1.ExecuteScriptAsyncWhenPageLoaded(js);
            }
        }

        public void SetupPubSubRedemptions()
        {
            DisablePubSubRedemptions();

            _pubSub = new TwitchPubSub();
            _pubSub.OnPubSubServiceConnected += _pubSub_OnPubSubServiceConnected;
            _pubSub.OnListenResponse += _pubSub_OnListenResponse;
            _pubSub.OnRewardRedeemed += _pubSub_OnRewardRedeemed;
            _pubSub.Connect();
        }

        public void DisablePubSubRedemptions()
        {
            if (_pubSub != null)
            {
                try {  _pubSub.OnPubSubServiceConnected -= _pubSub_OnPubSubServiceConnected; }
                catch { }
                try { _pubSub.OnListenResponse -= _pubSub_OnListenResponse; }
                catch { }
                try {  _pubSub.OnRewardRedeemed -= _pubSub_OnRewardRedeemed; }
                catch { }
                try { _pubSub.Disconnect(); }
                catch { }

                _pubSub = null;
            }
        }

        private void _pubSub_OnPubSubServiceConnected(object sender, System.EventArgs e)
        {
            if (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.ChannelID))
            {
                PushNewMessage("PubSub Service Connected");
                _pubSub.ListenToRewards(SettingsSingleton.Instance.genSettings.ChannelID);
                _pubSub.SendTopics("");
            }
        }

        private void _pubSub_OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (!e.Successful)
            {
                PushNewMessage($"Failed to listen! Response: {e.Response.Error}");
            }
            else
                PushNewMessage($"Success! Listening to topic: {e.Topic}");
        }

        private void _pubSub_OnRewardRedeemed(object sender, OnRewardRedeemedArgs e)
        {
            if (SettingsSingleton.Instance.genSettings.RedemptionsEnabled)
            {
                PushNewChatMessage(
                    $"redeemed '{e.RewardTitle}' ({e.RewardCost} points)", // ~ {e.ChannelId}",
                    e.DisplayName, "#708090");
            }
        }
    }

    public class JsCallbackFunctions
    {
        public string MediaFile;

        public JsCallbackFunctions()
        {
            this.MediaFile = "";
        }

        public void playSound()
        {
            try
            {
                if (!string.IsNullOrEmpty(this.MediaFile))
                {
                    SoundPlayer sp = new SoundPlayer(this.MediaFile);
                    //MediaPlayer mp = new MediaPlayer();
                    //mp.Open(new Uri(mediaFile));
                    //mp.Volume = 0.5f;
                    sp.Play();
                }
            } catch { }
        }

        public void showMessageBox(string msg)
        {
            try
            {
                MessageBox.Show(msg);
            } catch { }
        }
    }

    public class GeneralSettings
    {
        [Trackable]
        public StringCollection CustomWindows { get; set; }
        [Trackable]
        public string Username { get; set; }
        [Trackable]
        public bool FadeChat { get; set; }
        [Trackable]
        public string FadeTime { get; set; } // fade time in seconds or "false"
        [Trackable]
        public bool BlockBotActivity { get; set; }
        [Trackable]
        public string ChatNotificationSound { get; set; }
        [Trackable]
        public int ThemeIndex { get; set; }
        [Trackable]
        public string CustomCSS { get; set; }
        [Trackable]
        public int ChatType { get; set; }
        [Trackable]
        public string CustomURL { get; set; }
        [Trackable]
        public double ZoomLevel { get; set; }
        [Trackable]
        public byte OpacityLevel { get; set; }
        [Trackable]
        public bool AutoHideBorders { get; set; }
        [Trackable]
        public bool EnableTrayIcon { get; set; }
        [Trackable]
        public bool ConfirmClose { get; set; }
        [Trackable]
        public bool HideTaskbarIcon { get; set; }
        [Trackable]
        public bool AllowInteraction { get; set; }
        [Trackable]
        public double VersionTracker { get; set; }
        [Trackable]
        public bool HighlightUsersChat { get; set; }
        [Trackable]
        public bool AllowedUsersOnlyChat { get; set; }
        [Trackable]
        public bool FilterAllowAllMods { get; set; }
        [Trackable]
        public bool FilterAllowAllVIPs { get; set; }
        [Trackable]
        public StringCollection AllowedUsersList { get; set; }
        [Trackable]
        public StringCollection BlockedUsersList { get; set; }
        [Trackable]
        public bool RedemptionsEnabled { get; set; }
        [Trackable]
        public string ChannelID { get; set; }
        [Trackable]
        public string OAuthToken { get; set; }
        [Trackable]
        public bool BetterTtv { get; set; }
        [Trackable]
        public bool FrankerFaceZ { get; set; }
        [Trackable]
        public string jChatURL { get; set; }
    }
}
