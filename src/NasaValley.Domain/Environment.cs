namespace NasaValley.Domain;

/// <summary>
/// Real Gale Crater biome classification baked from PDS data (MOLA elevation,
/// TES thermal inertia, Odyssey GRS H2O, PSG Mars Explorer) by
/// <c>tools/mars_data/generate.py</c>. The grid is north-up, 96x56 cells,
/// one biome id per 32x32 pixel tile. See <c>data/mars/gale/meta.json</c> for
/// the sources and <c>meta.json</c> for the exact products and their DOIs.
/// </summary>
public static class EnvironmentSystem
{
    /// <summary>Natural light fraction in [0,1] at the current time, reduced by storms.</summary>
    public static double Light(GameState s, BalanceConfig b)
    {
        var hour = GameClock.HourOf(s.Time, b.SolLength);
        var day = 1 - GameClock.Darkness(hour, b.NightDarkness);
        if (s.Storm.Left > 0) day *= b.StormLightFactor;
        return Math.Clamp(day, 0, 1);
    }

    /// <summary>External temperature for a biome at the current time, with a day/night swing.</summary>
    public static double ExternalTemp(BiomeDefinition biome, GameState s, BalanceConfig b)
    {
        var hour = GameClock.HourOf(s.Time, b.SolLength);
        var day = 1 - GameClock.Darkness(hour, b.NightDarkness);
        var swing = biome.DayNightSwingC * (day - 0.5);
        var storm = s.Storm.Left > 0 ? -b.StormTempDrop : 0;
        return biome.BaseTempC + swing + storm;
    }
}
