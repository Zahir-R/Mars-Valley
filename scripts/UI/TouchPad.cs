using System;
using Godot;
using NasaValley.Input;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>On-screen D-pad + action buttons, shown on coarse pointers or small windows.</summary>
public sealed class TouchPad
{
    public Control Root { get; }

    public TouchPad(InputAdapter input, Action onEat)
    {
        var dpad = new GridContainer { Columns = 3 };
        dpad.AddThemeConstantOverride("h_separation", 4);
        dpad.AddThemeConstantOverride("v_separation", 4);
        dpad.AddChild(new Control { CustomMinimumSize = new Vector2(52, 52) });
        dpad.AddChild(Direction(input, InputAdapter.MoveUp, "▲", 0, 1));
        dpad.AddChild(new Control { CustomMinimumSize = new Vector2(52, 52) });
        dpad.AddChild(Direction(input, InputAdapter.MoveLeft, "◀", 1, 0));
        dpad.AddChild(new Control { CustomMinimumSize = new Vector2(52, 52) });
        dpad.AddChild(Direction(input, InputAdapter.MoveRight, "▶", 1, 2));
        dpad.AddChild(new Control { CustomMinimumSize = new Vector2(52, 52) });
        dpad.AddChild(Direction(input, InputAdapter.MoveDown, "▼", 2, 1));
        dpad.AddChild(new Control { CustomMinimumSize = new Vector2(52, 52) });

        var eat = UiTheme.SupportButton("F", "Comer");
        eat.CustomMinimumSize = new Vector2(76, 76);
        eat.Pressed += onEat;

        var interact = UiTheme.PrimaryButton("E", 20);
        interact.CustomMinimumSize = new Vector2(76, 76);
        interact.ButtonDown += () =>
        {
            input.SetVirtual(InputAdapter.Interact, true);
            input.TapVirtualInteract();
        };
        interact.ButtonUp += () => input.SetVirtual(InputAdapter.Interact, false);

        var acts = UiTheme.HBox(10);
        acts.AddChild(eat);
        acts.AddChild(interact);

        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var row = UiTheme.HBox(16);
        row.AddChild(dpad);
        row.AddChild(spacer);
        row.AddChild(acts);

        var wrapper = new MarginContainer();
        wrapper.AddThemeConstantOverride("margin_top", 12);
        wrapper.AddChild(row);
        Root = wrapper;
    }

    private static Button Direction(InputAdapter input, string action, string glyph, int row, int column)
    {
        var button = UiTheme.SupportButton(glyph, string.Empty);
        button.CustomMinimumSize = new Vector2(52, 52);
        button.ButtonDown += () => input.SetVirtual(action, true);
        button.ButtonUp += () => input.SetVirtual(action, false);
        button.ButtonMask = MouseButtonMask.Left | MouseButtonMask.Right;
        return button;
    }
}
