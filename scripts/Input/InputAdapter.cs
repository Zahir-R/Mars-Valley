using System.Collections.Generic;
using Godot;
using NasaValley.Domain;

namespace NasaValley.Input;

/// <summary>
/// Adapter that folds keyboard and on-screen controls into a single
/// <see cref="PlayerIntent"/>. The simulation never touches the input API.
/// </summary>
public sealed class InputAdapter
{
    private readonly Dictionary<string, bool> _virtualHeld = new();
    private bool _interactPressedVirtual;

    public const string MoveUp = "move_up";
    public const string MoveDown = "move_down";
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string Interact = "interact";
    public const string Eat = "eat";
    public const string Pause = "pause";

    public static void RegisterActions()
    {
        Add(MoveUp, Key.W, Key.Up);
        Add(MoveDown, Key.S, Key.Down);
        Add(MoveLeft, Key.A, Key.Left);
        Add(MoveRight, Key.D, Key.Right);
        Add(Interact, Key.E);
        Add(Eat, Key.F);
        Add(Pause, Key.P, Key.Escape);
    }

    private static void Add(string action, params Key[] keys)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action);
        foreach (var key in keys)
        {
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        }
    }

    public void SetVirtual(string action, bool down) => _virtualHeld[action] = down;

    public void TapVirtualInteract()
    {
        _virtualHeld[Interact] = true;
        _interactPressedVirtual = true;
    }

    public PlayerIntent GetIntent()
    {
        var up = Held(MoveUp);
        var down = Held(MoveDown);
        var left = Held(MoveLeft);
        var right = Held(MoveRight);
        var interactHeld = Held(Interact);

        var pressed = Godot.Input.IsActionJustPressed(Interact) || _interactPressedVirtual;
        _interactPressedVirtual = false;

        return new PlayerIntent(up, down, left, right, interactHeld, pressed);
    }

    public bool PausePressed() => Godot.Input.IsActionJustPressed(Pause);
    public bool EatPressed() => Godot.Input.IsActionJustPressed(Eat);

    public void Clear()
    {
        _virtualHeld.Clear();
        _interactPressedVirtual = false;
    }

    private bool Held(string action) =>
        Godot.Input.IsActionPressed(action) || _virtualHeld.GetValueOrDefault(action);
}
