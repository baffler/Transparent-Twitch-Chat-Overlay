using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jot;

namespace TransparentTwitchChatWPF
{

    static class Utilities
    {
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }

    public sealed class KapChat
    {
        public static readonly List<string> Themes = new List<string> {
            string.Empty,
            "bttv_blackchat",
            "bttv_dark",
            "bttv_light",
            "dark",
            "light",
            "s0n0s_1080",
            "s0n0s_1440"
        };
    }

    //public sealed class KapChatTheme
    //{
    //    private readonly string name;
    //    private readonly int value;

    //    public static readonly KapChatTheme NONE = new KapChatTheme(0, string.Empty);
    //    public static readonly KapChatTheme BTTV_BLACKCHAT = new KapChatTheme(1, "bttv_blackchat");
    //    public static readonly KapChatTheme BTTV_DARK = new KapChatTheme(2, "bttv_dark");
    //    public static readonly KapChatTheme BTTV_LIGHT = new KapChatTheme(3, "bttv_light");
    //    public static readonly KapChatTheme DARK = new KapChatTheme(4, "dark");
    //    public static readonly KapChatTheme LIGHT = new KapChatTheme(5, "light");
    //    public static readonly KapChatTheme S0N0S_1080 = new KapChatTheme(6, "s0n0s_1080");
    //    public static readonly KapChatTheme S0N0S_1440 = new KapChatTheme(7, "s0n0s_1440");

    //    private KapChatTheme(int value, string name)
    //    {
    //        this.name = name;
    //        this.value = value;
    //    }

    //    public override string ToString()
    //    {
    //        return name;
    //    }
    //}

    static class Services
    {
        public static StateTracker Tracker = new StateTracker();
    }
}
