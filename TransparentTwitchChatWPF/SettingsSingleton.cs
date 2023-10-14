using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TransparentTwitchChatWPF
{
    public sealed class SettingsSingleton
    {
        private static readonly Lazy<SettingsSingleton> lazy = new Lazy<SettingsSingleton>(() => new SettingsSingleton());

        public static SettingsSingleton Instance { get { return lazy.Value; } }

        public GeneralSettings genSettings;
        //private GeneralSettings genDefaultSettings;

        public static string Version { get { return "0.9.5"; } }

        private SettingsSingleton()
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
                ZoomLevel = 0,
                OpacityLevel = 0,
                AutoHideBorders = false,
                EnableTrayIcon = true,
                ConfirmClose = true,
                HideTaskbarIcon = false,
                AllowInteraction = true,
                VersionTracker = 0.7,
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
                OAuthToken = string.Empty,
                CheckForUpdates = true,
                ChatHighlightColor = Color.FromArgb(150, 245, 245, 0), // Yellow
                ChatHighlightModsColor = Color.FromArgb(150, 0, 173, 3), // Green
                ChatHighlightVIPsColor = Color.FromArgb(150, 219, 51, 179), // Purple
                OutputVolume = 1.0f,
                DeviceName = "",
                DeviceID = -1,
                SoundClipsFolder = "Default",
            };
        }

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
