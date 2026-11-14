namespace NasaValley.Domain;

/// <summary>Engine-agnostic 2D vector so the domain stays independent of Godot.</summary>
public readonly record struct Vec2(float X, float Y);

public enum Direction
{
    Down,
    Up,
    Left,
    Right,
}

public enum MessageKind
{
    Info,
    Warn,
    Good,
}

/// <summary>Interactable base stations that open UI modals.</summary>
public enum StationKind
{
    Ship,
    Workbench,
}

public enum PlotState
{
    Empty,
    Dry,
    Grow,
    Ready,
}

/// <summary>
/// High-level flow of a session. Replaces the loose running/paused/over booleans
/// from the original script with an explicit state machine.
/// </summary>
public enum FlowState
{
    Boot,
    Playing,
    Paused,
    Won,
    Lost,
}
