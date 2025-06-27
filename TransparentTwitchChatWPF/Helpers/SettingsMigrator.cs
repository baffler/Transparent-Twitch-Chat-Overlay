using Jot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TransparentTwitchChatWPF.Helpers;

public static class SettingsMigrator
{
    // A simple class to represent an item in the OLD settings file
    private class OldSettingItem
    {
        public string Name { get; set; }
        public JToken Value { get; set; } // Use JToken to handle any value type
    }

    public static GeneralSettings AttemptMigration(Tracker tracker)
    {
        string DataFolder = (tracker.Store as Jot.Storage.JsonFileStore).FolderPath;

        // Jot determines the file path from the tracked object's ID.
        // Old format was flat, likely using a different ID or default name.
        // New format is based on the 'AppSettings' class name.
        string oldSettingsPath = Path.Combine(DataFolder, "GeneralSettings_MainWindow.json");
        string newSettingsPath = Path.Combine(DataFolder, "AppSettings.json");

        // 1. If new settings already exist, do nothing.
        if (File.Exists(newSettingsPath))
        {
            return null; // Indicates no migration is needed
        }

        // 2. If old settings don't exist, do nothing.
        if (!File.Exists(oldSettingsPath))
        {
            return null; // No old settings to migrate from
        }

        try
        {
            // 3. Read the old settings file
            string json = File.ReadAllText(oldSettingsPath);
            var oldSettingsList = JsonConvert.DeserializeObject<List<OldSettingItem>>(json);

            if (oldSettingsList == null) return null;

            // 4. Map old values to the new GeneralSettings object
            var migratedSettings = new GeneralSettings();
            var oldSettingsDict = oldSettingsList.ToDictionary(s => s.Name, s => s.Value);

            // Go through each property of your new settings class
            foreach (var prop in typeof(GeneralSettings).GetProperties())
            {
                if (oldSettingsDict.TryGetValue(prop.Name, out JToken value))
                {
                    try
                    {
                        // Use JToken.ToObject to convert the value to the property's type
                        object convertedValue = value.ToObject(prop.PropertyType);
                        prop.SetValue(migratedSettings, convertedValue);
                    }
                    catch
                    {
                        // Handle cases where a type might have changed, e.g., string to int.
                        // For now just skip properties that fail to convert.
                    }
                }
            }

            // could rename the old file to prevent this from ever running again
            // File.Move(oldSettingsPath, oldSettingsPath + ".migrated");

            return migratedSettings;
        }
        catch (System.Exception ex)
        {
            // Log the error if something goes wrong during migration
            Debug.WriteLine($"Error during settings migration: {ex.Message}");
            return null; // Failed, so we'll use default settings
        }
    }
}
