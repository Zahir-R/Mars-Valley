using System.Linq;
using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class StationsAndModalsTests
{
    private static GameSession NewSession()
    {
        var session = new GameSession(rng: new XorShiftRandom(1234));
        session.Start();
        return session;
    }

    [Fact]
    public void WorkbenchRequestsTheWorkshopPanel()
    {
        var session = NewSession();
        var workbench = session.State.Stations.First(s => s.Kind == StationKind.Workbench);
        session.State.Px = workbench.X;
        session.State.Py = workbench.Y;

        string? requested = null;
        session.Events.PanelRequested += id => requested = id;

        session.Tick(new PlayerIntent(false, false, false, false, InteractHeld: false, InteractPressed: true), 0.1);

        Assert.Equal("taller", requested);
    }

    [Fact]
    public void ShipRequestsTheShipPanelWhenInteracted()
    {
        var session = NewSession();
        session.State.Px = session.Balance.ShipX + session.Balance.ShipWidth + 5;
        session.State.Py = session.Balance.ShipY + 40;

        string? requested = null;
        session.Events.PanelRequested += id => requested = id;

        session.Tick(new PlayerIntent(false, false, false, false, InteractHeld: false, InteractPressed: true), 0.1);

        Assert.Equal("nave", requested);
    }

    [Fact]
    public void OpenModalFreezesSimulationUntilClosed()
    {
        var session = NewSession();
        var before = session.State.Time;

        session.OpenModal();
        session.Tick(PlayerIntent.None, 1.0);
        Assert.Equal(before, session.State.Time);

        session.CloseModal();
        session.Tick(PlayerIntent.None, 1.0);
        Assert.True(session.State.Time > before);
    }
}
