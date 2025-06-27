using System;
using System.Collections.Generic;

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
}
