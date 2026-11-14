using System.Collections.Generic;
using Godot;
using NasaValley.Domain;

namespace NasaValley.Rendering;

/// <summary>
/// Stateless drawing helpers: a faithful port of the original canvas routines.
/// Each method receives a <see cref="CanvasItem"/> so it can be reused by any layer.
/// </summary>
public static class WorldDraw
{
    public static void Fill(CanvasItem c, float x, float y, float w, float h, Color col) =>
        c.DrawRect(new Rect2(x, y, w, h), col);

    public static void Stroke(CanvasItem c, float x, float y, float w, float h, Color col, float width = 1) =>
        c.DrawRect(new Rect2(x, y, w, h), col, false, width);

    public static void DrawAstronaut(CanvasItem c, GameState s)
    {
        var x = Mathf.Round(s.Px);
        var y = Mathf.Round(s.Py);
        var face = s.Face;
        var step = s.Moving ? Mathf.Sin((float)(s.Walk * 12)) : 0f;

        c.DrawSetTransform(new Vector2(x, y), 0, Vector2.One);

        Fill(c, -9, 11, 18, 5, new Color(0, 0, 0, 0.3f));
        var lk = Mathf.RoundToInt(step * 2);
        var suit = Palette.Suit;
        var grey = Color.FromHtml("#d9d2c2");
        var dark = Color.FromHtml("#6b5b52");

        Fill(c, -6, 6, 5, 7 - lk, grey);
        Fill(c, 1, 6, 5, 7 + lk, grey);
        Fill(c, -6, 12 - lk, 5, 3, dark);
        Fill(c, 1, 12 + lk, 5, 3, dark);

        if (face == Direction.Up)
        {
            Fill(c, -7, -4, 14, 11, suit);
            Fill(c, -8, -6, 16, 14, Color.FromHtml("#b9b2a2"));
            Fill(c, -8, 6, 16, 2, Color.FromHtml("#8c8678"));
            Fill(c, -2, -3, 4, 2, Palette.O2);
        }
        else
        {
            Fill(c, -7, -4, 14, 11, suit);
            Fill(c, -3, -1, 6, 4, Palette.Dust);
            Fill(c, -1, 0, 2, 2, Palette.O2);
        }

        Fill(c, -10, -3, 3, 9 + Mathf.RoundToInt(-step), suit);
        Fill(c, 7, -3, 3, 9 + Mathf.RoundToInt(step), suit);

        if (face is Direction.Left or Direction.Right)
        {
            Fill(c, face == Direction.Left ? 4 : -10, -6, 6, 13, Color.FromHtml("#b9b2a2"));
        }

        Fill(c, -8, -17, 16, 14, Color.FromHtml("#f4efe3"));
        Fill(c, -9, -15, 18, 10, Color.FromHtml("#f4efe3"));

        if (face != Direction.Up)
        {
            var vx = face == Direction.Left ? -7 : face == Direction.Right ? 1 : -6;
            var vw = face == Direction.Down ? 12 : 6;
            Fill(c, vx, -14, vw, 8, Color.FromHtml("#2a2230"));
            Fill(c, vx, -14, vw, 3, Color.FromHtml("#e0a23c"));
            Fill(c, vx + 1, -13, 2, 2, new Color(1, 1, 1, 0.5f));
        }
        else
        {
            Fill(c, -8, -17, 16, 3, Color.FromHtml("#d9d2c2"));
        }

        c.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    private static void Crystal(CanvasItem c, float x, float y, float w, float h, Color a, Color b)
    {
        c.DrawColoredPolygon(new[]
        {
            new Vector2(x - w / 2, y),
            new Vector2(x - w / 3, y - h),
            new Vector2(x + w / 4, y - h * 0.92f),
            new Vector2(x + w / 2, y),
        }, a);

        c.DrawColoredPolygon(new[]
        {
            new Vector2(x, y),
            new Vector2(x + w / 4, y - h * 0.92f),
            new Vector2(x + w / 2, y),
        }, b);
    }

    /// <summary>
    /// Subtle "you are here" ring under the astronaut so the player is easy to
    /// locate against the terrain. Purely a readability aid; the sprite is unchanged.
    /// </summary>
    public static void DrawPlayerMarker(CanvasItem c, GameState s, double clock)
    {
        var pulse = 0.5f + 0.5f * Mathf.Sin((float)clock * 3f);
        var color = Palette.O2;
        color.A = 0.16f + 0.2f * pulse;
        c.DrawArc(new Vector2(Mathf.Round(s.Px), Mathf.Round(s.Py) + 13), 13, 0, Mathf.Tau, 32, color, 2);
    }

    public static void DrawNode(CanvasItem c, ResourceNode n, NodeDefinition def)
    {
        var sh = n.Shake > 0 ? ((float)GD.Randf() - 0.5f) * 3 : 0;
        var col = Palette.FromHex(def.Color);

        c.DrawSetTransform(new Vector2(Mathf.Round(n.X + sh), Mathf.Round(n.Y)), 0, Vector2.One);
        Fill(c, -15, 6, 30, 6, new Color(0, 0, 0, 0.28f));

        if (n.Amount <= 0)
        {
            var faded = col;
            faded.A = 0.55f;
            Fill(c, -6, 4, 4, 3, faded);
            Fill(c, 3, 5, 3, 2, faded);
            c.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            return;
        }

        switch (def.Type)
        {
            case "ice":
                Crystal(c, -8, 8, 14, 20, Palette.FromHex("#8fd3f4"), Palette.FromHex("#4aa3e0"));
                Crystal(c, 6, 8, 12, 28, Palette.FromHex("#bfe8fa"), Palette.FromHex("#6fbbe6"));
                Crystal(c, -1, 9, 10, 14, Palette.FromHex("#d8f3ff"), Palette.FromHex("#8fd3f4"));
                Fill(c, 4, -14, 2, 5, Colors.White);
                break;
            case "iron":
                Fill(c, -14, -2, 28, 12, Color.FromHtml("#4a3a35"));
                Fill(c, -10, -8, 20, 8, Color.FromHtml("#4a3a35"));
                Fill(c, -12, -3, 24, 8, Color.FromHtml("#6d5a52"));
                Fill(c, -8, -8, 14, 5, Color.FromHtml("#6d5a52"));
                Fill(c, -6, -1, 3, 2, Color.FromHtml("#c0653a"));
                Fill(c, 4, 2, 4, 2, Color.FromHtml("#c0653a"));
                Fill(c, -2, -6, 2, 2, Color.FromHtml("#c0653a"));
                break;
            case "rock":
                Fill(c, -13, 2, 26, 10, Color.FromHtml("#5f574f"));
                Fill(c, -10, -2, 10, 8, Color.FromHtml("#7d746a"));
                Fill(c, 1, -4, 11, 9, Color.FromHtml("#8f867b"));
                Fill(c, -8, -1, 5, 4, Color.FromHtml("#a49a8e"));
                Fill(c, 4, 2, 4, 3, Color.FromHtml("#6a625a"));
                break;
            case "regolith":
                Fill(c, -15, 4, 30, 7, Color.FromHtml("#7a4a30"));
                Fill(c, -12, 0, 24, 6, Color.FromHtml("#a9704e"));
                Fill(c, -8, -3, 16, 5, Color.FromHtml("#c08a62"));
                Fill(c, -4, -5, 8, 4, Color.FromHtml("#d3a079"));
                break;
            case "basalt":
                Fill(c, -14, 3, 28, 8, Color.FromHtml("#2f3338"));
                Fill(c, -10, -8, 8, 12, Color.FromHtml("#4b525a"));
                Fill(c, -1, -12, 7, 16, Color.FromHtml("#565e67"));
                Fill(c, 7, -6, 6, 10, Color.FromHtml("#3d444b"));
                Fill(c, -8, -6, 3, 6, Color.FromHtml("#6a737d"));
                Fill(c, 0, -10, 2, 8, Color.FromHtml("#727c86"));
                break;
            default:
                Crystal(c, -7, 8, 12, 18, Palette.FromHex("#e8dcb5"), Palette.FromHex("#bfae82"));
                Crystal(c, 7, 8, 12, 24, Palette.FromHex("#f3ead0"), Palette.FromHex("#cdbd92"));
                Crystal(c, 0, 9, 9, 12, Palette.FromHex("#fff6dc"), Palette.FromHex("#e0d2a8"));
                Fill(c, 7, -12, 2, 2, Colors.White);
                break;
        }

        c.DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        for (var i = 0; i < n.Amount; i++)
        {
            Fill(c, Mathf.Round(n.X) - 9 + i * 5, Mathf.Round(n.Y) + 14, 3, 3, col);
        }
    }

    /// <summary>An interactable base station (workbench) with a soft beacon.</summary>
    public static void DrawStation(CanvasItem c, Station st, double clock)
    {
        var x = Mathf.Round(st.X);
        var y = Mathf.Round(st.Y);

        var glow = Palette.O2;
        glow.A = 0.12f + 0.10f * (0.5f + 0.5f * Mathf.Sin((float)clock * 2f));
        c.DrawCircle(new Vector2(x, y - 4), 17, glow);

        Fill(c, x - 12, y + 5, 24, 4, new Color(0, 0, 0, 0.28f));

        switch (st.Kind)
        {
            case StationKind.Workbench:
                Fill(c, x - 14, y - 4, 28, 10, Color.FromHtml("#6d5a52"));
                Fill(c, x - 14, y - 8, 28, 5, Color.FromHtml("#8a7568"));
                Fill(c, x - 10, y - 13, 5, 6, Color.FromHtml("#c9c3b4"));
                Fill(c, x + 3, y - 12, 4, 5, Color.FromHtml("#d0a24c"));
                Fill(c, x - 13, y + 6, 3, 6, Color.FromHtml("#4a3a35"));
                Fill(c, x + 10, y + 6, 3, 6, Color.FromHtml("#4a3a35"));
                break;
            default:
                Fill(c, x - 10, y - 12, 20, 16, Color.FromHtml("#2a3b3f"));
                Fill(c, x - 6, y - 8, 12, 8, Palette.O2);
                break;
        }

        Fill(c, x - 1, y - 20, 2, 6, Palette.O2);
        Fill(c, x - 3, y - 23, 6, 6, Palette.O2);
    }

    /// <summary>A loose item resting on the ground, gently bobbing with a soft glow.</summary>
    public static void DrawPickup(CanvasItem c, Pickup p, ItemDefinition def, double clock)
    {
        var bob = Mathf.Sin((float)clock * 3f + p.X * 0.05f) * 2f;
        var x = Mathf.Round(p.X);
        var y = Mathf.Round(p.Y) + bob;
        var col = Palette.FromHex(def.Color);

        Fill(c, x - 8, p.Y + 4, 16, 4, new Color(0, 0, 0, 0.25f));

        var glow = col;
        glow.A = 0.18f;
        c.DrawCircle(new Vector2(x, y), 12, glow);

        Fill(c, x - 5, y - 5, 10, 10, col);
        Fill(c, x - 3, y - 7, 6, 2, col.Lightened(0.3f));
        Fill(c, x + 6, y - 10, 2, 2, Colors.White);
    }

    public static void DrawShip(CanvasItem c, GameState s, BalanceConfig b, double clock)
    {
        var x = b.ShipX;
        var y = b.ShipY;
        var w = b.ShipWidth;
        var h = b.ShipHeight;
        bool R(string id) => s.Repairs.GetValueOrDefault(id);

        var panel = R("panel");
        var casco = R("casco");
        var combustible = R("combustible");
        var motor = R("motor");
        var antena = R("antena");

        Fill(c, x + 6, y + 14, w, h, new Color(40 / 255f, 15 / 255f, 8 / 255f, 0.38f));
        Fill(c, x + 30, y + 94, 8, 12, Color.FromHtml("#8c8678"));
        Fill(c, x + 132, y + 94, 8, 12, Color.FromHtml("#8c8678"));

        foreach (var wx in new[] { x, x + 134 })
        {
            var wy = y + 26;
            Fill(c, wx, wy, 36, 56, panel ? Color.FromHtml("#2f6fb0") : Color.FromHtml("#2a2f45"));
            var gridColor = panel ? Color.FromHtml("#9fd0f5") : Color.FromHtml("#4a506b");
            for (var i = 1; i < 3; i++)
                c.DrawLine(new Vector2(wx + i * 12 + 0.5f, wy), new Vector2(wx + i * 12 + 0.5f, wy + 56), gridColor);
            for (var j = 1; j < 4; j++)
                c.DrawLine(new Vector2(wx, wy + j * 14 + 0.5f), new Vector2(wx + 36, wy + j * 14 + 0.5f), gridColor);
            Stroke(c, wx + 0.5f, wy + 0.5f, 35, 55, panel ? Color.FromHtml("#e8e2d4") : Color.FromHtml("#8c8678"));
            if (!panel)
            {
                c.DrawPolyline(new[]
                {
                    new Vector2(wx + 4, wy + 6),
                    new Vector2(wx + 20, wy + 28),
                    new Vector2(wx + 14, wy + 50),
                }, Color.FromHtml("#0d0f1a"), 1);
            }
        }

        Fill(c, x + 36, y + 8, 98, 92, Color.FromHtml("#cfc8bb"));
        Stroke(c, x + 37, y + 9, 96, 90, Color.FromHtml("#5d564b"), 2);
        Fill(c, x + 38, y + 88, 94, 6, Palette.Dust);

        if (!casco)
        {
            Fill(c, x + 40, y + 12, 28, 16, Color.FromHtml("#4a4036"));
            Fill(c, x + 96, y + 44, 34, 10, Color.FromHtml("#4a4036"));
            Fill(c, x + 60, y + 76, 20, 10, Color.FromHtml("#4a4036"));
            Fill(c, x + 104, y + 68, 20, 14, Color.FromHtml("#1a1412"));
            c.DrawPolyline(new[]
            {
                new Vector2(x + 104, y + 68),
                new Vector2(x + 92, y + 56),
                new Vector2(x + 96, y + 44),
            }, Color.FromHtml("#1a1412"), 1);
        }
        else
        {
            Fill(c, x + 102, y + 66, 24, 16, Color.FromHtml("#e0dbcf"));
            foreach (var p in new[] { (104, 68), (122, 68), (104, 78), (122, 78) })
                Fill(c, x + p.Item1, y + p.Item2, 2, 2, Color.FromHtml("#8c8678"));
        }

        c.DrawCircle(new Vector2(x + 85, y + 34), 11, Color.FromHtml("#2a2230"));
        c.DrawArc(new Vector2(x + 85, y + 34), 11, 0, Mathf.Tau, 32, Color.FromHtml("#8c8678"), 2);
        if (casco)
            Fill(c, x + 79, y + 27, 6, 3, new Color(95 / 255f, 216 / 255f, 196 / 255f, 0.55f));
        else
            c.DrawPolyline(new[]
            {
                new Vector2(x + 78, y + 28),
                new Vector2(x + 86, y + 36),
                new Vector2(x + 92, y + 30),
            }, Color.FromHtml("#cfc8bb"), 1);

        c.DrawString(Fonts.MonoWeight(700), new Vector2(x + 85, y + 58), "ARES-7",
            HorizontalAlignment.Center, -1, 9, Color.FromHtml("#2a2230"));

        Fill(c, x + 42, y + 62, 20, 26, Color.FromHtml("#e8e2d4"));
        Fill(c, x + 44, y + 64, 16, 22, combustible ? Color.FromHtml("#8fd3f4") : Color.FromHtml("#2a2230"));
        if (combustible) Fill(c, x + 46, y + 66, 3, 16, Color.FromHtml("#d8f3ff"));

        Fill(c, x + 64, y + 100, 42, 16, Color.FromHtml("#3b3631"));
        if (motor)
        {
            var fl = 2 + Mathf.Sin((float)clock * 30) * 2;
            Fill(c, x + 70, y + 112, 30, 4 + fl, Palette.Energy);
            Fill(c, x + 78, y + 112, 14, 2 + fl, Palette.Health);
        }
        else
        {
            Fill(c, x + 70, y + 106, 30, 10, Color.FromHtml("#1a1412"));
        }

        if (antena)
        {
            Fill(c, x + 84, y + 2, 3, 8, Color.FromHtml("#e8e2d4"));
            c.DrawArc(new Vector2(x + 85, y + 3), 8, Mathf.Pi, Mathf.Tau, 16, Color.FromHtml("#e8e2d4"), 8);
            var blink = (int)Mathf.Floor((float)clock * 2) % 2 == 0;
            Fill(c, x + 84, y - 3, 3, 3, blink ? Palette.O2 : Color.FromHtml("#1d5c52"));
        }
        else
        {
            Fill(c, x + 82, y + 4, 6, 6, Color.FromHtml("#6b645a"));
            Fill(c, x + 76, y + 2, 8, 3, Color.FromHtml("#6b645a"));
        }
    }

    /// <summary>Translucent greenhouse dome covering its footprint.</summary>
    public static void DrawDome(CanvasItem c, Dome dome, BalanceConfig b)
    {
        var cx = dome.X + dome.Width / 2f;
        var cy = dome.Y + dome.Height / 2f;
        var rx = dome.Width / 2f;
        var ry = dome.Height / 2f;

        Fill(c, dome.X + 2, dome.Y + dome.Height * 0.45f, dome.Width - 4, dome.Height * 0.5f,
            Color.FromHtml("#2a1d18"));

        var glass = new Color(143 / 255f, 211 / 255f, 244 / 255f, 0.18f);
        var rim = Color.FromHtml("#8c8678");

        const int segments = 40;
        var poly = new Vector2[segments];
        for (var i = 0; i < segments; i++)
        {
            var a = Mathf.Tau * i / segments;
            poly[i] = new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry);
        }
        c.DrawColoredPolygon(poly, glass);
        for (var i = 0; i < segments; i++)
        {
            c.DrawLine(poly[i], poly[(i + 1) % segments], rim, 2);
        }
        c.DrawLine(new Vector2(cx - rx, cy), new Vector2(cx + rx, cy), new Color(1, 1, 1, 0.12f), 1);
    }

