using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF
{
    public class WindowSettings
    {
        public bool isCustomURL { get; set; }
        public string Title { get; set; }
        public string Username { get; set; }
        public string URL { get; set; }
        public bool ChatFade { get; set; }
        public string FadeTime { get; set; }
        public bool ShowBotActivity { get; set; }
        public bool ChatNotificationSound { get; set; }
        public int Theme { get; set; }
        public string CustomCSS { get; set; }
        public bool AutoHideBorders { get; set; }
        public bool EnableTrayIcon { get; set; }
        public bool ConfirmClose { get; set; }
    }
}
