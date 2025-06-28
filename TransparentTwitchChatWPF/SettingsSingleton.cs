
using NuGet.Versioning;
using Velopack;
using Velopack.Locators;

namespace TransparentTwitchChatWPF
{
    public sealed class SettingsSingleton
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
    }
}
