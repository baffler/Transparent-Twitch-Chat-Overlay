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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CefSharp.Wpf;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Interop;

/*
 * TODO:
 * Easier/better size grip, kinda hard to see it, or click on it right now
 * Hotkey and/or menu item to hide the chat and make it visible again
 * More options for kapchat, like setting the themes or other things
 * Show current viewers and other stats from twitch?
 * Allowing you to chat from the app?
 *
 * 
 * Done:
 x Chat fading option
 x Click-through the application
 * 
 */

namespace TransparentTwitchChatWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        bool hiddenBorders = false;
        string custom_url = "";
        string channel = "";
        string fade = "30"; // can also be "false"
        string bot_activity = "true";

        public string BotActivity
        {
            get
            {
                if (bot_activity == "true")
                    return "Turn Off Bot Activity";
                else
                    return "Turn On Bot Activity";
            }
            set
            {
                bot_activity = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void drawBorders()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowHelper.SetWindowExDefault(hwnd);

            btnClose.Visibility = System.Windows.Visibility.Visible;
            btnMin.Visibility = System.Windows.Visibility.Visible;
            btnHide.Visibility = System.Windows.Visibility.Visible;

            headerBorder.Background = Brushes.Black;
            this.BorderBrush = Brushes.Black;
            this.ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;

            hiddenBorders = false;

            this.Topmost = false;
            this.Activate();
            this.Topmost = true;
        }

        private void hideBorders()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowHelper.SetWindowExTransparent(hwnd);

            btnClose.Visibility = System.Windows.Visibility.Hidden;
            btnMin.Visibility = System.Windows.Visibility.Hidden;
            btnHide.Visibility = System.Windows.Visibility.Hidden;

            headerBorder.Background = Brushes.Transparent;
            this.BorderBrush = Brushes.Transparent;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;

            hiddenBorders = true;

            this.Topmost = false;
            this.Activate();
            this.Topmost = true;
        }

        private void ToggleBorderVisibility()
        {
            if (hiddenBorders)
                drawBorders();
            else
                hideBorders();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F9)
            {
                ToggleBorderVisibility();
            }
            else if (e.Key == Key.F8)
            {
                ShowInputDialogBox();

                if (!string.IsNullOrEmpty(channel))
                {
                    SetChatAddress(channel);
                }
            }
        }

        private void headerBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // Begin dragging the window
            this.DragMove();
        }

        private void CommandBinding_CanExecute_1(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
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
            string url = @"https://www.nightdev.com/hosted/obschat/?theme=bttv_blackchat&channel=";
            url += chatChannel;
            url += @"&fade=" + fade;
            url += @"&bot_activity=" + bot_activity;
            url += @"&prevent_clipping=false";
            Browser1.Load(url);
        }

        public void ShowInputDialogBox()
        {
            Input inputDialog = new Input();
            if (inputDialog.ShowDialog() == true)
            {
                // reset custom url
                this.custom_url = string.Empty;
                AppSettings.Default.custom_url = custom_url;

                this.channel = inputDialog.Channel;
                AppSettings.Default.channel = channel;
                AppSettings.Default.Save();
                AppSettings.Default.Reload();
            }
        }

        public void ShowInputFadeDialogBox()
        {
            Input_Fade inputDialog = new Input_Fade();
            if (inputDialog.ShowDialog() == true)
            {
                string fadeTime = inputDialog.Channel;
                int fadeTimeInt = 0;

                int.TryParse(fadeTime, out fadeTimeInt);

                // 0 (or less) to disable fade with "false"
                if (fadeTimeInt <= 0) fadeTime = "false";

                this.fade = fadeTime;
                AppSettings.Default.fade = fade;
                AppSettings.Default.Save();
                AppSettings.Default.Reload();
            }
        }

        public void ShowInputCustomChatDialogBox()
        {
            Input_Custom inputDialog = new Input_Custom();
            if (inputDialog.ShowDialog() == true)
            {
                string customURL = inputDialog.Url;

                AppSettings.Default.custom_url = customURL;
                AppSettings.Default.Save();
                AppSettings.Default.Reload();
            }
        }

        public void ToggleBotActivitySetting()
        {
            if (this.bot_activity == "false")
            {
                this.BotActivity = "true";
            }
            else
            {
                this.BotActivity = "false";
            }

            AppSettings.Default.bot_activity = bot_activity;
            AppSettings.Default.Save();
            AppSettings.Default.Reload();
            SetChatAddress(channel);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            drawBorders();

            this.Width = AppSettings.Default.window_width;
            this.Height = AppSettings.Default.window_height;
            this.Top = AppSettings.Default.window_top;
            this.Left = AppSettings.Default.window_left;

            custom_url = AppSettings.Default.custom_url;
            channel = AppSettings.Default.channel;
            fade = AppSettings.Default.fade;
            this.BotActivity = AppSettings.Default.bot_activity;

            this.Browser1.ZoomLevelIncrement = 0.25;

            if (!string.IsNullOrWhiteSpace(custom_url))
            {
                SetCustomChatAddress(custom_url);
            }
            else if (!string.IsNullOrWhiteSpace(channel))
            {
                SetChatAddress(channel);
            }
            else
            {
                CefSharp.WebBrowserExtensions.LoadHtml(Browser1, "<html><body style=\"font-size: x-large; color: white; text-shadow: -1px -1px 0 #000, 1px -1px 0 #000, -1px 1px 0 #000, 1px 1px 0 #000; \">Load a channel to connect to by right-clicking the tray icon.<br /><br />You can move and resize the window, then press the [o] button to hide borders, or use the tray icon menu.</body></html>");
            }
        }

        private void MenuItem_ToggleBorderVisible(object sender, RoutedEventArgs e)
        {
            ToggleBorderVisibility();
        }

        private void MenuItem_SetChannel(object sender, RoutedEventArgs e)
        {
            ShowInputDialogBox();

            if (!string.IsNullOrEmpty(channel))
            {
                SetChatAddress(channel);
            }
        }

        private void MenuItem_SetFade(object sender, RoutedEventArgs e)
        {
            ShowInputFadeDialogBox();
            SetChatAddress(channel);
        }

        private void MenuItem_SetCustomChat(object sender, RoutedEventArgs e)
        {
            ShowInputCustomChatDialogBox();
            if (!string.IsNullOrWhiteSpace(AppSettings.Default.custom_url))
                SetCustomChatAddress(AppSettings.Default.custom_url);
        }

        private void MenuItem_ToggleBotAcitivty(object sender, RoutedEventArgs e)
        {
            this.ToggleBotActivitySetting();
        }

        private void MenuItem_VisitWebsite(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/baffler/Transparent-Twitch-Chat-Overlay/releases");
        }

        private void MenuItem_Exit(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void btnHide_Click(object sender, RoutedEventArgs e)
        {
            hideBorders();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            AppSettings.Default.window_height = this.Height;
            AppSettings.Default.window_width = this.Width;
            AppSettings.Default.window_top = this.Top;
            AppSettings.Default.window_left = this.Left;
            AppSettings.Default.Save();
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
                this.Browser1.ZoomInCommand.Execute(null);
                AppSettings.Default.zoom_level = this.Browser1.ZoomLevel;
                AppSettings.Default.Save();
            }
        }

        private void MenuItem_ZoomOut(object sender, RoutedEventArgs e)
        {
            if (this.Browser1.ZoomOutCommand.CanExecute(null))
            {
                this.Browser1.ZoomOutCommand.Execute(null);
                AppSettings.Default.zoom_level = this.Browser1.ZoomLevel;
                AppSettings.Default.Save();
            }
        }

        private void MenuItem_ZoomReset(object sender, RoutedEventArgs e)
        {
            if (this.Browser1.ZoomResetCommand.CanExecute(null))
            {
                this.Browser1.ZoomResetCommand.Execute(null);
                AppSettings.Default.zoom_level = this.Browser1.ZoomLevel;
                AppSettings.Default.Save();
            }
        }

        private void Browser1_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                if (AppSettings.Default.zoom_level > 0)
                {
                    this.Browser1.Dispatcher.Invoke(new Action(() => { this.Browser1.ZoomLevel = AppSettings.Default.zoom_level; }));
                }
            }
        }
    }
}
