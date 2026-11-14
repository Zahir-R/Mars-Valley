using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class OccupancyTests
{
    private static GameSession NewSession()
    {
        var session = new GameSession(rng: new XorShiftRandom(1234));
        session.Start();
        return session;
    }

    private static void GiveDomeCost(GameSession session)
    {
        foreach (var (item, amount) in session.Balance.DomeCost) session.State.Inventory[item] = amount;
    }

    private static void ClearSceneObjects(GameSession session)
    {
        session.State.Nodes.Clear();
        session.State.Pickups.Clear();
        session.State.Stations.Clear();
    }

    private static Dome BuildDomeAt(GameSession session, float x, float y)
    {
        GiveDomeCost(session);
        Assert.True(BuildSystem.TryBuild(session, x, y, out var reason), reason);
        return session.State.Domes[^1];
    }

    [Fact]
    public void ActiveNodeBlocksPlacement()
    {
        var session = NewSession();
        ClearSceneObjects(session);
        GiveDomeCost(session);
        session.State.Nodes.Add(new ResourceNode("iron", 400, 200, 4));

        Assert.False(BuildSystem.CanPlaceAt(session, 300, 120, out var reason));
        Assert.Contains("veta mineral", reason);
    }

    [Fact]
    public void DepletedNodeAllowsPlacementButNeverRespawnsInsideAStructure()
    {
        var session = NewSession();
        ClearSceneObjects(session);
        GiveDomeCost(session);
        var node = new ResourceNode("iron", 410, 190, 0) { Regen = 100 };
        session.State.Nodes.Add(node);

        // A depleted vein is not a placement blocker.
        Assert.True(BuildSystem.CanPlaceAt(session, 300, 120, out _));
        var dome = BuildDomeAt(session, 300, 120);

        node.Regen = 0.5;
        NodeSystem.Update(session, 1.0);

        Assert.Equal(0, node.Amount);

        session.DestroyDome(dome);
        node.Regen = 0.5;
        NodeSystem.Update(session, 6.0);

        Assert.True(node.Amount > 0);
    }

    [Fact]
    public void UntakenPickupBlocksPlacementButTakenOneDoesNot()
    {
        var session = NewSession();
        ClearSceneObjects(session);
        GiveDomeCost(session);
        session.State.Pickups.Add(new Pickup("roca", 3, 400, 190));

        Assert.False(BuildSystem.CanPlaceAt(session, 300, 120, out var reason));
        Assert.Contains("objeto en el suelo", reason);

        session.State.Pickups.Clear();
        Assert.True(BuildSystem.CanPlaceAt(session, 300, 120, out _));
    }

    [Fact]
    public void StationBlocksPlacement()
    {
        var session = NewSession();
        ClearSceneObjects(session);
        GiveDomeCost(session);
        session.State.Stations.Add(new Station(StationKind.Workbench, 410, 190));

        Assert.False(BuildSystem.CanPlaceAt(session, 300, 120, out var reason));
        Assert.Contains("estación", reason);
    }

    [Fact]
    public void ShipBlocksPlacement()
    {
        var session = NewSession();
        ClearSceneObjects(session);
        GiveDomeCost(session);

        Assert.False(BuildSystem.CanPlaceAt(session, 64, 32, out var reason));
        Assert.Contains("nave", reason);
    }

    [Fact]
    public void TerminalOfExistingDomeIsAnObstacle()
    {
        var session = NewSession();
        ClearSceneObjects(session);

        var dome = BuildDomeAt(session, 300, 120);
        var terminal = WorldOccupancy.TerminalRect(dome);

        Assert.False(WorldOccupancy.IsClear(session, terminal, out var blocker));
        Assert.Contains("cúpula", blocker);
    }

    [Fact]
    public void TerminalAreaCannotOverlapASceneObject()
    {
        var session = NewSession();
        ClearSceneObjects(session);

        GiveDomeCost(session);
        var node = new ResourceNode("iron", 410, 289, 4);
        session.State.Nodes.Add(node);

        Assert.False(BuildSystem.CanPlaceAt(session, 300, 120, out var reason));
        Assert.Contains("terminal", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MiningOutTheOverlappingVeinsAllowsPlacement()
    {
        // The default map is dense, so the intended flow is: clear the veins, then build.
        var session = NewSession();
        GiveDomeCost(session);
        var dome = new WorldOccupancy.Rect(300, 120, session.Balance.DomeWidth, session.Balance.DomeHeight);
        var terminal = new WorldOccupancy.Rect(300 + session.Balance.DomeWidth / 2f - 14,
            120 + session.Balance.DomeHeight + 2, 28, 18);
        foreach (var node in session.State.Nodes)
        {
            if (dome.IntersectsCircle(node.X, node.Y, session.Balance.NodeFootprintRadius) ||
                terminal.IntersectsCircle(node.X, node.Y, session.Balance.NodeFootprintRadius))
            {
                node.Amount = 0;
            }
        }

        Assert.True(BuildSystem.CanPlaceAt(session, 300, 120, out var reason), reason);
    }

    [Fact]
    public void NodeInsideTheShipCannotRespawn()
    {
        var session = NewSession();
        ClearSceneObjects(session);
        var node = new ResourceNode("ice", session.Balance.ShipX + 40, session.Balance.ShipY + 40, 0) { Regen = 0.5 };
        session.State.Nodes.Add(node);

        NodeSystem.Update(session, 1.0);

        Assert.Equal(0, node.Amount);
    }
}
