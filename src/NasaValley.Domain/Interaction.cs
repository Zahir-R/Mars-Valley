namespace NasaValley.Domain;

public abstract record InteractionTarget;

public sealed record NodeTarget(ResourceNode Node) : InteractionTarget;

public sealed record PlotTarget(Plot Plot) : InteractionTarget;

public sealed record PickupTarget(Pickup Pickup) : InteractionTarget;

public sealed record StationTarget(Station Station) : InteractionTarget;

/// <summary>The management console placed below each built dome.</summary>
public sealed record DomeTerminalTarget(Dome Dome) : InteractionTarget;

/// <summary>An empty interior slot where the fixture chosen in the terminal is placed with E.</summary>
public sealed record DomeSlotTarget(Dome Dome, int SlotIndex) : InteractionTarget;

/// <summary>A utility already placed inside a dome (solar panel, battery).</summary>
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
            // Empty slots are only targeted after choosing a fixture in the terminal.
            if (s.PendingFixture != null)
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
                    return Loc.F("Necesitas herramienta para extraer {0}", Loc.T(item.Name));
                }
                return Loc.F("Extraer {0} ({1} restantes)", Loc.T(item.Name), nt.Node.Amount);

            case PickupTarget pt:
                var pickupDef = ctx.Items[pt.Pickup.ItemId];
                return Loc.F("Recoger {0} ({1})", Loc.T(pickupDef.Name), pt.Pickup.Amount);

            case StationTarget st:
                return st.Station.Kind switch
                {
                    StationKind.Workbench => Loc.T("Usar banco de trabajo"),
                    _ => Loc.T("Abrir consola"),
                };

            case ShipTarget:
                var unread = NpcQuestSystem.UnreadCount(s);
                return unread > 0
                    ? Loc.F("Abrir panel de la nave ({0} nuevo)", unread)
                    : Loc.T("Abrir panel de la nave");

            case SupplyCacheTarget:
                var have = s.Inventory[s.CacheItemId];
                var itemName = Loc.T(ctx.Items[s.CacheItemId].Name);
                if (have >= s.CacheAmount) return Loc.F("Entregar {0} {1}", s.CacheAmount, itemName);
                return Loc.F("Faltan {0} {1}", s.CacheAmount - have, itemName);

            case SurvivorSiteTarget:
                return Loc.T("Auxiliar a la superviviente");

            case DomeTerminalTarget dt:
                return Loc.F("Terminal de la cúpula {0} (nivel {1})", dt.Dome.Id, dt.Dome.Level);

            case DomeSlotTarget slot:
                var kind = s.PendingFixture ?? FixtureKind.Farm;
                return Loc.F("Colocar {0} ({1})", Loc.T(BuildSystem.FixtureName(kind)),
                    CostText(ctx, BuildSystem.FixtureCost(ctx.Balance, kind)));

            case DomeFixtureTarget ft:
                return ft.Fixture.Kind switch
                {
                    FixtureKind.SolarPanel => Loc.T(ft.Fixture.Deployed ? "Retraer panel" : "Extender panel"),
                    FixtureKind.Battery => Loc.T("Recargar energía"),
                    _ => Loc.T("Revisar instalación"),
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
                var selected = ctx.Plants[s.SelectedCrop];
                return Loc.T(s.Inventory[selected.SeedId] > 0
                    ? "Plantar… (1 semilla)"
                    : "Sin semillas para plantar");

            case PlotState.Dry:
                var need = crop?.WaterPerGrowth ?? 1;
                return s.Inventory["agua"] >= need
                    ? Loc.F("Regar ({0} agua)", need)
                    : Loc.F("Necesitas {0} de agua", need);

            case PlotState.Grow:
                var blocker = FarmingSystem.GrowthBlocker(ctx, plot);
                return blocker.Length > 0 ? Loc.T(blocker) : Loc.F("Creciendo… {0} s", Math.Ceiling(plot.Time));

            default:
                return crop != null ? Loc.F("Cosechar {0}", Loc.T(crop.Name)) : Loc.T("Cosechar");
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
        if (cost.Count == 0) return Loc.T("gratis");
        var text = string.Empty;
        foreach (var (item, amount) in cost)
        {
            if (text.Length > 0) text += " · ";
            text += $"{Loc.T(ctx.Items[item].Name)} x{amount}";
        }
        return text;
    }
}
