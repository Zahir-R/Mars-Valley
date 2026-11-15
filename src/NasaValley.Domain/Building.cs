namespace NasaValley.Domain;

/// <summary>
/// In-world placement and management of greenhouse domes. A dome is an empty shell;
/// farms and utilities are placed on its interior slots afterward. Its footprint is an
/// oxygen-safe zone, and each dome has a terminal console below it.
/// </summary>
public static class SafeZones
{
    public static bool Contains(Dome dome, float x, float y) =>
        x >= dome.X && x <= dome.X + dome.Width &&
        y >= dome.Y && y <= dome.Y + dome.Height;

    public static bool Contains(GameState s, float x, float y)
    {
        foreach (var dome in s.Domes)
        {
            if (Contains(dome, x, y)) return true;
        }
        return false;
    }
}

/// <summary>Interior slot grid of a dome (positions only; occupancy lives on the dome).</summary>
public static class DomeLayout
{
    public static int SlotCount(BalanceConfig b) => b.DomeSlotColumns * b.DomeSlotRows;

    public static (float X, float Y) SlotPosition(BalanceConfig b, Dome dome, int slotIndex)
    {
        var columns = b.DomeSlotColumns;
        var row = slotIndex / columns;
        var column = slotIndex % columns;
        var step = b.PlotSize + b.DomeSlotGap;
        return (dome.X + b.DomeSlotMarginX + column * step,
                dome.Y + b.DomeSlotMarginY + row * step);
    }
}

public static class BuildSystem
{
    public const string StoredDomeItem = "cupula";

    public static float Snap(float v) =>
        MathF.Floor(v / GeneratedBiomes.TileSize) * GeneratedBiomes.TileSize;

    /// <summary>Updates the ghost to follow the player, and places it on interact.</summary>
    public static void Update(IGameContext ctx, in PlayerIntent intent)
    {
        var s = ctx.State;
        var b = ctx.Balance;

        if (s.PlacingItem == "parcela")
        {
            s.BuildX = Snap(s.Px - b.PlotSize / 2f);
            s.BuildY = Snap(s.Py - b.PlotSize / 2f);

            if (!intent.InteractPressed) return;

            if (!TryPlaceField(ctx, s.BuildX, s.BuildY, out var fieldReason))
            {
                ctx.Say(fieldReason, MessageKind.Warn);
                return;
            }
            ctx.Say("Colocas una bandeja de cultivo en el terreno.", MessageKind.Good);
            return;
        }

        if (s.PlacingItem == "beacon")
        {
            s.BuildX = Snap(s.Px - b.PlotSize / 2f);
            s.BuildY = Snap(s.Py - b.PlotSize / 2f);

            if (!intent.InteractPressed) return;

            if (!TryPlaceBeacon(ctx, s.BuildX, s.BuildY, out var beaconReason))
            {
                ctx.Say(beaconReason, MessageKind.Warn);
                return;
            }
            return;
        }

        s.BuildX = Snap(s.Px - b.DomeWidth / 2f);
        s.BuildY = Snap(s.Py - b.DomeHeight / 2f);

        if (!intent.InteractPressed) return;

        if (s.MovingDomeId is int movingId)
        {
            if (!TryMove(ctx, movingId, s.BuildX, s.BuildY, out var moveReason)) ctx.Say(moveReason, MessageKind.Warn);
            return;
        }

        if (!TryBuild(ctx, s.BuildX, s.BuildY, out var reason)) ctx.Say(reason, MessageKind.Warn);
    }

    public static bool HasStoredDome(GameState s) => s.Inventory[StoredDomeItem] > 0;

