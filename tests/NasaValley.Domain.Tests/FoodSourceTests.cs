using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class FoodSourceTests
{
    private static GameSession NewSession()
    {
        var session = new GameSession(rng: new XorShiftRandom(1234));
        session.Start();
        return session;
    }

    [Fact]
    public void DefaultFoodSourceIsPotatoes()
    {
        var session = NewSession();
        Assert.Equal("papas", session.State.FoodSource);
    }

    [Fact]
    public void EatConsumesTheSelectedFoodSourceFirst()
    {
        var session = NewSession();
        session.State.Inventory["tomate"] = 2;
        session.SetFoodSource("tomate");
        var potatoesBefore = session.State.Inventory["papas"];

        session.Eat();

        Assert.Equal(1, session.State.Inventory["tomate"]);
        Assert.Equal(potatoesBefore, session.State.Inventory["papas"]);
    }

    [Fact]
    public void EatFallsBackWhenTheSelectedSourceIsEmpty()
    {
        var session = NewSession();
        session.State.Inventory["tomate"] = 0;
        session.SetFoodSource("tomate");
        var potatoesBefore = session.State.Inventory["papas"];

        session.Eat();

        Assert.Equal(potatoesBefore - 1, session.State.Inventory["papas"]);
    }

    [Fact]
    public void FoodSourceRejectsNonFoodItems()
    {
        var session = NewSession();
        session.SetFoodSource("hierro");
        Assert.Equal("papas", session.State.FoodSource);
    }

    [Fact]
    public void EatItemConsumesThatSpecificItem()
    {
        var session = NewSession();
        session.State.Inventory["lechuga"] = 1;

        session.Eat("lechuga");

        Assert.Equal(0, session.State.Inventory["lechuga"]);
    }
}
