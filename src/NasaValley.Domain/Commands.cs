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

        if (node.Amount <= 0) return;

        if (!ctx.HasToolTag(def.RequiredToolTag))
        {
            ctx.Say($"Necesitas una herramienta adecuada para extraer {item.Name.ToLowerInvariant()}.",
                MessageKind.Warn);
            return;
        }

        node.Shake = b.NodeShakeTime;
        s.Vitals.Energy = Math.Max(0, s.Vitals.Energy - b.MineEnergyCost);
        node.Progress++;

        if (node.Progress < def.HitsPerUnit)
        {
            SpawnParticles(ctx, node, def.Color, 2);
            return;
        }

        node.Progress = 0;
        node.Amount--;
        s.Inventory.Add(item.Id, 1);
        if (item.Id == "hielo") s.Stats.Ice++;

        SpawnParticles(ctx, node, def.Color, 5);
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

    private static void SpawnParticles(IGameContext ctx, ResourceNode node, string color, int count)
    {
        var s = ctx.State;
        for (var i = 0; i < count; i++)
        {
            s.Particles.Add(new Particle
            {
                X = node.X + ((float)ctx.Rng.NextDouble() - 0.5f) * 14,
                Y = node.Y + ((float)ctx.Rng.NextDouble() - 0.5f) * 8,
                Vx = ((float)ctx.Rng.NextDouble() - 0.5f) * 60,
                Vy = -20 - (float)ctx.Rng.NextDouble() * 40,
                Life = 0.5,
                Color = color,
            });
        }
    }
}

public sealed class PickupCommand(Pickup pickup) : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        if (pickup.Taken) return;

        var def = ctx.Items[pickup.ItemId];
        s.Inventory.Add(pickup.ItemId, pickup.Amount);
        pickup.Taken = true;

        s.Floats.Add(new FloatingText
        {
            X = pickup.X,
            Y = pickup.Y - 22,
            Text = "+" + pickup.Amount + " " + def.Name,
            Color = def.Color,
            Life = 0.9,
        });
        ctx.Say($"Recoges {pickup.Amount} {def.Name.ToLowerInvariant()}.", MessageKind.Good);
    }
}

public sealed class CraftCommand(RecipeDefinition recipe) : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        if (!s.Running || s.Paused || s.Over) return;

        foreach (var (item, amount) in recipe.Ingredients)
        {
            if (s.Inventory.Has(item, amount)) continue;
            ctx.Say($"Faltan materiales para {recipe.Name}.", MessageKind.Warn);
            return;
        }

        foreach (var (item, amount) in recipe.Ingredients) s.Inventory.Remove(item, amount);
        s.Inventory.Add(recipe.OutputItemId, recipe.OutputAmount);

        var name = ctx.Items.TryGet(recipe.OutputItemId, out var def) ? def.Name : recipe.OutputItemId;
        ctx.Say($"Fabricado: {name}.", MessageKind.Good);
    }
}

public sealed class PlantCommand(Plot plot, string plantId) : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        if (!ctx.Plants.TryGet(plantId, out var plant)) return;

        if (plot.Dome != null)
        {
            // Crops survive best inside a pressure-safe dome; open trays are riskier.
            if (!SafeZones.Contains(plot.Dome, plot.X + ctx.Balance.PlotSize / 2f, plot.Y + ctx.Balance.PlotSize / 2f))
            {
                ctx.Say("Solo puedes plantar dentro de una cúpula.", MessageKind.Warn);
                return;
            }
        }

        if (s.Inventory["semillas"] < 1)
        {
            ctx.Say("No tienes semillas.", MessageKind.Warn);
            return;
        }

        s.Inventory.Remove("semillas", 1);
        plot.PlantId = plantId;
        plot.State = PlotState.Dry;
        plot.Time = plant.GrowTime;
        s.Stats.Planted++;
        ctx.Say($"Plantas {plant.Name.ToLowerInvariant()}. Falta regarla.");
    }
}

public sealed class WaterCommand(Plot plot) : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        if (!ctx.Plants.TryGet(plot.PlantId, out var plant)) return;

        var need = plant.WaterPerGrowth;
        if (s.Inventory["agua"] < need)
        {
            ctx.Say($"Necesitas {need} de agua. Funde hielo en la nave.", MessageKind.Warn);
            return;
        }

        s.Inventory.Remove("agua", need);
        plot.State = PlotState.Grow;
        plot.Time = plant.GrowTime;
        ctx.Say($"Riegas la planta. Madura en unos {plant.GrowTime:0} s.");
    }
}

