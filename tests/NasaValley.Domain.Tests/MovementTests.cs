using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class MovementTests
{
    private static GameSession NewSession()
    {
        var session = new GameSession(rng: new XorShiftRandom(99));
        session.Start();
        session.State.Nodes.Clear();
        session.State.Stations.Clear();
        session.State.Pickups.Clear();
        return session;
    }

    private static PlayerIntent WalkRight => new(false, false, false, true, InteractHeld: false, InteractPressed: false);

    [Fact]
    public void MultiHitNodeBlocksMovementUntilDepleted()
    {
        var session = NewSession();
        var basalt = new ResourceNode("basalt", 220, 200, 4);
        session.State.Nodes.Add(basalt);
        session.State.Px = 150;
        session.State.Py = 200;

        for (var i = 0; i < 150; i++) session.Tick(WalkRight, 1.0 / 60.0);

        Assert.True(session.State.Px < basalt.X, "basalt should block the astronaut");

        basalt.Amount = 0;
        basalt.Regen = session.Balance.NodeRegenTime;
        for (var i = 0; i < 200; i++) session.Tick(WalkRight, 1.0 / 60.0);

        Assert.True(session.State.Px > basalt.X, "a depleted vein should be walkable");
    }

    [Fact]
    public void ActiveIceVeinAlsoBlocks()
    {
        var session = NewSession();
        var ice = new ResourceNode("ice", 220, 200, 4);
        session.State.Nodes.Add(ice);
        session.State.Px = 150;
        session.State.Py = 200;

        for (var i = 0; i < 150; i++) session.Tick(WalkRight, 1.0 / 60.0);

        Assert.True(session.State.Px < ice.X, "every active vein should block, single-hit ones included");
    }

    [Fact]
    public void WorkbenchBlocksMovement()
    {
        var session = NewSession();
        session.State.Stations.Add(new Station(StationKind.Workbench, 230, 200));
        session.State.Px = 150;
        session.State.Py = 200;

        for (var i = 0; i < 150; i++) session.Tick(WalkRight, 1.0 / 60.0);

        Assert.True(session.State.Px < 230, "the workbench should block the astronaut");
    }
}