    /// <summary>The small management console anchored below a dome.</summary>
    public static void DrawDomeTerminal(CanvasItem c, Dome dome, BalanceConfig b)
    {
        var rim = Color.FromHtml("#8c8678");
        var x = dome.CenterX - 13;
        var y = dome.Y + dome.Height + 6;

        Fill(c, dome.CenterX - 1, dome.Y + dome.Height - 2, 2, 9, rim.Darkened(0.25f));
        Fill(c, x, y, 26, 14, Color.FromHtml("#241a16"));
        Stroke(c, x, y, 26, 14, rim, 1);

        var glow = dome.Energy > 0
            ? new Color(143 / 255f, 211 / 255f, 244 / 255f, 0.55f)
            : new Color(210 / 255f, 90 / 255f, 80 / 255f, 0.55f);
        Fill(c, x + 3, y + 3, 20, 6, glow);
        c.DrawString(Fonts.MonoWeight(600), new Vector2(x + 13, y + 12), "N" + dome.Level,
            HorizontalAlignment.Center, -1, 7, Palette.Ink);
    }

    /// <summary>A single crop slot inside a dome, tinted by its plant and progress.</summary>
    public static void DrawPlantSlot(CanvasItem c, Plot p, PlantDefinition? plant, BalanceConfig b, double clock)
    {
        var x = Mathf.Round(p.X);
        var y = Mathf.Round(p.Y);
        var s = b.PlotSize;

        Fill(c, x, y, s, s, Color.FromHtml("#241812"));
        Stroke(c, x, y, s, s, Color.FromHtml("#4a3124"), 1);

        if (plant == null || p.State == PlotState.Empty)
        {
            Fill(c, x + s / 2f - 2, y + s / 2f - 2, 4, 4, Color.FromHtml("#3a2a1e"));
            return;
        }

        var col = Palette.FromHex(plant.Color);
        var midX = x + s / 2f;

        switch (p.State)
        {
            case PlotState.Dry:
                Fill(c, x + s / 2f - 3, y + s / 2f, 6, 3, col.Darkened(0.2f));
                break;
            case PlotState.Grow:
                var progress = 1f - (float)(p.Time / Mathf.Max(1f, (float)plant.GrowTime));
                var h = 4 + progress * (s - 10);
                Fill(c, midX - 1, y + s - 6 - h, 2, h, Color.FromHtml("#5c8a3a"));
                Fill(c, midX - 5, y + s - 8 - h, 5, 3, col);
                Fill(c, midX + 1, y + s - 12 - h, 5, 3, col);
                break;
            default:
                var bob = Mathf.Sin((float)clock * 2f + p.X) * 1f;
                Fill(c, midX - 1, y + 4 + bob, 2, s - 10, Color.FromHtml("#5c8a3a"));
                Fill(c, midX - 7, y + 6 + bob, 7, 5, col);
                Fill(c, midX + 1, y + 2 + bob, 7, 5, col.Lightened(0.1f));
                Fill(c, midX - 4, y - 1 + bob, 5, 4, col.Lightened(0.2f));
                break;
        }
    }

