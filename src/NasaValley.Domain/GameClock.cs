namespace NasaValley.Domain;

/// <summary>Pure time-of-day helpers derived from elapsed sol time.</summary>
public static class GameClock
{
    public static double HourOf(double t, double solLength) => (6 + t / solLength * 24) % 24;

    public static int DayNumber(double t, double solLength) =>
        1 + (int)Math.Floor((6 + t / solLength * 24) / 24);

    public static string FormatTime(double hour)
    {
        var hh = (int)Math.Floor(hour);
        var mm = (int)Math.Floor((hour - hh) * 60);
        return Pad2(hh) + ":" + Pad2(mm);
    }

    public static double TemperatureAt(double hour) =>
        -73 + 60 * (0.5 + 0.5 * Math.Sin((hour - 9) / 24 * 2 * Math.PI));

    public static double Darkness(double hour, double maxDarkness)
    {
        if (hour >= 7 && hour <= 18) return 0;
        if (hour > 18 && hour < 21) return (hour - 18) / 3 * maxDarkness;
        if (hour >= 21 || hour < 4) return maxDarkness;
        return maxDarkness * (1 - (hour - 4) / 3);
    }

    private static string Pad2(int n) => (n < 10 ? "0" : string.Empty) + n;
}
