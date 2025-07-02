using NAudio.Wave;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TransparentTwitchChatWPF.Utils;
using Path = System.IO.Path;

namespace TransparentTwitchChatWPF.View.Settings;

/// <summary>
/// Interaction logic for ChatSettingsPage.xaml
/// </summary>
public partial class ChatSettingsPage : UserControl
{
    public event Action TwitchConnectionPageRequested;

    public ChatSettingsPage()
    {
        InitializeComponent();

        tbPopoutCSS.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("CSS");
        tbCSS2.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("CSS");
    }

    public void SetupValues()
    {
        LoadSoundClips();
        var comboxBoxItem = comboChatSound.Items.OfType<ComboBoxItem>().
            FirstOrDefault(x => x.Content.ToString() == App.Settings.GeneralSettings.ChatNotificationSound);
        if (comboxBoxItem == null)
            this.comboChatSound.SelectedIndex = 0;
        else
            this.comboChatSound.SelectedIndex = this.comboChatSound.Items.IndexOf(comboxBoxItem);

        this.comboChatSound2.SelectedIndex = this.comboChatSound.SelectedIndex;

        this.tbUsername.Text = App.Settings.GeneralSettings.Username;
        this.tb_jChatURL.Text = App.Settings.GeneralSettings.jChatURL;
        this.tbUsername2.Text = App.Settings.GeneralSettings.Username;
        this.tbTwitchPopoutUsername.Text = App.Settings.GeneralSettings.Username;
        this.cbRedemptions.IsChecked = App.Settings.GeneralSettings.RedemptionsEnabled;
        this.cbRedemptions2.IsChecked = App.Settings.GeneralSettings.RedemptionsEnabled;
        this.btGetChannelID.IsEnabled = App.Settings.GeneralSettings.RedemptionsEnabled;
        this.btGetChannelID2.IsEnabled = App.Settings.GeneralSettings.RedemptionsEnabled;
        this.tbUsername2.IsEnabled = App.Settings.GeneralSettings.RedemptionsEnabled;
        this.cbFade.IsChecked = App.Settings.GeneralSettings.FadeChat;
        this.tbFadeTime.Text = App.Settings.GeneralSettings.FadeTime;
        this.tbFadeTime.IsEnabled = App.Settings.GeneralSettings.FadeChat;

        //this.cbBotActivity.IsChecked = App.Settings.GeneralSettings.ShowBotActivity;
        this.comboTheme.SelectedIndex = App.Settings.GeneralSettings.ThemeIndex;

        if (Enum.IsDefined(typeof(ChatTypes), App.Settings.GeneralSettings.ChatType))
        {
            var chatType = (ChatTypes)App.Settings.GeneralSettings.ChatType;

            if (chatType == ChatTypes.CustomURL)
            {
                this.kapChatGrid.Visibility = Visibility.Hidden;
                this.twitchPopoutChat.Visibility = Visibility.Hidden;
                this.customURLGrid.Visibility = Visibility.Visible;
                this.jChatGrid.Visibility = Visibility.Hidden;

                this.tbURL.Text = App.Settings.GeneralSettings.CustomURL;
                this.tbCSS2.Text = App.Settings.GeneralSettings.CustomCSS;
            }
            else if (chatType == ChatTypes.TwitchPopout)
            {
                this.kapChatGrid.Visibility = Visibility.Hidden;
                this.twitchPopoutChat.Visibility = Visibility.Visible;
                this.customURLGrid.Visibility = Visibility.Hidden;
                this.jChatGrid.Visibility = Visibility.Hidden;

                if (string.IsNullOrEmpty(App.Settings.GeneralSettings.TwitchPopoutCSS))
                    this.tbPopoutCSS.Text = CustomCSS_Defaults.TwitchPopoutChat;
                else
                    this.tbPopoutCSS.Text = App.Settings.GeneralSettings.TwitchPopoutCSS;

                this.cbBetterTtv.IsChecked = App.Settings.GeneralSettings.BetterTtv;
                this.cbFfz.IsChecked = App.Settings.GeneralSettings.FrankerFaceZ;
            }
            else if (chatType == ChatTypes.KapChat)
            {
                this.kapChatGrid.Visibility = Visibility.Visible;
                this.twitchPopoutChat.Visibility = Visibility.Hidden;
                this.customURLGrid.Visibility = Visibility.Hidden;
                this.jChatGrid.Visibility = Visibility.Hidden;

                this.tbURL.Text = string.Empty;

                if (string.IsNullOrEmpty(App.Settings.GeneralSettings.CustomCSS))
                {
                    this.tbCSS.Text = CustomCSS_Defaults.NoneTheme_CustomCSS;
                }
                else
                {
                    this.tbCSS.Text = App.Settings.GeneralSettings.CustomCSS;
                }
            }
            else if (chatType == ChatTypes.KapChat)
            {
                this.kapChatGrid.Visibility = Visibility.Hidden;
                this.twitchPopoutChat.Visibility = Visibility.Hidden;
                this.customURLGrid.Visibility = Visibility.Hidden;
                this.jChatGrid.Visibility = Visibility.Visible;

                this.tbURL.Text = string.Empty;
            }
        }
    }

