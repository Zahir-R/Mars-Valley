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
    public const string Build = "build";
    public const string Stats = "stats";
    public const string Minimap = "minimap";

    public sealed record BindingDefinition(string Action, string Label, Key DefaultKey, Key[] SecondaryKeys);

    /// <summary>Actions offered in the controls menu, in display order.</summary>
    public static readonly IReadOnlyList<BindingDefinition> BindingDefinitions = new[]
    {
        new BindingDefinition(MoveUp, "Mover arriba", Key.W, System.Array.Empty<Key>()),
        new BindingDefinition(MoveDown, "Mover abajo", Key.S, System.Array.Empty<Key>()),
        new BindingDefinition(MoveLeft, "Mover izquierda", Key.A, System.Array.Empty<Key>()),
        new BindingDefinition(MoveRight, "Mover derecha", Key.D, System.Array.Empty<Key>()),
        new BindingDefinition(Interact, "Interactuar", Key.E, System.Array.Empty<Key>()),
        new BindingDefinition(Eat, "Comer", Key.F, System.Array.Empty<Key>()),
        new BindingDefinition(Pause, "Pausa", Key.P, new[] { Key.Escape }),
        new BindingDefinition(Build, "Construir cúpula", Key.B, System.Array.Empty<Key>()),
        new BindingDefinition(Stats, "Estadísticas", Key.Tab, System.Array.Empty<Key>()),
        new BindingDefinition(Minimap, "Minimapa", Key.M, System.Array.Empty<Key>()),
    };

    public static void RegisterActions() => RegisterActions(null);

    /// <summary>Registers every action, using custom bindings when present.</summary>
    public static void RegisterActions(AppSettingsData? settings)
    {
        foreach (var binding in BindingDefinitions)
        {
            var action = binding.Action;
            if (InputMap.HasAction(action)) InputMap.EraseAction(action);
            InputMap.AddAction(action);

            var primary = ResolveKey(settings, action, binding.DefaultKey);
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = (Key)primary });
            foreach (var secondary in binding.SecondaryKeys)
            {
                InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = secondary });
            }
        }
    }

    public static int ResolveKey(AppSettingsData? settings, string action, Key fallback)
    {
        if (settings?.Bindings.TryGetValue(action, out var code) == true && code != 0) return code;
        return (int)fallback;
    }

    public static string DisplayBinding(AppSettingsData? settings, string action)
    {
        var code = ResolveKey(settings, action, DefaultKeyFor(action));
        return OS.GetKeycodeString((Key)code);
    }

    public static int DefaultCodeFor(string action)
    {
        foreach (var binding in BindingDefinitions)
        {
            if (binding.Action == action) return (int)binding.DefaultKey;
        }
        return (int)Key.None;
    }

    private static Key DefaultKeyFor(string action) => (Key)DefaultCodeFor(action);

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
    public bool BuildPressed() => Godot.Input.IsActionJustPressed(Build);
    public bool StatsPressed() => Godot.Input.IsActionJustPressed(Stats);
    public bool MinimapPressed() => Godot.Input.IsActionJustPressed(Minimap);

    public void Clear()
    {
        _virtualHeld.Clear();
        _interactPressedVirtual = false;
    }

    public static void RebuildFromSettings(AppSettingsData settings) => RegisterActions(settings);

    private bool Held(string action) =>
        Godot.Input.IsActionPressed(action) || _virtualHeld.GetValueOrDefault(action);
}
