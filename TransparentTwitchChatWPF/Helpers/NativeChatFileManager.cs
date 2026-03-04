#nullable enable
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TransparentTwitchChatWPF.Helpers;

/// <summary>
/// Manages the NativeChat overlay web files on disk.
/// Embeds a zip of the built web assets in the exe and extracts them when they are
/// missing, deleted, or outdated. Also supports restoring defaults and provides
/// the extraction path used by the future remote-update download flow.
/// </summary>
public class NativeChatFileManager
{
    // Logical name set by <LogicalName> in the .csproj EmbeddedResource item.
    private const string EmbeddedZipResourceName =
        "TransparentTwitchChatWPF.Resources.native-chat.zip";

    private const string VersionEntryName = "version.json";

    private readonly ILogger<NativeChatFileManager> _logger;

    public NativeChatFileManager(ILogger<NativeChatFileManager> logger)
    {
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called once on startup. Extracts the embedded zip if the overlay files are
    /// missing or the embedded version is newer than the installed version.
    /// Returns true if an extraction was performed.
    /// </summary>
    public bool EnsureFilesAreUpToDate()
    {
        bool filesExist = OverlayPathHelper.DoesOverlayExist("native-chat");
        string installedVersion = App.Settings.GeneralSettings.NativeChatVersion;
        string? embeddedVersion = GetEmbeddedVersion();

        if (embeddedVersion == null)
        {
            _logger.LogWarning(
                "Could not read version from embedded NativeChat zip. Skipping extraction check.");
            return false;
        }

        string reason;

        if (!filesExist)
        {
            reason = "overlay directory is missing";
        }
        else if (string.IsNullOrEmpty(installedVersion))
        {
            reason = "no installed version recorded";
        }
        else if (IsNewerVersion(embeddedVersion, installedVersion))
        {
            reason = $"embedded version {embeddedVersion} > installed {installedVersion}";
        }
        else
        {
            _logger.LogInformation(
                "NativeChat files are up to date (version {Version}).", installedVersion);
            return false;
        }

        _logger.LogInformation("Extracting NativeChat files: {Reason}.", reason);
        string overlayFolder = OverlayPathHelper.GetNativeChatPath();
        ExtractEmbeddedZip(overlayFolder);
        SaveInstalledVersion(embeddedVersion);
        return true;
    }

    /// <summary>
    /// Reads the version string from the embedded zip without fully extracting it.
    /// Returns null if the resource or version.json entry cannot be found or parsed.
    /// </summary>
    public string? GetEmbeddedVersion()
    {
        using var stream = OpenEmbeddedZipStream();
        if (stream == null) return null;
        return ReadVersionFromZipStream(stream);
    }

    /// <summary>
    /// Forces a full re-extraction of all files from the embedded zip, overwriting
    /// anything currently on disk.
    /// </summary>
    public void ForceRestoreDefaults()
    {
        string overlayFolder = OverlayPathHelper.GetNativeChatPath();
        string? embeddedVersion = GetEmbeddedVersion();

        _logger.LogInformation("Force-restoring NativeChat defaults to: {Path}", overlayFolder);
        ExtractEmbeddedZip(overlayFolder);

        if (embeddedVersion != null)
            SaveInstalledVersion(embeddedVersion);
    }

    /// <summary>
    /// Reads the version string from the root version.json inside any zip stream.
    /// Reused by the future remote-update download flow.
    /// Returns null if version.json is absent or unparseable.
    /// </summary>
    public string? ReadVersionFromZipStream(Stream zipStream)
    {
        try
        {
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
            var entry = archive.Entries.FirstOrDefault(e =>
                string.Equals(e.FullName, VersionEntryName, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                _logger.LogWarning("version.json not found in NativeChat zip.");
                return null;
            }

            using var reader = new StreamReader(entry.Open());
            string json = reader.ReadToEnd();
            var info = JsonSerializer.Deserialize<VersionInfo>(json);
            return info?.Version;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read version.json from zip stream.");
            return null;
        }
    }

    /// <summary>
    /// Extracts the zip at <paramref name="zipFilePath"/> into <paramref name="destinationFolder"/>,
    /// clearing the destination first. Used by the future remote-update download flow.
    /// </summary>
    public void ExtractFromZipFile(string zipFilePath, string destinationFolder)
    {
        if (!File.Exists(zipFilePath))
            throw new FileNotFoundException("Zip file not found.", zipFilePath);

        using var stream = File.OpenRead(zipFilePath);
        ExtractZipStream(stream, destinationFolder);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private Stream? OpenEmbeddedZipStream()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(EmbeddedZipResourceName);
        if (stream == null)
        {
            _logger.LogError(
                "Embedded resource '{Name}' not found. Available resources: {All}",
                EmbeddedZipResourceName,
                string.Join(", ", assembly.GetManifestResourceNames()));
        }
        return stream;
    }

    private void ExtractEmbeddedZip(string destinationFolder)
    {
        using var stream = OpenEmbeddedZipStream();
        if (stream == null)
            throw new InvalidOperationException(
                "Embedded NativeChat zip resource not found in assembly.");
        ExtractZipStream(stream, destinationFolder);
    }

    private void ExtractZipStream(Stream zipStream, string destinationFolder)
    {
        // Always do a full overwrite. The webpack build outputs content-hashed filenames
        // (e.g. main.a3f819d.js), so leaving stale files from an older build alongside
        // new files would waste disk space and could break the overlay.
        // The directory is entirely managed by this system, so a full wipe is safe.
        if (Directory.Exists(destinationFolder))
        {
            // Delete contents but preserve the directory entry itself.
            // Deleting the root directory can cause issues when WebView2 holds
            // an open handle to it via the virtual host mapping.
            foreach (string file in Directory.EnumerateFiles(
                         destinationFolder, "*", SearchOption.AllDirectories))
            {
                File.Delete(file);
            }
            foreach (string dir in Directory.EnumerateDirectories(destinationFolder))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        else
        {
            Directory.CreateDirectory(destinationFolder);
        }

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        foreach (var entry in archive.Entries)
        {
            // Skip directories and the version metadata file.
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
                continue;
            if (string.Equals(entry.FullName, VersionEntryName, StringComparison.OrdinalIgnoreCase))
                continue;

            string entryPath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
            string fullPath = Path.Combine(destinationFolder, entryPath);

            string? parentDir = Path.GetDirectoryName(fullPath);
            if (parentDir != null && !Directory.Exists(parentDir))
                Directory.CreateDirectory(parentDir);

            using var entryStream = entry.Open();
            using var destStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            entryStream.CopyTo(destStream);
        }

        _logger.LogInformation("Extracted NativeChat files to: {Path}", destinationFolder);
    }

    private void SaveInstalledVersion(string version)
    {
        App.Settings.GeneralSettings.NativeChatVersion = version;
        App.Settings.Persist();
    }

    /// <summary>
    /// Returns true if <paramref name="candidate"/> is strictly newer than <paramref name="current"/>.
    /// Uses System.Version for correct numeric comparison (1.10.0 > 1.9.0, unlike string compare).
    /// Falls back to ordinal string comparison if either value cannot be parsed.
    /// </summary>
    private bool IsNewerVersion(string candidate, string current)
    {
        if (Version.TryParse(candidate, out var candidateVer) &&
            Version.TryParse(current, out var currentVer))
        {
            return candidateVer > currentVer;
        }

        _logger.LogWarning(
            "Could not parse version strings as System.Version. " +
            "Candidate='{C}', Current='{Cu}'. Falling back to string comparison.",
            candidate, current);

        return string.Compare(candidate, current, StringComparison.OrdinalIgnoreCase) > 0;
    }

    private record VersionInfo([property: JsonPropertyName("version")] string Version);
}
