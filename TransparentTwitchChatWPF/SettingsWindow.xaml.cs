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
using System.Media;

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
            this.config.ChatType = this.comboChatType.SelectedIndex;

            if (this.config.ChatType == (int)ChatTypes.CustomURL)
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
            else if (this.config.ChatType == (int)ChatTypes.TwitchPopout)
            {
                if (string.IsNullOrEmpty(this.tbTwitchPopoutUsername.Text) || string.IsNullOrWhiteSpace(this.tbTwitchPopoutUsername.Text))
                {
                    this.tbTwitchPopoutUsername.Text = "username";
                }
                this.config.Username = this.tbTwitchPopoutUsername.Text;

                if (!string.IsNullOrWhiteSpace(this.tbPopoutCSS.Text) && !string.IsNullOrEmpty(this.tbPopoutCSS.Text)
                    && (this.tbPopoutCSS.Text.ToLower() != "css"))
                {
                    this.config.TwitchPopoutCSS = this.tbPopoutCSS.Text;
                }
                else
                    this.config.TwitchPopoutCSS = CustomCSS_Defaults.TwitchPopoutChat;
            }
            else if (this.config.ChatType == (int)ChatTypes.KapChat)
            {
                this.config.URL = string.Empty;
                this.config.Username = this.tbUsername.Text;
                this.config.ChatFade = this.cbFade.IsChecked ?? false;
                this.config.FadeTime = this.tbFadeTime.Text;
                this.config.ShowBotActivity = this.cbBotActivity.IsChecked ?? false;
                this.config.ChatNotificationSound = this.comboChatSound.SelectedValue.ToString();
                this.config.Theme = this.comboTheme.SelectedIndex;

                if (this.config.Theme == 0)
                {
                    this.config.CustomCSS = this.tbCSS.Text;
                }
            }

            this.config.AutoHideBorders  = this.cbAutoHideBorders.IsChecked ?? false;
            this.config.EnableTrayIcon   = this.cbEnableTrayIcon.IsChecked ?? false;
            this.config.ConfirmClose     = this.cbConfirmClose.IsChecked ?? false;
            this.config.HideTaskbarIcon  = this.cbTaskbar.IsChecked ?? false;
            this.config.AllowInteraction = this.cbInteraction.IsChecked ?? false;

            DialogResult = true;
        }

        private void SetupValues()
        {
            this.tbUsername.Text = this.config.Username;
            this.tbTwitchPopoutUsername.Text = this.config.Username;
            this.cbFade.IsChecked = this.config.ChatFade;

            this.tbFadeTime.Text = this.config.FadeTime;
            this.tbFadeTime.IsEnabled = this.config.ChatFade;

            this.cbBotActivity.IsChecked = this.config.ShowBotActivity;
            this.comboTheme.SelectedIndex = this.config.Theme;

            var comboxBoxItem = this.comboChatSound.Items.OfType<ComboBoxItem>().FirstOrDefault(
                x => x.Content.ToString() == this.config.ChatNotificationSound);
            if (comboxBoxItem == null)
                this.comboChatSound.SelectedIndex = 0;
            else
                this.comboChatSound.SelectedIndex = this.comboChatSound.Items.IndexOf(comboxBoxItem);

            // General
            this.cbAutoHideBorders.IsChecked = this.config.AutoHideBorders;
            this.cbEnableTrayIcon.IsChecked = this.config.EnableTrayIcon;
            this.cbConfirmClose.IsChecked = this.config.ConfirmClose;
            this.cbTaskbar.IsChecked = this.config.HideTaskbarIcon;
            this.cbInteraction.IsChecked = this.config.AllowInteraction;

            this.comboChatType.SelectedIndex = this.config.ChatType;

            // Custom URL
            if (this.config.ChatType == (int)ChatTypes.CustomURL)
            {
                this.kapChatGrid.Visibility = Visibility.Hidden;
                this.twitchPopoutChat.Visibility = Visibility.Hidden;
                this.customURLGrid.Visibility = Visibility.Visible;

                this.tbURL.Text = this.config.URL;
                this.tbCSS2.Text = this.config.CustomCSS;

                this.cbCustomURL.IsChecked = true;
            }
            else if (this.config.ChatType == (int)ChatTypes.TwitchPopout)
            {
                this.kapChatGrid.Visibility = Visibility.Hidden;
                this.twitchPopoutChat.Visibility = Visibility.Visible;
                this.customURLGrid.Visibility = Visibility.Hidden;

                if (string.IsNullOrEmpty(this.config.TwitchPopoutCSS))
                    this.tbPopoutCSS.Text = CustomCSS_Defaults.TwitchPopoutChat;
                else
                    this.tbPopoutCSS.Text = this.config.TwitchPopoutCSS;
            }
            else if (this.config.ChatType == (int)ChatTypes.KapChat)
            {
                this.kapChatGrid.Visibility = Visibility.Visible;
                this.twitchPopoutChat.Visibility = Visibility.Hidden;
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
            this.Height = 405;

            this.customURLGrid.Visibility = Visibility.Visible;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.customURLGrid.Visibility = Visibility.Hidden;
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
                    this.comboChatType.Visibility = Visibility.Visible;
                    this.aboutGrid.Visibility = Visibility.Hidden;
                    break;
                case 1: // General
                    this.chatGrid.Visibility = Visibility.Hidden;
                    this.generalGrid.Visibility = Visibility.Visible;
                    this.widgetGrid.Visibility = Visibility.Hidden;
                    this.comboChatType.Visibility = Visibility.Hidden;
                    this.aboutGrid.Visibility = Visibility.Hidden;
                    break;
                case 2: // Widgets
                    this.chatGrid.Visibility = Visibility.Hidden;
                    this.generalGrid.Visibility = Visibility.Hidden;
                    this.widgetGrid.Visibility = Visibility.Visible;
                    this.comboChatType.Visibility = Visibility.Hidden;
                    this.aboutGrid.Visibility = Visibility.Hidden;
                    break;
                case 3: // About
                    this.chatGrid.Visibility = Visibility.Hidden;
                    this.generalGrid.Visibility = Visibility.Hidden;
                    this.widgetGrid.Visibility = Visibility.Hidden;
                    this.comboChatType.Visibility = Visibility.Hidden;
                    this.aboutGrid.Visibility = Visibility.Visible;
                    break;
                default:
                    this.chatGrid.Visibility = Visibility.Hidden;
                    this.generalGrid.Visibility = Visibility.Hidden;
                    this.widgetGrid.Visibility = Visibility.Hidden;
                    this.comboChatType.Visibility = Visibility.Hidden;
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

        private void comboChatType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (comboChatType.SelectedIndex)
            {
                case (int)ChatTypes.KapChat:
                    this.kapChatGrid.Visibility = Visibility.Visible;
                    this.customURLGrid.Visibility = Visibility.Hidden;
                    this.twitchPopoutChat.Visibility = Visibility.Hidden;
                    break;
                case (int)ChatTypes.TwitchPopout:
                    this.kapChatGrid.Visibility = Visibility.Hidden;
                    this.customURLGrid.Visibility = Visibility.Hidden;
                    this.twitchPopoutChat.Visibility = Visibility.Visible;

                    if (string.IsNullOrEmpty(this.config.TwitchPopoutCSS))
                        this.tbPopoutCSS.Text = CustomCSS_Defaults.TwitchPopoutChat;
                    else 
                        this.tbPopoutCSS.Text = this.config.TwitchPopoutCSS;
                    break;
                case (int)ChatTypes.CustomURL:
                    this.kapChatGrid.Visibility = Visibility.Hidden;
                    this.customURLGrid.Visibility = Visibility.Visible;
                    this.twitchPopoutChat.Visibility = Visibility.Hidden;
                    break;
                default:
                    this.kapChatGrid.Visibility = Visibility.Hidden;
                    this.customURLGrid.Visibility = Visibility.Hidden;
                    this.twitchPopoutChat.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void comboChatSound_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void comboChatSound_DropDownClosed(object sender, EventArgs e)
        {
            Uri startupPath = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            string file = System.IO.Path.GetDirectoryName(startupPath.LocalPath) + "\\assets\\";
            file += this.comboChatSound.SelectedValue.ToString() + ".wav";

            if (System.IO.File.Exists(file))
            {
                SoundPlayer sp = new SoundPlayer(file);
                sp.Play();
            }
        }
    }
}
