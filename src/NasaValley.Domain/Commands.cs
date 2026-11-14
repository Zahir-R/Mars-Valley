namespace NasaValley.Domain;

/// <summary>Command pattern: every discrete player action is a self-contained object.</summary>
public interface IGameCommand
{
    void Execute(IGameContext ctx);
}

public sealed class MineNodeCommand(ResourceNode node) : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        var b = ctx.Balance;
        var def = ctx.Nodes[node.Type];
        var item = ctx.Items[def.ItemId];

        node.Amount--;
        node.Shake = b.NodeShakeTime;
        s.Inventory.Add(item.Id, 1);
        if (item.Id == "hielo") s.Stats.Ice++;
        s.Vitals.Energy = Math.Max(0, s.Vitals.Energy - b.MineEnergyCost);

        for (var i = 0; i < 5; i++)
        {
            s.Particles.Add(new Particle
            {
                X = node.X + ((float)ctx.Rng.NextDouble() - 0.5f) * 14,
                Y = node.Y + ((float)ctx.Rng.NextDouble() - 0.5f) * 8,
                Vx = ((float)ctx.Rng.NextDouble() - 0.5f) * 60,
                Vy = -20 - (float)ctx.Rng.NextDouble() * 40,
                Life = 0.5,
                Color = def.Color,
            });
        }

        s.Floats.Add(new FloatingText
        {
            X = node.X,
            Y = node.Y - 30,
            Text = "+1 " + item.Name,
            Color = def.Color,
            Life = 0.9,
        });

        if (node.Amount <= 0)
        {
            node.Regen = b.NodeRegenTime;
            ctx.Say($"Depósito de {item.Name.ToLowerInvariant()} agotado. Se regenera en unos {b.NodeRegenTime:0} s.");
        }
    }
}

public sealed class PlantCommand(Plot plot) : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        if (s.Inventory["semillas"] > 0)
        {
            s.Inventory.Remove("semillas", 1);
            plot.State = PlotState.Dry;
            s.Stats.Planted++;
            ctx.Say("Plantas una papa. Falta regarla.");
        }
        else
        {
            ctx.Say("No tienes semillas.", MessageKind.Warn);
        }
    }
}

public sealed class WaterCommand(Plot plot) : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        if (s.Inventory["agua"] > 0)
        {
            s.Inventory.Remove("agua", 1);
            plot.State = PlotState.Grow;
            plot.Time = ctx.Balance.GrowTime;
            ctx.Say($"Riegas la planta. Madura en unos {ctx.Balance.GrowTime:0} s.");
        }
        else
        {
            ctx.Say("Necesitas agua. Funde hielo en la nave.", MessageKind.Warn);
        }
    }
}

public sealed class HarvestCommand(Plot plot) : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        s.Inventory.Add("papas", ctx.Balance.HarvestPotatoes);
        var seed = ctx.Rng.NextDouble() < ctx.Balance.HarvestSeedChance;
        if (seed) s.Inventory.Add("semillas", 1);
        s.Stats.Harvested++;
        plot.State = PlotState.Empty;
        ctx.Say("Cosechas 2 papas" + (seed ? " y 1 semilla." : "."), MessageKind.Good);
    }
}

public sealed class RepairPartCommand(string partId) : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        var b = ctx.Balance;
        if (!s.Running || s.Paused || s.Over) return;
        if (RepairSystem.IsRepaired(s, partId)) return;

        if (!ctx.Parts.TryGet(partId, out var part)) return;
        if (RepairSystem.IsLocked(s, ctx.Parts, part)) return;
        if (!InteractionSystem.NearShip(s, b)) return;
        if (!RepairSystem.CanPay(s, part)) return;

        RepairSystem.Pay(s, part);
        s.Repairs[partId] = true;
        ctx.Say("Reparado: " + part.Name + ".", MessageKind.Good);

        if (RepairSystem.Count(s) >= ctx.Parts.Parts.Count)
        {
            s.WinTimer = b.WinDelay;
            ctx.Say("¡Motor encendido! Preparando el despegue…", MessageKind.Good);
        }
    }
}

public sealed class EatCommand : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        if (!s.Running || s.Paused || s.Over) return;
        if (s.Inventory["papas"] < 1)
        {
            ctx.Say("No tienes papas.", MessageKind.Warn);
            return;
        }

        s.Inventory.Remove("papas", 1);
        s.Vitals.Food = Math.Min(100, s.Vitals.Food + ctx.Balance.EatFoodGain);
        s.Vitals.Energy = Math.Min(100, s.Vitals.Energy + ctx.Balance.EatEnergyGain);
        ctx.Say("Comes una papa: +30 alimento, +10 energía.", MessageKind.Good);
    }
}

public sealed class RefillO2Command : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        var b = ctx.Balance;
        if (!s.Running || s.Paused || s.Over) return;
        if (!InteractionSystem.NearShip(s, b)) return;
        if (s.Inventory["hielo"] < 1 || s.Vitals.O2 >= 100) return;

        s.Inventory.Remove("hielo", 1);
        s.Vitals.O2 = Math.Min(100, s.Vitals.O2 + b.O2RefillAmount);
        ctx.Say("Recargas oxígeno: +25 %.", MessageKind.Good);
    }
}

public sealed class MeltIceCommand : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        var b = ctx.Balance;
        if (!s.Running || s.Paused || s.Over) return;
        if (!InteractionSystem.NearShip(s, b)) return;
        if (s.Inventory["hielo"] < 1) return;

        s.Inventory.Remove("hielo", 1);
        s.Inventory.Add("agua", b.MeltWaterGain);
        ctx.Say("Fundes 1 hielo: +2 de agua.");
    }
}

public sealed class InteractCommand(InteractionTarget target) : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        switch (target)
        {
            case PlotTarget pt:
                switch (pt.Plot.State)
                {
                    case PlotState.Empty:
                        new PlantCommand(pt.Plot).Execute(ctx);
                        break;
                    case PlotState.Dry:
                        new WaterCommand(pt.Plot).Execute(ctx);
                        break;
                    case PlotState.Ready:
                        new HarvestCommand(pt.Plot).Execute(ctx);
                        break;
                }
                break;
        }
    }
}
