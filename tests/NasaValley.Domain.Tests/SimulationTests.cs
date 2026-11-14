using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class SimulationTests
{
    private static GameSession NewSession()
    {
        var session = new GameSession(rng: new XorShiftRandom(1234));
        session.Start();
        return session;
    }

    [Fact]
    public void MiningAddsItemAndConsumesEnergy()
    {
        var session = NewSession();
        var node = session.State.Nodes[0];
        session.State.Px = node.X;
        session.State.Py = node.Y;
        var energyBefore = session.State.Vitals.Energy;

        session.Tick(new PlayerIntent(false, false, false, false, InteractHeld: true, InteractPressed: false), 0.1);

        Assert.Equal(3, node.Amount);
        Assert.Equal(session.Balance.StartIce + 1, session.State.Inventory["hielo"]);
        Assert.Equal(1, session.State.Stats.Ice);
        Assert.True(session.State.Vitals.Energy < energyBefore);
    }

    [Fact]
    public void EmptyNodeRegeneratesAfterCooldown()
    {
        var session = NewSession();
        var node = session.State.Nodes[0];
        node.Amount = 0;
        node.Regen = 0.5;

        NodeSystem.Update(session, 0.6);

        Assert.Equal(session.Balance.NodeAmount, node.Amount);
    }

    [Fact]
    public void FarmingLoopProducesPotatoesAndMaybeSeeds()
    {
        var session = NewSession();
        var plot = session.State.Plots[0];

        new PlantCommand(plot).Execute(session);
        Assert.Equal(PlotState.Dry, plot.State);
        Assert.Equal(1, session.State.Stats.Planted);

        new WaterCommand(plot).Execute(session);
        Assert.Equal(PlotState.Grow, plot.State);

        FarmingSystem.Update(session, session.Balance.GrowTime + 0.1);
        Assert.Equal(PlotState.Ready, plot.State);

        var potatoesBefore = session.State.Inventory["papas"];
        new HarvestCommand(plot).Execute(session);
        Assert.Equal(potatoesBefore + session.Balance.HarvestPotatoes, session.State.Inventory["papas"]);
        Assert.Equal(1, session.State.Stats.Harvested);
        Assert.Equal(PlotState.Empty, plot.State);
    }

    [Fact]
    public void RepairRequiresProximityAndMaterials()
    {
        var session = NewSession();
        session.State.Px = session.Balance.ShipX + session.Balance.ShipWidth + 5;
        session.State.Py = session.Balance.ShipY + 40;

        session.Repair("casco");
        Assert.False(RepairSystem.IsRepaired(session.State, "casco"));

        session.State.Inventory["hierro"] = 5;
        session.State.Inventory["silicio"] = 2;
        session.Repair("casco");

        Assert.True(RepairSystem.IsRepaired(session.State, "casco"));
        Assert.Equal(0, session.State.Inventory["hierro"]);
    }

    [Fact]
    public void MotorIsLockedUntilTheOtherPartsAreRepaired()
    {
        var session = NewSession();
        session.State.Px = session.Balance.ShipX + session.Balance.ShipWidth + 5;
        session.State.Py = session.Balance.ShipY + 40;
        foreach (var item in session.Items.Order) session.State.Inventory[item] = 99;

        session.Repair("motor");
        Assert.False(RepairSystem.IsRepaired(session.State, "motor"));

        foreach (var part in session.Parts.Parts)
        {
            if (part.Id != "motor") session.Repair(part.Id);
        }
        session.Repair("motor");

        Assert.True(RepairSystem.IsRepaired(session.State, "motor"));
        Assert.True(session.State.WinTimer > 0);
    }

    [Fact]
    public void ReparingEverythingEventuallyWins()
    {
        var session = NewSession();
        session.State.Px = session.Balance.ShipX + session.Balance.ShipWidth + 5;
        session.State.Py = session.Balance.ShipY + 40;
        foreach (var item in session.Items.Order) session.State.Inventory[item] = 99;

        foreach (var part in session.Parts.Parts) session.Repair(part.Id);

        session.Tick(PlayerIntent.None, session.Balance.WinDelay + 0.1);

        Assert.Equal(FlowState.Won, session.State.Flow);
        Assert.True(session.State.Win);
    }

    [Fact]
    public void StormStartsAndEnds()
    {
        var session = NewSession();
        session.State.Storm.Next = 0.05;

        session.Tick(PlayerIntent.None, 0.1);
        Assert.Equal(session.Balance.StormDuration, session.State.Storm.Left, 3);

        session.Tick(PlayerIntent.None, session.Balance.StormDuration + 0.1);
        Assert.True(session.State.Storm.Left <= 0);
    }

    [Fact]
    public void SuffocationEndsTheRun()
    {
        var session = NewSession();
        session.State.Vitals.O2 = 0;
        session.State.Vitals.Health = 1;

        session.Tick(PlayerIntent.None, 1.0);

        Assert.Equal(FlowState.Lost, session.State.Flow);
        Assert.False(session.State.Win);
    }

    [Fact]
    public void EatingRestoresFoodAndEnergy()
    {
        var session = NewSession();
        session.State.Vitals.Food = 10;
        session.State.Vitals.Energy = 10;

        session.Eat();

        Assert.Equal(10 + session.Balance.EatFoodGain, session.State.Vitals.Food);
        Assert.Equal(10 + session.Balance.EatEnergyGain, session.State.Vitals.Energy);
    }

    [Fact]
    public void MissionsTrackProgress()
    {
        var session = NewSession();
        var goal = session.Missions.Goals[0];
        Assert.False(MissionSystem.Evaluate(session.State, goal, session.Parts).Done);

        session.State.Stats.Ice = 5;

        var progress = MissionSystem.Evaluate(session.State, goal, session.Parts);
        Assert.True(progress.Done);
        Assert.Equal("5/5", progress.Progress);
    }
}
