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
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Auth;
using NAudio.Wave;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

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
        TwitchConnection _twitchConnection;

        public SettingsWindow(MainWindow mainWindow, WindowSettings windowConfig)
        {
            this.config = windowConfig;
            this._main = mainWindow;

            _api = new TwitchAPI();
            _api.Settings.ClientId = "yv4bdnndvd4gwsfw7jnfixp0mnofn7";

            this._twitchConnection = new TwitchConnection();
            TwitchConnection.AccessTokenResponse += TwitchConnection_AccessTokenResponse;

            ValidateTwitchConnection();

            InitializeComponent();

            tbPopoutCSS.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("CSS");
            tbWidgetCustomCSS.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("CSS");
            tbCSS2.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("CSS");
        }

        private void LoadDevices()
        {
            DevicesComboBox.Items.Add(new { Id = -1, Name = "Default" });

            for (int deviceId = 0; deviceId < WaveOut.DeviceCount; deviceId++)
            {
                var capabilities = WaveOut.GetCapabilities(deviceId);
                DevicesComboBox.Items.Add(new { Id = deviceId, Name = capabilities.ProductName });
            }

            DevicesComboBox.DisplayMemberPath = "Name";
            DevicesComboBox.SelectedValuePath = "Id";

            DevicesComboBox.SelectedValue = SettingsSingleton.Instance.genSettings.DeviceID;
            if (!DevicesComboBox.Text.StartsWith(SettingsSingleton.Instance.genSettings.DeviceName))
            {
                DevicesComboBox.SelectedValue = -1;
            }
        }

        private string GetSoundClipsFolder()
        {
            string path = tbSoundClipsFolder.Text;
            if (path == "Default")
            {
                path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\assets\\";
            }
            else if (!Directory.Exists(path))
            {
                path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\assets\\";
            }

            if (!path.EndsWith("\\")) path += "\\";

            return path;
        }

        private void LoadSoundClips()
        {
            comboChatSound.Items.Clear();
            comboChatSound2.Items.Clear();

            comboChatSound.Items.Add(new ComboBoxItem() { Content = "None" });
            comboChatSound2.Items.Add(new ComboBoxItem() { Content = "None" });

            string path = GetSoundClipsFolder();

            string[] filesWav = System.IO.Directory.GetFiles(path, "*.wav");
            string[] filesMp3 = System.IO.Directory.GetFiles(path, "*.mp3");

            foreach (string file in filesWav)
            {
                string fileName = System.IO.Path.GetFileName(file);
                comboChatSound.Items.Add(new ComboBoxItem() { Content = fileName });
                comboChatSound2.Items.Add(new ComboBoxItem() { Content = fileName });
            }

            foreach (string file in filesMp3)
            {
                string fileName = System.IO.Path.GetFileName(file);
                comboChatSound.Items.Add(new ComboBoxItem() { Content = fileName });
                comboChatSound2.Items.Add(new ComboBoxItem() { Content = fileName });
            }
        }

        private void TwitchConnection_AccessTokenResponse(object sender, string e)
        {
            SettingsSingleton.Instance.genSettings.OAuthToken = e;
            _api.Settings.AccessToken = e;

            _ = FetchUserDataAsync();
            _ = ValidateAuthToken(e);
        }

        private async Task FetchUserDataAsync()
        {
            try
            {
                var getUser = await _api.Helix.Users.GetUsersAsync();
                GetUsersResponseCallback(getUser);
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message, "Fetch User Data Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GetUsersResponseCallback(GetUsersResponse e)
        {
            string userName = e.Users[0].DisplayName;
            string userID = e.Users[0].Id;
            string profileImageUrl = e.Users[0].ProfileImageUrl;

            lblTwitch.Content = $"{userName} ({userID})";

            SettingsSingleton.Instance.genSettings.ChannelID = userID;

            var bitmap = TwitchConnectionUtils.LoadImageFromUrl(profileImageUrl);
            imgTwitch.Source = bitmap;
        }

        private async Task ValidateAuthToken(string accessToken)
        {
            try
            {
                var t = await _api.Auth.ValidateAccessTokenAsync(accessToken);
                if (t == null)
                {
                    lblTwitchConnected.Foreground = Brushes.Red;
                    lblTwitchConnected2.Foreground = Brushes.Red;
                    lblTwitchConnected.Content = "Auth Token Error";
                    lblTwitchConnected2.Content = "Auth Token Error";
                }
                else
                    AuthTokenValidatedCallback(t);
            }
            catch (Exception e)
            {
                lblTwitchConnected.Foreground = Brushes.Red;
                lblTwitchConnected2.Foreground = Brushes.Red;
                lblTwitchConnected.Content = "Auth Token Error";
                lblTwitchConnected2.Content = "Auth Token Error";

                //MessageBox.Show(e.Message, "Validate Auth Token Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AuthTokenValidatedCallback(ValidateAccessTokenResponse e)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(e.ExpiresIn);
            string expires = string.Format("{0} Days {1} Hours", timeSpan.Days, timeSpan.Hours);

            string status = "Connected";
            if (cbRedemptions.IsChecked ?? false)
            {
                lblTwitchConnected.Foreground = Brushes.Green;
                lblTwitchConnected2.Foreground = Brushes.Green;
                status += " (Active)";
            }
            else
            {
                lblTwitchConnected.Foreground = Brushes.Gray;
                lblTwitchConnected2.Foreground = Brushes.Gray;
                status += " (Inactive)";
            }

            lblTwitchConnected.Content = status;
            lblTwitchConnected2.Content = status;
            btGetChannelID.Visibility = Visibility.Hidden;
            btGetChannelID2.Visibility = Visibility.Hidden;

            lblTwitchStatus.Content = $"Expires: {expires}";
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
                    SettingsSingleton.Instance.genSettings.TwitchPopoutCSS = this.tbPopoutCSS.Text;
                }
                else
                {
                    SettingsSingleton.Instance.genSettings.TwitchPopoutCSS = CustomCSS_Defaults.TwitchPopoutChat;
                }

                this.config.BetterTtv = this.cbBetterTtv.IsChecked ?? false;
                this.config.FrankerFaceZ = this.cbFfz.IsChecked ?? false;
            }
            else if (this.config.ChatType == (int)ChatTypes.KapChat)
            {
                this.config.URL = string.Empty;
                this.config.Username = this.tbUsername.Text;
                this.config.RedemptionsEnabled = this.cbRedemptions.IsChecked ?? false;
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
                this.config.ChatNotificationSound = this.comboChatSound2.SelectedValue.ToString();
            }

            this.config.AutoHideBorders  = this.cbAutoHideBorders.IsChecked ?? false;
            this.config.EnableTrayIcon   = this.cbEnableTrayIcon.IsChecked ?? false;
            //this.config.ConfirmClose     = this.cbConfirmClose.IsChecked ?? false;
            this.config.HideTaskbarIcon  = this.cbTaskbar.IsChecked ?? false;
            this.config.AllowInteraction = this.cbInteraction.IsChecked ?? false;

            SettingsSingleton.Instance.genSettings.CheckForUpdates = this.cbCheckForUpdates.IsChecked ?? false;

            SettingsSingleton.Instance.genSettings.DeviceID = (int)DevicesComboBox.SelectedValue;
            SettingsSingleton.Instance.genSettings.DeviceName = DevicesComboBox.Text;

            double ClampBetween0And1(double value)
            {
                double s = Math.Round((value * 0.01), 2);
                return Math.Max(0, Math.Min(1, s));
            }

            SettingsSingleton.Instance.genSettings.OutputVolume = (float)ClampBetween0And1(this.OutputVolumeSlider.Value);

            SettingsSingleton.Instance.genSettings.SoundClipsFolder = this.tbSoundClipsFolder.Text;

            DialogResult = true;
        }

        private void SetupValues()
        {
            // Load this first so GetSoundsClipsFolder() gets the correct value
            this.tbSoundClipsFolder.Text = SettingsSingleton.Instance.genSettings.SoundClipsFolder;

            this.tbUsername.Text = this.config.Username;
            this.tb_jChatURL.Text = this.config.jChatURL;
            this.tbUsername2.Text = this.config.Username;
            this.tbTwitchPopoutUsername.Text = this.config.Username;
            this.cbRedemptions.IsChecked = this.config.RedemptionsEnabled;
            this.cbRedemptions2.IsChecked = this.config.RedemptionsEnabled;
            this.btGetChannelID.IsEnabled = this.config.RedemptionsEnabled;
            
            this.btGetChannelID2.IsEnabled = this.config.RedemptionsEnabled;
            this.tbUsername2.IsEnabled = this.config.RedemptionsEnabled;

            this.cbFade.IsChecked = this.config.ChatFade;

            this.tbFadeTime.Text = this.config.FadeTime;
            this.tbFadeTime.IsEnabled = this.config.ChatFade;

            //this.cbBotActivity.IsChecked = this.config.ShowBotActivity;
            this.comboTheme.SelectedIndex = this.config.Theme;

            LoadSoundClips();
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
            //this.cbConfirmClose.IsChecked = this.config.ConfirmClose;
            this.cbTaskbar.IsChecked = this.config.HideTaskbarIcon;
            this.cbInteraction.IsChecked = this.config.AllowInteraction;
            this.cbCheckForUpdates.IsChecked = SettingsSingleton.Instance.genSettings.CheckForUpdates;
            this.cbMultiInstance.IsChecked = TransparentTwitchChatWPF.Properties.Settings.Default.allowMultipleInstances;

            LoadDevices();
            this.OutputVolumeSlider.Value = SettingsSingleton.Instance.genSettings.OutputVolume * 100;

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

                if (string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.TwitchPopoutCSS))
                    this.tbPopoutCSS.Text = CustomCSS_Defaults.TwitchPopoutChat;
                else
                    this.tbPopoutCSS.Text = SettingsSingleton.Instance.genSettings.TwitchPopoutCSS;

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
            }
            else
            {
                tbCSS.Visibility = Visibility.Hidden;
                lblCSS.Visibility = Visibility.Hidden;
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.kapChatGrid.Visibility = Visibility.Hidden;
            this.customURLGrid.Visibility = Visibility.Visible;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.customURLGrid.Visibility = Visibility.Hidden;
            this.kapChatGrid.Visibility = Visibility.Visible;
        }


        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            SetupValues();
        }

        private void ValidateTwitchConnection()
        {
            if (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.OAuthToken))
            {
                _api.Settings.AccessToken = SettingsSingleton.Instance.genSettings.OAuthToken;
                _ = FetchUserDataAsync();
                _ = ValidateAuthToken(SettingsSingleton.Instance.genSettings.OAuthToken);
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.lvSettings.SelectedIndex)
            {
                case 0: // Chat
                    this.chatGrid.Visibility = Visibility.Visible;
                    this.generalGrid.Visibility = Visibility.Hidden;
                    this.connectionsGrid.Visibility = Visibility.Hidden;
                    this.widgetGrid.Visibility = Visibility.Hidden;
                    this.comboChatType.Visibility = Visibility.Visible;
                    this.aboutGrid.Visibility = Visibility.Hidden;
                    break;
                case 1: // General
                    this.chatGrid.Visibility = Visibility.Hidden;
                    this.generalGrid.Visibility = Visibility.Visible;
                    this.connectionsGrid.Visibility = Visibility.Hidden;
                    this.widgetGrid.Visibility = Visibility.Hidden;
                    this.comboChatType.Visibility = Visibility.Hidden;
                    this.aboutGrid.Visibility = Visibility.Hidden;
                    break;
                case 2: // Connections
                    this.chatGrid.Visibility = Visibility.Hidden;
                    this.generalGrid.Visibility = Visibility.Hidden;
                    this.connectionsGrid.Visibility = Visibility.Visible;
                    this.widgetGrid.Visibility = Visibility.Hidden;
                    this.comboChatType.Visibility = Visibility.Hidden;
                    this.aboutGrid.Visibility = Visibility.Hidden;
                    ValidateTwitchConnection();
                    break;
                case 3: // Widgets
                    this.chatGrid.Visibility = Visibility.Hidden;
                    this.generalGrid.Visibility = Visibility.Hidden;
                    this.connectionsGrid.Visibility = Visibility.Hidden;
                    this.widgetGrid.Visibility = Visibility.Visible;
                    this.comboChatType.Visibility = Visibility.Hidden;
                    this.aboutGrid.Visibility = Visibility.Hidden;
                    break;
                case 4: // About
                    this.chatGrid.Visibility = Visibility.Hidden;
                    this.generalGrid.Visibility = Visibility.Hidden;
                    this.connectionsGrid.Visibility = Visibility.Hidden;
                    this.widgetGrid.Visibility = Visibility.Hidden;
                    this.comboChatType.Visibility = Visibility.Hidden;
                    this.aboutGrid.Visibility = Visibility.Visible;
                    break;
                default:
                    this.chatGrid.Visibility = Visibility.Hidden;
                    this.generalGrid.Visibility = Visibility.Hidden;
                    this.connectionsGrid.Visibility = Visibility.Hidden;
                    this.widgetGrid.Visibility = Visibility.Hidden;
                    this.comboChatType.Visibility = Visibility.Hidden;
                    this.aboutGrid.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void NewWidgetButton_Click(object sender, RoutedEventArgs e)
        {
            this._main.CreateNewWindow(this.tbUrlForWidget.Text, this.tbWidgetCustomCSS.Text);
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

                    if (string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.TwitchPopoutCSS))
                        this.tbPopoutCSS.Text = CustomCSS_Defaults.TwitchPopoutChat;
                    else 
                        this.tbPopoutCSS.Text = SettingsSingleton.Instance.genSettings.TwitchPopoutCSS;
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

        private void PlayAudioFile(string file)
        {
            if (File.Exists(file))
            {
                var audioFileReader = new AudioFileReader(file);
                {
                    audioFileReader.Volume = (float)(this.OutputVolumeSlider.Value * 0.01);
                    var waveOutDevice = new WaveOutEvent();
                    {
                        waveOutDevice.Init(audioFileReader);
                        waveOutDevice.PlaybackStopped += (s, e) =>
                        {
                            audioFileReader.Dispose();
                            waveOutDevice.Dispose();
                        };
                        waveOutDevice.Play();
                    }
                }
            }
        }

        private void comboChatSound_DropDownClosed(object sender, EventArgs e)
        {
            string file = GetSoundClipsFolder();
            file += this.comboChatSound.SelectedValue.ToString();

            PlayAudioFile(file);
        }

        private void comboChatSound_DropDownClosed2(object sender, EventArgs e)
        {
            string file = GetSoundClipsFolder();
            file += this.comboChatSound2.SelectedValue.ToString();

            PlayAudioFile(file);
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
            this.lvSettings.SelectedIndex = 2;
        }

        private void cbRedemptions_Checked(object sender, RoutedEventArgs e)
        {
            bool isActive = false;

            if (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.ChannelID) &&
                !string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.OAuthToken))
            {
                isActive = true;
            }

            string status = "Connected (Active)";
            if (!isActive) status = "Not Connected";

            this.lblTwitchConnected.Content = status;
            this.lblTwitchConnected.Foreground = isActive ? Brushes.Green : Brushes.Gray;
            this.btGetChannelID.Visibility = isActive ? Visibility.Hidden : Visibility.Visible;
            this.btGetChannelID.IsEnabled = !isActive;

            this.lblTwitchConnected2.Content = status;
            this.lblTwitchConnected2.Foreground = isActive ? Brushes.Green : Brushes.Gray;
            this.btGetChannelID2.Visibility = isActive ? Visibility.Hidden : Visibility.Visible;
            this.btGetChannelID2.IsEnabled = !isActive;
        }

        private void cbRedemptions_Unchecked(object sender, RoutedEventArgs e)
        {
            bool isActive = false;

            if (!string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.ChannelID) &&
                !string.IsNullOrEmpty(SettingsSingleton.Instance.genSettings.OAuthToken))
            {
                isActive = true;
            }

            string status = "Connected (Inactive)";
            if (!isActive) status = "Not Connected";

            this.lblTwitchConnected.Content = status;
            this.lblTwitchConnected.Foreground = Brushes.Gray;
            this.btGetChannelID.Visibility = Visibility.Hidden;

            this.lblTwitchConnected2.Content = status;
            this.lblTwitchConnected2.Foreground = Brushes.Gray;
            this.btGetChannelID2.Visibility = Visibility.Hidden;
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

        private void cbMultiInstance_Checked(object sender, RoutedEventArgs e)
        {
            TransparentTwitchChatWPF.Properties.Settings.Default.allowMultipleInstances = true;
            TransparentTwitchChatWPF.Properties.Settings.Default.Save();
        }

        private void cbMultiInstance_Unchecked(object sender, RoutedEventArgs e)
        {
            TransparentTwitchChatWPF.Properties.Settings.Default.allowMultipleInstances = false;
            TransparentTwitchChatWPF.Properties.Settings.Default.Save();
        }

        private void btConnect_Click(object sender, RoutedEventArgs e)
        {
            this._twitchConnection.ConnectTwitchAccount();
            System.Windows.MessageBox.Show("Please check your default browser. A new tab should have opened and you can authorize the app to be connected there.", "Twitch Connection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void btDisconnect_Click(object sender, RoutedEventArgs e)
        {
            lblTwitch.Content = "Not Connected";
            lblTwitchStatus.Content = "";
            imgTwitch.Source = null;

            lblTwitchConnected.Foreground = Brushes.Gray;
            lblTwitchConnected2.Foreground = Brushes.Gray;
            lblTwitchConnected.Content = "Not Connected";
            lblTwitchConnected2.Content = "Not Connected";
            btGetChannelID.Visibility = Visibility.Visible;
            btGetChannelID2.Visibility = Visibility.Visible;

            SettingsSingleton.Instance.genSettings.ChannelID = string.Empty;
            SettingsSingleton.Instance.genSettings.OAuthToken = string.Empty;
            _api.Settings.AccessToken = string.Empty;
        }

        private void btChangeSoundClipsFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedPath = dialog.SelectedPath;
                    this.tbSoundClipsFolder.Text = selectedPath;
                    this.LoadSoundClips();
                    this.comboChatSound.SelectedIndex = 0;
                }
            }
        }

        private void btDefaultSoundClipsFolder_Click(object sender, RoutedEventArgs e)
        {
            this.tbSoundClipsFolder.Text = "Default";
            this.LoadSoundClips();
            this.comboChatSound.SelectedIndex = 0;
        }
    }
}
