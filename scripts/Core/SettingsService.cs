using Godot;
using NasaValley.Domain;

namespace NasaValley.Core;

/// <summary>Persistent settings (master volume + key bindings) loaded at startup.</summary>
public static class SettingsService
{
    private const string Path = "user://settings.json";

    public static AppSettingsData Current { get; } = Load();

    public static float GetVolume() => Current.MasterVolume;

    public static int GetBinding(string action, int fallback) =>
        Current.Bindings.TryGetValue(action, out var code) && code != 0 ? code : fallback;

    public static void SetVolume(float value)
    {
        Current.MasterVolume = Mathf.Clamp(value, 0f, 1f);
        ApplyAudio();
        Save();
    }

    public static void SetBinding(string action, int keyCode)
    {
        Current.Bindings[action] = keyCode;
        NasaValley.Input.InputAdapter.RegisterActions(Current);
        Save();
    }

    public static void ResetBindings()
    {
        Current.Bindings.Clear();
        NasaValley.Input.InputAdapter.RegisterActions(Current);
        Save();
    }

    /// <summary>Applies the master volume to the engine's Master bus at startup and on change.</summary>
    public static void ApplyAudio()
    {
        var volume = Mathf.Clamp(Current.MasterVolume, 0f, 1f);
        AudioServer.SetBusVolumeDb(0, volume <= 0f ? -80f : Mathf.LinearToDb(volume));
        AudioServer.SetBusMute(0, volume <= 0f);
    }

    public static void Save()
    {
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        if (file is null) return;
        file.StoreString(SettingsSerializer.Serialize(Current));
    }

    private static AppSettingsData Load()
    {
        if (!FileAccess.FileExists(Path)) return new AppSettingsData();

        try
        {
            using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
            if (file is null) return new AppSettingsData();
            return SettingsSerializer.Deserialize(file.GetAsText()) ?? new AppSettingsData();
        }
        catch (System.Exception)
        {
            return new AppSettingsData();
        }
    }
}
