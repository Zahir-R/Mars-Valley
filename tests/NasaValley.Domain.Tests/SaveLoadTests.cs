using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class SaveLoadTests
{
    private static GameSession NewSession()
    {
        var session = new GameSession(rng: new XorShiftRandom(1234));
        session.Start();
        return session;
    }

    private static void ClearSceneObjects(GameSession session)
    {
        session.State.Nodes.Clear();
        session.State.Pickups.Clear();
        session.State.Stations.Clear();
    }

    private static Dome BuildDome(GameSession session, float x = 300, float y = 120)
    {
        ClearSceneObjects(session);
        foreach (var (item, amount) in session.Balance.DomeCost) session.State.Inventory[item] = amount;
        Assert.True(BuildSystem.TryBuild(session, x, y, out _));
        return session.State.Domes[^1];
    }

    private static void AddFixture(GameSession session, Dome dome, int slot, FixtureKind kind)
    {
        foreach (var (item, amount) in BuildSystem.FixtureCost(session.Balance, kind))
        {
            session.State.Inventory[item] = amount;
        }
        Assert.True(BuildSystem.BuildFixture(session, dome, slot, kind, out var reason), reason);
    }

    [Fact]
    public void FullRoundTripPreservesDynamicState()
    {
        var session = NewSession();
        var dome = BuildDome(session);

        AddFixture(session, dome, 0, FixtureKind.Farm);
        AddFixture(session, dome, 1, FixtureKind.SolarPanel);
        AddFixture(session, dome, 2, FixtureKind.Battery);
        AddFixture(session, dome, 3, FixtureKind.Bunk);

        var farm = dome.Plots[0];
        new PlantCommand(farm, "tomate").Execute(session);
        new WaterCommand(farm).Execute(session);
        farm.Time = 12.5;
        farm.State = PlotState.Grow;

        session.State.Inventory.Add("hierro", 17);
        session.State.Inventory.Add("tomate", 2);
        session.State.Vitals.O2 = 55;
        session.State.Vitals.Food = 33;
        session.State.Vitals.Energy = 22;
        session.State.Vitals.Health = 88;
        session.State.Time = 90;
        session.State.Px = 310;
        session.State.Py = 140;
        session.State.Face = Direction.Left;
        session.State.Storm.Next = 123;
        session.State.Storm.Left = 5;
        session.State.Stats.Ice = 9;
        session.State.Stats.Planted = 3;
        session.State.Stats.Harvested = 2;
        session.State.Repairs["casco"] = true;
        session.State.RestCooldown = 4.5;
        session.State.FoodSource = "tomate";
        session.State.SelectedCrop = "trigo";

        var pickup = new Pickup("basalto", 6, 500, 400) { Taken = true };
        session.State.Pickups.Add(pickup);

        session.State.Building = true;
        session.State.ActiveDomeId = 12;
        session.State.MovingDomeId = 7;
        session.State.ModalOpen = true;

        var json = SaveSerializer.Serialize(session.CaptureSave());

        var restored = NewSession();
        Assert.NotNull(restored.State);
        var data = SaveSerializer.Deserialize(json);
        Assert.NotNull(data);
        restored.ApplySave(data!);

        // Transient dome/build state is not persisted.
        Assert.False(restored.State.Building);
        Assert.False(restored.State.ModalOpen);
        Assert.Null(restored.State.MovingDomeId);
        Assert.Null(restored.State.ActiveDomeId);

        // Vitals and clock.
        Assert.Equal(55, restored.State.Vitals.O2, 3);
        Assert.Equal(33, restored.State.Vitals.Food, 3);
        Assert.Equal(22, restored.State.Vitals.Energy, 3);
        Assert.Equal(88, restored.State.Vitals.Health, 3);
        Assert.Equal(90, restored.State.Time, 3);
        Assert.Equal(310, restored.State.Px);
        Assert.Equal(140, restored.State.Py);
        Assert.Equal(Direction.Left, restored.State.Face);
        Assert.Equal(123, restored.State.Storm.Next, 3);
        Assert.Equal(5, restored.State.Storm.Left, 3);
        Assert.Equal(4.5, restored.State.RestCooldown, 3);

        // Inventory and mission progress.
        Assert.Equal(17, restored.State.Inventory["hierro"]);
        Assert.Equal(9, restored.State.Stats.Ice);
        Assert.Equal(3, restored.State.Stats.Planted);
        Assert.Equal(2, restored.State.Stats.Harvested);
        Assert.True(restored.State.Repairs["casco"]);

        // Selected crop and food source survive.
        Assert.Equal("trigo", restored.State.SelectedCrop);
        Assert.Equal("tomate", restored.State.FoodSource);

        // Nodes and pickups are restored exactly.
        Assert.Empty(restored.State.Nodes);
        Assert.Contains(restored.State.Pickups, p => p.ItemId == "basalto" && p.Taken);

        // The dome, its fixtures and the farm come back linked together.
        var restoredDome = Assert.Single(restored.State.Domes);
        Assert.Equal(1, restoredDome.Level);
        Assert.Equal(3, restoredDome.Fixtures.Count);
        Assert.Contains(restoredDome.Fixtures, f => f.Kind == FixtureKind.SolarPanel && f.SlotIndex == 1);
        Assert.Contains(restoredDome.Fixtures, f => f.Kind == FixtureKind.Battery && f.SlotIndex == 2);
        Assert.Contains(restoredDome.Fixtures, f => f.Kind == FixtureKind.Bunk && f.SlotIndex == 3);

        var restoredPlot = Assert.Single(restored.State.Plots);
        Assert.Same(restoredDome, restoredPlot.Dome);
        Assert.Equal(0, restoredPlot.SlotIndex);
        Assert.Equal("tomate", restoredPlot.PlantId);
        Assert.Equal(PlotState.Grow, restoredPlot.State);
        Assert.Equal(12.5, restoredPlot.Time, 3);
    }

    [Fact]
    public void CorruptOrIncompatibleJsonIsRejected()
    {
        Assert.Null(SaveSerializer.Deserialize(""));
        Assert.Null(SaveSerializer.Deserialize("not json"));
        Assert.NotNull(SaveSerializer.Deserialize("{}"));
        Assert.Null(SaveSerializer.Deserialize("""{"Version":999}"""));
    }

    [Fact]
    public void SavedAndRestoredNodesAndPickupsMatch()
    {
        var session = NewSession();
        ClearSceneObjects(session);
        session.State.Nodes.Add(new ResourceNode("iron", 10, 20, 0) { Regen = 42, Progress = 2 });
        session.State.Pickups.Add(new Pickup("roca", 3, 30, 40) { Taken = false });

        var json = SaveSerializer.Serialize(session.CaptureSave());
        var restored = NewSession();
        var data = SaveSerializer.Deserialize(json);
        restored.ApplySave(data!);

        var node = Assert.Single(restored.State.Nodes);
        Assert.Equal("iron", node.Type);
        Assert.Equal(10, node.X);
        Assert.Equal(20, node.Y);
        Assert.Equal(0, node.Amount);
        Assert.Equal(2, node.Progress);
        Assert.Equal(42, node.Regen, 3);

        var pickup = Assert.Single(restored.State.Pickups);
        Assert.Equal("roca", pickup.ItemId);
        Assert.Equal(3, pickup.Amount);
        Assert.Equal(30, pickup.X);
        Assert.Equal(40, pickup.Y);
        Assert.False(pickup.Taken);
    }

    [Fact]
    public void MissingSaveMetadataFallsBackToDefaultsGracefully()
    {
        var session = NewSession();
        ClearSceneObjects(session);

        var data = new SaveGameData(); // Default values are used; nothing should crash.
        var json = SaveSerializer.Serialize(data);
        var restored = NewSession();
        restored.ApplySave(SaveSerializer.Deserialize(json)!);

        Assert.Equal(FlowState.Playing, restored.State.Flow);
        Assert.Empty(restored.State.Domes);
        Assert.Empty(restored.State.Plots);
        Assert.False(restored.State.Building);
        Assert.False(restored.State.ModalOpen);
    }
}
