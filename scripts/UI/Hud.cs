using Godot;
using NasaValley.Domain;
using NasaValley.Input;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// Assembles the whole page shell and drives the ~8 Hz UI refresh, exactly like
/// the original renderUI loop. Reads state, never mutates it.
/// </summary>
public partial class Hud : Control
{
    private const float MaxContentWidth = 1280f;

    public Control Root => this;

    private readonly GameSession _session;
    private readonly InputAdapter _input;

    private readonly TopBar _topBar;
    private readonly VitalsPanel _vitals = new();
    private readonly LogPanel _log = new();
    private readonly Hotbar _hotbar;
    private readonly MissionTabs _tabs;
    private readonly TouchPad _pad;
    private readonly SubViewportContainer _viewport;
    private readonly GameFrame _frame;
    private readonly StageLayout _stage;
    private readonly MarginContainer _margin;

    private double _uiTimer;
    private float _lastViewportWidth = -1;
    private int _lastSideMargin = -1;
    private int _mode = -1;

    public Hud(GameSession session, InputAdapter input)
    {
        _session = session;
        _input = input;

        _topBar = new TopBar(session, session.TogglePause);
        _hotbar = new Hotbar(session, session.Eat);
        _tabs = new MissionTabs(session);
        _pad = new TouchPad(input, session.Eat);
        _frame = new GameFrame { Session = session };

        _viewport = BuildViewport(session, _frame);

        var center = UiTheme.VBox(10);
        center.AddChild(FrameViewport(_viewport));
        center.AddChild(_pad.Root);
        center.AddChild(_hotbar.Root);

        _stage = new StageLayout(_vitals.Root, center, _log.Root, _tabs.Root);

        var page = UiTheme.VBox(14);
        page.AddChild(_topBar.Root);
        page.AddChild(_stage);

        _margin = new MarginContainer();
        _margin.AddThemeConstantOverride("margin_top", 16);
        _margin.AddThemeConstantOverride("margin_bottom", 24);
        _margin.AddChild(page);
        _margin.SetAnchorsPreset(LayoutPreset.FullRect);

        AddChild(_margin);
        SetAnchorsPreset(LayoutPreset.FullRect);

        session.Events.Message += OnMessage;
        session.Events.PanelRequested += _tabs.ShowTab;
        session.Events.StateChanged += OnStateChanged;
    }

    public override void _Ready()
    {
        GetWindow().MinSize = new Vector2I(360, 480);
    }

    private static SubViewportContainer BuildViewport(GameSession session, GameFrame frame)
    {
        var sub = new SubViewport
        {
            Size = new Vector2I(768, 448),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            TransparentBg = false,
        };
        sub.AddChild(new WorldView { Session = session });
        sub.AddChild(frame);

        var container = new SubViewportContainer
        {
            Stretch = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(320, 320 * 448f / 768f),
        };
        container.AddChild(sub);
        return container;
    }

    private static PanelContainer FrameViewport(SubViewportContainer viewport)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", UiTheme.Box(Palette.Ink, Palette.Line, 1));
        panel.AddChild(viewport);
        return panel;
    }

    private void OnMessage(string text, MessageKind kind)
    {
        _frame.OnMessage(text, kind);
        _log.Rebuild(_session);
    }

    private void OnStateChanged()
    {
        _log.Rebuild(_session);
    }

    public override void _Process(double delta)
    {
        UpdateResponsive();

        _uiTimer += delta;
        if (_uiTimer < 0.12) return;
        _uiTimer = 0;

        _topBar.Refresh(_session);
        _vitals.Refresh(_session);
        _hotbar.Refresh(_session);
        _tabs.Refresh();
        _log.Rebuild(_session);
    }

    private void UpdateResponsive()
    {
        var width = Size.X > 0 ? Size.X : GetViewportRect().Size.X;

        var mode = width >= 1040 ? 0 : width >= 760 ? 1 : 2;
        if (mode != _mode)
        {
            _mode = mode;
            _stage.SetMode(mode == 0 ? StageLayout.Mode.Wide : StageLayout.Mode.Stacked);
            _vitals.SetColumns(mode == 0 ? 1 : mode == 1 ? 4 : 2);
            foreach (var control in _topBar.HideSmall) control.Visible = mode != 2;
        }

        var touch = mode == 2 || DisplayServer.IsTouchscreenAvailable();
        _pad.Root.Visible = touch;
        _hotbar.Keys.Visible = !touch;

        var side = Mathf.Max(16, Mathf.RoundToInt((width - MaxContentWidth) * 0.5f));
        if (side != _lastSideMargin)
        {
            _lastSideMargin = side;
            _margin.AddThemeConstantOverride("margin_left", side);
            _margin.AddThemeConstantOverride("margin_right", side);
        }

        var viewportWidth = _viewport.Size.X;
        if (viewportWidth > 0 && Mathf.Abs(viewportWidth - _lastViewportWidth) > 1)
        {
            _lastViewportWidth = viewportWidth;
            _viewport.CustomMinimumSize = new Vector2(0, viewportWidth * 448f / 768f);
        }
    }
}
