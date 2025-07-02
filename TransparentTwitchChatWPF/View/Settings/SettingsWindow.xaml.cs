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

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        SettingsWindowActive?.Invoke(false);
    }
}
