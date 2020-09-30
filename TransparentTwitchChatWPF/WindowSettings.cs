using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF
{
    public enum ChatTypes
    {
        KapChat = 0,
        TwitchPopout = 1,
        CustomURL = 2
    }

    public class WindowSettings
    {
        public int ChatType { get; set; }
        public string Title { get; set; }
        public string Username { get; set; }
        public string URL { get; set; }
        public bool ChatFade { get; set; }
        public string FadeTime { get; set; }
        public bool ShowBotActivity { get; set; }
        public string ChatNotificationSound { get; set; }
        public int Theme { get; set; }
        public string CustomCSS { get; set; }
        public string TwitchPopoutCSS { get; set; }
        public bool AutoHideBorders { get; set; }
        public bool EnableTrayIcon { get; set; }
        public bool ConfirmClose { get; set; }
        public bool HideTaskbarIcon { get; set; }
        public bool AllowInteraction { get; set; }
    }

    public static class CustomCSS_Defaults
    {
        public static string TwitchPopoutChat = @"body { background-color: rgba(0,0,0,0.1) !important; }
.chat-input { display:none; }
.stream-chat .stream-chat-header { display:none; background-color: rgba(0,0,0,0) !important; color:white !important; }
.chat-room__notifcations { display:none; }
.tw-z-default { display:none; }
.tw-flex { background-color: rgba(0,0,0,0) !important; }
.tw-root { background-color: rgba(0,0,0,0) !important; }
.tw-root--theme-dark { background-color: rgba(0,0,0,0) !important; }
.stream-chat { background-color: rgba(0,0,0,0) !important; }
.chat-room { background-color: rgba(0,0,0,0) !important; }
.chat-list { background-color: rgba(0,0,0,0) !important; }
.scrollable-area { background-color: rgba(0,0,0,0) !important; color: white; }
";
        public static string WebCaptioner = @"body { background-color: rgba(0,0,0,0.1) !important; }
.transcript { background-color: rgba(0,0,0,0) !important; margin-bottom: -1em !important; }
.bg-dark { background-color: rgba(0,0,0,0) !important; }";
    }
}
