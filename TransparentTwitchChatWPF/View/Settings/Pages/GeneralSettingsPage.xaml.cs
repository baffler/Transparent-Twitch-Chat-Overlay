using NAudio.Wave;
using System.Windows;
using System.Windows.Controls;
using TransparentTwitchChatWPF.Utils;

namespace TransparentTwitchChatWPF.View.Settings;

/// <summary>
/// Interaction logic for GeneralSettingsPage.xaml
/// </summary>
public partial class GeneralSettingsPage : UserControl
{
    public event Action SoundClipsFolderChanged;
    public event Action CheckForUpdateRequested;

    public GeneralSettingsPage()
    {
        InitializeComponent();
    }

    public void SetupValues()
    {
        LoadDevices();

        tbSoundClipsFolder.Text = App.Settings.GeneralSettings.SoundClipsFolder;

        this.cbAutoHideBorders.IsChecked = App.Settings.GeneralSettings.AutoHideBorders;
        this.cbEnableTrayIcon.IsChecked = true; //TODO: Temp fix for a bug ~ this.config.EnableTrayIcon;
        //this.cbConfirmClose.IsChecked = App.Settings.GeneralSettings.ConfirmClose;
        this.cbTaskbar.IsChecked = App.Settings.GeneralSettings.HideTaskbarIcon;
        this.cbInteraction.IsChecked = App.Settings.GeneralSettings.AllowInteraction;
        this.cbCheckForUpdates.IsChecked = App.Settings.GeneralSettings.CheckForUpdates;
        this.cbMultiInstance.IsChecked = App.Settings.GeneralSettings.AllowMultipleInstances;

        this.hotkeyInputToggleBorders.Hotkey = App.Settings.GeneralSettings.ToggleBordersHotkey;
        this.hotkeyInputToggleInteractable.Hotkey = App.Settings.GeneralSettings.ToggleInteractableHotkey;
        this.hotkeyInputBringToTop.Hotkey = App.Settings.GeneralSettings.BringToTopHotkey;

        this.OutputVolumeSlider.Value = App.Settings.GeneralSettings.OutputVolume * 100;
    }

    public void SaveValues()
    {
        App.Settings.GeneralSettings.AutoHideBorders = this.cbAutoHideBorders.IsChecked ?? false;
        App.Settings.GeneralSettings.EnableTrayIcon = this.cbEnableTrayIcon.IsChecked ?? false;
        //App.Settings.GeneralSettings.ConfirmClose     = this.cbConfirmClose.IsChecked ?? false;
        App.Settings.GeneralSettings.HideTaskbarIcon = this.cbTaskbar.IsChecked ?? false;
        App.Settings.GeneralSettings.AllowInteraction = this.cbInteraction.IsChecked ?? false;
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

        DevicesComboBox.SelectedValue = App.Settings.GeneralSettings.DeviceID;
        if (!DevicesComboBox.Text.StartsWith(App.Settings.GeneralSettings.DeviceName))
        {
            DevicesComboBox.SelectedValue = -1;
        }
    }

    private void btChangeSoundClipsFolder_Click(object sender, RoutedEventArgs e)
    {
        using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
        {
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string selectedPath = dialog.SelectedPath;
                tbSoundClipsFolder.Text = selectedPath;
                App.Settings.GeneralSettings.SoundClipsFolder = selectedPath;

                SoundClipsFolderChanged?.Invoke();
            }
        }
    }

    private void btDefaultSoundClipsFolder_Click(object sender, RoutedEventArgs e)
    {
        this.tbSoundClipsFolder.Text = "Default";
        App.Settings.GeneralSettings.SoundClipsFolder = "Default";
        SoundClipsFolderChanged?.Invoke();
    }

    private void setHotkeyToggleBorders_Click(object sender, RoutedEventArgs e)
    {
        if (hotkeyInputToggleBorders.IsCapturing)
        {
            hotkeyInputToggleBorders.StopCapturing();
            btCaptureHotkeyToggleBorders.Content = "Capture Hotkey";
        }
        else
        {
            hotkeyInputToggleBorders.StartCapturing();
            btCaptureHotkeyToggleBorders.Content = "Set Hotkey";
        }
    }


    private void setHotkeyToggleInteractable_Click(object sender, RoutedEventArgs e)
    {
        if (hotkeyInputToggleInteractable.IsCapturing)
        {
            hotkeyInputToggleInteractable.StopCapturing();
            btCaptureHotkeyInteractable.Content = "Capture Hotkey";
        }
        else
        {
            hotkeyInputToggleInteractable.StartCapturing();
            btCaptureHotkeyInteractable.Content = "Set Hotkey";
        }
    }

    private void setHotkeyBringToTop_Click(object sender, RoutedEventArgs e)
    {
        if (hotkeyInputBringToTop.IsCapturing)
        {
            hotkeyInputBringToTop.StopCapturing();
            btCaptureHotkeyBringToTop.Content = "Capture Hotkey";
        }
        else
        {
            hotkeyInputBringToTop.StartCapturing();
            btCaptureHotkeyBringToTop.Content = "Set Hotkey";
        }
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        ShellHelper.OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
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
        App.Settings.GeneralSettings.AllowMultipleInstances = true;
    }

    private void cbMultiInstance_Unchecked(object sender, RoutedEventArgs e)
    {
        App.Settings.GeneralSettings.AllowMultipleInstances = false;
    }

    private void btnCheckForUpdatesNow_Click(object sender, RoutedEventArgs e)
    {
        CheckForUpdateRequested?.Invoke();
    }
}
