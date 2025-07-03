using NuGet.Versioning;
using Velopack.Locators;

namespace TransparentTwitchChatWPF;

public static class AppInfo
{
    public static string Version { 
        get 
        {
            IVelopackLocator locator = VelopackLocator.Current;
            SemanticVersion currentVersion = locator.CurrentlyInstalledVersion;

            if (currentVersion != null)
            {
                return currentVersion.ToString();
            }
            else
            {
                return $"1.1.x";
            }
        } 
    }

    public static string ContentDir
    {
        get
        {
            IVelopackLocator locator = VelopackLocator.Current;
            if (locator == null)
            {
                return string.Empty;
            }
            return locator.AppContentDir;
        }
    }

    public static bool IsPortable {
        get {
            IVelopackLocator locator = VelopackLocator.Current;
            if (locator == null)
            {
                return false;
            }
            return locator.IsPortable;
        }
    }
}
