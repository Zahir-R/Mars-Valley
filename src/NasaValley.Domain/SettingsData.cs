using System.Text.Json;

namespace NasaValley.Domain;

public sealed class AppSettingsData
{
    public int Version { get; set; } = SettingsSerializer.CurrentVersion;
    public float MasterVolume { get; set; } = 0.8f;
    public Dictionary<string, int> Bindings { get; set; } = new();
}

public static class SettingsSerializer
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static string Serialize(AppSettingsData data) => JsonSerializer.Serialize(data, Options);

    public static AppSettingsData? Deserialize(string json)
    {
        try
        {
            var data = JsonSerializer.Deserialize<AppSettingsData>(json, Options);
            return data?.Version == CurrentVersion ? data : null;
        }
        catch (System.Exception)
        {
            return null;
        }
    }
}
