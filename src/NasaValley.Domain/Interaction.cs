namespace NasaValley.Domain;

public abstract record InteractionTarget;

public sealed record NodeTarget(ResourceNode Node) : InteractionTarget;

public sealed record PlotTarget(Plot Plot) : InteractionTarget;

public sealed record PickupTarget(Pickup Pickup) : InteractionTarget;

public sealed record StationTarget(Station Station) : InteractionTarget;

/// <summary>The management console placed below each built dome.</summary>
public sealed record DomeTerminalTarget(Dome Dome) : InteractionTarget;

/// <summary>An empty interior slot of a dome where a farm or utility can be built.</summary>
public sealed record DomeSlotTarget(Dome Dome, int SlotIndex) : InteractionTarget;

/// <summary>A utility already placed inside a dome (solar panel, battery, bunk).</summary>
public sealed record DomeFixtureTarget(Dome Dome, Fixture Fixture) : InteractionTarget;

public sealed record ShipTarget : InteractionTarget;

/// <summary>The beacon where the player leaves supplies for the survivor.</summary>
public sealed record SupplyCacheTarget : InteractionTarget;

/// <summary>The stranded survivor's site, reachable after the coordinates letter.</summary>
public sealed record SurvivorSiteTarget : InteractionTarget;

/// <summary>
/// Strategy for "what is the player looking at" and "what should the prompt say".
/// Extracted from the original findTarget/promptFor pair.
/// </summary>
public static class InteractionSystem
{
    public static float ShipDistance(GameState s, BalanceConfig b)
    {
        var dx = MathF.Max(MathF.Max(b.ShipX - s.Px, 0), s.Px - (b.ShipX + b.ShipWidth));
        var dy = MathF.Max(MathF.Max(b.ShipY - s.Py, 0), s.Py - (b.ShipY + b.ShipHeight));
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public static bool NearShip(GameState s, BalanceConfig b) =>
        ShipDistance(s, b) < b.ShipNearRadius;

    public static InteractionTarget? FindTarget(GameState s, BalanceConfig b)
    {
        InteractionTarget? best = null;
        var bestDistance = float.MaxValue;

        foreach (var n in s.Nodes)
        {
            if (n.Amount <= 0) continue;
            var d = Distance(n.X, n.Y, s.Px, s.Py);
            if (d < b.NodeInteractRadius && d < bestDistance)
            {
                bestDistance = d;
                best = new NodeTarget(n);
            }
        }

        foreach (var p in s.Plots)
        {
            var d = Distance(p.X + b.PlotSize / 2f, p.Y + b.PlotSize / 2f, s.Px, s.Py);
            if (d < b.PlotInteractRadius && d < bestDistance)
            {
                bestDistance = d;
                best = new PlotTarget(p);
            }
        }

        foreach (var dome in s.Domes)
        {
            var slotCount = DomeLayout.SlotCount(b);
            for (var i = 0; i < slotCount; i++)
            {
                if (dome.IsSlotOccupied(i)) continue;
                var (sx, sy) = DomeLayout.SlotPosition(b, dome, i);
                var d = Distance(sx + b.PlotSize / 2f, sy + b.PlotSize / 2f, s.Px, s.Py);
                if (d < b.PlotInteractRadius && d < bestDistance)
                {
                    bestDistance = d;
                    best = new DomeSlotTarget(dome, i);
                }
            }

            foreach (var fixture in dome.Fixtures)
            {
                var (fx, fy) = DomeLayout.SlotPosition(b, dome, fixture.SlotIndex);
                var d = Distance(fx + b.PlotSize / 2f, fy + b.PlotSize / 2f, s.Px, s.Py);
                if (d < b.PlotInteractRadius && d < bestDistance)
                {
                    bestDistance = d;
                    best = new DomeFixtureTarget(dome, fixture);
                }
            }
        }

        foreach (var p in s.Pickups)
        {
            if (p.Taken) continue;
            var d = Distance(p.X, p.Y, s.Px, s.Py);
            if (d < b.PickupInteractRadius && d < bestDistance)
            {
                bestDistance = d;
                best = new PickupTarget(p);
            }
        }

        foreach (var station in s.Stations)
        {
            var d = Distance(station.X, station.Y, s.Px, s.Py);
            if (d < b.StationInteractRadius && d < bestDistance)
            {
                bestDistance = d;
                best = new StationTarget(station);
            }
        }

        foreach (var dome in s.Domes)
        {
            var d = Distance(dome.TerminalX, dome.TerminalY, s.Px, s.Py);
            if (d < b.DomeTerminalRadius && d < bestDistance)
            {
                bestDistance = d;
                best = new DomeTerminalTarget(dome);
            }
        }

        var shipDistance = ShipDistance(s, b);
        if (shipDistance < b.ShipTargetRadius && shipDistance < bestDistance)
        {
            best = new ShipTarget();
        }

        if (s.NpcPhase == NpcPhase.AwaitingSupplies && !s.CacheFilled)
        {
            var d = Distance(s.CacheX, s.CacheY, s.Px, s.Py);
            if (d < b.CacheInteractRadius && d < bestDistance)
            {
                bestDistance = d;
                best = new SupplyCacheTarget();
            }
        }

        if (s.SiteUnlocked && !s.SurvivorFound && !s.SiteSkipped)
        {
            var d = Distance(s.SurvivorX, s.SurvivorY, s.Px, s.Py);
            if (d < b.SurvivorInteractRadius && d < bestDistance)
            {
                best = new SurvivorSiteTarget();
            }
        }

        return best;
    }

    public static string PromptFor(InteractionTarget? target, IGameContext ctx)
    {
        if (target is null) return string.Empty;

        var s = ctx.State;
        switch (target)
        {
            case NodeTarget nt:
                var nodeDef = ctx.Nodes[nt.Node.Type];
                var item = ctx.Items[nodeDef.ItemId];
                if (!ctx.HasToolTag(nodeDef.RequiredToolTag))
                {
                    return $"Necesitas herramienta para extraer {item.Name.ToLowerInvariant()}";
                }
                return $"Extraer {item.Name.ToLowerInvariant()} ({nt.Node.Amount} restantes)";

            case PickupTarget pt:
                var pickupDef = ctx.Items[pt.Pickup.ItemId];
                return $"Recoger {pickupDef.Name.ToLowerInvariant()} ({pt.Pickup.Amount})";

            case StationTarget st:
                return st.Station.Kind switch
                {
                    StationKind.Workbench => "Usar banco de trabajo",
                    _ => "Abrir consola",
                };

            case ShipTarget:
                var unread = NpcQuestSystem.UnreadCount(s);
                return unread > 0 ? $"Abrir panel de la nave ({unread} nuevo)" : "Abrir panel de la nave";

            case SupplyCacheTarget:
                var have = s.Inventory[s.CacheItemId];
                var itemName = ctx.Items[s.CacheItemId].Name.ToLowerInvariant();
                if (have >= s.CacheAmount) return $"Entregar {s.CacheAmount} {itemName}";
                return $"Faltan {s.CacheAmount - have} {itemName}";

            case SurvivorSiteTarget:
                return "Auxiliar a la superviviente";

            case DomeTerminalTarget dt:
                return $"Terminal de la cúpula {dt.Dome.Id} (nivel {dt.Dome.Level})";

            case DomeSlotTarget slot:
                var kind = s.PendingFixture ?? FixtureKind.Farm;
                return $"Colocar {BuildSystem.FixtureName(kind)} ({CostText(ctx, BuildSystem.FixtureCost(ctx.Balance, kind))})";

            case DomeFixtureTarget ft:
                return ft.Fixture.Kind switch
                {
                    FixtureKind.Bunk => "Descansar en la litera",
                    FixtureKind.SolarPanel => "Panel solar · abrir terminal",
                    FixtureKind.Battery => "Batería · abrir terminal",
                    _ => "Revisar instalación",
                };

            case PlotTarget pt:
                return PlotPrompt(ctx, pt.Plot);

            default:
                return string.Empty;
        }
    }

    private static string PlotPrompt(IGameContext ctx, Plot plot)
    {
        var s = ctx.State;
        ctx.Plants.TryGet(plot.PlantId, out var crop);

        switch (plot.State)
        {
            case PlotState.Empty:
                return s.Inventory["semillas"] > 0
                    ? "Plantar… (1 semilla)"
                    : "Sin semillas para plantar";

            case PlotState.Dry:
                var need = crop?.WaterPerGrowth ?? 1;
                return s.Inventory["agua"] >= need
                    ? $"Regar ({need} agua)"
                    : $"Necesitas {need} de agua";

            case PlotState.Grow:
                var blocker = FarmingSystem.GrowthBlocker(ctx, plot);
                return blocker.Length > 0 ? blocker : $"Creciendo… {Math.Ceiling(plot.Time)} s";

            default:
                return crop != null ? $"Cosechar {crop.Name.ToLowerInvariant()}" : "Cosechar";
        }
    }

    private static float Distance(float ax, float ay, float bx, float by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static string CostText(IGameContext ctx, IReadOnlyDictionary<string, int> cost)
    {
        if (cost.Count == 0) return "gratis";
        var text = string.Empty;
        foreach (var (item, amount) in cost)
        {
            if (text.Length > 0) text += " · ";
            text += $"{ctx.Items[item].Name.ToLowerInvariant()} x{amount}";
        }
        return text;
    }
}
