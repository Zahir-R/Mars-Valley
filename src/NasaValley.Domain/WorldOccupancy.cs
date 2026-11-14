namespace NasaValley.Domain;

/// <summary>
/// Footprint occupancy rules shared by placement and resource respawn. A dome is not
/// a solid wall for the player, but it is an object that other things must respect:
/// its rectangle, the terminal console below it, and the objects already in the world.
/// </summary>
public static class WorldOccupancy
{
    public readonly record struct Rect(float X, float Y, float W, float H)
    {
        public float Right => X + W;
        public float Bottom => Y + H;

        public bool Overlaps(Rect other) =>
            X < other.Right && Right > other.X &&
            Y < other.Bottom && Bottom > other.Y;

        public bool ContainsPoint(float px, float py) =>
            px >= X && px <= Right && py >= Y && py <= Bottom;

        public bool IntersectsCircle(float cx, float cy, float radius)
        {
            var nx = Math.Clamp(cx, X, Right);
            var ny = Math.Clamp(cy, Y, Bottom);
            var dx = cx - nx;
            var dy = cy - ny;
            return dx * dx + dy * dy <= radius * radius;
        }
    }

    public static Rect DomeRect(Dome dome) => new(dome.X, dome.Y, dome.Width, dome.Height);

    /// <summary>The console hanging below the dome; must stay clear of other objects.</summary>
    public static Rect TerminalRect(Dome dome) =>
        new(dome.CenterX - 14, dome.Y + dome.Height + 2, 28, 18);

    /// <summary>Rect a new dome plus its terminal would occupy at (x, y).</summary>
    public static Rect PlacementRect(BalanceConfig b, float x, float y) =>
        new(x, y, b.DomeWidth, b.DomeHeight);

    public static Rect PlacementTerminalRect(BalanceConfig b, float x, float y) =>
        new(x + b.DomeWidth / 2f - 14, y + b.DomeHeight + 2, 28, 18);

    /// <summary>True when <paramref name="rect"/> is free of scene objects; otherwise returns the blocker.</summary>
    public static bool IsClear(IGameContext ctx, Rect rect, out string blocker)
    {
        var s = ctx.State;
        var b = ctx.Balance;

        // Active resource veins reserve their spot; depleted ones may be built over.
        foreach (var node in s.Nodes)
        {
            if (node.Amount <= 0) continue;
            if (rect.IntersectsCircle(node.X, node.Y, b.NodeFootprintRadius))
            {
                blocker = "una veta mineral activa";
                return false;
            }
        }

        foreach (var pickup in s.Pickups)
        {
            if (pickup.Taken) continue;
            if (rect.IntersectsCircle(pickup.X, pickup.Y, b.PickupFootprintRadius))
            {
                blocker = "un objeto en el suelo";
                return false;
            }
        }

        foreach (var station in s.Stations)
        {
            if (rect.IntersectsCircle(station.X, station.Y, b.StationFootprintRadius))
            {
                blocker = "una estación";
                return false;
            }
        }

        var ship = new Rect(b.ShipX, b.ShipY, b.ShipWidth, b.ShipHeight);
        if (rect.Overlaps(ship))
        {
            blocker = "la nave";
            return false;
        }

        foreach (var dome in s.Domes)
        {
            if (dome.Id == s.MovingDomeId) continue;
            if (rect.Overlaps(DomeRect(dome)) || rect.Overlaps(TerminalRect(dome)))
            {
                blocker = "otra cúpula";
                return false;
            }
        }

        blocker = string.Empty;
        return true;
    }

    /// <summary>True when (x, y) lies inside a structure's protected footprint.</summary>
    public static bool InsideAnyStructure(GameState s, BalanceConfig b, float x, float y)
    {
        if (x >= b.ShipX && x <= b.ShipX + b.ShipWidth &&
            y >= b.ShipY && y <= b.ShipY + b.ShipHeight) return true;

        foreach (var station in s.Stations)
        {
            var dx = x - station.X;
            var dy = y - station.Y;
            if (dx * dx + dy * dy <= b.StationFootprintRadius * b.StationFootprintRadius) return true;
        }

        foreach (var dome in s.Domes)
        {
            if (DomeRect(dome).ContainsPoint(x, y)) return true;
            if (TerminalRect(dome).ContainsPoint(x, y)) return true;
        }

        return false;
    }
}
