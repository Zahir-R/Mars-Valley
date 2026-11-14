using System.Linq;
using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class CraftingTests
{
    private static GameSession NewSession()
    {
        var session = new GameSession(rng: new XorShiftRandom(1234));
        session.Start();
        return session;
    }

    private static ResourceNode Node(GameSession session, string type) =>
        session.State.Nodes.First(n => n.Type == type);

    [Fact]
    public void RockIsHarvestableWithBareHands()
    {
        var session = NewSession();
        var rock = Node(session, "rock");

        new MineNodeCommand(rock).Execute(session);

        Assert.Equal(1, session.State.Inventory["roca"]);
        Assert.Equal(session.Balance.NodeAmount - 1, rock.Amount);
    }

    [Fact]
    public void BasaltRequiresAMiningTool()
    {
        var session = NewSession();
        var basalt = Node(session, "basalt");

        new MineNodeCommand(basalt).Execute(session);

        Assert.Equal(0, session.State.Inventory["basalto"]);
        Assert.Equal(session.Balance.NodeAmount, basalt.Amount);
        Assert.Equal(0, basalt.Progress);
    }

    [Fact]
    public void HammerMinesBasaltAfterItsHitCount()
    {
        var session = NewSession();
        session.State.Inventory["martillo"] = 1;
        var basalt = Node(session, "basalt");
        var hits = session.Nodes["basalt"].HitsPerUnit;

        for (var i = 0; i < hits; i++) new MineNodeCommand(basalt).Execute(session);

        Assert.Equal(1, session.State.Inventory["basalto"]);
        Assert.Equal(session.Balance.NodeAmount - 1, basalt.Amount);
        Assert.Equal(0, basalt.Progress);
    }

    [Fact]
    public void RegolithRequiresAShovel()
    {
        var session = NewSession();
        var regolith = Node(session, "regolith");

        new MineNodeCommand(regolith).Execute(session);
        Assert.Equal(0, session.State.Inventory["regolito"]);

        session.State.Inventory["pala"] = 1;
        new MineNodeCommand(regolith).Execute(session);
        Assert.Equal(1, session.State.Inventory["regolito"]);
    }

    [Fact]
    public void CraftingConsumesIngredientsAndYieldsTheTool()
    {
        var session = NewSession();
        session.State.Inventory["roca"] = 3;
        session.State.Inventory["fijaciones"] = 2;

        session.Craft("martillo");

        Assert.Equal(1, session.State.Inventory["martillo"]);
        Assert.Equal(0, session.State.Inventory["roca"]);
        Assert.Equal(0, session.State.Inventory["fijaciones"]);
    }

    [Fact]
    public void CraftingFailsWithoutConsumingMaterials()
    {
        var session = NewSession();
        session.State.Inventory["roca"] = 1;
        session.State.Inventory["fijaciones"] = 2;

        session.Craft("martillo");

        Assert.Equal(0, session.State.Inventory["martillo"]);
        Assert.Equal(1, session.State.Inventory["roca"]);
        Assert.Equal(2, session.State.Inventory["fijaciones"]);
    }

    [Fact]
    public void ReinforcedPickIsCraftedFromHammerAndBasalt()
    {
        var session = NewSession();
        session.State.Inventory["martillo"] = 1;
        session.State.Inventory["basalto"] = 2;
        session.State.Inventory["fijaciones"] = 1;

        session.Craft("pico_reforzado");

        Assert.Equal(1, session.State.Inventory["pico_reforzado"]);
        Assert.Equal(0, session.State.Inventory["martillo"]);
        Assert.True(session.HasToolTag("Mineria"));
        Assert.True(session.HasToolTag("Excavacion"));
    }

    [Fact]
    public void PickingUpALooseItemAddsItAndIsIdempotent()
    {
        var session = NewSession();
        var pickup = session.State.Pickups[0];
        var before = session.State.Inventory[pickup.ItemId];

        new PickupCommand(pickup).Execute(session);
        Assert.Equal(before + pickup.Amount, session.State.Inventory[pickup.ItemId]);
        Assert.True(pickup.Taken);

        new PickupCommand(pickup).Execute(session);
        Assert.Equal(before + pickup.Amount, session.State.Inventory[pickup.ItemId]);
    }
}
