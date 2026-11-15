using System.Collections.Generic;
using Godot;

namespace NasaValley.Core;

/// <summary>
/// Central SFX registry. Streams are loaded once by <see cref="AudioDirector"/> at
/// startup; any system can fire one-shots through <see cref="Play"/>.
/// </summary>
public static class AudioSfx
{
    private static readonly Dictionary<string, AudioStream> Streams = new();
    private static Node? _owner;

    /// <summary>Registers a named one-shot stream. Called by the director on boot.</summary>
    public static void Register(string key, string path, Node owner)
    {
        var stream = GD.Load<AudioStream>(path);
        if (stream != null) Streams[key] = stream;
        _owner = owner;
    }

    /// <summary>Fires a named one-shot if it exists and the owning director is alive.</summary>
    public static void Play(string key, float volumeDb = 0f)
    {
        if (_owner == null || !GodotObject.IsInstanceValid(_owner)) return;
        if (!Streams.TryGetValue(key, out var stream)) return;

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = volumeDb,
            Bus = "Master",
        };
        _owner.AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
    }
}
