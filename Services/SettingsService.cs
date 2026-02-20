using System;
using System.IO;
using System.Text.Json;

namespace FocusPanel.Services;

public class SettingsService
{
    private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    
    public AppSettings CurrentSettings { get; private set; }

    public SettingsService()
    {
        LoadSettings();
    }

    public void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                string json = File.ReadAllText(SettingsFile);
                CurrentSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                CurrentSettings = new AppSettings();
            }
        }
        catch
        {
            CurrentSettings = new AppSettings();
        }
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonSerializer.Serialize(CurrentSettings);
            File.WriteAllText(SettingsFile, json);
        }
        catch { }
    }
}

public class AppSettings
{
    public string ImageSavePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
    public string GlobalCustomFieldsJson { get; set; } = string.Empty;
}
