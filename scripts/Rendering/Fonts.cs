using Godot;

namespace NasaValley.Rendering;

/// <summary>
/// Loads the bundled fonts (IBM Plex Sans/Mono, Silkscreen) and gracefully falls
/// back to the engine default if a file is missing. Fonts are loaded dynamically
/// so the game works even before the editor imports them.
/// </summary>
public static class Fonts
{
    private static Font? _sans;
    private static Font? _mono;
    private static Font? _display;
    private static readonly System.Collections.Generic.Dictionary<(Font, int), Font> WeightCache = new();

    public static Font Sans => _sans ??= Load("res://assets/fonts/IBMPlexSans.ttf");
    public static Font Mono => _mono ??= Load("res://assets/fonts/IBMPlexMono-Regular.ttf");
    public static Font Display => _display ??= Load("res://assets/fonts/Silkscreen-Regular.ttf");

    public static Font SansWeight(int weight) => Weight(Sans, weight);
    public static Font MonoWeight(int weight) => Weight(Mono, weight);
    public static Font DisplayWeight(int weight) => Weight(Display, weight);

    private static Font Load(string path)
    {
        var font = new FontFile();
        var error = font.LoadDynamicFont(path);
        return error == Error.Ok ? font : ThemeDB.FallbackFont;
    }

    private static Font Weight(Font baseFont, int weight)
    {
        var key = (baseFont, weight);
        if (WeightCache.TryGetValue(key, out var cached)) return cached;

        var variation = new FontVariation
        {
            BaseFont = baseFont,
            VariationOpentype = new Godot.Collections.Dictionary { { "wght", weight } },
        };
        WeightCache[key] = variation;
        return variation;
    }
}
