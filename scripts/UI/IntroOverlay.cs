using System;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// Full-screen intro video shown at the start of a new run. Press E (or the
/// on-screen hint) to skip. The simulation stays in Boot while it plays.
/// </summary>
public sealed partial class IntroOverlay : Control
{
    private const string VideoPath = "res://assets/animation.ogv";

    private readonly VideoStreamPlayer _player;
    private readonly PanelContainer _skipPanel;
    private Action? _onFinished;
    private bool _playing;

    public bool IsPlaying => _playing;

    public IntroOverlay()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var background = new ColorRect { Color = Colors.Black, MouseFilter = MouseFilterEnum.Stop };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var aspect = new AspectRatioContainer
        {
            Ratio = 16f / 9f,
            StretchMode = AspectRatioContainer.StretchModeEnum.Fit,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        aspect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(aspect);

        _player = new VideoStreamPlayer
        {
            Expand = true,
            Loop = false,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _player.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _player.Finished += OnFinished;
        aspect.AddChild(_player);

        _skipPanel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        _skipPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        _skipPanel.OffsetLeft = -250;
        _skipPanel.OffsetTop = -56;
        _skipPanel.OffsetRight = -18;
        _skipPanel.OffsetBottom = -18;
        var style = UiTheme.Box(new Color(0, 0, 0, 0.6f), Palette.Line, 1);
        UiTheme.Pad(style, 12, 8);
        _skipPanel.AddThemeStyleboxOverride("panel", style);

        var hint = UiTheme.Label(Loc.T("Pulsa E para saltar"), 13, Palette.Suit);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.VerticalAlignment = VerticalAlignment.Center;
        _skipPanel.AddChild(hint);
        AddChild(_skipPanel);
    }

    /// <summary>Starts the intro. Returns false when the video is unavailable.</summary>
    public bool TryPlay(Action onFinished)
    {
        var stream = GD.Load<VideoStream>(VideoPath);
        if (stream == null)
        {
            GD.PushWarning($"Intro video not found at {VideoPath}; starting immediately.");
            return false;
        }

        _onFinished = onFinished;
        _player.Stream = stream;
        Visible = true;
        _playing = true;
        _player.Play();
        return true;
    }

    public override void _Input(InputEvent @event)
    {
        if (!_playing) return;
        if (@event is InputEventKey { Pressed: true, Echo: false } key &&
            (key.Keycode == Key.E || key.PhysicalKeycode == Key.E))
        {
            GetViewport().SetInputAsHandled();
            Skip();
        }
    }

    private void Skip()
    {
        if (!_playing) return;
        _player.Stop();
        Finish();
    }

    private void OnFinished() => Finish();

    private void Finish()
    {
        if (!_playing) return;
        _playing = false;
        Visible = false;
        _player.Stream = null;
        var callback = _onFinished;
        _onFinished = null;
        callback?.Invoke();
    }
}
