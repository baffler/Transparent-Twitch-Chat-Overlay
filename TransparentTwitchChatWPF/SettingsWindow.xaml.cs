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
using TwitchLib.Api;
using TwitchLib.Api.V5.Models.Users;

namespace TransparentTwitchChatWPF
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        WindowSettings config;
        MainWindow _main;
        TwitchAPI _api;

        public SettingsWindow(MainWindow mainWindow, WindowSettings windowConfig)
        {
            this.config = windowConfig;
            this._main = mainWindow;

            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.config.ChatType = this.comboChatType.SelectedIndex;

            this.config.RedemptionsEnabled = false;

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
                {
                    this.config.TwitchPopoutCSS = CustomCSS_Defaults.TwitchPopoutChat;
                }

                this.config.BetterTtv = this.cbBetterTtv.IsChecked ?? false;
                this.config.FrankerFaceZ = this.cbFfz.IsChecked ?? false;
            }
            else if (this.config.ChatType == (int)ChatTypes.KapChat)
            {
                this.config.URL = string.Empty;
                this.config.Username = this.tbUsername.Text;
                this.config.RedemptionsEnabled = this.cbRedemptions.IsChecked ?? false;
                this.config.ChannelID = this.tbChannelID.Text;
                this.config.ChatFade = this.cbFade.IsChecked ?? false;
                this.config.FadeTime = this.tbFadeTime.Text;
                //this.config.ShowBotActivity = this.cbBotActivity.IsChecked ?? false;
                this.config.ChatNotificationSound = this.comboChatSound.SelectedValue.ToString();
                this.config.Theme = this.comboTheme.SelectedIndex;

                if (this.config.Theme == 0)
                {
                    this.config.CustomCSS = this.tbCSS.Text;
                }
            }
            else if (this.config.ChatType == (int)ChatTypes.jChat)
            {
                this.config.URL = string.Empty;
                this.config.jChatURL = this.tb_jChatURL.Text;
                this.config.RedemptionsEnabled = this.cbRedemptions2.IsChecked ?? false;
                if (this.config.RedemptionsEnabled)
                    this.config.Username = this.tbUsername2.Text;
                this.config.ChannelID = this.tbChannelID2.Text;
                this.config.ChatNotificationSound = this.comboChatSound2.SelectedValue.ToString();
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
            this.tb_jChatURL.Text = this.config.jChatURL;
            this.tbUsername2.Text = this.config.Username;
            this.tbTwitchPopoutUsername.Text = this.config.Username;
            this.cbRedemptions.IsChecked = this.config.RedemptionsEnabled;
            this.cbRedemptions2.IsChecked = this.config.RedemptionsEnabled;
            this.tbChannelID.Text = this.config.ChannelID;
            this.tbChannelID.IsEnabled = this.config.RedemptionsEnabled;
            this.btGetChannelID.IsEnabled = this.config.RedemptionsEnabled;
            
            this.tbChannelID2.Text = this.config.ChannelID;
            this.tbChannelID2.IsEnabled = this.config.RedemptionsEnabled;
            this.btGetChannelID2.IsEnabled = this.config.RedemptionsEnabled;
            this.tbUsername2.IsEnabled = this.config.RedemptionsEnabled;

            this.cbFade.IsChecked = this.config.ChatFade;

            this.tbFadeTime.Text = this.config.FadeTime;
            this.tbFadeTime.IsEnabled = this.config.ChatFade;

            //this.cbBotActivity.IsChecked = this.config.ShowBotActivity;
            this.comboTheme.SelectedIndex = this.config.Theme;

            var comboxBoxItem = this.comboChatSound.Items.OfType<ComboBoxItem>().FirstOrDefault(
                x => x.Content.ToString() == this.config.ChatNotificationSound);
            if (comboxBoxItem == null)
                this.comboChatSound.SelectedIndex = 0;
            else
                this.comboChatSound.SelectedIndex = this.comboChatSound.Items.IndexOf(comboxBoxItem);

            this.comboChatSound2.SelectedIndex = this.comboChatSound.SelectedIndex;

            // General
            this.cbAutoHideBorders.IsChecked = this.config.AutoHideBorders;
            this.cbEnableTrayIcon.IsChecked = true; //TODO: Temp fix for a bug ~ this.config.EnableTrayIcon;
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
                this.jChatGrid.Visibility = Visibility.Hidden;

                this.tbURL.Text = this.config.URL;
                this.tbCSS2.Text = this.config.CustomCSS;

                this.cbCustomURL.IsChecked = true;
            }
            else if (this.config.ChatType == (int)ChatTypes.TwitchPopout)
            {
                this.kapChatGrid.Visibility = Visibility.Hidden;
                this.twitchPopoutChat.Visibility = Visibility.Visible;
                this.customURLGrid.Visibility = Visibility.Hidden;
                this.jChatGrid.Visibility = Visibility.Hidden;

                if (string.IsNullOrEmpty(this.config.TwitchPopoutCSS))
                    this.tbPopoutCSS.Text = CustomCSS_Defaults.TwitchPopoutChat;
                else
                    this.tbPopoutCSS.Text = this.config.TwitchPopoutCSS;

                this.cbBetterTtv.IsChecked = this.config.BetterTtv;
                this.cbFfz.IsChecked = this.config.FrankerFaceZ;
            }
            else if (this.config.ChatType == (int)ChatTypes.KapChat)
            {
                this.kapChatGrid.Visibility = Visibility.Visible;
                this.twitchPopoutChat.Visibility = Visibility.Hidden;
                this.customURLGrid.Visibility = Visibility.Hidden;
                this.jChatGrid.Visibility = Visibility.Hidden;

                this.tbURL.Text = string.Empty;

                if (string.IsNullOrEmpty(this.config.CustomCSS))
                {
                    this.tbCSS.Text = CustomCSS_Defaults.NoneTheme_CustomCSS;
                }
                else
                {
                    this.tbCSS.Text = this.config.CustomCSS;
                }
            }
            else if (this.config.ChatType == (int)ChatTypes.KapChat)
            {
                this.kapChatGrid.Visibility = Visibility.Hidden;
                this.twitchPopoutChat.Visibility = Visibility.Hidden;
                this.customURLGrid.Visibility = Visibility.Hidden;
                this.jChatGrid.Visibility = Visibility.Visible;

                this.tbURL.Text = string.Empty;
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
                    this.jChatGrid.Visibility = Visibility.Hidden;
                    break;
                case (int)ChatTypes.TwitchPopout:
                    this.kapChatGrid.Visibility = Visibility.Hidden;
                    this.customURLGrid.Visibility = Visibility.Hidden;
                    this.twitchPopoutChat.Visibility = Visibility.Visible;
                    this.jChatGrid.Visibility = Visibility.Hidden;

                    if (string.IsNullOrEmpty(this.config.TwitchPopoutCSS))
                        this.tbPopoutCSS.Text = CustomCSS_Defaults.TwitchPopoutChat;
                    else 
                        this.tbPopoutCSS.Text = this.config.TwitchPopoutCSS;
                    break;
                case (int)ChatTypes.CustomURL:
                    this.kapChatGrid.Visibility = Visibility.Hidden;
                    this.customURLGrid.Visibility = Visibility.Visible;
                    this.twitchPopoutChat.Visibility = Visibility.Hidden;
                    this.jChatGrid.Visibility = Visibility.Hidden;
                    break;
                case (int)ChatTypes.jChat:
                    this.kapChatGrid.Visibility = Visibility.Hidden;
                    this.customURLGrid.Visibility = Visibility.Hidden;
                    this.twitchPopoutChat.Visibility = Visibility.Hidden;
                    this.jChatGrid.Visibility = Visibility.Visible;
                    break;
                default:
                    this.kapChatGrid.Visibility = Visibility.Hidden;
                    this.customURLGrid.Visibility = Visibility.Hidden;
                    this.twitchPopoutChat.Visibility = Visibility.Hidden;
                    this.jChatGrid.Visibility = Visibility.Hidden;
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

        private void comboChatSound_DropDownClosed2(object sender, EventArgs e)
        {
            Uri startupPath = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            string file = System.IO.Path.GetDirectoryName(startupPath.LocalPath) + "\\assets\\";
            file += this.comboChatSound2.SelectedValue.ToString() + ".wav";

            if (System.IO.File.Exists(file))
            {
                SoundPlayer sp = new SoundPlayer(file);
                sp.Play();
            }
        }

        private void btOpenChatFilterSettings_Click(object sender, RoutedEventArgs e)
        {
            ChatFilters chatFiltersWindow = new ChatFilters();

            if (chatFiltersWindow.ShowDialog() == true)
            {
            }
        }

        private void btGetChannelID_Click(object sender, RoutedEventArgs e)
        {
            string _clientid = "gp762nuuoqcoxypju8c569th9wz7q5";

            btGetChannelID.IsEnabled = false;
            btGetChannelID2.IsEnabled = false;

            _api = new TwitchAPI();
            _api.Settings.ClientId = _clientid;

            _getChannelID(tbUsername.Text);

            /*GetID_Window getidWindow = new GetID_Window(this.tbUsername.Text);
            if (getidWindow.ShowDialog() == true)
            {
                this.tbChannelID.Text = SettingsSingleton.Instance.genSettings.ChannelID;
            }*/
        }

        private void btGetChannelID_Click2(object sender, RoutedEventArgs e)
        {
            string _clientid = "gp762nuuoqcoxypju8c569th9wz7q5";

            btGetChannelID.IsEnabled = false;
            btGetChannelID2.IsEnabled = false;

            _api = new TwitchAPI();
            _api.Settings.ClientId = _clientid;

            _getChannelID(tbUsername2.Text);
        }

        private async void _getChannelID(string channel)
        {
            Users users = null;

            try
            {
                users = await _api.V5.Users.GetUserByNameAsync(channel);
            }
            catch (Exception e)
            {
                tbChannelID.Text = "0";
                tbChannelID2.Text = "0";
                MessageBox.Show(e.Message);
            }

            if (users != null)
            {
                if (users.Total > 0)
                {
                    SettingsSingleton.Instance.genSettings.ChannelID = users.Matches[0].Id;
                    tbChannelID.Text = users.Matches[0].Id;
                    btGetChannelID.IsEnabled = true;
                    tbChannelID2.Text = users.Matches[0].Id;
                    btGetChannelID2.IsEnabled = true;
                }
                else
                {
                    tbChannelID.Text = "0";
                    btGetChannelID.IsEnabled = true;
                    tbChannelID2.Text = "0";
                    btGetChannelID2.IsEnabled = true;
                }
            }
            else
            {
                tbChannelID.Text = "0";
                btGetChannelID.IsEnabled = true;
                tbChannelID2.Text = "0";
                btGetChannelID2.IsEnabled = true;
            }
        }

        private void cbRedemptions_Checked(object sender, RoutedEventArgs e)
        {
            this.tbChannelID.IsEnabled = true;
            this.btGetChannelID.IsEnabled = true;
            this.tbChannelID2.IsEnabled = true;
            this.btGetChannelID2.IsEnabled = true;
            this.tbUsername2.IsEnabled = true;
        }

        private void cbRedemptions_Unchecked(object sender, RoutedEventArgs e)
        {
            this.tbChannelID.IsEnabled = false;
            this.btGetChannelID.IsEnabled = false;
            this.tbChannelID2.IsEnabled = false;
            this.btGetChannelID2.IsEnabled = false;
            this.tbUsername2.IsEnabled = false;
        }

        private void cbTaskbar_Checked(object sender, RoutedEventArgs e)
        {
            //this.cbEnableTrayIcon.IsEnabled = false;
            //this.cbEnableTrayIcon.IsChecked = true;
        }

        private void cbTaskbar_Unchecked(object sender, RoutedEventArgs e)
        {
            //this.cbEnableTrayIcon.IsEnabled = true;
            //this.cbEnableTrayIcon.IsChecked = true;
        }
    }
}