    public void SaveValues()
    {
        //this.config.RedemptionsEnabled = false;

        if (Enum.IsDefined(typeof(ChatTypes), App.Settings.GeneralSettings.ChatType))
        {
            var chatType = (ChatTypes)App.Settings.GeneralSettings.ChatType;

            if (chatType == ChatTypes.CustomURL)
            {
                App.Settings.GeneralSettings.CustomURL = this.tbURL.Text;

                if (!string.IsNullOrWhiteSpace(this.tbCSS2.Text) && !string.IsNullOrEmpty(this.tbCSS2.Text)
                    && (this.tbCSS2.Text.ToLower() != "css"))
                {
                    App.Settings.GeneralSettings.CustomCSS = this.tbCSS2.Text;
                }
                else
                    App.Settings.GeneralSettings.CustomCSS = string.Empty;
            }
            else if (chatType == ChatTypes.TwitchPopout)
            {
                if (string.IsNullOrEmpty(this.tbTwitchPopoutUsername.Text) || string.IsNullOrWhiteSpace(this.tbTwitchPopoutUsername.Text))
                {
                    this.tbTwitchPopoutUsername.Text = "username";
                }
                App.Settings.GeneralSettings.Username = this.tbTwitchPopoutUsername.Text;

                if (!string.IsNullOrWhiteSpace(this.tbPopoutCSS.Text) && !string.IsNullOrEmpty(this.tbPopoutCSS.Text)
                    && (this.tbPopoutCSS.Text.ToLower() != "css"))
                {
                    App.Settings.GeneralSettings.TwitchPopoutCSS = this.tbPopoutCSS.Text;
                }
                else
                {
                    App.Settings.GeneralSettings.TwitchPopoutCSS = CustomCSS_Defaults.TwitchPopoutChat;
                }

                App.Settings.GeneralSettings.BetterTtv = this.cbBetterTtv.IsChecked ?? false;
                App.Settings.GeneralSettings.FrankerFaceZ = this.cbFfz.IsChecked ?? false;
            }
            else if (chatType == (int)ChatTypes.KapChat)
            {
                App.Settings.GeneralSettings.CustomURL = string.Empty;
                App.Settings.GeneralSettings.Username = this.tbUsername.Text;
                App.Settings.GeneralSettings.RedemptionsEnabled = this.cbRedemptions.IsChecked ?? false;
                App.Settings.GeneralSettings.FadeChat = this.cbFade.IsChecked ?? false;
                App.Settings.GeneralSettings.FadeTime = this.tbFadeTime.Text;
                //App.Settings.GeneralSettings.ShowBotActivity = this.cbBotActivity.IsChecked ?? false;
                App.Settings.GeneralSettings.ChatNotificationSound = this.comboChatSound.SelectedValue.ToString();
                App.Settings.GeneralSettings.ThemeIndex = this.comboTheme.SelectedIndex;

                if (App.Settings.GeneralSettings.ThemeIndex == 0)
                {
                    App.Settings.GeneralSettings.CustomCSS = this.tbCSS.Text;
                }
            }
            else if (chatType == ChatTypes.jCyan)
            {
                App.Settings.GeneralSettings.CustomURL = string.Empty;
                App.Settings.GeneralSettings.jChatURL = this.tb_jChatURL.Text;
                App.Settings.GeneralSettings.RedemptionsEnabled = this.cbRedemptions2.IsChecked ?? false;
                if (App.Settings.GeneralSettings.RedemptionsEnabled)
                    App.Settings.GeneralSettings.Username = this.tbUsername2.Text;
                App.Settings.GeneralSettings.ChatNotificationSound = this.comboChatSound2.SelectedValue.ToString();
            }
        }
    }

