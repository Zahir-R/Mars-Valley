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
        foreach (var (item, amount) in session.Balance.DomeCost) session.State.Inventory[item] = amount;
        Assert.True(BuildSystem.TryBuild(session, x, y, out _));
        return session.State.Domes[^1];
    }

    [Fact]
    public void AreasPartitionTheMapByTemperature()
    {
        var session = NewSession();
        Assert.Equal("crater", session.Areas.At(100, 100).Id);
        Assert.Equal("llanura", session.Areas.At(400, 100).Id);
        Assert.Equal("afloramiento", session.Areas.At(700, 100).Id);
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
    public void BuildingADomeCreatesItsPlotSlots()
    {
        var session = NewSession();

        var dome = BuildDome(session);

        Assert.Single(session.State.Domes);
        Assert.Equal(session.Balance.DomeSlotColumns * session.Balance.DomeSlotRows, session.State.Plots.Count);
        Assert.All(session.State.Plots, p => Assert.Same(dome, p.Dome));
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
        BuildDome(session);
        var plot = session.State.Plots[0];

        new PlantCommand(plot, "tomate").Execute(session);
        new WaterCommand(plot).Execute(session);
        var dome = plot.Dome!;

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
        BuildDome(session);
        session.SetCrop("tomate");
        var plot = session.State.Plots[0];

        new InteractCommand(new PlotTarget(plot)).Execute(session);

        Assert.Equal("tomate", plot.PlantId);
        Assert.Equal(PlotState.Dry, plot.State);
    }
}
