using Godot;

namespace NasaValley.Rendering;

/// <summary>
/// Central colour + font registry. Mirrors the CSS custom properties from the
/// original page so a single edit keeps the whole game consistent.
/// </summary>
public static class Palette
{
    public static readonly Color Bg = Color.FromHtml("#150f0d");
    public static readonly Color Panel = Color.FromHtml("#1f1613");
    public static readonly Color Panel2 = Color.FromHtml("#2a1d18");
    public static readonly Color Line = Color.FromHtml("#3d2a22");
    public static readonly Color Dust = Color.FromHtml("#d2693c");
    public static readonly Color DustDeep = Color.FromHtml("#7a3d28");
    public static readonly Color Suit = Color.FromHtml("#ece4d3");
    public static readonly Color Muted = Color.FromHtml("#a8968a");
    public static readonly Color O2 = Color.FromHtml("#5fd8c4");
    public static readonly Color Energy = Color.FromHtml("#f2b84b");
    public static readonly Color Food = Color.FromHtml("#9ccf5a");
    public static readonly Color Health = Color.FromHtml("#ef5b4b");
    public static readonly Color Water = Color.FromHtml("#4aa3e0");
    public static readonly Color Ink = Color.FromHtml("#1a0f0b");

    public static Color FromHex(string hex) => Color.FromHtml(hex);
}
