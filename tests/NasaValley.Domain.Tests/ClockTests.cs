using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class ClockTests
{
    private const double SolLength = 240;

    [Fact]
    public void StartsAtSixAmOnSolOne()
    {
        Assert.Equal(6, GameClock.HourOf(0, SolLength), 3);
        Assert.Equal(1, GameClock.DayNumber(0, SolLength));
    }

    [Fact]
    public void AFullSolAdvancesTheDay()
    {
        Assert.Equal(6, GameClock.HourOf(SolLength, SolLength), 3);
        Assert.Equal(2, GameClock.DayNumber(SolLength, SolLength));
    }

    [Fact]
    public void FormatsTimeWithLeadingZeroes()
    {
        Assert.Equal("06:00", GameClock.FormatTime(6));
        Assert.Equal("18:30", GameClock.FormatTime(18.5));
    }

    [Fact]
    public void TemperaturePeaksMidAfternoon()
    {
        Assert.True(GameClock.TemperatureAt(15) > GameClock.TemperatureAt(3));
        Assert.Equal(-73, GameClock.TemperatureAt(3), 3);
    }

    [Fact]
    public void DarknessIsZeroDuringTheDayAndMaxAtNight()
    {
        Assert.Equal(0, GameClock.Darkness(12, 0.55), 3);
        Assert.Equal(0.55, GameClock.Darkness(23, 0.55), 3);
        Assert.True(GameClock.Darkness(19.5, 0.55) > 0);
    }
}
