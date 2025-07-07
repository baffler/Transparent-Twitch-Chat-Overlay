using System;
using System.IO;
using System.Text.RegularExpressions;
using TwitchLib.Api.Helix;

namespace TransparentTwitchChatWPF.Helpers
{
    internal static class LocalHtmlHelper
    {   
        private static readonly string BrowserBasePath = Path.Combine(
            AppContext.BaseDirectory, "browser");
        
        public static string GetIndexHtmlPath()
        {
            return Path.Combine(BrowserBasePath, "index.html");
        }
        
        public static string GetJChatIndexPath()
        {
            return Path.Combine(BrowserBasePath, "jchat.html");
        }
    }

    /// <summary>
    /// Provides direct paths to overlay HTML files located within the application's "browser" directory.
    /// This helper assumes the application has read access to its installation folder.
    /// </summary>
    internal static class OverlayPathHelper
    {
        /// <summary>
        /// The base path to the "browser" directory within the application's folder.
        /// </summary>
        private static readonly string BrowserBasePath = Path.Combine(AppContext.BaseDirectory, "browser");

        /// <summary>
        /// Gets the full, absolute path to the settings page for the Native Chat overlay.
        /// </summary>
        /// <returns>The full path to native-chat\index.html.</returns>
        public static string GetNativeChatSettingsIndexFilePath()
        {
            return Path.Combine(BrowserBasePath, "overlays", "native-chat", "index.html");
        }

        /// <summary>
        /// Gets the full, absolute path to the Native Chat overlay.
        /// </summary>
        /// <returns>The full path to native-chat.</returns>
        public static string GetNativeChatPath()
        {
            return Path.Combine(BrowserBasePath, "overlays", "native-chat");
        }

        /// <summary>
        /// Gets the full, absolute path to the actual chat overlay for the Native Chat overlay.
        /// </summary>
        /// <returns>The full path to native-chat\v2\index.html.</returns>
        public static string GetNativeChatOverlayPath()
        {
            return Path.Combine(BrowserBasePath, "overlays", "native-chat", "v2", "index.html");
        }

        /// <summary>
        /// Checks if a given overlay's base directory exists.
        /// </summary>
        /// <param name="overlayId">The ID (folder name) of the overlay.</param>
        /// <returns>True if the directory exists, otherwise false.</returns>
        public static bool DoesOverlayExist(string overlayId)
        {
            if (string.IsNullOrEmpty(overlayId)) return false;

            string overlayPath = Path.Combine(BrowserBasePath, "overlays", overlayId);
            return Directory.Exists(overlayPath);
        }
    }
}
