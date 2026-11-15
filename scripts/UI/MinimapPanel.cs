using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// Compact map of the whole Gale Crater biome grid, showing the player, the ship,
/// the domes and the stations. Toggled with the "Minimapa" binding (default M).
/// </summary>
public partial class MinimapPanel : Control
{
    private const int Cell = 2;

    private readonly GameSession _session;
    private double _refreshTimer;
    private bool _hidden;

    public MinimapPanel(GameSession session)
    {
        _session = session;
        MouseFilter = MouseFilterEnum.Ignore;
        Size = new Vector2(GeneratedBiomes.Width * Cell + 8, GeneratedBiomes.Height * Cell + 26);
    }

    public void Toggle()
    {
        _hidden = !_hidden;
        Visible = !_hidden;
    }

    public override void _Process(double delta)
    {
        _refreshTimer += delta;
        if (_refreshTimer < 0.25) return;
        _refreshTimer = 0;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var s = _session.State;
        var b = _session.Balance;
        var panel = new Rect2(0, 0, Size.X, Size.Y);

        DrawRect(panel, new Color(0.05f, 0.04f, 0.03f, 0.78f), true);
        DrawRect(panel, new Color(1, 1, 1, 0.16f), false, 1);

        var origin = new Vector2(4, 4);

        for (var y = 0; y < GeneratedBiomes.Height; y++)
        for (var x = 0; x < GeneratedBiomes.Width; x++)
        {
            var color = Color.FromHtml(GeneratedBiomes.Get(GeneratedBiomes.Grid[y * GeneratedBiomes.Width + x]).Color);
            DrawRect(new Rect2(origin.X + x * Cell, origin.Y + y * Cell, Cell, Cell), color);
        }

        foreach (var node in s.Nodes)
        {
            if (node.Amount <= 0) continue;
            DrawRect(new Rect2(origin.X + node.X / b.WorldWidth * GeneratedBiomes.Width * Cell,
                origin.Y + node.Y / b.WorldHeight * GeneratedBiomes.Height * Cell, 1, 1),
                new Color(1, 1, 1, 0.35f));
        }

        foreach (var dome in s.Domes)
        {
            var rx = origin.X + (dome.X + dome.Width / 2f) / b.WorldWidth * GeneratedBiomes.Width * Cell;
            var ry = origin.Y + (dome.Y + dome.Height / 2f) / b.WorldHeight * GeneratedBiomes.Height * Cell;
            DrawRect(new Rect2(rx - 3, ry - 2, 6, 4), new Color(0.56f, 0.9f, 1f, 0.95f), true);
        }

        foreach (var station in s.Stations)
        {
            var rx = origin.X + station.X / b.WorldWidth * GeneratedBiomes.Width * Cell;
            var ry = origin.Y + station.Y / b.WorldHeight * GeneratedBiomes.Height * Cell;
            DrawRect(new Rect2(rx - 1, ry - 1, 2, 2), new Color(0.85f, 0.85f, 0.8f, 0.9f), true);
        }

        var shipX = origin.X + (b.ShipX + b.ShipWidth / 2f) / b.WorldWidth * GeneratedBiomes.Width * Cell;
        var shipY = origin.Y + (b.ShipY + b.ShipHeight / 2f) / b.WorldHeight * GeneratedBiomes.Height * Cell;
        DrawRect(new Rect2(shipX - 3, shipY - 2, 6, 4), new Color(1f, 0.83f, 0.35f, 0.95f), true);

        var scale = new Vector2((float)GeneratedBiomes.Width * Cell / b.WorldWidth,
            (float)GeneratedBiomes.Height * Cell / b.WorldHeight);
        var camRect = new Rect2(origin.X + (s.Px - 384) * scale.X, origin.Y + (s.Py - 224) * scale.Y,
            768 * scale.X, 448 * scale.Y);
        DrawRect(camRect, new Color(1, 1, 1, 0.4f), false, 1);

        var clock = Time.GetTicksMsec() / 1000.0;
        var pulse = 0.6f + 0.4f * Mathf.Sin((float)clock * 3f);
        var px = origin.X + s.Px / b.WorldWidth * GeneratedBiomes.Width * Cell;
        var py = origin.Y + s.Py / b.WorldHeight * GeneratedBiomes.Height * Cell;
        DrawCircle(new Vector2(px, py), 2.4f * pulse + 1.2f, new Color(1, 1, 1, 0.9f));

        DrawString(Fonts.MonoWeight(600), new Vector2(4, Size.Y - 6), "GALILEO / Crater Gale",
            HorizontalAlignment.Left, -1, 9, new Color(1, 1, 1, 0.65f));
    }
}
