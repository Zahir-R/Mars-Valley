using System;
using Godot;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// Full-screen modal shell: dimmed backdrop plus a centered card. One panel is
/// hosted at a time. The world is frozen by the domain while this is open.
/// </summary>
public partial class ModalHost : Control
{
    public Action? OnClosed;

    private readonly Label _title;
    private readonly MarginContainer _content;
    private Control? _active;

    public ModalHost()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;

        var backdrop = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.62f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var card = new PanelContainer { CustomMinimumSize = new Vector2(540, 0) };
        card.AddThemeStyleboxOverride("panel", UiTheme.Card(20));
        center.AddChild(card);

        _title = UiTheme.Text(string.Empty, 22, Palette.Suit, Fonts.DisplayWeight(700));
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var close = UiTheme.GhostButton("Cerrar  Esc", 12);
        close.Pressed += () => OnClosed?.Invoke();

        var header = UiTheme.HBox(12);
        header.AddChild(_title);
        header.AddChild(close);

        _content = new MarginContainer();
        _content.AddThemeConstantOverride("margin_top", 14);

        var body = UiTheme.VBox(0);
        body.AddChild(header);
        body.AddChild(_content);
        card.AddChild(body);
    }

    public bool IsOpen => Visible;

    public void Open(string title, Control content)
    {
        if (_active != null && _active.GetParent() == _content) _content.RemoveChild(_active);
        _active = content;
        _content.AddChild(content);
        _title.Text = title;
        Visible = true;
    }

    public void Close()
    {
        if (_active != null && _active.GetParent() == _content) _content.RemoveChild(_active);
        _active = null;
        Visible = false;
    }
}
