using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using TransparentTwitchChatWPF.Utils;
using TransparentTwitchChatWPF.View.Settings;
using Brushes = System.Windows.Media.Brushes;
using MessageBox = System.Windows.MessageBox;

namespace TransparentTwitchChatWPF;

/// <summary>
/// Interaction logic for Settings.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    public static event Action<bool> SettingsWindowActive;

    // Create instances of your pages. 
    // You can do this once to preserve state if the user switches back and forth.
    private readonly ChatSettingsPage _chatSettingsPage = new ChatSettingsPage();
    private readonly GeneralSettingsPage _generalSettingsPage = new GeneralSettingsPage();
    private readonly ConnectionSettingsPage _connectionSettingsPage = new ConnectionSettingsPage();
    private readonly WidgetSettingsPage _widgetSettingsPage = new WidgetSettingsPage();
    private readonly AboutSettingsPage _aboutSettingsPage = new AboutSettingsPage();

    // This will hold the TEMPORARY "draft" copy of the settings.
    //private readonly GeneralSettings _draftSettings;

    private MainWindow _main;

    public SettingsWindow(MainWindow mainWindow)
    {
        InitializeComponent();

        // Set the initial page
        SettingsContentControl.Content = _chatSettingsPage;

        //_draftSettings = App.Settings.GeneralSettings.Clone();

        this._main = mainWindow;

        SettingsWindowActive?.Invoke(true);
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        /*
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
                App.Settings.GeneralSettings.TwitchPopoutCSS = this.tbPopoutCSS.Text;
            }
            else
            {
                App.Settings.GeneralSettings.TwitchPopoutCSS = CustomCSS_Defaults.TwitchPopoutChat;
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
        else if (this.config.ChatType == (int)ChatTypes.jCyan)
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

        App.Settings.GeneralSettings.CheckForUpdates = this.cbCheckForUpdates.IsChecked ?? false;

        // Hotkeys
        App.Settings.GeneralSettings.ToggleBordersHotkey = hotkeyInputToggleBorders.Hotkey;
        App.Settings.GeneralSettings.ToggleInteractableHotkey = hotkeyInputToggleInteractable.Hotkey;
        App.Settings.GeneralSettings.BringToTopHotkey = hotkeyInputBringToTop.Hotkey;


        App.Settings.GeneralSettings.DeviceID = (int)DevicesComboBox.SelectedValue;
        App.Settings.GeneralSettings.DeviceName = DevicesComboBox.Text;

        double ClampBetween0And1(double value)
        {
            double s = Math.Round((value * 0.01), 2);
            return Math.Max(0, Math.Min(1, s));
        }

        App.Settings.GeneralSettings.OutputVolume = (float)ClampBetween0And1(this.OutputVolumeSlider.Value);

        App.Settings.GeneralSettings.SoundClipsFolder = this.tbSoundClipsFolder.Text;
        */

        DialogResult = true;
    }

    private void SetupValues()
    {
        /*
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

        // General
        this.cbAutoHideBorders.IsChecked = this.config.AutoHideBorders;
        this.cbEnableTrayIcon.IsChecked = true; //TODO: Temp fix for a bug ~ this.config.EnableTrayIcon;
        //this.cbConfirmClose.IsChecked = this.config.ConfirmClose;
        this.cbTaskbar.IsChecked = this.config.HideTaskbarIcon;
        this.cbInteraction.IsChecked = this.config.AllowInteraction;
        this.cbCheckForUpdates.IsChecked = App.Settings.GeneralSettings.CheckForUpdates;
        // TODO: add single instance back?
        this.cbMultiInstance.IsChecked = true;// TransparentTwitchChatWPF.Properties.Settings.Default.allowMultipleInstances;

        this.hotkeyInputToggleBorders.Hotkey = App.Settings.GeneralSettings.ToggleBordersHotkey;
        this.hotkeyInputToggleInteractable.Hotkey = App.Settings.GeneralSettings.ToggleInteractableHotkey;
        this.hotkeyInputBringToTop.Hotkey = App.Settings.GeneralSettings.BringToTopHotkey;

        
        this.OutputVolumeSlider.Value = App.Settings.GeneralSettings.OutputVolume * 100;

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

            if (string.IsNullOrEmpty(App.Settings.GeneralSettings.TwitchPopoutCSS))
                this.tbPopoutCSS.Text = CustomCSS_Defaults.TwitchPopoutChat;
            else
                this.tbPopoutCSS.Text = App.Settings.GeneralSettings.TwitchPopoutCSS;

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
        }*/
    }


    private void Window_SourceInitialized(object sender, EventArgs e)
    {
        SetupValues();

        _generalSettingsPage.SetupValues();
        _generalSettingsPage.SoundClipsFolderChanged += _chatSettingsPage.LoadSoundClips;

        _chatSettingsPage.SetupValues();
        _chatSettingsPage.TwitchConnectionPageRequested += ShowTwitchConnectionPage;

        _connectionSettingsPage.SetupValues();
        _connectionSettingsPage.TwitchConnectionStatusChanged += _chatSettingsPage.OnTwitchConnectionStatusChanged;
    }

    private void ShowTwitchConnectionPage()
    {
        //SettingsContentControl.Content = _connectionSettingsPage;
        lvSettings.SelectedIndex = 2;
    }

    private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (lvSettings.SelectedItem is ListViewItem selectedItem && selectedItem.Content is StackPanel stackPanel)
        {
            var textBlock = stackPanel.Children.OfType<TextBlock>().FirstOrDefault();
            if (textBlock == null) return;

            switch (textBlock.Text)
            {
                case "Chat":
                    SettingsContentControl.Content = _chatSettingsPage;
                    break;
                case "General":
                    SettingsContentControl.Content = _generalSettingsPage;
                    break;
                case "Connections":
                    SettingsContentControl.Content = _connectionSettingsPage;
                    break;
                case "Widgets":
                    SettingsContentControl.Content = _widgetSettingsPage;
                    break;
                case "About":
                    SettingsContentControl.Content = _aboutSettingsPage;
                    break;
            }
        }
    }

    private void comboChatType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Enum.TryParse<ChatTypes>(comboChatType.SelectedValue.ToString(), out ChatTypes chatType))
        {
            _chatSettingsPage.ChatTypeChanged(chatType);
        }
        else
        {
            Debug.WriteLine("Invalid chat type selected.");
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        SettingsWindowActive?.Invoke(false);
    }
}
