using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class DomeManagementTests
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
    public void DomesAreLargerThanTheLegacyFootprint()
    {
        var session = NewSession();
        Assert.True(session.Balance.DomeWidth >= 200);
        Assert.True(session.Balance.DomeHeight >= 140);
        Assert.True(DomeLayout.SlotCount(session.Balance) > 4);
    }

    [Fact]
    public void OxygenIsNotLostInsideADome()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        session.State.Px = dome.CenterX;
        session.State.Py = dome.CenterY;
        session.State.Vitals.O2 = 50;

        SurvivalSystem.Update(session, 10);

        Assert.Equal(50, session.State.Vitals.O2, 3);
    }

    [Fact]
    public void OxygenDecaysOutsideEveryDome()
    {
        var session = NewSession();
        BuildDome(session);
        session.State.Px = 250;
        session.State.Py = 215;
        session.State.Vitals.O2 = 50;

        SurvivalSystem.Update(session, 10);

        Assert.True(session.State.Vitals.O2 < 50);
    }

    [Fact]
    public void OpenTraysCanBePlantedOutsideADome()
    {
        var session = NewSession();
        session.State.Inventory["semillas"] = 5;
        session.State.Inventory["parcela"] = 1;

        Assert.True(BuildSystem.TryPlaceField(session, 200, 200, out var reason), reason);
        var plot = Assert.Single(session.State.Plots);
        Assert.Null(plot.Dome);

        new PlantCommand(plot, "papa").Execute(session);

        Assert.Equal(PlotState.Dry, plot.State);
    }

    [Fact]
    public void StoringADomePutsItInTheInventoryAndDestroysItsCrops()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        var plot = AddFarm(session, dome);
        new PlantCommand(plot, "papa").Execute(session);
        new WaterCommand(plot).Execute(session);

        session.StoreDome(dome);

        Assert.Empty(session.State.Domes);
        Assert.Empty(session.State.Plots);
        Assert.Equal(1, session.State.Inventory[BuildSystem.StoredDomeItem]);
    }

    [Fact]
    public void AStoredDomeCanBePlacedWithoutSpendingMaterials()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        session.StoreDome(dome);

        foreach (var (item, _) in session.Balance.DomeCost) session.State.Inventory[item] = 0;
        Assert.True(session.State.Inventory[BuildSystem.StoredDomeItem] > 0);

        Assert.True(BuildSystem.TryBuild(session, 300, 120, out var reason), reason);

        Assert.Single(session.State.Domes);
        Assert.Equal(0, session.State.Inventory[BuildSystem.StoredDomeItem]);
        Assert.Empty(session.State.Plots);
    }

    [Fact]
    public void MovingADomeKeepsItsCropsAndMovesThem()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        var plot = AddFarm(session, dome);
        new PlantCommand(plot, "papa").Execute(session);
        new WaterCommand(plot).Execute(session);
        var oldPlotX = plot.X;

        Assert.True(BuildSystem.TryMove(session, dome.Id, 450, 150, out var reason), reason);

        Assert.Equal(450, dome.X);
        Assert.Equal(150, dome.Y);
        Assert.Equal(PlotState.Grow, plot.State);
        Assert.NotEqual(oldPlotX, plot.X);
        Assert.Same(dome, plot.Dome);
    }

    [Fact]
    public void UpgradingADomeRaisesItsLevelAndEnergyCapacity()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        var baseCapacity = dome.EnergyCapacity(session.Balance);

        foreach (var (item, amount) in BuildSystem.UpgradeCost(session.Balance, 2))
        {
            session.State.Inventory[item] = amount;
        }

        Assert.True(session.UpgradeDome(dome, out var reason), reason);

        Assert.Equal(2, dome.Level);
        Assert.True(dome.EnergyCapacity(session.Balance) > baseCapacity);
    }

    [Fact]
    public void DomesCannotBeUpgradedPastTheMaximumLevel()
    {
        var session = NewSession();
        var dome = BuildDome(session);

        for (var level = 2; level <= session.Balance.DomeMaxLevel; level++)
        {
            foreach (var (item, amount) in BuildSystem.UpgradeCost(session.Balance, level))
            {
                session.State.Inventory[item] = amount;
            }
            Assert.True(session.UpgradeDome(dome, out _));
        }

        Assert.False(session.CanUpgradeDome(dome, out _));
    }

    [Fact]
    public void TheDomeTerminalIsAnInteractionTarget()
    {
        var session = NewSession();
        var dome = BuildDome(session, 120, 200);
        session.State.Px = dome.TerminalX;
        session.State.Py = dome.TerminalY;

        var target = InteractionSystem.FindTarget(session.State, session.Balance);

        var terminal = Assert.IsType<DomeTerminalTarget>(target);
        Assert.Same(dome, terminal.Dome);
    }

    [Fact]
    public void DevModeGrantsEveryItemAndSpeedsUpCrops()
    {
        var session = NewSession();
        session.ToggleDevMode();

        Assert.True(session.State.DevMode);
        foreach (var def in session.Items.Items)
        {
            Assert.True(session.State.Inventory[def.Id] >= session.Balance.DevItemAmount, def.Id);
        }

        var dome = BuildDome(session);
        var plot = AddFarm(session, dome);
        new PlantCommand(plot, "papa").Execute(session);
        new WaterCommand(plot).Execute(session);

        var seconds = session.Plants["papa"].GrowTime / session.Balance.DevGrowSpeedMultiplier + 0.5;
        FarmingSystem.Update(session, seconds);

        Assert.Equal(PlotState.Ready, plot.State);
    }

    [Fact]
    public void FoodSourceFallsBackToAnotherFoodWhenTheSelectedOneRunsOut()
    {
        var session = NewSession();
        session.State.Inventory["papas"] = 0;
        session.State.Inventory["tomate"] = 2;
        session.State.Inventory["lechuga"] = 1;
        session.SetFoodSource("tomate");

        session.Eat(); // tomato 2 -> 1, source unchanged
        Assert.Equal("tomate", session.State.FoodSource);

        session.Eat(); // tomato 1 -> 0, source should fall back to lettuce
        Assert.Equal("lechuga", session.State.FoodSource);
    }

    [Fact]
    public void FoodSourceClearsWhenNothingIsLeftAndPicksUpNewFood()
    {
        var session = NewSession();
        session.State.Inventory["papas"] = 1;
        session.SetFoodSource("papas");

        session.Eat(); // last potato eaten
        Assert.True(session.State.FoodSource == null || session.State.Inventory[session.State.FoodSource] > 0);

        session.State.Inventory["rabano"] = 1;
        session.NormalizeFoodSource();
        Assert.Equal("rabano", session.State.FoodSource);
    }

    [Fact]
    public void DomeAndFarmAreIndependent()
    {
        var session = NewSession();
        var dome = BuildDome(session);

        Assert.Empty(dome.Plots);
        Assert.Empty(dome.Fixtures);
        Assert.True(dome.HasFreeSlot(DomeLayout.SlotCount(session.Balance)));

        var plot = AddFarm(session, dome, 5);

        Assert.Single(dome.Plots);
        Assert.Equal(5, plot.SlotIndex);
        Assert.Same(dome, plot.Dome);
        Assert.True(dome.IsSlotOccupied(5));
    }

    [Fact]
    public void PlacingAFarmSpendsMaterialsAndOccupiesOneSlot()
    {
        var session = NewSession();
        var dome = BuildDome(session);

        foreach (var (item, _) in session.Balance.FarmCost) session.State.Inventory[item] = 0;
        Assert.False(BuildSystem.BuildFixture(session, dome, 0, FixtureKind.Farm, out _));
        Assert.Empty(dome.Plots);

        foreach (var (item, amount) in session.Balance.FarmCost) session.State.Inventory[item] = amount;
        Assert.True(BuildSystem.BuildFixture(session, dome, 0, FixtureKind.Farm, out var reason), reason);

        Assert.True(dome.IsSlotOccupied(0));
        Assert.False(BuildSystem.CanBuildFixture(session, dome, 0, FixtureKind.Farm, out _));
    }

    [Fact]
    public void SolarPanelChargesTheDomeEnergy()
    {
        var session = NewSession();
        var dome = BuildDome(session);

        dome.Energy = 0;
        DomeSystem.Update(session, 1.0);
        var passive = dome.Energy;
        Assert.True(passive > 0);

        foreach (var (item, amount) in session.Balance.SolarPanelCost) session.State.Inventory[item] = amount;
        Assert.True(BuildSystem.BuildFixture(session, dome, 0, FixtureKind.SolarPanel, out _));
        Assert.Equal(1, dome.PanelCount);

        dome.Energy = 0;
        DomeSystem.Update(session, 1.0);

        Assert.True(dome.Energy > passive);
    }

    [Fact]
    public void BatteryIncreasesEnergyCapacity()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        var baseCapacity = dome.EnergyCapacity(session.Balance);

        foreach (var (item, amount) in session.Balance.BatteryCost) session.State.Inventory[item] = amount;
        Assert.True(BuildSystem.BuildFixture(session, dome, 0, FixtureKind.Battery, out _));

        Assert.Equal(1, dome.BatteryCount);
        Assert.Equal(baseCapacity + session.Balance.BatteryStorage, dome.EnergyCapacity(session.Balance), 3);
    }

    [Fact]
    public void BunkRestoresEnergyWithACooldown()
    {
        var session = NewSession();
        var dome = BuildDome(session);
        foreach (var (item, amount) in session.Balance.BunkCost) session.State.Inventory[item] = amount;
        Assert.True(BuildSystem.BuildFixture(session, dome, 0, FixtureKind.Bunk, out _));
        Assert.True(dome.HasBunk);

        session.State.Vitals.Energy = 10;
        session.Rest(dome);

        Assert.Equal(10 + session.Balance.RestEnergyGain, session.State.Vitals.Energy);
        Assert.True(session.State.RestCooldown > 0);

        session.Rest(dome);
        Assert.Equal(10 + session.Balance.RestEnergyGain, session.State.Vitals.Energy);
    }

    [Fact]
    public void EmptySlotsAreInteractionTargetsForInteriorBuilding()
    {
        var session = NewSession();
        var dome = BuildDome(session, 120, 200);
        var (sx, sy) = DomeLayout.SlotPosition(session.Balance, dome, 0);
        session.State.Px = sx + session.Balance.PlotSize / 2f;
        session.State.Py = sy + session.Balance.PlotSize / 2f;

        var target = InteractionSystem.FindTarget(session.State, session.Balance);

        var slot = Assert.IsType<DomeSlotTarget>(target);
        Assert.Same(dome, slot.Dome);
        Assert.Equal(0, slot.SlotIndex);
    }

    [Fact]
    public void PendingFixtureIsBuiltOnAnEmptySlotWithInteract()
    {
        var session = NewSession();
        var dome = BuildDome(session, 120, 200);
        foreach (var (item, amount) in session.Balance.SolarPanelCost) session.State.Inventory[item] = amount;

        session.RequestFixture(FixtureKind.SolarPanel);
        var (sx, sy) = DomeLayout.SlotPosition(session.Balance, dome, 2);
        session.State.Px = sx + session.Balance.PlotSize / 2f;
        session.State.Py = sy + session.Balance.PlotSize / 2f;

        session.Tick(new PlayerIntent(false, false, false, false, InteractHeld: false, InteractPressed: true), 0.016);

        Assert.Equal(1, dome.PanelCount);
        Assert.True(dome.IsSlotOccupied(2));
        Assert.Null(session.State.PendingFixture);
    }
}
