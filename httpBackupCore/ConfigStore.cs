using System;
using System.IO;
using System.Text.Json;

namespace httpBackupCore;

public static class ConfigStore
{
    private const string CompanyFolder = "HttpBackup";
    private const string ConfigFileName = "config.json";

    public static string GetConfigPath()
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var dir = Path.Combine(programData, CompanyFolder);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, ConfigFileName);
    }

    public static AppConfig LoadOrCreateDefault()
    {
        var path = GetConfigPath();
        if (!File.Exists(path))
        {
            var cfg = new AppConfig();
            Save(cfg);
            return cfg;
        }

        var json = File.ReadAllText(path);
        var cfgLoaded = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return cfgLoaded ?? new AppConfig();
    }

    public static void Save(AppConfig config)
    {
        var path = GetConfigPath();
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
    }
}
