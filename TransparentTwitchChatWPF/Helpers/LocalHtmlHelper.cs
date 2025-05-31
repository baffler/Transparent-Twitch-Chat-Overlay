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
        private static readonly string AppDataBrowserPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TransparentTwitchChatWPF", "browser");
        
        private static readonly string SourceBrowserPath = Path.Combine(
            AppContext.BaseDirectory, "browser");
        
        /// <summary>
        /// Copies or updates the bundled "browser" folder to the user's AppData.
        /// This method should be called at application startup to ensure the
        /// local files are the latest versions from the application bundle.
        /// It will overwrite existing files in the destination.
        /// </summary>
        public static void EnsureLocalBrowserFiles()
        {
            // Call CopyDirectory, ensuring overwrite is enabled
            CopyDirectory(SourceBrowserPath, AppDataBrowserPath, true);
        }
        
        /// <summary>
        /// Gets the full path to the local "index.html" file in AppData.
        /// Assumes EnsureLocalBrowserFiles() has been called (e.g., at application startup).
        /// </summary>
        /// <returns>The full path to index.html.</returns>
        public static string GetIndexHtmlPath()
        {
            return Path.Combine(AppDataBrowserPath, "index.html");
        }
        
        /// <summary>
        /// Gets the full path to the local "jchat.html" file in AppData.
        /// Assumes EnsureLocalBrowserFiles() has been called (e.g., at application startup).
        /// </summary>
        /// <returns>The full path to jchat.html.</returns>
        public static string GetJChatIndexPath()
        {
            return Path.Combine(AppDataBrowserPath, "jchat.html");
        }
        

        /// <summary>
        /// Recursively copies a directory from a source to a destination.
        /// </summary>
        /// <param name="sourceDir">The source directory path.</param>
        /// <param name="destDir">The destination directory path.</param>
        /// <param name="overwriteFiles">If true, existing files in the destination will be overwritten.</param>
        private static void CopyDirectory(string sourceDir, string destDir, bool overwriteFiles)
        {
            // Check if the source directory exists
            if (!Directory.Exists(sourceDir))
            {
                Console.WriteLine($"Warning: Source directory not found: '{sourceDir}'. Nothing will be copied.");
                // Consider if you need to clean up destDir if sourceDir is missing
                return;
            }

            // Ensure the destination directory exists
            Directory.CreateDirectory(destDir);

            // Copy all files from the source to the destination
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                try
                {
                    File.Copy(file, destFile, overwriteFiles);
                }
                catch (IOException ex)
                {
                    // Log or handle errors, e.g., file might be in use
                    Console.WriteLine($"Error copying file '{Path.GetFileName(file)}' to '{destFile}': {ex.Message}");
                }
            }

            // Recursively copy subdirectories
            foreach (var sourceSubDir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(sourceSubDir));
                CopyDirectory(sourceSubDir, destSubDir, overwriteFiles);
            }
        }
    }
}
