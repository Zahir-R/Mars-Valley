namespace NasaValley.Domain;

/// <summary>
/// Everything a command or system is allowed to touch. Passing this instead of a
/// global singleton keeps the dependency graph explicit and easy to fake in tests.
/// </summary>
public interface IGameContext
{
    GameState State { get; }
    BalanceConfig Balance { get; }
    ItemDatabase Items { get; }
    NodeDatabase Nodes { get; }
    PartDatabase Parts { get; }
    MissionSet Missions { get; }
    IRandom Rng { get; }
    GameEvents Events { get; }

    void Say(string text, MessageKind kind = MessageKind.Info);
    void Finish(bool win);
}