    public static bool CanPlaceAt(IGameContext ctx, float x, float y, out string reason)
    {
        var s = ctx.State;
        var b = ctx.Balance;

        // Leave room below the dome so its terminal stays reachable.
        if (x < b.MoveMinX || y < b.MoveMinY ||
            x + b.DomeWidth > b.MoveMaxX + 14 || y + b.DomeHeight + 20 > b.MoveMaxY)
        {
            reason = "La cúpula no cabe ahí.";
            return false;
        }

        if (!WorldOccupancy.IsClear(ctx, WorldOccupancy.PlacementRect(b, x, y), out var blocker))
        {
            reason = blocker == "la nave"
                ? "No puedes construir sobre la nave."
                : $"Ahí hay {blocker}.";
            return false;
        }

        if (!WorldOccupancy.IsClear(ctx, WorldOccupancy.PlacementTerminalRect(b, x, y), out blocker))
        {
            reason = Loc.F("El terminal chocaría con {0}.", blocker);
            return false;
        }

        // Moving an existing dome and placing a stored one cost no materials.
        if (s.MovingDomeId == null && !HasStoredDome(s))
        {
            foreach (var (item, amount) in b.DomeCost)
            {
                if (!s.Inventory.Has(item, amount))
                {
                    reason = Loc.F("Faltan materiales: {0}.", Loc.T(ctx.Items[item].Name));
                    return false;
                }
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

        var usedStored = HasStoredDome(s);
        if (usedStored) s.Inventory.Remove(StoredDomeItem, 1);
        else foreach (var (item, amount) in b.DomeCost) s.Inventory.Remove(item, amount);

        var id = s.Domes.Count == 0 ? 1 : s.Domes[^1].Id + 1;
        var dome = new Dome(id, x, y, b.DomeWidth, b.DomeHeight)
        {
            InternalTempC = b.DomeTargetTempC,
            ExternalTempC = EnvironmentSystem.ExternalTemp(GeneratedBiomes.AtWorld(x + b.DomeWidth / 2f, y + b.DomeHeight / 2f), s, b),
        };
        dome.Energy = Math.Min(b.DomeStartEnergy, dome.EnergyCapacity(b));

        s.Domes.Add(dome);
        s.Building = false;
        s.MovingDomeId = null;
        s.PlacingItem = null;
        ctx.Say(usedStored ? "Colocas una cúpula almacenada." : "Construyes una cúpula de invernadero vacía.",
            MessageKind.Good);
        return true;
    }

    /// <summary>Places an open-air crop tray on the nearest snapped cell.</summary>
    public static bool CanPlaceField(IGameContext ctx, float x, float y, out string reason)
    {
        var s = ctx.State;
        var b = ctx.Balance;

        if (x < b.MoveMinX || y < b.MoveMinY ||
            x + b.PlotSize > b.MoveMaxX || y + b.PlotSize > b.MoveMaxY)
        {
            reason = "La bandeja no cabe ahí.";
            return false;
        }

        if (!WorldOccupancy.IsClear(ctx, WorldOccupancy.PlacementRect(b, x, y, b.PlotSize, b.PlotSize), out var blocker))
        {
            reason = Loc.F("Ahí hay {0}.", blocker);
            return false;
        }

        foreach (var (item, amount) in b.FieldCost)
        {
            if (!s.Inventory.Has(item, amount))
            {
                reason = Loc.F("Faltan materiales: {0}.", Loc.T(ctx.Items[item].Name));
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    public static bool TryPlaceField(IGameContext ctx, float x, float y, out string reason)
    {
        if (!CanPlaceField(ctx, x, y, out reason)) return false;

        var s = ctx.State;
        var b = ctx.Balance;

        foreach (var (item, amount) in b.FieldCost) s.Inventory.Remove(item, amount);
        var plot = new Plot(x, y) { SlotIndex = -1 };
        s.Plots.Add(plot);

        s.Building = false;
        s.PlacingItem = null;
        reason = string.Empty;
        ctx.Say("Colocas una bandeja de cultivo en el terreno.", MessageKind.Good);
        return true;
    }

    /// <summary>Places an outdoor beacon that lights and heats nearby open-air plots.</summary>
    public static bool CanPlaceBeacon(IGameContext ctx, float x, float y, out string reason)
    {
        var s = ctx.State;
        var b = ctx.Balance;

        if (x < b.MoveMinX || y < b.MoveMinY ||
            x + b.PlotSize > b.MoveMaxX || y + b.PlotSize > b.MoveMaxY)
        {
            reason = "La baliza no cabe ahí.";
            return false;
        }

        if (!WorldOccupancy.IsClear(ctx, WorldOccupancy.PlacementRect(b, x, y, b.PlotSize, b.PlotSize), out var blocker))
        {
            reason = Loc.F("Ahí hay {0}.", blocker);
            return false;
        }

        foreach (var (item, amount) in b.BeaconCost)
        {
            if (!s.Inventory.Has(item, amount))
            {
                reason = Loc.F("Faltan materiales: {0}.", Loc.T(ctx.Items[item].Name));
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    public static bool TryPlaceBeacon(IGameContext ctx, float x, float y, out string reason)
    {
        if (!CanPlaceBeacon(ctx, x, y, out reason)) return false;

        var s = ctx.State;
        var b = ctx.Balance;
        foreach (var (item, amount) in b.BeaconCost) s.Inventory.Remove(item, amount);

        s.Beacons.Add(new Beacon(x + b.PlotSize / 2f, y + b.PlotSize / 2f));
        s.Building = false;
        s.PlacingItem = null;
        ctx.Say("Colocas una baliza de luz y calor.", MessageKind.Good);
        return true;
    }

    /// <summary>Relocates a dome (and everything placed inside) without spending materials.</summary>
    public static bool TryMove(IGameContext ctx, int domeId, float x, float y, out string reason)
    {
        var s = ctx.State;
        var dome = Find(s, domeId);
        if (dome == null)
        {
            reason = "La cúpula ya no existe.";
            return false;
        }

        var previous = s.MovingDomeId;
        s.MovingDomeId = domeId;
        var ok = CanPlaceAt(ctx, x, y, out reason);
        s.MovingDomeId = previous;
        if (!ok) return false;

        dome.X = x;
        dome.Y = y;
        RepositionPlots(ctx, dome);

        s.Building = false;
        s.MovingDomeId = null;
        s.PlacingItem = null;
        ctx.Say(Loc.F("Mueves la cúpula {0}.", dome.Id), MessageKind.Good);
        return true;
    }

    // ---- Interior fixtures ----------------------------------------------------

    public static IReadOnlyDictionary<string, int> FixtureCost(BalanceConfig b, FixtureKind kind) => kind switch
    {
        FixtureKind.Farm => b.FarmCost,
        FixtureKind.SolarPanel => b.SolarPanelCost,
        FixtureKind.Battery => b.BatteryCost,
        _ => EmptyCost,
    };

    public static string FixtureName(FixtureKind kind) => kind switch
    {
        FixtureKind.Farm => "parcela de cultivo",
        FixtureKind.SolarPanel => "panel solar",
        FixtureKind.Battery => "batería",
        _ => "objeto",
    };

    public static int FirstFreeSlot(BalanceConfig b, Dome dome)
    {
        var count = DomeLayout.SlotCount(b);
        for (var i = 0; i < count; i++)
        {
            if (!dome.IsSlotOccupied(i)) return i;
        }
        return -1;
    }

    public static bool CanBuildFixture(IGameContext ctx, Dome dome, int slotIndex, FixtureKind kind, out string reason)
    {
        var b = ctx.Balance;
        var s = ctx.State;

        if (slotIndex < 0 || slotIndex >= DomeLayout.SlotCount(b))
        {
            reason = "Esa parcela no existe.";
            return false;
        }
        if (dome.IsSlotOccupied(slotIndex))
        {
            reason = "Ese hueco ya está ocupado.";
            return false;
        }

        foreach (var (item, amount) in FixtureCost(b, kind))
        {
            if (!s.Inventory.Has(item, amount))
            {
                reason = Loc.F("Faltan materiales: {0}.", Loc.T(ctx.Items[item].Name));
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    public static bool BuildFixture(IGameContext ctx, Dome dome, int slotIndex, FixtureKind kind, out string reason)
    {
        if (!CanBuildFixture(ctx, dome, slotIndex, kind, out reason)) return false;

        var s = ctx.State;
        var b = ctx.Balance;
        foreach (var (item, amount) in FixtureCost(b, kind)) s.Inventory.Remove(item, amount);

        if (kind == FixtureKind.Farm)
        {
            var (x, y) = DomeLayout.SlotPosition(b, dome, slotIndex);
            var plot = new Plot(x, y) { Dome = dome, SlotIndex = slotIndex };
            dome.Plots.Add(plot);
            s.Plots.Add(plot);
        }
        else
        {
            dome.Fixtures.Add(new Fixture(kind, slotIndex));
        }

        ctx.Say(Loc.F("Colocas una {0} en la cúpula {1}.", Loc.T(FixtureName(kind)), dome.Id), MessageKind.Good);
        return true;
    }

    // ---- Dome management ------------------------------------------------------

    public static bool CanUpgrade(BalanceConfig b, Dome dome, Inventory inventory, out IReadOnlyDictionary<string, int> cost, out string reason)
    {
        if (dome.Level >= b.DomeMaxLevel)
        {
            cost = EmptyCost;
            reason = "La cúpula ya está al máximo.";
            return false;
        }

        cost = UpgradeCost(b, dome.Level + 1);
        foreach (var (item, amount) in cost)
        {
            if (!inventory.Has(item, amount))
            {
                reason = "Faltan materiales para la mejora.";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    public static bool Upgrade(IGameContext ctx, Dome dome, out string reason)
    {
        if (!CanUpgrade(ctx.Balance, dome, ctx.State.Inventory, out var cost, out reason)) return false;

        foreach (var (item, amount) in cost) ctx.State.Inventory.Remove(item, amount);
        dome.Level++;
        dome.Energy = Math.Min(dome.EnergyCapacity(ctx.Balance), dome.Energy + ctx.Balance.DomeStartEnergy);
        ctx.Say(Loc.F("Cúpula {0} mejorada al nivel {1}.", dome.Id, dome.Level), MessageKind.Good);
        return true;
    }

    /// <summary>Removes a dome and everything inside. If <paramref name="store"/> the shell goes to the inventory.</summary>
    public static void Remove(IGameContext ctx, Dome dome, bool store)
    {
        var s = ctx.State;

        foreach (var plot in dome.Plots)
        {
            if (plot.State != PlotState.Empty)
            {
                ctx.Say("La cúpula se retira: los cultivos de dentro se pierden.", MessageKind.Warn);
                break;
            }
        }

        foreach (var plot in dome.Plots) s.Plots.Remove(plot);
        dome.Plots.Clear();
        dome.Fixtures.Clear();
        s.Domes.Remove(dome);
        if (s.ActiveDomeId == dome.Id) s.ActiveDomeId = null;
        if (s.MovingDomeId == dome.Id) s.MovingDomeId = null;

        if (store)
        {
            s.Inventory.Add(StoredDomeItem, 1);
            ctx.Say("Guardas la cúpula en el inventario. Puedes colocarla sin gastar materiales.", MessageKind.Good);
        }
        else
        {
            ctx.Say("Desmontas la cúpula.", MessageKind.Warn);
        }
    }

    public static IReadOnlyDictionary<string, int> UpgradeCost(BalanceConfig b, int targetLevel) =>
        b.DomeUpgradeCost.TryGetValue(targetLevel, out var cost) ? cost : EmptyCost;

    public static Dome? Find(GameState s, int id)
    {
        foreach (var dome in s.Domes)
        {
            if (dome.Id == id) return dome;
        }
        return null;
    }

    private static readonly IReadOnlyDictionary<string, int> EmptyCost = new Dictionary<string, int>();

    private static void RepositionPlots(IGameContext ctx, Dome dome)
    {
        var b = ctx.Balance;
        for (var i = 0; i < dome.Plots.Count; i++)
        {
            var plot = dome.Plots[i];
            var slot = plot.SlotIndex >= 0 ? plot.SlotIndex : i;
            var (x, y) = DomeLayout.SlotPosition(b, dome, slot);
            plot.MoveTo(x, y);
        }
    }
}
