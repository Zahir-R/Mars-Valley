using Godot;
using NasaValley.Domain;

namespace NasaValley.Rendering;

/// <summary>
/// Static terrain. Baked once at startup from the real 96x56 biome grid baked from
/// MOLA/TES/Odyssey/PSG data by <c>tools/mars_data/generate.py</c>. Drawn once as a
/// nearest-filtered texture over the whole world; Godot keeps the command buffer so it
/// costs nothing per frame.
/// </summary>
public partial class GroundLayer : Node2D
{
    private ImageTexture? _texture;

    public override void _Ready()
    {
        var image = Image.CreateEmpty(GeneratedBiomes.Width, GeneratedBiomes.Height, false, Image.Format.Rgba8);
        for (var y = 0; y < GeneratedBiomes.Height; y++)
        for (var x = 0; x < GeneratedBiomes.Width; x++)
        {
            var color = Color.FromHtml(GeneratedBiomes.Get(GeneratedBiomes.Grid[y * GeneratedBiomes.Width + x]).Color);
            // Cheap deterministic per-cell noise to avoid a flat look.
            var hash = (uint)(x * 73856093 ^ y * 19349663);
            var noise = ((hash & 0xffff) / 65535f - 0.5f) * 0.10f;
            var shaded = color.Lightened(noise);
            image.SetPixel(x, y, shaded);
        }

        _texture = ImageTexture.CreateFromImage(image);
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        if (_texture == null) return;
        DrawTextureRect(_texture, new Rect2(0, 0, GeneratedBiomes.WorldWidth, GeneratedBiomes.WorldHeight), false);
    }
}
