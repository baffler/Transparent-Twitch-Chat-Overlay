using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using TransparentTwitchChatWPF.Utils;
using TransparentTwitchChatWPF.View.Settings;

namespace TransparentTwitchChatWPF;

/// <summary>
/// Interaction logic for Settings.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    public event Action<string, string> CreateWidgetRequested;
    public event Action CheckForUpdateRequested;

    private readonly ChatSettingsPage _chatSettingsPage;
    private readonly AppearanceSettingsPage _appearanceSettingsPage;
    private readonly GeneralSettingsPage _generalSettingsPage;
    private readonly ConnectionSettingsPage _connectionSettingsPage;
    private readonly WidgetSettingsPage _widgetSettingsPage;
    private readonly AboutSettingsPage _aboutSettingsPage;

    public SettingsWindow(
        ConnectionSettingsPage connectionPage,
        ChatSettingsPage chatPage,
        AppearanceSettingsPage appearancePage,
        GeneralSettingsPage generalPage,
        WidgetSettingsPage widgetPage,
        AboutSettingsPage aboutPage)
    {
        InitializeComponent();

        _connectionSettingsPage = connectionPage;
        _chatSettingsPage = chatPage;
        _appearanceSettingsPage = appearancePage;
        _generalSettingsPage = generalPage;
        _widgetSettingsPage = widgetPage;
        _aboutSettingsPage = aboutPage;

        _generalSettingsPage.CheckForUpdateRequested += () => {
            // When the GeneralSettingsPage requests a check for updates, fire this window's own event.
            CheckForUpdateRequested?.Invoke();
        };

        // Subscribe to the page's event and bubble it up.
        _widgetSettingsPage.WidgetCreationRequested += (url, css) => {
            // When the page requests a widget, fire this window's own event.
            CreateWidgetRequested?.Invoke(url, css);
        };

        // Set the initial page
        SettingsContentControl.Content = _chatSettingsPage;
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        _generalSettingsPage.SaveValues();
        _chatSettingsPage.SaveValues();

        App.Settings.GeneralSettings.ChatType = this.comboChatType.SelectedIndex;
        App.Settings.Persist();

        DialogResult = true;
    }

    private void SetupValues()
    {
        this.comboChatType.SelectedIndex = App.Settings.GeneralSettings.ChatType;
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
                case "Appearance":
                    SettingsContentControl.Content = _appearanceSettingsPage;
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
        int index = comboChatType.SelectedIndex;

        if (Enum.IsDefined(typeof(ChatTypes), index))
        {
            _chatSettingsPage.ChatTypeChanged((ChatTypes)index);
        }
        else
        {
            Debug.WriteLine("Invalid chat type selected. Index = " + index.ToString());
        }
    }
}
