using System;
using Godot;
using NasaValley.Domain;

namespace NasaValley.Rendering;

/// <summary>
/// Static terrain. Ported from the original procedurally generated ground canvas.
/// Drawn once; Godot keeps the command buffer so it costs nothing per frame.
/// </summary>
public partial class GroundLayer : Node2D
{
    public override void _Draw()
    {
        var b = BalanceConfig.Default;
        var cw = b.ViewWidth;
        var ch = b.ViewHeight;
        var rng = new XorShiftRandom(11);

        var palette = new[]
        {
            Color.FromHtml("#94452a"),
            Color.FromHtml("#9b4a2d"),
            Color.FromHtml("#a24f30"),
            Color.FromHtml("#a85634"),
            Color.FromHtml("#b05c38"),
        };

        for (var y = 0; y < b.GridHeight * 2; y++)
        for (var x = 0; x < b.GridWidth * 2; x++)
        {
            var n = Mathf.Sin(x * 0.33f) + Mathf.Cos(y * 0.41f) + Mathf.Sin((x + y) * 0.19f);
            var i = (int)Mathf.Floor((n + 3) / 6 * palette.Length);
            if (rng.NextDouble() < 0.2) i += 1;
            i = Math.Clamp(i, 0, palette.Length - 1);
            WorldDraw.Fill(this, x * 16, y * 16, 16, 16, palette[i]);
        }

        for (var i = 0; i < 700; i++)
        {
            var color = rng.NextDouble() < 0.5 ? Color.FromHtml("#7b3922") : Color.FromHtml("#b9653f");
            var size = rng.NextDouble() < 0.8 ? 2 : 4;
            WorldDraw.Fill(this,
                Mathf.Floor((float)(rng.NextDouble() * cw / 2)) * 2,
                Mathf.Floor((float)(rng.NextDouble() * ch / 2)) * 2,
                size, size, color);
        }

        var rims = new[]
        {
            (430f, 40f, 70f, 34f),
            (110f, 250f, 54f, 26f),
            (640f, 270f, 62f, 30f),
        };

        foreach (var (cx, cy, rx, ry) in rims)
        {
            EllipseArc(this, cx, cy, rx, ry, 0, 6.3f, new Color(70 / 255f, 28 / 255f, 16 / 255f, 0.4f), 5);
            EllipseArc(this, cx - 2, cy - 3, rx - 4, ry - 3, 3.4f, 5.9f, new Color(200 / 255f, 110 / 255f, 70 / 255f, 0.25f), 2);
        }

        for (var i = 0; i < 46; i++)
        {
            var rx = Mathf.Floor((float)(rng.NextDouble() * cw));
            var ry = Mathf.Floor((float)(rng.NextDouble() * ch));
            var rw = 6 + Mathf.Floor((float)(rng.NextDouble() * 10));
            var rh = 4 + Mathf.Floor((float)(rng.NextDouble() * 6));
            WorldDraw.Fill(this, rx, ry + rh - 2, rw, 3, Color.FromHtml("#5f2c1b"));
            WorldDraw.Fill(this, rx, ry, rw, rh - 1, Color.FromHtml("#74382a"));
            WorldDraw.Fill(this, rx + 1, ry, rw - 3, 2, Color.FromHtml("#8f4a37"));
        }
    }

    private static void EllipseArc(CanvasItem c, float cx, float cy, float rx, float ry,
        float start, float end, Color color, float width)
    {
        const int segments = 48;
        var points = new Vector2[segments + 1];
        for (var i = 0; i <= segments; i++)
        {
            var a = Mathf.Lerp(start, end, i / (float)segments);
            points[i] = new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry);
        }
        c.DrawPolyline(points, color, width, true);
    }
}
