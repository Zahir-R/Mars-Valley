using System.Collections.Generic;
using Godot;
using NasaValley.Domain;

namespace NasaValley.Rendering;

/// <summary>
/// Static terrain. Baked once at startup from the real 96x56 biome grid baked from
/// MOLA/TES/Odyssey/PSG data by <c>tools/mars_data/generate.py</c>. Synthesised at
/// full resolution with warped cell boundaries, fine grain and coarse mottling so the
/// ground reads as crisp regolith instead of blurred biome blocks.
/// </summary>
public partial class GroundLayer : Node2D
{
    private const int Cell = 32;

    private ImageTexture? _texture;

    public override void _Ready()
    {
        var image = Image.CreateEmpty(GeneratedBiomes.WorldWidth, GeneratedBiomes.WorldHeight, false, Image.Format.Rgba8);

        var cellColors = new Color[GeneratedBiomes.Width * GeneratedBiomes.Height];
        for (var i = 0; i < cellColors.Length; i++)
        {
            cellColors[i] = Color.FromHtml(GeneratedBiomes.Get(GeneratedBiomes.Grid[i]).Color);
        }

        for (var y = 0; y < GeneratedBiomes.WorldHeight; y++)
        for (var x = 0; x < GeneratedBiomes.WorldWidth; x++)
        {
            // Gently warp the sampling coordinates so biome borders are organic, not 32px blocks.
            var wx = x + (Hash01(x / 48, y / 48, 11) - 0.5f) * 26f;
            var wy = y + (Hash01(x / 48, y / 48, 29) - 0.5f) * 26f;
            var cx = Mathf.Clamp((int)(wx / Cell), 0, GeneratedBiomes.Width - 1);
            var cy = Mathf.Clamp((int)(wy / Cell), 0, GeneratedBiomes.Height - 1);
            var pixel = cellColors[cy * GeneratedBiomes.Width + cx];

            // Coarse mottling (≈9px blotches).
            var coarse = (Hash01(x / 9, y / 9, 41) - 0.5f) * 0.15f;
            pixel = Shade(pixel, coarse);

            // Fine 1px grain.
            var fine = (Hash01(x, y, 13) - 0.5f) * 0.22f;
            pixel = Shade(pixel, fine);

            // Occasional dark scuff.
            if (Hash01(x, y, 57) < 0.10f) pixel = pixel.Darkened(0.15f);

            image.SetPixel(x, y, pixel);
        }

        // Sparse, muted ground strata only; nothing here reads as a gatherable ore.
        var rng = new RandomNumberGenerator { Seed = 20261114 };
        ScatterSheet(image, rng,
            new[] { "sedimento_estratificado_oxidado_035.png", "sedimento_estratificado_oxidado_043.png",
                    "sedimento_estratificado_oxidado_044.png", "sedimento_estratificado_oxidado_045.png",
                    "sedimento_estratificado_oxidado_046.png", "sedimento_estratificado_oxidado_057.png",
                    "sedimento_estratificado_oxidado_058.png", "sedimento_estratificado_oxidado_066.png",
                    "sedimento_estratificado_oxidado_067.png" },
            count: 100, scale: 0.62f);

        _texture = ImageTexture.CreateFromImage(image);
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
    }

    private static float Hash01(int x, int y, int salt)
    {
        var h = (uint)(x * 374761393 + y * 668265263 + salt * 1442695041);
        h = (h ^ (h >> 13)) * 1274126177u;
        h ^= h >> 16;
        return (h & 0xffffff) / 16777215f;
    }

    private static Color Shade(Color baseColor, float amount) =>
        amount >= 0 ? baseColor.Lightened(amount) : baseColor.Darkened(-amount);

    private static void ScatterSheet(Image image, RandomNumberGenerator rng, IReadOnlyList<string> files, int count, float scale = 1f)
    {
        var sprites = new List<Image>(files.Count);
        foreach (var file in files)
        {
            var source = SpriteArt.GetImage(file);
            if (source == null) continue;
            var copy = (Image)source.Duplicate();
            if (copy.GetFormat() != Image.Format.Rgba8) copy.Convert(Image.Format.Rgba8);
            copy.Resize(Mathf.Max(8, (int)(copy.GetWidth() * scale)), Mathf.Max(8, (int)(copy.GetHeight() * scale)), Image.Interpolation.Nearest);
            sprites.Add(copy);
        }
        if (sprites.Count == 0) return;

        for (var i = 0; i < count; i++)
        {
            var sprite = sprites[rng.RandiRange(0, sprites.Count - 1)];
            var x = rng.RandiRange(0, GeneratedBiomes.WorldWidth - sprite.GetWidth());
            var y = rng.RandiRange(0, GeneratedBiomes.WorldHeight - sprite.GetHeight());
            image.BlendRect(sprite, new Rect2I(0, 0, sprite.GetWidth(), sprite.GetHeight()), new Vector2I(x, y));
        }
    }

    public override void _Draw()
    {
        if (_texture == null) return;
        DrawTextureRect(_texture, new Rect2(0, 0, GeneratedBiomes.WorldWidth, GeneratedBiomes.WorldHeight), false);
    }
}