    /// <summary>An empty interior slot where the player can build a fixture.</summary>
    public static void DrawDomeSlot(CanvasItem c, float x, float y, BalanceConfig b, bool highlighted)
    {
        var s = b.PlotSize;
        var fill = new Color(143 / 255f, 211 / 255f, 244 / 255f, highlighted ? 0.14f : 0.05f);
        var edge = highlighted
            ? new Color(0.55f, 0.95f, 0.65f, 0.85f)
            : new Color(0.55f, 0.65f, 0.62f, 0.35f);

        Fill(c, x, y, s, s, fill);
        DrawDashedRect(c, new Rect2(x, y, s, s), edge, 1, 4);

        var cx = x + s / 2f;
        var cy = y + s / 2f;
        Fill(c, cx - 4, cy - 1, 8, 2, edge);
        Fill(c, cx - 1, cy - 4, 2, 8, edge);
    }

    /// <summary>A utility placed on a dome slot (solar panel, battery or bunk).</summary>
    public static void DrawFixture(CanvasItem c, Fixture fixture, float x, float y, BalanceConfig b, double clock)
    {
        var s = b.PlotSize;
        Fill(c, x, y, s, s, Color.FromHtml("#241812"));
        Stroke(c, x, y, s, s, Color.FromHtml("#4a3124"), 1);

        switch (fixture.Kind)
        {
            case FixtureKind.SolarPanel:
                Fill(c, x + 4, y + 6, s - 8, s - 12, Color.FromHtml("#1d3550"));
                Stroke(c, x + 4, y + 6, s - 8, s - 12, Color.FromHtml("#4aa3e0"), 1);
                for (var i = 1; i < 3; i++)
                {
                    Fill(c, x + 4 + i * (s - 8) / 3f, y + 6, 1, s - 12, Color.FromHtml("#4aa3e0"));
                }
                Fill(c, x + 4, y + s / 2f, s - 8, 1, Color.FromHtml("#4aa3e0"));
                break;

            case FixtureKind.Battery:
                Fill(c, x + 7, y + 8, s - 14, s - 14, Color.FromHtml("#2d2a20"));
                Stroke(c, x + 7, y + 8, s - 14, s - 14, Color.FromHtml("#d9c45a"), 1);
                Fill(c, x + s / 2f - 3, y + 4, 6, 4, Color.FromHtml("#d9c45a"));
                for (var i = 0; i < 3; i++)
                {
                    Fill(c, x + 10 + i * 4, y + s - 14, 2, 5, Color.FromHtml("#9ccf5a"));
                }
                break;

            case FixtureKind.Bunk:
                Fill(c, x + 4, y + 9, s - 8, s - 16, Color.FromHtml("#7a4b33"));
                Stroke(c, x + 4, y + 9, s - 8, s - 16, Color.FromHtml("#c9a45a"), 1);
                Fill(c, x + 6, y + 11, 7, s - 20, Color.FromHtml("#e8e2d4"));
                Fill(c, x + 14, y + 11, s - 20, s - 20, Color.FromHtml("#2f5d7a"));
                break;

            default:
                Fill(c, x + 6, y + 6, s - 12, s - 12, Color.FromHtml("#3a2a1e"));
                break;
        }
    }

