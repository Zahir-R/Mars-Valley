using Godot;
using NasaValley.Core;
using NasaValley.Domain;
using NasaValley.Input;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// Full-screen game shell: the world fills the window, a minimal overlay HUD floats
/// over it, a left stats drawer toggles with Tab, and base stations open modal panels.
/// </summary>
public partial class Hud : Control
{
    public Control Root => this;

    private readonly GameSession _session;

    private readonly InventoryPanel _inventory;
    private readonly LogPanel _log = new();
    private readonly ShipPanel _ship;
    private readonly WorkshopPanel _workshop;
    private readonly GoalsPanel _goals;
    private readonly DomePanel _dome;
    private readonly PlantPanel _plant;
    private readonly BuildMenuPanel _buildMenu;
    private readonly StatsDrawer _stats;
    private readonly MinimapPanel _minimap;
    private readonly ToastHost _toast = new();
    private readonly SubViewportContainer _viewport;
    private readonly GameFrame _frame;
    private readonly ModalHost _modal;
    private readonly HBoxContainer _topButtons;
    private readonly MenuHost _menu;
    private readonly TutorialPanel _tutorial;
    private readonly IntroOverlay _intro;

    private double _uiTimer;

    public Hud(GameSession session, InputAdapter input)
    {
        _session = session;

        _frame = new GameFrame { Session = session };
        _viewport = BuildViewport(session, _frame);
        AddChild(_viewport);
        _viewport.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        _inventory = new InventoryPanel(session, PlaceItem);
        _ship = new ShipPanel(session, CloseModal);
        _workshop = new WorkshopPanel(session);
        _goals = new GoalsPanel(session);
        _dome = new DomePanel(session, CloseModal);
        _plant = new PlantPanel(session, CloseModal);
        _buildMenu = new BuildMenuPanel(session, CloseModal);
        _stats = new StatsDrawer(session, ToggleStats);
        _minimap = new MinimapPanel(session);

        AddChild(_stats.Root);
        AddChild(_stats.Icon);
        AddChild(_minimap);

        _tutorial = new TutorialPanel(session);
        AddChild(_tutorial.Root);

        _topButtons = BuildTopButtons();
        AddChild(_topButtons);

        _modal = new ModalHost { OnClosed = CloseModal };
        AddChild(_modal);
        _modal.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        AddChild(_toast);

        _menu = new MenuHost(session)
        {
            Continue = LoadAutosave,
            NewGame = StartNewGame,
            QuitToMenu = QuitToMenu,
            Quit = QuitGame,
            SaveSlotConfirmed = SaveCurrentSlot,
            LoadSlotConfirmed = LoadCurrentSlot,
            DeleteSlotConfirmed = DeleteSlot,
        };
        _menu.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_menu);

