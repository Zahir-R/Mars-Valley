namespace NasaValley.Domain;

/// <summary>
/// In-world placement of greenhouse domes. The ghost follows the player snapped to the
/// grid; placement validates bounds, the ship, and other domes, then spends materials.
/// </summary>
public static class BuildSystem
{
    public static float Snap(BalanceConfig b, float v) =>
        MathF.Floor(v / b.TileSize) * b.TileSize;

    /// <summary>Updates the ghost to follow the player, and places it on interact.</summary>
    public static void Update(IGameContext ctx, in PlayerIntent intent)
    {
        var s = ctx.State;
        var b = ctx.Balance;
        s.BuildX = Snap(b, s.Px - b.DomeWidth / 2f);
        s.BuildY = Snap(b, s.Py - b.DomeHeight / 2f);

        if (!intent.InteractPressed) return;
        if (!TryBuild(ctx, s.BuildX, s.BuildY, out var reason)) ctx.Say(reason, MessageKind.Warn);
    }

    public static bool CanPlaceAt(IGameContext ctx, float x, float y, out string reason)
    {
        var s = ctx.State;
        var b = ctx.Balance;

        if (x < b.MoveMinX || y < b.MoveMinY ||
            x + b.DomeWidth > b.MoveMaxX + 14 || y + b.DomeHeight > b.MoveMaxY + 18)
        {
            reason = "La cúpula no cabe ahí.";
            return false;
        }

        var ship = new Rect(b.ShipX, b.ShipY, b.ShipWidth, b.ShipHeight);
        if (Overlaps(x, y, b.DomeWidth, b.DomeHeight, ship))
        {
            reason = "No puedes construir sobre la nave.";
            return false;
        }

        foreach (var dome in s.Domes)
        {
            if (Overlaps(x, y, b.DomeWidth, b.DomeHeight, new Rect(dome.X, dome.Y, dome.Width, dome.Height)))
            {
                reason = "Ya hay una cúpula ahí.";
                return false;
            }
        }

        foreach (var (item, amount) in b.DomeCost)
        {
            if (!s.Inventory.Has(item, amount))
            {
                reason = $"Faltan materiales: {ctx.Items[item].Name}.";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    public static bool TryBuild(IGameContext ctx, float x, float y, out string reason)
    {
        if (!CanPlaceAt(ctx, x, y, out reason)) return false;

        var s = ctx.State;
        var b = ctx.Balance;
        foreach (var (item, amount) in b.DomeCost) s.Inventory.Remove(item, amount);

        var id = s.Domes.Count == 0 ? 1 : s.Domes[^1].Id + 1;
        var dome = new Dome(id, x, y, b.DomeWidth, b.DomeHeight)
        {
            Energy = b.DomeStartEnergy,
            InternalTempC = b.DomeTargetTempC,
            ExternalTempC = EnvironmentSystem.ExternalTemp(ctx.Areas.At(x + b.DomeWidth / 2f, y + b.DomeHeight / 2f), s, b),
        };

        var slot = b.PlotSize;
        for (var r = 0; r < b.DomeSlotRows; r++)
        for (var c = 0; c < b.DomeSlotColumns; c++)
        {
            var px = x + b.DomeSlotMarginX + c * (slot + b.DomeSlotGap);
            var py = y + b.DomeSlotMarginY + r * (slot + b.DomeSlotGap);
            var plot = new Plot(px, py) { Dome = dome };
            dome.Plots.Add(plot);
            s.Plots.Add(plot);
        }

        s.Domes.Add(dome);
        s.Building = false;
        ctx.Say("Construyes una cúpula de invernadero.", MessageKind.Good);
        return true;
    }

    private static bool Overlaps(float ax, float ay, float aw, float ah, Rect other) =>
        ax < other.X + other.W && ax + aw > other.X &&
        ay < other.Y + other.H && ay + ah > other.Y;

    private readonly record struct Rect(float X, float Y, float W, float H);
}
