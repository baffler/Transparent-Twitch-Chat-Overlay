using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF.Utils;

public static class Growl
{
    public static event Action<string> GrowlMessageRequested;

    public static void Info(string message)
    {
        GrowlMessageRequested?.Invoke("[INFO] " + message);
    }

    public static void Warning(string message)
    {
        GrowlMessageRequested?.Invoke("[WARNING] " + message);
    }

    public static void Error(string message)
    {
        GrowlMessageRequested?.Invoke("[ERROR] " + message);
    }
}