    /// <summary>Placement ghost shown while building; green when valid, red otherwise.</summary>
    public static void DrawBuildGhost(CanvasItem c, float x, float y, BalanceConfig b, bool valid)
    {
        var color = valid ? new Color(0.55f, 0.9f, 0.6f, 0.22f) : new Color(0.95f, 0.35f, 0.3f, 0.22f);
        var edge = valid ? new Color(0.55f, 0.95f, 0.65f, 0.9f) : new Color(1f, 0.4f, 0.35f, 0.9f);

        Fill(c, x, y, b.DomeWidth, b.DomeHeight, color);
        Stroke(c, x, y, b.DomeWidth, b.DomeHeight, edge, 2);

        for (var r = 0; r < b.DomeSlotRows; r++)
        for (var col = 0; col < b.DomeSlotColumns; col++)
        {
            var px = x + b.DomeSlotMarginX + col * (b.PlotSize + b.DomeSlotGap);
            var py = y + b.DomeSlotMarginY + r * (b.PlotSize + b.DomeSlotGap);
            Stroke(c, px, py, b.PlotSize, b.PlotSize, edge, 1);
        }
    }

    public static void DrawRing(CanvasItem c, float x, float y, float w, float h)
    {
        var color = Palette.O2;
        DrawDashedRect(c, new Rect2(x, y, w, h), color, 2, 5);
    }

    private static void DrawDashedRect(CanvasItem c, Rect2 r, Color color, float width, float dash)
    {
        var tl = r.Position;
        var tr = new Vector2(r.End.X, r.Position.Y);
        var br = r.End;
        var bl = new Vector2(r.Position.X, r.End.Y);
        c.DrawDashedLine(tl, tr, color, width, dash);
        c.DrawDashedLine(tr, br, color, width, dash);
        c.DrawDashedLine(br, bl, color, width, dash);
        c.DrawDashedLine(bl, tl, color, width, dash);
    }

    public static void DrawText(CanvasItem c, string text, float x, float y, Color color, int size, Font? font = null)
    {
        c.DrawString(font ?? Fonts.MonoWeight(600), new Vector2(x, y), text,
            HorizontalAlignment.Center, -1, size, color);
    }
}