public sealed class HarvestCommand(Plot plot) : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        if (!ctx.Plants.TryGet(plot.PlantId, out var plant)) return;

        var name = ctx.Items[plant.HarvestItemId].Name;
        s.Inventory.Add(plant.HarvestItemId, plant.HarvestYield);
        var seed = ctx.Rng.NextDouble() < ctx.Balance.HarvestSeedChance;
        if (seed) s.Inventory.Add("semillas", 1);
        s.Stats.Harvested++;
        plot.State = PlotState.Empty;
        ctx.Say($"Cosechas {plant.HarvestYield} {name.ToLowerInvariant()}" + (seed ? " y 1 semilla." : "."),
            MessageKind.Good);
    }
}

public sealed class RestCommand(Dome dome) : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        var b = ctx.Balance;
        if (!s.Running || s.Paused || s.Over) return;
        if (!dome.HasBunk)
        {
            ctx.Say("No hay litera en esta cúpula.", MessageKind.Warn);
            return;
        }
        if (s.RestCooldown > 0)
        {
            ctx.Say($"La litera aún no está lista ({Math.Ceiling(s.RestCooldown)} s).", MessageKind.Warn);
            return;
        }
        if (s.Vitals.Energy >= 100)
        {
            ctx.Say("Ya tienes toda la energía.", MessageKind.Info);
            return;
        }

        s.Vitals.Energy = Math.Min(100, s.Vitals.Energy + b.RestEnergyGain);
        s.RestCooldown = b.RestCooldown;
        ctx.Say($"Descansas en la litera: +{b.RestEnergyGain} energía.", MessageKind.Good);
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
    private static readonly string[] Foods = { "papas", "lechuga", "tomate", "trigo", "rabano" };

    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        if (!s.Running || s.Paused || s.Over) return;

        string? food = null;
        if (s.FoodSource != null && s.Inventory[s.FoodSource] > 0 &&
            ctx.Items.TryGet(s.FoodSource, out var source) && source.HasTag("Comida"))
        {
            food = s.FoodSource;
        }

        if (food == null)
        {
            foreach (var id in Foods)
            {
                if (s.Inventory[id] > 0) { food = id; break; }
            }
        }

        if (food == null)
        {
            ctx.Say("No tienes comida. Cultiva algo en una cúpula.", MessageKind.Warn);
            return;
        }

        EatOne(ctx, food);
    }

    internal static void EatOne(IGameContext ctx, string foodId)
    {
        var s = ctx.State;
        s.Inventory.Remove(foodId, 1);

        // If the selected source just ran out, switch the displayed source automatically.
        if (s.FoodSource == foodId && s.Inventory[foodId] <= 0)
        {
            string? next = null;
            foreach (var id in Foods)
            {
                if (s.Inventory[id] > 0) { next = id; break; }
            }
            s.FoodSource = next;
        }

        s.Vitals.Food = Math.Min(100, s.Vitals.Food + ctx.Balance.EatFoodGain);
        s.Vitals.Energy = Math.Min(100, s.Vitals.Energy + ctx.Balance.EatEnergyGain);
        ctx.Say($"Comes {ctx.Items[foodId].Name.ToLowerInvariant()}: +{ctx.Balance.EatFoodGain} alimento, +{ctx.Balance.EatEnergyGain} energía.",
            MessageKind.Good);
    }
}

public sealed class EatItemCommand(string itemId) : IGameCommand
{
    public void Execute(IGameContext ctx)
    {
        var s = ctx.State;
        if (!s.Running || s.Paused || s.Over) return;
        if (!ctx.Items.TryGet(itemId, out var def) || !def.HasTag("Comida")) return;

        if (s.Inventory[itemId] < 1)
        {
            ctx.Say($"No tienes {def.Name.ToLowerInvariant()}.", MessageKind.Warn);
            return;
        }

        EatCommand.EatOne(ctx, itemId);
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
        if (s.ShipO2Charges <= 0)
        {
            ctx.Say("El O₂ de la nave está agotado.", MessageKind.Warn);
            return;
        }
        if (s.Inventory["hielo"] < 1 || s.Vitals.O2 >= 100) return;

        s.Inventory.Remove("hielo", 1);
        s.Vitals.O2 = 100;
        s.ShipO2Charges -= 1;
        ctx.Say($"Recargas oxígeno: +100 %. ({s.ShipO2Charges} cargas restantes)", MessageKind.Good);
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
            case PickupTarget pk:
                new PickupCommand(pk.Pickup).Execute(ctx);
                break;
            case PlotTarget pt:
                switch (pt.Plot.State)
                {
                    case PlotState.Empty:
                        new PlantCommand(pt.Plot, ctx.State.SelectedCrop).Execute(ctx);
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
