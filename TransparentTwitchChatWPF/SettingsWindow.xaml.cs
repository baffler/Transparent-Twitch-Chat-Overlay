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

namespace TransparentTwitchChatWPF
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        WindowSettings config;
        MainWindow _main;

        public SettingsWindow(MainWindow mainWindow, WindowSettings windowConfig)
        {
            this.config = windowConfig;
            this._main = mainWindow;

            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.config.isCustomURL = this.cbCustomURL.IsChecked ?? false;

            if (this.config.isCustomURL)
            {
                this.config.URL = this.tbURL.Text;

                if (!string.IsNullOrWhiteSpace(this.tbCSS2.Text) && !string.IsNullOrEmpty(this.tbCSS2.Text)
                    && (this.tbCSS2.Text.ToLower() != "css"))
                {
                    this.config.CustomCSS = this.tbCSS2.Text;
                }
                else
                    this.config.CustomCSS = string.Empty;
            }
            else
            {
                this.config.URL = string.Empty;
                this.config.Username = this.tbUsername.Text;
                this.config.ChatFade = this.cbFade.IsChecked ?? false;
                this.config.FadeTime = this.tbFadeTime.Text;
                this.config.ShowBotActivity = this.cbBotActivity.IsChecked ?? false;
                this.config.ChatNotificationSound = this.cbChatSound.IsChecked ?? false;
                this.config.Theme = this.comboTheme.SelectedIndex;

                if (this.config.Theme == 0)
                {
                    this.config.CustomCSS = this.tbCSS.Text;
                }
            }

            this.config.AutoHideBorders = this.cbAutoHideBorders.IsChecked ?? false;
            this.config.EnableTrayIcon  = this.cbEnableTrayIcon.IsChecked ?? false;
            this.config.ConfirmClose    = this.cbConfirmClose.IsChecked ?? false;

            DialogResult = true;
        }

        private void SetupValues()
        {
            this.tbUsername.Text = this.config.Username;
            this.cbFade.IsChecked = this.config.ChatFade;

            this.tbFadeTime.Text = this.config.FadeTime;
            this.tbFadeTime.IsEnabled = this.config.ChatFade;

            this.cbBotActivity.IsChecked = this.config.ShowBotActivity;
            this.cbChatSound.IsChecked = this.config.ChatNotificationSound;
            this.comboTheme.SelectedIndex = this.config.Theme;

            // General
            this.cbAutoHideBorders.IsChecked = this.config.AutoHideBorders;
            this.cbEnableTrayIcon.IsChecked = this.config.EnableTrayIcon;
            this.cbConfirmClose.IsChecked = this.config.ConfirmClose;

            // Custom URL
            if (this.config.isCustomURL)
            {
                this.kapChatGrid.Visibility = Visibility.Hidden;
                this.customURLGrid.Visibility = Visibility.Visible;

                this.tbURL.Text = this.config.URL;
                this.tbCSS2.Text = this.config.CustomCSS;

                this.cbCustomURL.IsChecked = true;
            }
            else
            {
                this.kapChatGrid.Visibility = Visibility.Visible;
                this.customURLGrid.Visibility = Visibility.Hidden;

                this.tbURL.Text = string.Empty;

                if (string.IsNullOrEmpty(this.config.CustomCSS))
                {
                    this.tbCSS.Text = @"::-webkit-scrollbar {
    visibility: hidden;
}

#chat_box {

}

.chat_line {

}

.chat_line .nick {

}

.chat_line .message {

}
";
                }
                else
                {
                    this.tbCSS.Text = this.config.CustomCSS;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void cbFade_Checked(object sender, RoutedEventArgs e)
        {
            this.tbFadeTime.IsEnabled = true;
        }

        private void cbFade_Unchecked(object sender, RoutedEventArgs e)
        {
            this.tbFadeTime.IsEnabled = false;
        }

        private void comboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboTheme.SelectedIndex == 0)
            {
                tbCSS.Visibility = Visibility.Visible;
                lblCSS.Visibility = Visibility.Visible;
                this.Height = 510;
            }
            else
            {
                tbCSS.Visibility = Visibility.Hidden;
                lblCSS.Visibility = Visibility.Hidden;
                this.Height = 405;
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.kapChatGrid.Visibility = Visibility.Hidden;
            //this.sp1.Visibility = Visibility.Hidden;
            //this.sp2.Visibility = Visibility.Hidden;
            this.Height = 405;

            this.customURLGrid.Visibility = Visibility.Visible;
            //this.tbURL.Visibility = Visibility.Visible;
            //this.tbCSS2.Visibility = Visibility.Visible;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.customURLGrid.Visibility = Visibility.Hidden;
            //this.tbURL.Visibility = Visibility.Hidden;
            //this.tbCSS2.Visibility = Visibility.Hidden;

            //this.sp1.Visibility = Visibility.Visible;
            //this.sp2.Visibility = Visibility.Visible;
            this.kapChatGrid.Visibility = Visibility.Visible;

            if (comboTheme.SelectedIndex == 0)
                this.Height = 510;
            else
                this.Height = 405;
        }



        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            SetupValues();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.lvSettings.SelectedIndex)
            {
                case 0: // Chat
                    this.chatGrid.Visibility = Visibility.Visible;
                    this.generalGrid.Visibility = Visibility.Hidden;
                    this.widgetGrid.Visibility = Visibility.Hidden;
                    this.cbCustomURL.Visibility = Visibility.Visible;
                    this.aboutGrid.Visibility = Visibility.Hidden;
                    break;
                case 1: // General
                    this.chatGrid.Visibility = Visibility.Hidden;
                    this.generalGrid.Visibility = Visibility.Visible;
                    this.widgetGrid.Visibility = Visibility.Hidden;
                    this.cbCustomURL.Visibility = Visibility.Hidden;
                    this.aboutGrid.Visibility = Visibility.Hidden;
                    break;
                case 2: // Widgets
                    this.chatGrid.Visibility = Visibility.Hidden;
                    this.generalGrid.Visibility = Visibility.Hidden;
                    this.widgetGrid.Visibility = Visibility.Visible;
                    this.cbCustomURL.Visibility = Visibility.Hidden;
                    this.aboutGrid.Visibility = Visibility.Hidden;
                    break;
                case 3: // About
                    this.chatGrid.Visibility = Visibility.Hidden;
                    this.generalGrid.Visibility = Visibility.Hidden;
                    this.widgetGrid.Visibility = Visibility.Hidden;
                    this.cbCustomURL.Visibility = Visibility.Hidden;
                    this.aboutGrid.Visibility = Visibility.Visible;
                    break;
                default:
                    this.chatGrid.Visibility = Visibility.Hidden;
                    this.generalGrid.Visibility = Visibility.Hidden;
                    this.widgetGrid.Visibility = Visibility.Hidden;
                    this.cbCustomURL.Visibility = Visibility.Hidden;
                    this.aboutGrid.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void NewWidgetButton_Click(object sender, RoutedEventArgs e)
        {
            this._main.CreateNewWindow(this.tbUrlForWidget.Text);
            this.tbUrlForWidget.Text = string.Empty;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
