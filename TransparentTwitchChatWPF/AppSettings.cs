using Jot;
using Jot.Storage;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using TransparentTwitchChatWPF.Helpers;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;

namespace TransparentTwitchChatWPF;

public class AppSettings
{
    public Tracker Tracker;
    public GeneralSettings GeneralSettings { get; set; }
    public jChatConfig jChatSettings { get; set; }

    public string UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TransparentTwitchChatWPF");

    private bool _isInitialized = false;

    public void Init()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        this.Tracker.Configure(this)
            .Properties<AppSettings>(w => new { w.GeneralSettings, w.jChatSettings })
            .Id(w => w.GetType().Name, null, false);

        Application.Current.Exit += (s, e) => {
            this.Tracker.Persist(this);
            this.Tracker.StopTracking(this);
        };

        this.Tracker.Track(this);
    }

    public AppSettings()
    {
        if (AppInfo.IsPortable)
        {
            UserDataFolder = Path.Combine(AppContext.BaseDirectory, "settings");
            Tracker = new Tracker(new JsonFileStore(UserDataFolder));
            this.GeneralSettings = new GeneralSettings();
        }
        else
        {
            Tracker = new Tracker(new JsonFileStore(UserDataFolder));
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
                // Defaults are within the class constructor now
                this.GeneralSettings = new GeneralSettings();
            }

            this.jChatSettings = new jChatConfig();
        }
    }

    public void Persist()
    {
        this.Tracker.Persist(this);
    }

    public void RevertChanges()
    {
        this.Tracker.Track(this);
    }


    /// <summary>
    /// Deserializes a JSON string into the jChatSettings object and persists the changes.
    /// </summary>
    /// <param name="jsonConfig">The JSON string received from the WebView.</param>
    public void UpdateJChatConfig(string jsonConfig)
    {
        if (string.IsNullOrWhiteSpace(jsonConfig)) return;

        try
        {
            // Use the System.Text.Json serializer to convert the JSON string
            // into a new instance of your jChatConfig class.
            var newSettings = JsonSerializer.Deserialize<jChatConfig>(jsonConfig);

            if (newSettings != null)
            {
                // Replace the existing settings object with the new one.
                this.jChatSettings = newSettings;

                // Don't save the AppSettings just yet
                // We'll call Persist() after the user clicks Save button in the settings dialog.
                //this.Persist();
            }
        }
        catch (JsonException ex)
        {
            // It's good practice to log any errors if the JSON is malformed.
            Debug.WriteLine($"Error deserializing jChatConfig: {ex.Message}");
        }
    }

    public void SyncJChatSettings()
    {
        this.jChatSettings.HighlightUsers   = this.GeneralSettings.HighlightUsersChat;
        this.jChatSettings.AllowedUsersOnly = this.GeneralSettings.AllowedUsersOnlyChat;
        this.jChatSettings.PlaySound = this.GeneralSettings.ChatNotificationSound?.ToLower() != "none";
        this.jChatSettings.FilterAllowAllVIPs = this.GeneralSettings.FilterAllowAllVIPs;
        this.jChatSettings.FilterAllowAllMods = this.GeneralSettings.FilterAllowAllMods;
        this.jChatSettings.CustomCSS = GenerateHighlightCSS();

        List<string> vipList = new List<string>();
        if (this.GeneralSettings.AllowedUsersList != null)
        {
            foreach (string item in this.GeneralSettings.AllowedUsersList)
                vipList.Add(item.ToLowerInvariant());
        }

        List<string> blockList = new List<string>();
        if (this.GeneralSettings.BlockedUsersList != null)
        {
            foreach (string item in this.GeneralSettings.BlockedUsersList)
                blockList.Add(item.ToLowerInvariant());
        }

        this.jChatSettings.Vips = vipList.ToArray();
        this.jChatSettings.BlockList = blockList.ToArray();
    }

    private string GenerateHighlightCSS()
    {
        string css = string.Empty;

        // If the user has provided their own manual CSS, we can prepend/append to it

        // 1. Default Highlight
        Color c = this.GeneralSettings.ChatHighlightColor;
        float aL = 0.1f;
        float aR = (c.A / 255f);
        string rgbaL = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, aL);
        string rgbaR = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, aR);

        // Note: Added !important to ensure NativeChat doesn't override the backgrounds
        css += $$"""
        .highlight {
            background: linear-gradient(to right, {{rgbaL}}, {{rgbaR}}) !important;
            border-radius: 4px;
        }
        """;

        // 2. Mods Highlight
        c = this.GeneralSettings.ChatHighlightModsColor;
        aL = 0.1f;
        aR = (c.A / 255f);
        rgbaL = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, aL);
        rgbaR = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, aR);

        css += $$"""
        
        .highlightMod { 
            background: linear-gradient(to right, {{rgbaL}}, {{rgbaR}}) !important;
            border-radius: 4px;
        }
        """;

        // 3. VIPs Highlight
        c = this.GeneralSettings.ChatHighlightVIPsColor;
        aL = 0.1f;
        aR = (c.A / 255f);
        rgbaL = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, aL);
        rgbaR = string.Format("rgba({0},{1},{2},{3:0.00})", c.R, c.G, c.B, aR);

        css += $$"""
        
        .highlightVIP {
            background: linear-gradient(to right, {{rgbaL}}, {{rgbaR}}) !important;
            border-radius: 4px;
        }
        """;

        // Append any user-defined custom CSS at the very end
        if (!string.IsNullOrEmpty(this.GeneralSettings.CustomCSS))
        {
            css += "\n" + this.GeneralSettings.CustomCSS;
        }

        return css;
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
    public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;
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
    public string NativeChatVersion { get; set; } = string.Empty;
}

