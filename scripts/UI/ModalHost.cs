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
    private readonly ScrollContainer _scroll;
    private readonly VBoxContainer _body;
    private readonly PanelContainer _card;
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

        _card = new PanelContainer { CustomMinimumSize = new Vector2(540, 0) };
        _card.AddThemeStyleboxOverride("panel", UiTheme.Card(20));
        center.AddChild(_card);

        _title = UiTheme.Text(string.Empty, 22, Palette.Suit, Fonts.DisplayWeight(700));
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var close = UiTheme.GhostButton("Cerrar  Esc", 12);
        close.Pressed += () => OnClosed?.Invoke();

        var header = UiTheme.HBox(12);
        header.AddChild(_title);
        header.AddChild(close);

        _scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _content = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _content.AddThemeConstantOverride("margin_top", 14);
        _scroll.AddChild(_content);

        _body = UiTheme.VBox(0);
        _body.AddChild(header);
        _body.AddChild(_scroll);
        _card.AddChild(_body);
    }

    public bool IsOpen => Visible;

    public void Open(string title, Control content)
    {
        if (_active != null && _active.GetParent() == _content) _content.RemoveChild(_active);
        _active = content;
        _content.AddChild(content);
        _title.Text = title;

        // Bring up scrollbars when the content exceeds the available window height.
        var cap = Mathf.Max(240f, GetViewportRect().Size.Y - 120f);
        var wanted = content.GetCombinedMinimumSize().Y + 64f;
        _scroll.CustomMinimumSize = new Vector2(0, Mathf.Min(Mathf.Max(wanted, 140f), cap));
        Visible = true;
    }

    public void Close()
    {
        if (_active != null && _active.GetParent() == _content) _content.RemoveChild(_active);
        _active = null;
        Visible = false;
    }
}