        _intro = new IntroOverlay();
        _intro.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_intro);

        _frame.BackToMenuRequested += () => QuitToMenu();

        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        session.Events.Message += OnMessage;
        session.Events.PanelRequested += OnPanelRequested;
        session.Events.StateChanged += OnStateChanged;
    }

    public override void _Ready()
    {
        GetWindow().MinSize = new Vector2I(480, 360);
    }

    private (string, Control) PlantPanelFor(Plot? plot)
    {
        _plant.SetPlot(plot);
        return ("Plantar", _plant.Root);
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

        var overlays = new CanvasLayer { Name = "Overlays" };
        overlays.AddChild(frame);
        frame.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        sub.AddChild(overlays);

        var container = new SubViewportContainer { Stretch = true };
        container.AddChild(sub);
        return container;
    }

    private void PlaceItem(string itemId)
    {
        CloseModal();
        _session.StartPlacement(itemId);
    }

    private HBoxContainer BuildTopButtons()
    {
        var box = UiTheme.HBox(8);
        box.AddChild(ColoredButton(Loc.T("Diario"), "#2e4a38", () => OpenPanel("log")));
        box.AddChild(ColoredButton(Loc.T("Misiones"), "#5c3a28", () => OpenPanel("missions")));
        box.AddChild(ColoredButton(Loc.T("Inventario"), "#274356", () => OpenPanel("inventory")));
        box.AddChild(ColoredButton(Loc.T("Pausa"), "#4a3a52", _session.TogglePause));
        return box;
    }

    private static Button ColoredButton(string text, string hex, System.Action onPressed)
    {
        var bg = Color.FromHtml(hex);
        var button = UiTheme.GhostButton(text, 12);
        var normal = UiTheme.Box(bg, bg.Lightened(0.25f), 1);
        UiTheme.Pad(normal, 12, 6);
        var hover = UiTheme.Box(bg.Lightened(0.12f), Palette.Dust, 1);
        UiTheme.Pad(hover, 12, 6);

        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.AddThemeColorOverride("font_color", new Color(0.93f, 0.91f, 0.86f));
        button.Pressed += onPressed;
        return button;
    }

    private void OnMessage(string text, MessageKind kind)
    {
        if (_menu.Visible)
        {
            _menu.SetStatus(text, kind);
        }
        else
        {
            _toast.ShowMessage(text, kind);
        }
        _log.Rebuild(_session);
    }

    public bool MenuOpen => _menu.Visible;

    private void LoadAutosave()
    {
        if (SaveManager.TryLoad(_session, SaveSlots.Autosave, out var error))
        {
            _session.Say("Partida cargada.", MessageKind.Good);
        }
        else
        {
            _session.Say(error, MessageKind.Warn);
        }

        _menu.RefreshSlots();
    }

    private void StartNewGame()
    {
        if (_intro.TryPlay(() => _session.Reset())) return;
        _session.Reset();
    }

    private void SaveCurrentSlot(string slot)
    {
        if (SaveManager.SaveTo(_session, slot))
        {
            _session.Say(Loc.F("Guardado en {0}.", SaveSlots.NameOf(slot)), MessageKind.Good);
        }
        else
        {
            _session.Say("No se pudo guardar la partida.", MessageKind.Warn);
        }

        _menu.RefreshSlots();
    }

    private void LoadCurrentSlot(string slot)
    {
        if (SaveManager.TryLoad(_session, slot, out var error))
        {
            _session.Say(Loc.F("Partida cargada desde {0}.", SaveSlots.NameOf(slot)), MessageKind.Good);
        }
        else
        {
            _session.Say(error, MessageKind.Warn);
        }

        _menu.RefreshSlots();
    }

    private void DeleteSlot(string slot)
    {
        SaveManager.Delete(slot);
        _menu.RefreshSlots();
        _session.Say(Loc.F("Ranura {0} borrada.", SaveSlots.NameOf(slot)), MessageKind.Info);
    }

    private void QuitToMenu()
    {
        SaveManager.SaveTo(_session, SaveSlots.Autosave);
        _session.ReturnToMenu();
    }

    private void QuitGame() => GetTree().Quit();

    private void OnStateChanged() => _log.Rebuild(_session);

    private void OnPanelRequested(string id) => OpenPanel(id);

    public void ToggleStats() => _stats.Toggle();

    public void ToggleMinimap() => _minimap.Toggle();

    /// <summary>Opens a panel inside the modal shell and freezes the simulation.</summary>
    public void OpenPanel(string id)
    {
        if (!_session.State.Running) return;

        if (id == "nave" && _session.State.InterferenceActive) _session.DecodeNpcInterference();

        var (title, content) = id switch
        {
            "nave" => (Loc.T("Nave"), _ship.Root),
            "taller" => (Loc.T("Taller"), _workshop.Root),
            "plant" => PlantPanelFor(_session.State.ActivePlot),
            "log" => (Loc.T("Bitácora"), _log.Root),
            "missions" => (Loc.T("Objetivos"), _goals.Root),
            "inventory" => (Loc.T("Inventario"), _inventory.Root),
            "cupula" => (Loc.T("Terminal de cúpula"), _dome.Root),
            "build" => (Loc.T("Construir"), _buildMenu.Root),
            _ => (string.Empty, (Control)null!),
        };
        if (content == null) return;

        _session.OpenModal();
        _modal.Open(title, content);
        AudioSfx.Play("inventario", -10f);
    }

    public void CloseModal()
    {
        _modal.Close();
        _session.CloseModal();
        AudioSfx.Play("inventario", -16f);
    }

    private void BuildDomeFromModal()
    {
        CloseModal();
        _session.StartBuild();
    }

    public override void _Process(double delta)
    {
        _stats.Tick(delta);
        LayoutOverlays();

        _uiTimer += delta;
        if (_uiTimer < 0.12) return;
        _uiTimer = 0;

        _stats.Refresh();
        _inventory.Refresh();
        _ship.Refresh();
        _workshop.Refresh();
        _goals.Refresh();
        _dome.Refresh();
        _plant.Refresh();
        _tutorial.Refresh();
        _log.Rebuild(_session);
    }

    private void LayoutOverlays()
    {
        var size = Size.X > 0 ? Size : GetViewportRect().Size;

        _stats.Icon.ResetSize();
        _stats.Icon.Position = new Vector2(0, size.Y * 0.5f - _stats.Icon.Size.Y / 2f);

        _stats.Root.ResetSize();
        var pw = _stats.Root.Size.X;
        var x = -pw + _stats.Slide * (pw + 8f);
        _stats.Root.Position = new Vector2(x, Mathf.Max(12, size.Y * 0.5f - _stats.Root.Size.Y / 2f));

        _tutorial.Root.ResetSize();
        _tutorial.Root.Position = new Vector2(12, 164);

        _topButtons.ResetSize();
        _topButtons.Position = new Vector2(size.X - _topButtons.Size.X - 12, 12);

        _minimap.Position = new Vector2(12, 12);

    }
}

