namespace NasaValley.Domain;

/// <summary>
/// A map region with its own baseline temperature and solar exposure. The map is
/// small, so areas are simple rectangles that partition the world.
/// </summary>
public sealed record AreaDefinition(
    string Id,
    string Name,
    float MinX,
    float MinY,
    float MaxX,
    float MaxY,
    float BaseTempC,
    float SolarFactor)
{
    public bool Contains(float x, float y) => x >= MinX && x < MaxX && y >= MinY && y < MaxY;
}

public sealed class AreaDatabase
{
    public AreaDatabase(IEnumerable<AreaDefinition> areas) => Areas = areas.ToList();

    public IReadOnlyList<AreaDefinition> Areas { get; }

    public AreaDefinition At(float x, float y)
    {
        foreach (var area in Areas)
        {
            if (area.Contains(x, y)) return area;
        }
        return Areas[Areas.Count - 1];
    }

    public AreaDefinition this[string id] => Areas.First(a => a.Id == id);

    /// <summary>
    /// Three simplified zones: the cold, rocky outcrop; the temperate Gale crater
    /// floor (base); and the warmer open plains. Values are gameplay simplifications.
    /// </summary>
    public static AreaDatabase CreateDefault() => new(new[]
    {
        new AreaDefinition("afloramiento", "Afloramiento rocoso", 520, 0, 768, 448, -4f, 0.85f),
        new AreaDefinition("llanura", "Llanura abierta", 300, 0, 520, 448, 10f, 1.10f),
        new AreaDefinition("crater", "Cráter Gale", 0, 0, 300, 448, 4f, 1.00f),
    });
}

/// <summary>
/// Derives the growing environment from time of day and weather. Domes react to
/// light (day/night + dust storms) and to the external temperature of their area.
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

    /// <summary>External temperature for an area at the current time, with a day/night swing.</summary>
    public static double ExternalTemp(AreaDefinition area, GameState s, BalanceConfig b)
    {
        var hour = GameClock.HourOf(s.Time, b.SolLength);
        var day = 1 - GameClock.Darkness(hour, b.NightDarkness);
        var swing = b.DayNightSwing * (day - 0.5);
        var storm = s.Storm.Left > 0 ? -b.StormTempDrop : 0;
        return area.BaseTempC + swing + storm;
    }
}
