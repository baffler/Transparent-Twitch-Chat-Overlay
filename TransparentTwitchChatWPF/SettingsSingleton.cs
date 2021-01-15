using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF
{
    public sealed class SettingsSingleton
    {
        private static readonly Lazy<SettingsSingleton> lazy = new Lazy<SettingsSingleton>(() => new SettingsSingleton());

        public static SettingsSingleton Instance { get { return lazy.Value; } }

        public GeneralSettings genSettings;

        private SettingsSingleton()
        {
            this.genSettings = new GeneralSettings
            {
                CustomWindows = new StringCollection(),
                Username = string.Empty,
                FadeChat = false,
                FadeTime = "120",
                ShowBotActivity = false,
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
                AllowedUsersList = new StringCollection()
            };
        }
    }
}
