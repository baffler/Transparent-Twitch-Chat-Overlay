using Jot;
using Jot.Storage;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TransparentTwitchChatWPF.Helpers;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;

namespace TransparentTwitchChatWPF;

public class AppSettings
{
    public Tracker Tracker;
    public GeneralSettings GeneralSettings { get; set; }

    public string UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TransparentTwitchChatWPF");

    private bool _isInitialized = false;

    public void Init()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        this.Tracker.Configure(this)
            .Properties<AppSettings>(w => new { w.GeneralSettings })
            .Id(w => w.GetType().Name, null, false);

        Application.Current.Exit += (s, e) => {
            this.Tracker.Persist(this);
            this.Tracker.StopTracking(this);
        };

        this.Tracker.Track(this);
    }

    public AppSettings()
    {
        Tracker = new Tracker(new JsonFileStore(UserDataFolder));

        //DisplaySettings = new DisplaySettings();

        // Attempt to migrate settings from the old format.
        var migratedSettings = SettingsMigrator.AttemptMigration(this.Tracker);

        if (migratedSettings != null)
        {
            Debug.WriteLine("Migrate GeneralSettings");
            // If migration was successful, use the migrated settings object.
            this.GeneralSettings = migratedSettings;
        }
        else
        {
            Debug.WriteLine("Default GeneralSettings");
            // Defaults are within the class constructor now
            this.GeneralSettings = new GeneralSettings();

            /*
            this.GeneralSettings = new GeneralSettings
            {
                CustomWindows = new StringCollection(),
                Username = string.Empty,
                FadeChat = false,
                FadeTime = "120",
                BlockBotActivity = true,
                ChatNotificationSound = "None",
                ThemeIndex = 1,
                ChatType = 0,
                CustomURL = string.Empty,
                ZoomLevel = 1,
                OpacityLevel = 0,
                AutoHideBorders = false,
                EnableTrayIcon = true,
                ConfirmClose = true,
                HideTaskbarIcon = false,
                AllowInteraction = true,
                VersionTracker = 0.96,
                HighlightUsersChat = false,
                AllowedUsersOnlyChat = false,
                FilterAllowAllMods = false,
                FilterAllowAllVIPs = false,
                AllowedUsersList = new StringCollection(),
                BlockedUsersList = new StringCollection(),
                RedemptionsEnabled = false,
                ChannelID = string.Empty,
                BetterTtv = false,
                FrankerFaceZ = false,
                jChatURL = string.Empty,
                CustomCSS = string.Empty,
                TwitchPopoutCSS = string.Empty,
                OAuthToken = string.Empty,
                CheckForUpdates = true,
                ChatHighlightColor = Color.FromArgb(150, 245, 245, 0), // Yellow
                ChatHighlightModsColor = Color.FromArgb(150, 0, 173, 3), // Green
                ChatHighlightVIPsColor = Color.FromArgb(150, 219, 51, 179), // Purple
                OutputVolume = 1.0f,
                DeviceName = "",
                DeviceID = -1,
                SoundClipsFolder = "Default",
                ToggleBordersHotkey = new Hotkey(Key.F9, ModifierKeys.Control | ModifierKeys.Alt),
                ToggleInteractableHotkey = new Hotkey(Key.F7, ModifierKeys.Control | ModifierKeys.Alt),
                BringToTopHotkey = new Hotkey(Key.F8, ModifierKeys.Control | ModifierKeys.Alt)
            };*/
        }
    }

    public void Persist()
    {
        this.Tracker.Persist(this);
    }
}

public class GeneralSettings
{
    public StringCollection CustomWindows { get; set; } = new StringCollection();
    public string Username { get; set; } = string.Empty;
    public bool FadeChat { get; set; } = false;
    public string FadeTime { get; set; } = "120"; // fade time in seconds or "false"
    public bool BlockBotActivity { get; set; } = true;
    public string ChatNotificationSound { get; set; } = "None";
    public int ThemeIndex { get; set; } = 1;
    public string CustomCSS { get; set; } = string.Empty;
    public string TwitchPopoutCSS { get; set; } = string.Empty;
    public int ChatType { get; set; } = 0;
    public string CustomURL { get; set; } = string.Empty;
    public double ZoomLevel { get; set; } = 1;
    public byte OpacityLevel { get; set; } = 0;
    public bool AutoHideBorders { get; set; } = false;
    public bool EnableTrayIcon { get; set; } = true;
    public bool ConfirmClose { get; set; } = true;
    public bool HideTaskbarIcon { get; set; } = false;
    public bool AllowInteraction { get; set; } = true;
    public double VersionTracker { get; set; } = 0.96;
    public bool HighlightUsersChat { get; set; } = false;
    public bool AllowedUsersOnlyChat { get; set; } = false;
    public bool FilterAllowAllMods { get; set; } = false;
    public bool FilterAllowAllVIPs { get; set; } = false;
    public StringCollection AllowedUsersList { get; set; } = new StringCollection();
    public StringCollection BlockedUsersList { get; set; } = new StringCollection();
    public bool RedemptionsEnabled { get; set; } = false;
    public string ChannelID { get; set; } = string.Empty;
    public string OAuthToken { get; set; } = string.Empty;
    public bool BetterTtv { get; set; } = false;
    public bool FrankerFaceZ { get; set; } = false;
    public string jChatURL { get; set; } = string.Empty;
    public bool CheckForUpdates { get; set; } = true;
    public Color ChatHighlightColor { get; set; } = Color.FromArgb(150, 245, 245, 0); // Yellow
    public Color ChatHighlightModsColor { get; set; } = Color.FromArgb(150, 0, 173, 3); // Green
    public Color ChatHighlightVIPsColor { get; set; } = Color.FromArgb(150, 219, 51, 179); // Purple
    public float OutputVolume { get; set; } = 1.0f;
    public string DeviceName { get; set; } = string.Empty;
    public int DeviceID { get; set; } = -1;
    public string SoundClipsFolder { get; set; } = "Default";
    public Hotkey ToggleBordersHotkey { get; set; } = new Hotkey(Key.F9, ModifierKeys.Control | ModifierKeys.Alt);
    public Hotkey ToggleInteractableHotkey { get; set; } = new Hotkey(Key.F7, ModifierKeys.Control | ModifierKeys.Alt);
    public Hotkey BringToTopHotkey { get; set; } = new Hotkey(Key.F8, ModifierKeys.Control | ModifierKeys.Alt);
    public bool AllowMultipleInstances { get; set; } = false;
}
