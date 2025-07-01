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
