using System;
using System.Collections.Generic;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

public sealed class Hotbar
{
    public Control Root { get; }
    public Control Keys { get; }

    private readonly Dictionary<string, ItemSlot> _slots = new();
    private readonly Button _eat;

    public Hotbar(GameSession session, Action onEat)
    {
        var flow = new HFlowContainer();
        flow.AddThemeConstantOverride("h_separation", 8);
        flow.AddThemeConstantOverride("v_separation", 8);
        flow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        foreach (var id in session.Items.Order)
        {
            var slot = new ItemSlot(session.Items[id]);
            _slots[id] = slot;
            flow.AddChild(slot.Root);
        }

        _eat = UiTheme.PrimaryButton(Loc.T("Comer papa  F"));
        UiTheme.ApplyButton(_eat, Palette.Food, Palette.Ink, Color.FromHtml("#4f6e2a"), 13);
        _eat.CustomMinimumSize = new Vector2(140, 42);
        _eat.Pressed += onEat;
        flow.AddChild(_eat);

        Keys = UiTheme.Label(Loc.T("WASD mover · E acción (mantén para extraer) · F comer · P pausa"), 12, Palette.Muted);

        var box = UiTheme.VBox(10);
        box.AddChild(flow);
        box.AddChild(Keys);
        Root = box;
    }

    public void Refresh(GameSession session)
    {
        foreach (var (id, slot) in _slots) slot.Refresh(session.State.Inventory[id]);
        _eat.Disabled = session.State.Inventory["papas"] < 1;
    }
}
