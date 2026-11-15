using System.Collections.Generic;
using Godot;

namespace NasaValley.Rendering;

/// <summary>Loads and caches the pixel-art sprites from SpritesRenombrados2.</summary>
public static class SpriteArt
{
    private const string Structures = "res://assets/SpritesRenombrados2/Estructuras_frames/";
    private const string Minerals = "res://assets/SpritesRenombrados2/Minerales_frames/";

    private static readonly Dictionary<string, Texture2D> Cache = new();

    /// <summary>
    /// World-sprite alpha. 1 shows all interactable world art; background ground decals
    /// are hidden separately in <c>GroundLayer</c> because the player cannot interact with them.
    /// </summary>
    public static float WorldAlpha = 1f;

    public static Texture2D? Get(string fileName)
    {
        if (Cache.TryGetValue(fileName, out var cached)) return cached;

        var path = Structures + fileName;
        if (!ResourceLoader.Exists(path)) path = Minerals + fileName;
        var texture = GD.Load<Texture2D>(path);
        Cache[fileName] = texture;
        return texture;
    }

    public static Image? GetImage(string fileName) => Get(fileName)?.GetImage();

    /// <summary>Draws a sprite centred on <paramref name="center"/>, scaled to <paramref name="targetHeight"/>.</summary>
    public static void DrawSprite(CanvasItem c, Texture2D? texture, Vector2 center, float targetHeight, Color? modulate = null)
    {
        if (texture == null || WorldAlpha <= 0f) return;
        var size = texture.GetSize();
        if (size.Y <= 0) return;
        var scale = targetHeight / size.Y;
        var drawSize = size * scale;
        var mod = modulate ?? Colors.White;
        mod.A *= WorldAlpha;
        c.DrawTextureRect(texture, new Rect2(center - drawSize / 2f, drawSize), false, mod);
    }
}
