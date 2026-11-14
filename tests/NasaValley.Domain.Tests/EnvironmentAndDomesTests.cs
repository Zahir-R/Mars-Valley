using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class EnvironmentAndDomesTests
{
    private static GameSession NewSession()
    {
        var session = new GameSession(rng: new XorShiftRandom(1234));
        session.Start();
        return session;
    }

    private static Dome BuildDome(GameSession session, float x = 300, float y = 120)
    {
        ClearSceneObjects(session);
        foreach (var (item, amount) in session.Balance.DomeCost) session.State.Inventory[item] = amount;
        Assert.True(BuildSystem.TryBuild(session, x, y, out _));
        return session.State.Domes[^1];
    }

    private static void ClearSceneObjects(GameSession session)
    {
        session.State.Nodes.Clear();
        session.State.Pickups.Clear();
        session.State.Stations.Clear();
    }

    private static Plot AddFarm(GameSession session, Dome dome, int slot = 0)
    {
        foreach (var (item, amount) in session.Balance.FarmCost) session.State.Inventory[item] = amount;
        Assert.True(BuildSystem.BuildFixture(session, dome, slot, FixtureKind.Farm, out var reason), reason);
        return dome.Plots[^1];
    }

    [Fact]
    public void BiomesPartitionTheMap()
    {
        var session = NewSession();
        Assert.Equal("regolith_plains", GeneratedBiomes.At(3, 8).Id);
        Assert.Equal("gale_floor", GeneratedBiomes.At(3, 14).Id);
        Assert.Equal("high_dusty_plains", GeneratedBiomes.At(9, 25).Id);
    }

    [Fact]
    public void CannotBuildWithoutMaterials()
    {
        var session = NewSession();

        Assert.False(BuildSystem.CanPlaceAt(session, 300, 120, out var reason));
        Assert.False(string.IsNullOrEmpty(reason));
        Assert.Empty(session.State.Domes);
    }

    [Fact]
    public void BuildingADomeCreatesEmptyInteriorSlotsNotFarms()
    {
        var session = NewSession();

        var dome = BuildDome(session);

        Assert.Single(session.State.Domes);
        Assert.Empty(session.State.Plots);
        Assert.Empty(dome.Fixtures);
        Assert.True(DomeLayout.SlotCount(session.Balance) > 4);
        Assert.True(dome.HasFreeSlot(DomeLayout.SlotCount(session.Balance)));
    }

    [Fact]
    public void DomesCannotOverlap()
    {
        var session = NewSession();
        BuildDome(session, 300, 120);
        foreach (var (item, amount) in session.Balance.DomeCost) session.State.Inventory[item] = amount;

        Assert.False(BuildSystem.CanPlaceAt(session, 320, 130, out _));
    }

    [Fact]
    public void DomeChargesEnergyInDaylight()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        dome.Energy = 0;

        Assert.True(EnvironmentSystem.Light(session.State, session.Balance) > 0);
        DomeSystem.Update(session, 1.0);

        Assert.True(dome.Energy > 0);
    }

    [Fact]
    public void ColdPausesGrowthUntilTheDomeWarms()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        var plot = AddFarm(session, dome);

        new PlantCommand(plot, "tomate").Execute(session);
        new WaterCommand(plot).Execute(session);

        dome.InternalTempC = 10;
        var before = plot.Time;
        FarmingSystem.Update(session, 5);
        Assert.Equal(before, plot.Time);

        dome.InternalTempC = 22;
        FarmingSystem.Update(session, 5);
        Assert.True(plot.Time < before);
    }

    [Fact]
    public void PlantingUsesTheSelectedCrop()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        session.SetCrop("tomate");
        var plot = AddFarm(session, dome);

        new InteractCommand(new PlotTarget(plot)).Execute(session);

        Assert.Equal("tomate", plot.PlantId);
        Assert.Equal(PlotState.Dry, plot.State);
    }
}
