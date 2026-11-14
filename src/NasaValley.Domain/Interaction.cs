namespace NasaValley.Domain;

public abstract record InteractionTarget;

public sealed record NodeTarget(ResourceNode Node) : InteractionTarget;

public sealed record PlotTarget(Plot Plot) : InteractionTarget;

public sealed record PickupTarget(Pickup Pickup) : InteractionTarget;

public sealed record StationTarget(Station Station) : InteractionTarget;

public sealed record ShipTarget : InteractionTarget;

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

        var shipDistance = ShipDistance(s, b);
        if (shipDistance < b.ShipTargetRadius && shipDistance < bestDistance)
        {
            best = new ShipTarget();
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
                    StationKind.Farm => "Consola del invernadero",
                    _ => "Abrir consola",
                };

            case ShipTarget:
                return "Abrir panel de la nave";

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
                return s.Inventory["semillas"] > 0
                    ? $"Plantar {selected.Name.ToLowerInvariant()} (1 semilla)"
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
}