public class jChatConfig
{
    [JsonPropertyName("channel")]
    public string Channel { get; set; }

    // Boolean toggles
    [JsonPropertyName("animate")]
    public bool Animate { get; set; } = false;

    [JsonPropertyName("center")] // OLD: centerTheme
    public bool Center { get; set; } = false;

    [JsonPropertyName("sms")] // OLD: smsTheme
    public bool Sms { get; set; } = false;

    [JsonPropertyName("showBots")]
    public bool ShowBots { get; set; } = false;

    [JsonPropertyName("hideCommands")]
    public bool HideCommands { get; set; } = false;

    [JsonPropertyName("hideBadges")]
    public bool HideBadges { get; set; } = false;

    [JsonPropertyName("hidePaints")] // OLD: hide7tvPaints
    public bool HidePaints { get; set; } = false;

    [JsonPropertyName("hideColon")]
    public bool HideColon { get; set; } = false;

    [JsonPropertyName("smallCaps")] // OLD: useSmallCaps
    public bool SmallCaps { get; set; } = false;

    [JsonPropertyName("invert")] // OLD: invertChat
    public bool Invert { get; set; } = false;

    [JsonPropertyName("readable")] // OLD: readableColors
    public bool Readable { get; set; } = false;

    [JsonPropertyName("disableSync")] // OLD: disableEmoteSync
    public bool DisableSync { get; set; } = false;

    [JsonPropertyName("disablePruning")] // OLD: disableMessagePruning
    public bool DisablePruning { get; set; } = false;

    [JsonPropertyName("bigSoloEmotes")] // OLD: bigWhenOnlyEmotes
    public bool BigSoloEmotes { get; set; } = false;

    [JsonPropertyName("showPronouns")]
    public bool ShowPronouns { get; set; } = false;

    // Numeric settings
    [JsonPropertyName("fade")] // OLD: fadeTimeout
    public int Fade { get; set; } = 360;

    [JsonPropertyName("size")] // OLD: textSize
    public int Size { get; set; } = 1;

    [JsonPropertyName("height")] // OLD: lineHeight
    public int Height { get; set; } = 3;

    [JsonPropertyName("weight")] // OLD: textWeight
    public int Weight { get; set; } = 4;

    [JsonPropertyName("stroke")] // OLD: textStroke
    public int Stroke { get; set; } = 0;

    [JsonPropertyName("shadow")] // OLD: textShadow
    public int Shadow { get; set; } = 0;

    [JsonPropertyName("emoteScale")]
    public int EmoteScale { get; set; } = 1;

    [JsonPropertyName("scale")] // OLD: chatScale
    public float Scale { get; set; } = 1.0f;

    // String settings
    [JsonPropertyName("font")]
    public string Font { get; set; } = "0";

    [JsonPropertyName("blockedUsers")]
    public string BlockedUsers { get; set; } = "";

    [JsonPropertyName("nicknameColor")]
    public string NicknameColor { get; set; } = "";

    [JsonPropertyName("regex")] // OLD: regexBlacklist
    public string Regex { get; set; } = "";

    [JsonPropertyName("yt")] // OLD: youtubeChannel
    public string Yt { get; set; } = "";

    [JsonPropertyName("voice")] // OLD: ttsVoice
    public string Voice { get; set; } = "";

    [JsonPropertyName("messageImage")] // OLD: smsMessageImage
    public string MessageImage { get; set; } = "";

    [JsonPropertyName("disabledCommands")]
    public string DisabledCommands { get; set; } = "";

    [JsonPropertyName("pronounColorMode")]
    public string PronounColorMode { get; set; } = "default";

    [JsonPropertyName("pronounSingleColor1")]
    public string PronounSingleColor1 { get; set; } = "#a8edea";

    [JsonPropertyName("pronounSingleColor2")]
    public string PronounSingleColor2 { get; set; } = "#fed6e3";

    [JsonPropertyName("pronounCustomColors")]
    public string PronounCustomColors { get; set; } = "{}";

    [JsonPropertyName("highlightUsers")]
    public bool HighlightUsers { get; set; } = false;
    [JsonPropertyName("allowedUsersOnly")]
    public bool AllowedUsersOnly { get; set; } = false;
    [JsonPropertyName("playSound")]
    public bool PlaySound { get; set; } = false;
    [JsonPropertyName("filterAllowAllVIPs")]
    public bool FilterAllowAllVIPs { get; set; } = false;
    [JsonPropertyName("filterAllowAllMods")]
    public bool FilterAllowAllMods { get; set; } = false;
    [JsonPropertyName("vips")]
    public string[] Vips { get; set; }
    [JsonPropertyName("blockList")]
    public string[] BlockList { get; set; }
    [JsonPropertyName("customCSS")]
    public string CustomCSS { get; set; } = "";
}