using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF.ChatProviders;

public class DiscoveredOverlayProvider : IChatProvider
{
    private readonly OverlayManifest _manifest;
    public DiscoveredOverlayProvider(OverlayManifest manifest)
    {
        _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
    }

    public Uri GetNavigationUri()
    {
        string fullPath = Path.Combine(_manifest.BasePath, _manifest.EntryPoints.Overlay);
        return new Uri(fullPath);
    }
}

public class OverlayManifest
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string BasePath { get; set; } // We'll add this to store its folder path
    public EntryPoints EntryPoints { get; set; }
}

public class EntryPoints
{
    public string Overlay { get; set; }
    public string Settings { get; set; }
}
