
namespace TransparentTwitchChatWPF
{
    public sealed class SettingsSingleton
    {
        public static string Version { 
            get 
            {
                //Version version = Assembly.GetExecutingAssembly().GetName().Version;
                return $"1.1.0";
            } 
        }
    }
}
