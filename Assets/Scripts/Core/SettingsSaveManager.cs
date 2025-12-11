using UnityEngine;
using System.IO;

public static class SettingsSaveManager
{
    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, "settings.json");

    public static void Save(SettingsData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(FilePath, json);
        Debug.Log("Settings Saved: " + FilePath);
    }

    public static SettingsData Load()
    {
        if (!File.Exists(FilePath))
        {
            Debug.Log("Settings: No save file, using defaults.");
            return null;
        }

        string json = File.ReadAllText(FilePath);
        return JsonUtility.FromJson<SettingsData>(json);
    }
}
