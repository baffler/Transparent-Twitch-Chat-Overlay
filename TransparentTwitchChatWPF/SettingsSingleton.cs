using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Input;

namespace TransparentTwitchChatWPF
{
    public sealed class SettingsSingleton
    {
        //private static readonly Lazy<SettingsSingleton> lazy = new Lazy<SettingsSingleton>(() => new SettingsSingleton());

        //public static SettingsSingleton Instance { get { return lazy.Value; } }

        //public GeneralSettings genSettings;
        //private GeneralSettings genDefaultSettings;

        public static string Version { 
            get 
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            } 
        }

        /*private SettingsSingleton()
        {
            this.genSettings = new GeneralSettings
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
                BringToTopHotkey = new Hotkey(Key.F8, ModifierKeys.Control | ModifierKeys.Alt),
            };
        }*/

        //public void ResetGeneralSettingsToDefault()
        //{
        //    this.genSettings = new GeneralSettings
        //    {
        //        CustomWindows = new StringCollection(),
        //        Username = string.Empty,
        //        FadeChat = this.genDefaultSettings.FadeChat,
        //        FadeTime = this.genDefaultSettings.FadeTime,
        //        BlockBotActivity = this.genDefaultSettings.BlockBotActivity,
        //        ChatNotificationSound = this.genDefaultSettings.ChatNotificationSound,
        //        ThemeIndex = this.genDefaultSettings.ThemeIndex,
        //        ChatType = this.genDefaultSettings.ChatType,
        //        CustomURL = string.Empty,
        //        ZoomLevel = this.genDefaultSettings.ZoomLevel,
        //        OpacityLevel = this.genDefaultSettings.OpacityLevel,
        //        AutoHideBorders = this.genDefaultSettings.AutoHideBorders,
        //        EnableTrayIcon = this.genDefaultSettings.EnableTrayIcon,
        //        ConfirmClose = this.genDefaultSettings.ConfirmClose,
        //        HideTaskbarIcon = this.genDefaultSettings.HideTaskbarIcon,
        //        AllowInteraction = this.genDefaultSettings.AllowInteraction,
        //        VersionTracker = this.genDefaultSettings.VersionTracker,
        //        HighlightUsersChat = this.genDefaultSettings.HighlightUsersChat,
        //        AllowedUsersOnlyChat = this.genDefaultSettings.AllowedUsersOnlyChat,
        //        FilterAllowAllMods = this.genDefaultSettings.FilterAllowAllMods,
        //        FilterAllowAllVIPs = this.genDefaultSettings.FilterAllowAllVIPs,
        //        AllowedUsersList = new StringCollection(),
        //        BlockedUsersList = new StringCollection(),
        //        RedemptionsEnabled = this.genDefaultSettings.RedemptionsEnabled,
        //        ChannelID = string.Empty,
        //        BetterTtv = this.genDefaultSettings.BetterTtv,
        //        FrankerFaceZ = this.genDefaultSettings.FrankerFaceZ,
        //        jChatURL = string.Empty
        //    };
        //}
    }
}
