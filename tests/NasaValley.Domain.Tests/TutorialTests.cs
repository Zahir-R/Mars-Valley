using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class TutorialTests
{
    private static GameSession NewSession()
    {
        var session = new GameSession(rng: new XorShiftRandom(99));
        session.Start();
        return session;
    }

    [Fact]
    public void TutorialAdvancesThroughAllFiveSteps()
    {
        var session = NewSession();
        Assert.Equal(0, session.TutorialIndex);

        session.State.Moving = true;
        TutorialSystem.Update(session);
        Assert.Equal(1, session.TutorialIndex);

        session.State.Stats.Ice = 1;
        TutorialSystem.Update(session);
        Assert.Equal(2, session.TutorialIndex);

        session.State.ShipO2Charges = session.Balance.ShipO2ChargesMax - 1;
        TutorialSystem.Update(session);
        Assert.Equal(3, session.TutorialIndex);

        session.State.Inventory["martillo"] = 1;
        TutorialSystem.Update(session);
        Assert.Equal(4, session.TutorialIndex);

        session.State.Domes.Add(new Dome(1, 300, 120, session.Balance.DomeWidth, session.Balance.DomeHeight));
        session.State.Stats.Planted = 1;
        TutorialSystem.Update(session);
        Assert.True(session.State.TutorialDone);
        Assert.Null(session.CurrentTutorialStep);
    }

    [Fact]
    public void SkippingTheTutorialStopsIt()
    {
        var session = NewSession();
        session.SkipTutorial();

        Assert.True(session.State.TutorialSkipped);
        Assert.True(session.State.TutorialDone);
        Assert.Null(session.CurrentTutorialStep);
    }

    [Fact]
    public void TutorialProgressSurvivesSaveRoundTrip()
    {
        var session = NewSession();
        session.State.TutorialStep = 3;
        session.State.TutorialSkipped = false;

        var json = SaveSerializer.Serialize(session.CaptureSave());
        var restored = NewSession();
        restored.ApplySave(SaveSerializer.Deserialize(json)!);

        Assert.Equal(3, restored.State.TutorialStep);
    }
}
