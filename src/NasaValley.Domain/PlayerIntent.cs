namespace NasaValley.Domain;

/// <summary>
/// Adapter-friendly input snapshot. Keyboard, gamepad or on-screen controls all
/// collapse into this struct before reaching the simulation.
/// </summary>
public readonly record struct PlayerIntent(
    bool Up,
    bool Down,
    bool Left,
    bool Right,
    bool InteractHeld,
    bool InteractPressed)
{
    public static PlayerIntent None => default;

    public float AxisX => (Right ? 1 : 0) - (Left ? 1 : 0);
    public float AxisY => (Down ? 1 : 0) - (Up ? 1 : 0);
    public bool Moving => AxisX != 0 || AxisY != 0;
}
