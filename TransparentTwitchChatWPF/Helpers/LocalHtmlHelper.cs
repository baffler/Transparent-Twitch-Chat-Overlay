using System;
using System.IO;

namespace TransparentTwitchChatWPF.Helpers
{
    /// <summary>
    /// Copies the bundled “browser” folder to the user’s AppData once,
    /// then returns the full path to index.html ready for WebView2.
    /// </summary>
    internal static class LocalHtmlHelper
    {   
        private static readonly string SourceBrowserPath = Path.Combine(
            AppContext.BaseDirectory, "browser");
        
        /// <summary>
        /// Gets the full path to the local "index.html" file in AppData.
        /// Assumes EnsureLocalBrowserFiles() has been called (e.g., at application startup).
        /// </summary>
        /// <returns>The full path to index.html.</returns>
        public static string GetIndexHtmlPath()
        {
            return Path.Combine(SourceBrowserPath, "index.html");
        }
        
        /// <summary>
        /// Gets the full path to the local "jchat.html" file in AppData.
        /// Assumes EnsureLocalBrowserFiles() has been called (e.g., at application startup).
        /// </summary>
        /// <returns>The full path to jchat.html.</returns>
        public static string GetJChatIndexPath()
        {
            return Path.Combine(SourceBrowserPath, "jchat.html");
        }
    }
}
