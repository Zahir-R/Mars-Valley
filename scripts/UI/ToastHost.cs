using System.Collections.Generic;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// Window-space toast stack shown above the modal overlay but below the pause
/// menu. Anchored to the bottom-right corner of its parent; grows upward and
/// leftward as toasts arrive.
/// </summary>
public partial class ToastHost : Control
{
    private const int MaxToasts = 3;

    private readonly VBoxContainer _toasts;
    private readonly List<Control> _list = new();

    public ToastHost()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        _toasts = UiTheme.VBox(6);
        _toasts.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        _toasts.Alignment = BoxContainer.AlignmentMode.End;
        _toasts.GrowHorizontal = GrowDirection.Begin;
        _toasts.GrowVertical = GrowDirection.Begin;
        _toasts.OffsetRight = -12;
        _toasts.OffsetBottom = -12;
        _toasts.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_toasts);
    }

    public void ShowMessage(string text, MessageKind kind)
    {
        var label = UiTheme.Label(text, 12, Palette.Suit);
        var naturalW = label.GetMinimumSize().X;
        var vpW = GetViewportRect().Size.X;
        if (vpW <= 0) vpW = 1280;
        var maxW = Mathf.Max(140f, vpW * 0.40f);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(Mathf.Min(naturalW + 24f, maxW), 0);

        var accent = kind switch
        {
            MessageKind.Warn => Palette.Energy,
            MessageKind.Good => Palette.Food,
            _ => Palette.O2,
        };
        var style = UiTheme.Box(new Color(21 / 255f, 15 / 255f, 13 / 255f, 0.92f), accent, 1);
        style.BorderWidthLeft = 3;
        UiTheme.Pad(style, 10, 6);

        var panel = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = new Color(1, 1, 1, 0),
        };
        panel.AddThemeStyleboxOverride("panel", style);
        panel.AddChild(label);

        _toasts.AddChild(panel);
        _list.Add(panel);

        var tween = CreateTween();
        tween.TweenProperty(panel, "modulate:a", 1.0f, 0.18f);

        while (_list.Count > MaxToasts)
        {
            var oldest = _list[0];
            _list.RemoveAt(0);
            oldest.QueueFree();
        }

        var timer = GetTree().CreateTimer(3.4);
        timer.Timeout += () => { _list.Remove(panel); panel.QueueFree(); };
    }
}
