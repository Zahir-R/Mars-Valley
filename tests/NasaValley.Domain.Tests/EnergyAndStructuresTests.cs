using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class EnergyAndStructuresTests
{
    private static GameSession NewSession()
    {
        var session = new GameSession(rng: new XorShiftRandom(2024));
        session.Start();
        return session;
    }

    private static void ClearScene(GameSession session)
    {
        session.State.Nodes.Clear();
        session.State.Pickups.Clear();
        session.State.Stations.Clear();
    }

    private static Dome BuildDome(GameSession session, float x = 300, float y = 120)
    {
        ClearScene(session);
        foreach (var (item, amount) in session.Balance.DomeCost) session.State.Inventory[item] = amount;
        Assert.True(BuildSystem.TryBuild(session, x, y, out _));
        return session.State.Domes[^1];
    }

    private static Fixture AddFixture(GameSession session, Dome dome, int slot, FixtureKind kind)
    {
        foreach (var (item, amount) in BuildSystem.FixtureCost(session.Balance, kind))
        {
            session.State.Inventory[item] = amount;
        }
        Assert.True(BuildSystem.BuildFixture(session, dome, slot, kind, out var reason), reason);
        return dome.Fixtures[^1];
    }

    [Fact]
    public void BeaconMakesAnOpenAirPlotDomeLike()
    {
        var session = NewSession();
        var plot = new Plot(200, 200);
        session.State.Plots.Add(plot);
        session.State.Beacons.Add(new Beacon(210, 210) { Powered = true });

        new PlantCommand(plot, "tomate").Execute(session);
        new WaterCommand(plot).Execute(session);
        Assert.Equal(PlotState.Grow, plot.State);

        var (light, temp) = FarmingSystem.Conditions(session, plot);
        Assert.True(light >= session.Balance.BeaconLight - 1e-6);
        Assert.True(temp >= session.Balance.DomeTargetTempC - 1e-6);

        var before = plot.Time;
        FarmingSystem.Update(session, 5);
        Assert.True(plot.Time < before);
    }

    [Fact]
    public void RetractedPanelsAreSafeFromStorms()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        var panel = AddFixture(session, dome, 0, FixtureKind.SolarPanel);

        session.State.Storm.Left = 10;
        panel.Deployed = false;
        DamageSystem.Update(session, 60);

        Assert.Single(dome.Fixtures);
        Assert.Equal(0, panel.Damage, 3);

        panel.Deployed = true;
        DamageSystem.Update(session, 60);
        Assert.Empty(dome.Fixtures);
    }

    [Fact]
    public void StormDestroysOpenAirCrops()
    {
        var session = NewSession();
        var plot = new Plot(200, 200) { State = PlotState.Grow };
        session.State.Plots.Add(plot);
        session.State.Storm.Left = 10;

        DamageSystem.Update(session, 50);

        Assert.Empty(session.State.Plots);
    }

    [Fact]
    public void StormDestroysBeacons()
    {
        var session = NewSession();
        session.State.Beacons.Add(new Beacon(200, 200));
        session.State.Storm.Left = 10;

        DamageSystem.Update(session, 60);

        Assert.Empty(session.State.Beacons);
    }

    [Fact]
    public void NoHealthDrainInsideDomeAtZeroOxygen()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        session.State.Px = dome.CenterX;
        session.State.Py = dome.CenterY;
        session.State.Vitals.O2 = 0;
        session.State.Vitals.Food = 100;
        session.State.Vitals.Health = 55;

        SurvivalSystem.Update(session, 10);

        Assert.Equal(55, session.State.Vitals.Health, 3);
    }

    [Fact]
    public void BatteryChargesToFortyWithAPanel()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        AddFixture(session, dome, 0, FixtureKind.SolarPanel);
        var battery = AddFixture(session, dome, 1, FixtureKind.Battery);

        Assert.Equal(0, battery.Stored, 3);
        DomeSystem.Update(session, 200);
        Assert.Equal(session.Balance.BatteryStorage, battery.Stored, 3);
    }

    [Fact]
    public void BatteryDoesNotChargeWithoutAPanel()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        var battery = AddFixture(session, dome, 0, FixtureKind.Battery);

        DomeSystem.Update(session, 200);
        Assert.Equal(0, battery.Stored, 3);
    }

    [Fact]
    public void BeaconDrainsNearbyDomeEnergy()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        dome.Energy = 100;
        var beacon = new Beacon(dome.CenterX + 40, dome.CenterY);
        session.State.Beacons.Add(beacon);

        BeaconSystem.Update(session, 1.0);

        Assert.True(beacon.Powered);
        Assert.True(dome.Energy < 100);
    }
}
