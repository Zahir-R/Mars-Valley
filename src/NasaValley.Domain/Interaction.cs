namespace NasaValley.Domain;

public abstract record InteractionTarget;

public sealed record NodeTarget(ResourceNode Node) : InteractionTarget;

public sealed record PlotTarget(Plot Plot) : InteractionTarget;

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
                var item = ctx.Items[ctx.Nodes[nt.Node.Type].ItemId];
                return $"Extraer {item.Name.ToLowerInvariant()} ({nt.Node.Amount} restantes)";

            case ShipTarget:
                return "Abrir panel de la nave";

            case PlotTarget pt:
                var plot = pt.Plot;
                return plot.State switch
                {
                    PlotState.Empty => s.Inventory["semillas"] > 0
                        ? "Plantar papa (1 semilla)"
                        : "Sin semillas para plantar",
                    PlotState.Dry => s.Inventory["agua"] > 0
                        ? "Regar (1 agua)"
                        : "Necesita agua: fúndela en la nave",
                    PlotState.Grow => "Creciendo… " + Math.Ceiling(plot.Time) + " s",
                    _ => "Cosechar papas",
                };

            default:
                return string.Empty;
        }
    }

    private static float Distance(float ax, float ay, float bx, float by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
