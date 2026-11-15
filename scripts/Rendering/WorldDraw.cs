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

        var sprite = def.Type switch
        {
            "ice" => SpriteArt.Get("hielo de agua mediano.png"),
            "iron" => SpriteArt.Get("cristal oxido hierro rojizo mediano.png"),
            "si" => SpriteArt.Get("siliceo.png"),
            "regolith" => SpriteArt.Get("regolito_oxidado_granular_001.png"),
            "basalt" => SpriteArt.Get("basaltomarciano_oscuro grande.png"),
            _ => SpriteArt.Get("muestra_roca_sedimentaria_068.png"),
        };

        c.DrawSetTransform(new Vector2(Mathf.Round(n.X + sh), Mathf.Round(n.Y)), 0, Vector2.One);
        Fill(c, -22, 8, 44, 6, new Color(0, 0, 0, 0.28f));

        if (n.Amount <= 0)
        {
            // Depleted veins cannot be interacted with, so their art stays hidden.
            c.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            return;
        }

        SpriteArt.DrawSprite(c, sprite, new Vector2(0, -4), def.Type == "ice" ? 42 : 46);

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
                SpriteArt.DrawSprite(c, SpriteArt.Get("estacion_trabajo_modular_037.png"), new Vector2(x, y - 2), 56);
                break;
            default:
                SpriteArt.DrawSprite(c, SpriteArt.Get("maquinaria_procesamiento_mineral_046.png"), new Vector2(x, y - 2), 56);
                break;
        }

    }

    /// <summary>The supply beacon the player fills for the survivor.</summary>
    public static void DrawSupplyCache(CanvasItem c, GameState s, double clock)
    {
        var x = Mathf.Round(s.CacheX);
        var y = Mathf.Round(s.CacheY);
        var pulse = 0.5f + 0.5f * Mathf.Sin((float)clock * 3f);

        var glow = new Color(1f, 0.83f, 0.35f, 0.10f + 0.14f * pulse);
        c.DrawCircle(new Vector2(x, y - 12), 22, glow);

        Fill(c, x - 15, y + 6, 30, 5, new Color(0, 0, 0, 0.28f));
        Fill(c, x - 14, y - 8, 28, 20, Color.FromHtml("#3b3631"));
        Stroke(c, x - 14, y - 8, 28, 20, Color.FromHtml("#8c8678"));
        Fill(c, x - 14, y + 1, 28, 3, Color.FromHtml("#5d564b"));
        Fill(c, x - 4, y - 5, 8, 3, Color.FromHtml("#c9a45a"));

        Fill(c, x - 1, y - 28, 2, 22, Color.FromHtml("#8c8678"));
        c.DrawCircle(new Vector2(x, y - 29), 3.5f, pulse > 0.5f ? Palette.Energy : Color.FromHtml("#6b645a"));
    }

    /// <summary>The stranded survivor's wreck site with a distress beacon.</summary>
    public static void DrawSurvivorSite(CanvasItem c, GameState s, double clock)
    {
        var x = Mathf.Round(s.SurvivorX);
        var y = Mathf.Round(s.SurvivorY);
        var pulse = 0.5f + 0.5f * Mathf.Sin((float)clock * 4f);

        var glow = new Color(1f, 0.45f, 0.38f, 0.10f + 0.16f * pulse);
        c.DrawCircle(new Vector2(x, y - 8), 26, glow);

        Fill(c, x - 20, y + 8, 40, 5, new Color(0, 0, 0, 0.28f));
        Fill(c, x - 16, y - 4, 32, 14, Color.FromHtml("#4a4036"));
        Stroke(c, x - 16, y - 4, 32, 14, Color.FromHtml("#8c8678"));
        Fill(c, x - 10, y - 10, 14, 8, Color.FromHtml("#cfc8bb"));
        Stroke(c, x - 10, y - 10, 14, 8, Color.FromHtml("#5d564b"));
        Fill(c, x + 8, y - 16, 3, 12, Color.FromHtml("#8c8678"));
        c.DrawCircle(new Vector2(x + 9, y - 18), 3f, pulse > 0.45f ? Palette.Health : Color.FromHtml("#6b645a"));
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

        var sprite = def.Id switch
        {
            "hielo" => SpriteArt.Get("agua hielo procesada.png"),
            "hierro" => SpriteArt.Get("cristal_oxido_hierro_rojizo_pequeno_029.png"),
            "silicio" => SpriteArt.Get("fragmento_silice_pequeno_038.png"),
            "regolito" => SpriteArt.Get("regolito_oxidado_granular_002.png"),
            "basalto" => SpriteArt.Get("fragmento_basaltico_pequeno_016.png"),
            _ => SpriteArt.Get("fragmento_roca_silicatica_003.png"),
        };

        SpriteArt.DrawSprite(c, sprite, new Vector2(x, y), 30);
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
    public static void DrawDome(CanvasItem c, Dome dome, BalanceConfig b, float playerX = -1f, float playerY = -1f)
    {
        var cx = dome.X + dome.Width / 2f;
        var cy = dome.Y + dome.Height / 2f;
        var rx = dome.Width / 2f;
        var ry = dome.Height / 2f;

        var inside = playerX >= dome.X && playerX <= dome.X + dome.Width &&
                     playerY >= dome.Y && playerY <= dome.Y + dome.Height;
        var modulate = inside ? new Color(1, 1, 1, 0.6f) : Colors.White;

        Fill(c, dome.X + 2, dome.Y + dome.Height * 0.45f, dome.Width - 4, dome.Height * 0.5f,
            new Color(42 / 255f, 29 / 255f, 24 / 255f, 0.45f));

        SpriteArt.DrawSprite(c, SpriteArt.Get("domo_habitat_presurizado_020.png"),
            new Vector2(cx, cy - 4), dome.Height + 2, modulate);

        var glass = new Color(143 / 255f, 211 / 255f, 244 / 255f, 0.09f);
        var rim = Color.FromHtml("#8c8678");

        const int segments = 40;
        var poly = new Vector2[segments];
        for (var i = 0; i < segments; i++)
        {
            var a = Mathf.Tau * i / segments;
            poly[i] = new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry);
        }
        c.DrawColoredPolygon(poly, glass);
        c.DrawArc(new Vector2(cx, cy), Mathf.Min(rx, ry), 0, Mathf.Tau, 40, rim, 1f);
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
    public static void DrawPlantSlot(CanvasItem c, Plot p, PlantDefinition? plant, BalanceConfig b, double clock, bool dimmed = false)
    {
        var dim = dimmed ? 0.5f : 1f;
        Color A(Color col) => new(col.R, col.G, col.B, col.A * dim);

        var x = Mathf.Round(p.X);
        var y = Mathf.Round(p.Y);
        var s = b.PlotSize;

        Fill(c, x, y, s, s, A(Color.FromHtml("#241812")));
        Stroke(c, x, y, s, s, A(Color.FromHtml("#4a3124")), 1);

        if (plant == null || p.State == PlotState.Empty)
        {
            Fill(c, x + s / 2f - 2, y + s / 2f - 2, 4, 4, A(Color.FromHtml("#3a2a1e")));
            return;
        }

        var col = Palette.FromHex(plant.Color);
        var midX = x + s / 2f;

        switch (p.State)
        {
            case PlotState.Dry:
                Fill(c, x + s / 2f - 3, y + s / 2f, 6, 3, A(col.Darkened(0.2f)));
                break;
            case PlotState.Grow:
                var progress = 1f - (float)(p.Time / Mathf.Max(1f, (float)plant.GrowTime));
                var h = 4 + progress * (s - 10);
                Fill(c, midX - 1, y + s - 6 - h, 2, h, A(Color.FromHtml("#5c8a3a")));
                Fill(c, midX - 5, y + s - 8 - h, 5, 3, A(col));
                Fill(c, midX + 1, y + s - 12 - h, 5, 3, A(col));
                break;
            default:
                var bob = Mathf.Sin((float)clock * 2f + p.X) * 1f;
                Fill(c, midX - 1, y + 4 + bob, 2, s - 10, A(Color.FromHtml("#5c8a3a")));
                Fill(c, midX - 7, y + 6 + bob, 7, 5, A(col));
                Fill(c, midX + 1, y + 2 + bob, 7, 5, A(col.Lightened(0.1f)));
                Fill(c, midX - 4, y - 1 + bob, 5, 4, A(col.Lightened(0.2f)));
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

    /// <summary>A utility placed on a dome slot (solar panel or battery).</summary>
    public static void DrawFixture(CanvasItem c, Fixture fixture, float x, float y, BalanceConfig b, double clock, bool dimmed = false)
    {
        var dim = dimmed ? 0.5f : 1f;
        var hurt = (float)Mathf.Clamp(fixture.Damage / b.StructureMaxDamage, 0, 1);
        var mod = new Color(1f, 1f - hurt * 0.5f, 1f - hurt * 0.5f, dim);
        var s = b.PlotSize;
        Fill(c, x, y, s, s, new Color(36 / 255f, 24 / 255f, 18 / 255f, dim));
        Stroke(c, x, y, s, s, new Color(74 / 255f, 49 / 255f, 36 / 255f, dim), 1);

        switch (fixture.Kind)
        {
            case FixtureKind.SolarPanel:
                if (fixture.Deployed)
                {
                    SpriteArt.DrawSprite(c, SpriteArt.Get("sistema_energia_panel_bateria_064.png"),
                        new Vector2(x + s / 2f, y + s / 2f), s - 6, mod);
                }
                else
                {
                    // Retracted: a flat, dark panel with a single seam.
                    Fill(c, x + 5, y + s / 2f - 2, s - 10, 4, new Color(0.30f, 0.33f, 0.38f, dim));
                    Fill(c, x + 5, y + s / 2f - 1, s - 10, 1, new Color(0.55f, 0.6f, 0.66f, dim));
                }
                break;

            case FixtureKind.Battery:
                SpriteArt.DrawSprite(c, SpriteArt.Get("sistema_energia_panel_bateria_071.png"),
                    new Vector2(x + s / 2f, y + s / 2f + 1), s - 8, mod);
                DrawBatteryBar(c, fixture, x, y, s, b, dim);
                break;

            default:
                SpriteArt.DrawSprite(c, SpriteArt.Get("recipiente_metalico_105.png"),
                    new Vector2(x + s / 2f, y + s / 2f), s - 10, mod);
                break;
        }

        if (hurt > 0.05f)
        {
            Stroke(c, x + 1, y + 1, s - 2, s - 2, new Color(0.95f, 0.3f, 0.25f, 0.35f + 0.5f * hurt), 1);
        }
    }

    private static void DrawBatteryBar(CanvasItem c, Fixture fixture, float x, float y, float s, BalanceConfig b, float dim)
    {
        var pct = Mathf.Clamp((float)(fixture.Stored / b.BatteryStorage), 0f, 1f);
        var bx = x + 4;
        var by = y + s + 1;
        var bw = s - 8;
        const float bh = 4;
        Fill(c, bx, by, bw, bh, new Color(0.06f, 0.04f, 0.03f, 0.85f * dim));
        if (pct > 0)
        {
            var col = Palette.Energy;
            col.A = dim;
            Fill(c, bx, by, bw * pct, bh, col);
        }
    }

    /// <summary>An outdoor beacon that lights and heats nearby plots when powered.</summary>
    public static void DrawBeacon(CanvasItem c, Beacon beacon, BalanceConfig b, double clock)
    {
        var x = Mathf.Round(beacon.X);
        var y = Mathf.Round(beacon.Y);
        var pulse = 0.5f + 0.5f * Mathf.Sin((float)clock * 3f);

        var lightColor = beacon.Powered ? new Color(1f, 0.85f, 0.45f, 0.18f) : new Color(0.4f, 0.4f, 0.4f, 0.10f);
        c.DrawCircle(new Vector2(x, y - 14), b.BeaconRadius * 0.35f, new Color(lightColor.R, lightColor.G, lightColor.B, lightColor.A * pulse));

        Fill(c, x - 10, y + 6, 20, 4, new Color(0, 0, 0, 0.28f));
        Fill(c, x - 6, y - 6, 12, 14, Color.FromHtml("#3b3631"));
        Stroke(c, x - 6, y - 6, 12, 14, Color.FromHtml("#8c8678"));
        Fill(c, x - 1, y - 26, 2, 20, Color.FromHtml("#8c8678"));
        var bulb = beacon.Powered
            ? (pulse > 0.4f ? new Color(1f, 0.88f, 0.5f) : new Color(1f, 0.7f, 0.3f))
            : new Color(0.35f, 0.35f, 0.35f);
        c.DrawCircle(new Vector2(x, y - 28), 4f, bulb);

        var hurt = (float)Mathf.Clamp(beacon.Damage / b.StructureMaxDamage, 0, 1);
        if (hurt > 0.05f)
        {
            Stroke(c, x - 7, y - 7, 14, 16, new Color(0.95f, 0.3f, 0.25f, 0.4f + 0.5f * hurt), 1);
        }
    }

    /// <summary>Placement ghost for an outdoor beacon; green when valid.</summary>
    public static void DrawBeaconGhost(CanvasItem c, float x, float y, BalanceConfig b, bool valid)
    {
        var color = valid ? new Color(0.55f, 0.9f, 0.6f, 0.22f) : new Color(0.95f, 0.35f, 0.3f, 0.22f);
        var edge = valid ? new Color(0.55f, 0.95f, 0.65f, 0.9f) : new Color(1f, 0.4f, 0.35f, 0.9f);
        Fill(c, x, y, b.PlotSize, b.PlotSize, color);
        Stroke(c, x, y, b.PlotSize, b.PlotSize, edge, 2);
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

    /// <summary>Placement ghost for an open-air crop tray; green when valid, red otherwise.</summary>
    public static void DrawFieldGhost(CanvasItem c, float x, float y, BalanceConfig b, bool valid)
    {
        var color = valid ? new Color(0.55f, 0.9f, 0.6f, 0.22f) : new Color(0.95f, 0.35f, 0.3f, 0.22f);
        var edge = valid ? new Color(0.55f, 0.95f, 0.65f, 0.9f) : new Color(1f, 0.4f, 0.35f, 0.9f);

        Fill(c, x, y, b.PlotSize, b.PlotSize, color);
        Stroke(c, x, y, b.PlotSize, b.PlotSize, edge, 2);
        Fill(c, x + 6, y + 6, b.PlotSize - 12, b.PlotSize - 12, color);
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
