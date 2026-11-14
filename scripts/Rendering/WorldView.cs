using System;
using System.Collections.Generic;
using Godot;
using NasaValley.Domain;

namespace NasaValley.Rendering;

/// <summary>
/// The 768x448 game viewport contents. Composes the static ground layer with the
/// dynamic world (farm, ship, entities, effects, weather) each frame.
/// </summary>
public partial class WorldView : Node2D
{
    public GameSession Session { get; set; } = null!;

    private const float ViewWidth = 768f;
    private const float ViewHeight = 448f;

    private SubViewport _sub = null!;
    private Vector2I _lastSize = Vector2I.Zero;

    public override void _Ready()
    {
        AddChild(new GroundLayer { Name = "GroundLayer", ShowBehindParent = true });
        _sub = GetParent<SubViewport>();
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        var size = _sub.Size;
        if (size != _lastSize && size.X > 0 && size.Y > 0)
        {
            _lastSize = size;
            Scale = new Vector2(size.X / ViewWidth, size.Y / ViewHeight);
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var s = Session.State;
        var b = Session.Balance;
        var clock = Time.GetTicksMsec() / 1000.0;

        WorldDraw.DrawFarm(this, s, b, clock);
        WorldDraw.DrawShip(this, s, b, clock);

        DrawEntities(s, clock);

        if (s.Running && !s.Paused && !s.Over)
        {
            DrawTargetRing(b);
        }

        DrawParticles(s);
        DrawFloats(s);
        DrawWeather(s, b, clock);
    }

    private void DrawEntities(GameState s, double clock)
    {
        var drawables = new List<(float Y, Action Draw)>(s.Nodes.Count + s.Pickups.Count + 1);

        foreach (var node in s.Nodes)
        {
            var def = Session.Nodes[node.Type];
            drawables.Add((node.Y, () => WorldDraw.DrawNode(this, node, def)));
        }

        foreach (var pickup in s.Pickups)
        {
            if (pickup.Taken) continue;
            drawables.Add((pickup.Y + 6, () => WorldDraw.DrawPickup(this, pickup, Session.Items[pickup.ItemId], clock)));
        }

        drawables.Add((s.Py + 12, () => WorldDraw.DrawAstronaut(this, s)));
        drawables.Sort((a, c) => a.Y.CompareTo(c.Y));

        WorldDraw.DrawPlayerMarker(this, s, clock);
        foreach (var (_, draw) in drawables) draw();
    }

    private void DrawTargetRing(BalanceConfig b)
    {
        switch (Session.CurrentTarget)
        {
            case NodeTarget nt:
                WorldDraw.DrawRing(this, nt.Node.X - 20, nt.Node.Y - 26, 40, 44);
                break;
            case PlotTarget pt:
                WorldDraw.DrawRing(this, pt.Plot.X - 1, pt.Plot.Y - 1, b.PlotSize + 2, b.PlotSize + 2);
                break;
            case ShipTarget:
                WorldDraw.DrawRing(this, b.ShipX - 3, b.ShipY - 3, b.ShipWidth + 6, b.ShipHeight + 6);
                break;
        }
    }

    private void DrawParticles(GameState s)
    {
        foreach (var p in s.Particles)
        {
            var color = Palette.FromHex(p.Color);
            color.A = Mathf.Max(0, (float)p.Life * 2);
            WorldDraw.Fill(this, Mathf.Round(p.X), Mathf.Round(p.Y), 3, 3, color);
        }
    }

    private void DrawFloats(GameState s)
    {
        foreach (var f in s.Floats)
        {
            var alpha = Mathf.Clamp((float)f.Life * 2, 0, 1);
            var color = Palette.FromHex(f.Color);
            color.A = alpha;
            var x = Mathf.Round(f.X);
            var y = Mathf.Round(f.Y);
            WorldDraw.DrawText(this, f.Text, x + 1, y + 1, new Color(0, 0, 0, alpha), 11);
            WorldDraw.DrawText(this, f.Text, x, y, color, 11);
        }
    }

    private void DrawWeather(GameState s, BalanceConfig b, double clock)
    {
        var hour = GameClock.HourOf(s.Time, b.SolLength);
        var darkness = GameClock.Darkness(hour, b.NightDarkness);
        if (darkness > 0)
        {
            WorldDraw.Fill(this, 0, 0, b.ViewWidth, b.ViewHeight,
                new Color(12 / 255f, 14 / 255f, 46 / 255f, (float)darkness));
        }

        if (s.Storm.Left <= 0) return;

        WorldDraw.Fill(this, 0, 0, b.ViewWidth, b.ViewHeight,
            new Color(190 / 255f, 110 / 255f, 60 / 255f, 0.30f));

        var streak = new Color(240 / 255f, 190 / 255f, 140 / 255f, 0.35f);
        for (var i = 0; i < 60; i++)
        {
            var sx = ((i * 97 + clock * 300) % (b.ViewWidth + 80)) - 40;
            var sy = (i * 53) % b.ViewHeight + Mathf.Sin(i + (float)clock * 2) * 8;
            var len = 14 + (i % 5) * 6;
            DrawLine(new Vector2((float)sx, sy), new Vector2((float)sx - len, sy + len * 0.25f), streak, 2);
        }
    }
}
