using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class NpcQuestTests
{
    private static GameSession NewSession()
    {
        var session = new GameSession(rng: new XorShiftRandom(77));
        session.Start();
        return session;
    }

    private static void ToAwaitingSupplies(GameSession session)
    {
        session.State.InterferenceActive = true;
        session.DecodeNpcInterference();
        Assert.Equal(NpcPhase.AwaitingSupplies, session.State.NpcPhase);
    }

    [Fact]
    public void InterferenceRepeatsUntilAnswered()
    {
        var session = NewSession();
        session.State.NpcInterferenceTimer = 0.01;

        session.Tick(PlayerIntent.None, 0.02);
        Assert.True(session.State.InterferenceActive);
        Assert.Equal(session.Balance.NpcInterferenceInterval, session.State.NpcInterferenceTimer, 3);

        // It keeps the signal up without decoding.
        session.Tick(PlayerIntent.None, session.Balance.NpcInterferenceInterval + 0.1);
        Assert.True(session.State.InterferenceActive);
    }

    [Fact]
    public void DecodingDeliversTheContactLetter()
    {
        var session = NewSession();
        ToAwaitingSupplies(session);

        Assert.False(session.State.InterferenceActive);
        Assert.Contains(NpcQuestDatabase.Contact, session.State.NpcLetters);
        Assert.Contains(NpcQuestDatabase.Contact, session.State.NpcUnread);
        Assert.Equal(NpcQuestDatabase.Contact, session.State.ActiveNpcLetterId);
    }

    [Fact]
    public void DepositingSuppliesUnlocksCoordinates()
    {
        var session = NewSession();
        ToAwaitingSupplies(session);

        session.State.Inventory["agua"] = 1;
        Assert.False(session.DepositSupplies());

        session.State.Inventory["agua"] = session.State.CacheAmount;
        Assert.True(session.DepositSupplies());

        Assert.True(session.State.CacheFilled);
        Assert.True(session.State.SiteUnlocked);
        Assert.Equal(NpcPhase.SuppliesDelivered, session.State.NpcPhase);
        Assert.Equal(0, session.State.Inventory["agua"]);
        Assert.Contains(NpcQuestDatabase.Coordinates, session.State.NpcLetters);
    }

    [Fact]
    public void MeetingTheSurvivorGrantsRewardOnce()
    {
        var session = NewSession();
        ToAwaitingSupplies(session);
        session.State.Inventory["agua"] = session.State.CacheAmount;
        session.DepositSupplies();

        Assert.True(session.MeetSurvivor());
        Assert.True(session.State.SurvivorFound);
        Assert.True(session.State.RewardGranted);
        Assert.Equal(NpcPhase.SiteVisited, session.State.NpcPhase);
        Assert.True(session.State.Inventory["pico_reforzado"] >= 1);

        var charges = session.State.ShipO2Charges;
        Assert.False(session.MeetSurvivor());
        Assert.Equal(charges, session.State.ShipO2Charges);
    }

    [Fact]
    public void ChoosingNotToGoSkipsTheSite()
    {
        var session = NewSession();
        ToAwaitingSupplies(session);
        session.State.Inventory["agua"] = session.State.CacheAmount;
        session.DepositSupplies();

        session.ChooseSite(false);

        Assert.True(session.State.SiteSkipped);
        Assert.Equal(NpcPhase.SiteSkipped, session.State.NpcPhase);
        Assert.Contains(NpcQuestDatabase.Skipped, session.State.NpcLetters);
    }

    [Fact]
    public void ShipRepairDeliversFinalLetterAndHoldsLaunch()
    {
        var session = NewSession();
        ToAwaitingSupplies(session);
        session.State.Inventory["agua"] = session.State.CacheAmount;
        session.DepositSupplies();
        session.MeetSurvivor();

        NpcQuestSystem.OnShipRepaired(session);

        Assert.True(session.State.PendingFinalChoice);
        Assert.Equal(0, session.State.WinTimer);
        Assert.Contains(NpcQuestDatabase.Final, session.State.NpcLetters);

        session.ChooseFinal(true);

        Assert.Equal(NpcOutcome.Rescued, session.State.NpcOutcome);
        Assert.False(session.State.PendingFinalChoice);
        Assert.Equal(NpcPhase.Done, session.State.NpcPhase);
        Assert.True(session.State.WinTimer > 0);
    }

    [Fact]
    public void SkippedQuestEndsWithLostContact()
    {
        var session = NewSession();
        ToAwaitingSupplies(session);
        session.State.Inventory["agua"] = session.State.CacheAmount;
        session.DepositSupplies();
        session.ChooseSite(false);

        NpcQuestSystem.OnShipRepaired(session);

        Assert.Equal(NpcOutcome.LostContact, session.State.NpcOutcome);
        Assert.True(session.State.PendingFinalChoice);
        Assert.Contains(NpcQuestDatabase.FinalLost, session.State.NpcLetters);
    }

    [Fact]
    public void QuestStateSurvivesSaveRoundTrip()
    {
        var session = NewSession();
        ToAwaitingSupplies(session);
        session.State.Inventory["agua"] = session.State.CacheAmount;
        session.DepositSupplies();
        session.MeetSurvivor();

        var json = SaveSerializer.Serialize(session.CaptureSave());
        var restored = NewSession();
        restored.ApplySave(SaveSerializer.Deserialize(json)!);

        Assert.Equal(NpcPhase.SiteVisited, restored.State.NpcPhase);
        Assert.True(restored.State.SurvivorFound);
        Assert.True(restored.State.RewardGranted);
        Assert.Equal(session.State.CacheX, restored.State.CacheX, 3);
        Assert.Equal(session.State.SurvivorY, restored.State.SurvivorY, 3);
        Assert.Contains(NpcQuestDatabase.Found, restored.State.NpcLetters);
    }
}
