using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class ProceduralWorldTests
{
    private static GameSession NewSession(int seed = 1234) =>
        new GameSession(rng: new XorShiftRandom((uint)seed));

    [Fact]
    public void TheGeneratedWorldContainsManyNodesAndPickups()
    {
        var session = NewSession();
        session.Start();

        Assert.True(session.State.Nodes.Count >= 50);
        Assert.True(session.State.Pickups.Count >= 10);
        Assert.Contains(session.State.Nodes, n => n.Type == "ice");
        Assert.Contains(session.State.Nodes, n => n.Type == "rock");

        foreach (var node in session.State.Nodes)
        {
            Assert.True(session.Nodes.TryGet(node.Type, out _));
            Assert.True(node.X >= 0 && node.X <= BalanceConfig.Default.WorldWidth);
            Assert.True(node.Y >= 0 && node.Y <= BalanceConfig.Default.WorldHeight);
        }
    }

    [Fact]
    public void DifferentSeedsProduceDifferentLayouts()
    {
        var a = NewSession(1);
        var b = NewSession(2);
        a.Start();
        b.Start();

        Assert.NotEqual(a.State.Nodes[0].X, b.State.Nodes[0].X);
    }

    [Fact]
    public void AFreshRunStartsInsideATileWithKnownBiome()
    {
        var session = NewSession();
        session.Start();

        var biome = GeneratedBiomes.AtWorld(session.State.Px, session.State.Py);
        Assert.NotNull(biome);
        Assert.True(biome.BaseTempC < 22);
    }

    [Fact]
    public void BiomeMapIsFullyDefinedAndValuesAreValid()
    {
        Assert.Equal(GeneratedBiomes.Width * GeneratedBiomes.Height, GeneratedBiomes.Grid.Length);
        for (var i = 0; i < GeneratedBiomes.Grid.Length; i++)
        {
            Assert.InRange(GeneratedBiomes.Grid[i], (byte)0, (byte)5);
        }

        Assert.True(GeneratedBiomes.ById.Count == 6);
        Assert.Equal(96, GeneratedBiomes.Width);
        Assert.Equal(56, GeneratedBiomes.Height);
    }

    [Fact]
    public void PlacingAnOpenTrayCreatesAPlotOutsideTheDome()
    {
        var session = NewSession();
        session.Start();
        foreach (var (item, amount) in session.Balance.FieldCost) session.State.Inventory[item] = amount;

        Assert.True(BuildSystem.TryPlaceField(session, 1500, 1100, out var reason), reason);

        var plot = Assert.Single(session.State.Plots);
        Assert.Null(plot.Dome);
        Assert.Same(plot, session.State.Plots[^1]);
    }
}