    public void OnTwitchConnectionStatusChanged(TwitchConnectionStatus twitchConnectionStatus)
    {
        lblTwitchConnected.Foreground = Brushes.Blue;
        lblTwitchConnected2.Foreground = Brushes.Blue;

        Visibility getChannelButtonVisibility = Visibility.Hidden;

        if (twitchConnectionStatus.StatusState == TwitchConnectionStatusState.NotConnected)
        {
            getChannelButtonVisibility = Visibility.Visible;

            lblTwitchConnected.Foreground = Brushes.Gray;
            lblTwitchConnected2.Foreground = Brushes.Gray;
        }
        else if (twitchConnectionStatus.StatusState == TwitchConnectionStatusState.Active)
        {
            lblTwitchConnected.Foreground = Brushes.Green;
            lblTwitchConnected2.Foreground = Brushes.Green;
        }
        else if (twitchConnectionStatus.StatusState == TwitchConnectionStatusState.Inactive)
        {
            lblTwitchConnected.Foreground = Brushes.Gray;
            lblTwitchConnected2.Foreground = Brushes.Gray;
        }
        else if (twitchConnectionStatus.StatusState == TwitchConnectionStatusState.Error)
        {
            getChannelButtonVisibility = Visibility.Visible;

            lblTwitchConnected.Foreground = Brushes.Red;
            lblTwitchConnected2.Foreground = Brushes.Red;
        }

        btGetChannelID.Visibility = getChannelButtonVisibility;
        btGetChannelID2.Visibility = getChannelButtonVisibility;

        lblTwitchConnected.Content = twitchConnectionStatus.Message;
        lblTwitchConnected2.Content = twitchConnectionStatus.Message;
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

    public void LoadSoundClips()
    {
        comboChatSound.Items.Clear();
        comboChatSound2.Items.Clear();

        comboChatSound.Items.Add(new ComboBoxItem() { Content = "None" });
        comboChatSound2.Items.Add(new ComboBoxItem() { Content = "None" });

        comboChatSound.SelectedIndex = 0;
        comboChatSound2.SelectedIndex = 0;

        string path = GetSoundClipsFolder();

        if (!Directory.Exists(path)) return;

        string[] filesWav = Directory.GetFiles(path, "*.wav");
        string[] filesMp3 = Directory.GetFiles(path, "*.mp3");

        foreach (string file in filesWav)
        {
            string fileName = Path.GetFileName(file);
            comboChatSound.Items.Add(new ComboBoxItem() { Content = fileName });
            comboChatSound2.Items.Add(new ComboBoxItem() { Content = fileName });
        }

        foreach (string file in filesMp3)
        {
            string fileName = Path.GetFileName(file);
            comboChatSound.Items.Add(new ComboBoxItem() { Content = fileName });
            comboChatSound2.Items.Add(new ComboBoxItem() { Content = fileName });
        }
    }

    private void PlayAudioFile(string file)
    {
        if (File.Exists(file))
        {
            var audioFileReader = new AudioFileReader(file);
            {
                audioFileReader.Volume = App.Settings.GeneralSettings.OutputVolume;
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

    public void ChatTypeChanged(ChatTypes chatType)
    {
        App.Settings.GeneralSettings.ChatType = (int)chatType;

        switch (chatType)
        {
            case ChatTypes.KapChat:
                this.kapChatGrid.Visibility = Visibility.Visible;
                this.customURLGrid.Visibility = Visibility.Hidden;
                this.twitchPopoutChat.Visibility = Visibility.Hidden;
                this.jChatGrid.Visibility = Visibility.Hidden;
                break;
            case ChatTypes.TwitchPopout:
                this.kapChatGrid.Visibility = Visibility.Hidden;
                this.customURLGrid.Visibility = Visibility.Hidden;
                this.twitchPopoutChat.Visibility = Visibility.Visible;
                this.jChatGrid.Visibility = Visibility.Hidden;

                if (string.IsNullOrEmpty(App.Settings.GeneralSettings.TwitchPopoutCSS))
                    this.tbPopoutCSS.Text = CustomCSS_Defaults.TwitchPopoutChat;
                else
                    this.tbPopoutCSS.Text = App.Settings.GeneralSettings.TwitchPopoutCSS;
                break;
            case ChatTypes.CustomURL:
                this.kapChatGrid.Visibility = Visibility.Hidden;
                this.customURLGrid.Visibility = Visibility.Visible;
                this.twitchPopoutChat.Visibility = Visibility.Hidden;
                this.jChatGrid.Visibility = Visibility.Hidden;
                break;
            case ChatTypes.jCyan:
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

    // --- Event Handlers --------------------------------------------------------------------------------

    private void cbRedemptions_Unchecked(object sender, RoutedEventArgs e)
    {
        bool isActive = false;

        if (!string.IsNullOrEmpty(App.Settings.GeneralSettings.ChannelID) &&
            !string.IsNullOrEmpty(App.Settings.GeneralSettings.OAuthToken))
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

    private void comboChatSound_DropDownClosed(object sender, EventArgs e)
    {
        string file = Path.Combine(GetSoundClipsFolder(), this.comboChatSound.SelectedValue.ToString());

        PlayAudioFile(file);
    }

    private void comboChatSound_DropDownClosed2(object sender, EventArgs e)
    {
        string file = Path.Combine(GetSoundClipsFolder(), this.comboChatSound2.SelectedValue.ToString());

        PlayAudioFile(file);
    }

    private void cbRedemptions_Checked(object sender, RoutedEventArgs e)
    {
        bool isActive = false;

        if (!string.IsNullOrEmpty(App.Settings.GeneralSettings.ChannelID) &&
            !string.IsNullOrEmpty(App.Settings.GeneralSettings.OAuthToken))
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

        App.Settings.GeneralSettings.RedemptionsEnabled = cbRedemptions.IsChecked ?? false;
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

    private void cbFade_Checked(object sender, RoutedEventArgs e)
    {
        this.tbFadeTime.IsEnabled = true;
    }

    private void cbFade_Unchecked(object sender, RoutedEventArgs e)
    {
        this.tbFadeTime.IsEnabled = false;
    }

    private void btGetChannelID_Click(object sender, RoutedEventArgs e)
    {
        TwitchConnectionPageRequested?.Invoke();
    }

    private void btOpenChatFilterSettings_Click(object sender, RoutedEventArgs e)
    {
        ChatFilters chatFiltersWindow = new ChatFilters();
        chatFiltersWindow.ShowDialog();
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        ShellHelper.OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}
