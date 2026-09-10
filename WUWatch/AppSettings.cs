using System.Text.Json;

namespace WUWatch;

public sealed class AppSettings
{
    public int SampleIntervalSeconds { get; set; } = 1;
    public int WarningAverageSeconds { get; set; } = 30;
    public int DiskShowPercent { get; set; } = 80;
    public int CpuShowPercent { get; set; } = 85;
    public int DiskHidePercent { get; set; } = 50;
    public int CpuHidePercent { get; set; } = 60;
    public int WarningHideQuietMinutes { get; set; } = 15;
    public int ArmedEpisodeHours { get; set; } = 8;
    public List<int> RelevantWindowsUpdateEventIds { get; set; } = [41];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string GetSettingsPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "WUWatch", "settings.json");
    }

    public static AppSettings Load()
    {
        string path = GetSettingsPath();

        if (!File.Exists(path))
        {
            var defaults = new AppSettings();
            defaults.Save();
            return defaults;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        string path = GetSettingsPath();
        string? dir = Path.GetDirectoryName(path);

        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json);
    }
}
