using System;
using System.Collections.Generic;
using Godot;
using NasaValley.Core;
using NasaValley.Domain;
using NasaValley.Input;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// In-game start and pause menus. Also hosts the options/controls views and the save/load
/// slot lists, all reachable from either menu. Built entirely in code; no .tscn files.
/// </summary>
public partial class MenuHost : Control
{
    private readonly GameSession _session;

    public Action? Continue { get; set; }
    public Action? NewGame { get; set; }
    public Action? QuitToMenu { get; set; }
    public Action? Quit { get; set; }
    /// <summary>Called with a slot id after the user confirms a save/load slot.</summary>
    public Action<string>? SaveSlotConfirmed { get; set; }
    public Action<string>? LoadSlotConfirmed { get; set; }
    public Action<string>? DeleteSlotConfirmed { get; set; }

    private readonly Control _startView;
    private readonly Control _pauseView;
    private readonly Control _optionsView;
    private readonly Control _controlsView;
    private readonly Control _saveSlotsView;
    private readonly Control _loadSlotsView;
    private readonly SlotListView _saveSlots;
    private readonly SlotListView _loadSlots;
    private readonly VBoxContainer _controlsList;
    private readonly HSlider _volumeSlider;
    private readonly Label _volumeValue;
    private readonly Dictionary<string, Label> _keyLabels = new();
    private readonly Dictionary<string, Button> _changeButtons = new();
    private Control? _visibleView;
    private Label _status = null!;
    private int _statusSerial;

    private string? _capturing;
    private MenuContext _context = MenuContext.Start;
    private MenuContext _returnTo = MenuContext.Start;

    public enum MenuContext { Start, Pause }

    private enum MenuView { Start, Pause, Options, Controls, SaveSlots, LoadSlots }

    public MenuHost(GameSession session)
    {
        _session = session;

        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        var backdrop = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.72f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        _continueButtonHolder = UiTheme.PrimaryButton("Continuar", 15);
        (_continueButtonHolder).Pressed += () => Continue?.Invoke();

        _newGameButtonHolder = UiTheme.PrimaryButton("Nuevo juego", 14);
        (_newGameButtonHolder).Pressed += () => NewGame?.Invoke();

        _loadButtonHolder = UiTheme.GhostButton("Cargar partida", 13);
        (_loadButtonHolder).Pressed += () => ShowSlots(load: true);

        _optionsButtonHolder = UiTheme.GhostButton("Opciones", 13);
        (_optionsButtonHolder).Pressed += () => ShowView(MenuView.Options, _context);

        _quitButtonHolder = UiTheme.GhostButton("Salir", 13);
        (_quitButtonHolder).Pressed += () => Quit?.Invoke();

        _resumeButtonHolder = UiTheme.PrimaryButton("Continuar", 14);
        (_resumeButtonHolder).Pressed += () => Continue?.Invoke();

        _saveButtonHolder = UiTheme.PrimaryButton("Guardar partida", 13);
        (_saveButtonHolder).Pressed += () => ShowSlots(load: false);

        _pauseLoadButtonHolder = UiTheme.GhostButton("Cargar partida", 13);
        (_pauseLoadButtonHolder).Pressed += () => ShowSlots(load: true);

        _pauseOptionsButtonHolder = UiTheme.GhostButton("Opciones", 13);
        (_pauseOptionsButtonHolder).Pressed += () => ShowView(MenuView.Options, MenuContext.Pause);

        _quitMenuButtonHolder = UiTheme.GhostButton("Salir al menú", 13);
        (_quitMenuButtonHolder).Pressed += () => QuitToMenu?.Invoke();

        _startView = MakeCard(
            "Varados en Marte",
            "El módulo de ascenso quedó dañado en el Cráter Gale. Repara las cinco piezas antes de que se agote tu oxígeno.",
            _continueButtonHolder, _newGameButtonHolder, _loadButtonHolder, _optionsButtonHolder, _quitButtonHolder);

        _pauseView = MakeCard(
            "Pausa",
            "El reloj marciano está detenido.",
            _resumeButtonHolder, _saveButtonHolder, _pauseLoadButtonHolder, _pauseOptionsButtonHolder, _quitMenuButtonHolder);

        _volumeSlider = new HSlider
        {
            MinValue = 0,
            MaxValue = 100,
            Step = 1,
            CustomMinimumSize = new Vector2(280, 28),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            FocusMode = Control.FocusModeEnum.None,
        };

        _volumeValue = UiTheme.Label(string.Empty, 12, Palette.Muted);
        var volumeRow = UiTheme.VBox(6);
        volumeRow.AddChild(UiTheme.Label("Volumen maestro", 13, Palette.Suit));
        volumeRow.AddChild(_volumeSlider);
        volumeRow.AddChild(_volumeValue);

        var controlsButton = UiTheme.GhostButton("Controles", 12);
        controlsButton.Pressed += () => ShowView(MenuView.Controls, _context);

        var optionsBack = UiTheme.GhostButton("Volver", 12);
        optionsBack.Pressed += () => ShowRoot(_returnTo);

        var optionsHint = UiTheme.Label(
            "El volumen y los controles se aplican al instante y se guardan para la próxima vez.",
            11, Palette.Muted);
        optionsHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var optionsBody = UiTheme.VBox(10);
        optionsBody.AddChild(UiTheme.Caption("Opciones"));
        optionsBody.AddChild(volumeRow);
        optionsBody.AddChild(controlsButton);
        optionsBody.AddChild(optionsHint);
        optionsBody.AddChild(optionsBack);

        _optionsView = MakeCard("Opciones", null, optionsBody);

        _controlsList = UiTheme.VBox(6);
        var resetButton = UiTheme.GhostButton("Restablecer controles", 11);
        resetButton.Pressed += ResetControls;
        var controlsTitle = UiTheme.Text("Controles", 15, Palette.Suit, Fonts.SansWeight(700));
        var controlsBack = UiTheme.GhostButton("Volver", 12);
        controlsBack.Pressed += () => ShowView(MenuView.Options, _returnTo);
        var controlsHint = UiTheme.Label("Pulsa «Cambiar» y luego una tecla para reasignar. Esc cancela.", 11, Palette.Muted);
        var controlsBody = UiTheme.VBox(8);
        controlsBody.AddChild(controlsTitle);
        controlsBody.AddChild(controlsHint);
        controlsBody.AddChild(_controlsList);
        controlsBody.AddChild(resetButton);
        controlsBody.AddChild(controlsBack);
        _controlsView = MakeCard("Controles", null, controlsBody);

        _saveSlots = new SlotListView(saveMode: true);
        _loadSlots = new SlotListView(saveMode: false);
        _saveSlots.ActionConfirmed += id => SaveSlotConfirmed?.Invoke(id);
        _loadSlots.ActionConfirmed += id => LoadSlotConfirmed?.Invoke(id);
        _saveSlots.DeleteConfirmed += SlotHostDelete;
        _loadSlots.DeleteConfirmed += id => DeleteSlotConfirmed?.Invoke(id);

        var saveBack = UiTheme.GhostButton("Volver", 12);
        saveBack.Pressed += () => ShowRoot(MenuContext.Pause);
        var saveBody = UiTheme.VBox(8);
        saveBody.AddChild(UiTheme.Caption("Guardar partida"));
        saveBody.AddChild(_saveSlots.Root);
        saveBody.AddChild(saveBack);
        _saveSlotsView = MakeCard("Guardar partida", null, saveBody);

        var loadBack = UiTheme.GhostButton("Volver", 12);
        loadBack.Pressed += () => ShowRoot(_returnTo);
        var loadBody = UiTheme.VBox(8);
        loadBody.AddChild(UiTheme.Caption("Cargar partida"));
        loadBody.AddChild(_loadSlots.Root);
        loadBody.AddChild(loadBack);
        _loadSlotsView = MakeCard("Cargar partida", null, loadBody);

        BuildControlsList();

        _volumeSlider.ValueChanged += OnVolumeChanged;
        _volumeSlider.SetValueNoSignal(SettingsService.GetVolume() * 100f);
        _volumeValue.Text = $"{Mathf.RoundToInt(SettingsService.GetVolume() * 100f)} %";

        AddChild(_startView);
        AddChild(_pauseView);
        AddChild(_optionsView);
        AddChild(_controlsView);
        AddChild(_saveSlotsView);
        AddChild(_loadSlotsView);

        _status = UiTheme.Label(string.Empty, 12, Palette.Suit);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _status.CustomMinimumSize = new Vector2(320, 0);
        _status.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        _status.OffsetTop -= 18;
        _status.OffsetBottom -= 18;
        _status.MouseFilter = MouseFilterEnum.Ignore;
        _status.Visible = false;
        AddChild(_status);
    }

    /// <summary>Brief status line shown inside the menu; hidden after a few seconds.</summary>
    public void SetStatus(string text, MessageKind kind)
    {
        var accent = kind switch
        {
            MessageKind.Warn => Palette.Energy,
            MessageKind.Good => Palette.Food,
            _ => Palette.O2,
        };
        _status.Text = text;
        _status.AddThemeColorOverride("font_color", accent);
        _status.Visible = true;
        var serial = ++_statusSerial;
        var timer = GetTree().CreateTimer(2.6);
        timer.Timeout += () => { if (serial == _statusSerial) _status.Visible = false; };
    }

    public void ShowRoot(MenuContext context)
    {
        _context = context;
        _returnTo = context;
        ShowView(context == MenuContext.Pause ? MenuView.Pause : MenuView.Start, context);
    }

    public void RefreshSlots()
    {
        _saveSlots.Rebuild();
        _loadSlots.Rebuild();
        RefreshContinueButton();
    }

    public override void _Input(InputEvent @event)
    {
        if (_capturing == null) return;
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

        GetViewport().SetInputAsHandled();
        var action = _capturing;
        _capturing = null;

        if (key.Keycode == Key.Escape || key.PhysicalKeycode == Key.Escape)
        {
            RefreshControls();
            return;
        }

        var code = (int)key.PhysicalKeycode;
        if (IsBindingUsed(action, code))
        {
            _session.Say("Esa tecla ya está asignada a otra acción.", MessageKind.Warn);
            RefreshControls();
            return;
        }

        SettingsService.SetBinding(action, code);
        RefreshControls();
    }

    public override void _Process(double delta)
    {
        var flow = _session.State.Flow;
        var show = flow == FlowState.Boot || flow == FlowState.Paused;

        if (show && !Visible)
        {
            var context = flow == FlowState.Paused ? MenuContext.Pause : MenuContext.Start;
            ShowRoot(context);
            RefreshSlots();
            Visible = true;
        }
        else if (!show && Visible)
        {
            Visible = false;
        }
        else if (Visible && flow == FlowState.Paused && _context == MenuContext.Start && _visibleView == _startView)
        {
            ShowRoot(MenuContext.Pause);
        }
        else if (Visible && flow == FlowState.Boot && _context == MenuContext.Pause && _visibleView == _pauseView)
        {
            ShowRoot(MenuContext.Start);
        }
    }

    private void ShowView(MenuView view, MenuContext returnTo)
    {
        _returnTo = returnTo;
        var next = view switch
        {
            MenuView.Start => _startView,
            MenuView.Pause => _pauseView,
            MenuView.Options => _optionsView,
            MenuView.Controls => _controlsView,
            MenuView.SaveSlots => _saveSlotsView,
            MenuView.LoadSlots => _loadSlotsView,
            _ => _startView,
        };

        foreach (var control in new[] { _startView, _pauseView, _optionsView, _controlsView, _saveSlotsView, _loadSlotsView })
        {
            control.Visible = control == next;
        }

        _visibleView = next;
    }

    private void ShowSlots(bool load)
    {
        if (load)
        {
            _loadSlots.Rebuild();
            ShowView(MenuView.LoadSlots, _context);
        }
        else
        {
            _saveSlots.Rebuild();
            ShowView(MenuView.SaveSlots, _context);
        }

        _returnTo = _context;
    }

    private Control MakeCard(string title, string? description, params Control[] buttons)
    {
        var titleLabel = UiTheme.Text(title, 18, Palette.Suit, Fonts.SansWeight(700));
        var buttonBox = UiTheme.VBox(8);
        foreach (var button in buttons) buttonBox.AddChild(button);

        var body = UiTheme.VBox(10);
        body.AddChild(titleLabel);
        if (description is not null)
        {
            var text = UiTheme.Label(description, 12, Palette.Muted);
            text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            text.CustomMinimumSize = new Vector2(420, 0);
            body.AddChild(text);
        }
        body.AddChild(buttonBox);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.Card(18));
        panel.CustomMinimumSize = new Vector2(440, 0);
        panel.AddChild(body);

        var root = new Control();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.MouseFilter = MouseFilterEnum.Stop;

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        center.AddChild(panel);
        root.AddChild(center);
        return root;
    }

    private void BuildControlsList()
    {
        foreach (var def in InputAdapter.BindingDefinitions)
        {
            var name = UiTheme.Label(def.Label, 12, Palette.Suit);
            name.CustomMinimumSize = new Vector2(200, 0);

            var key = UiTheme.Label(DisplayBinding(def.Action), 12, Palette.Muted);
            key.CustomMinimumSize = new Vector2(90, 0);
            key.HorizontalAlignment = HorizontalAlignment.Center;

            var change = UiTheme.GhostButton("Cambiar", 11);
            change.CustomMinimumSize = new Vector2(110, 0);
            change.Pressed += () => StartCapture(def.Action, change);

            var row = UiTheme.HBox(6);
            row.AddChild(name);
            row.AddChild(key);
            row.AddChild(change);

            _controlsList.AddChild(row);
            _keyLabels[def.Action] = key;
            _changeButtons[def.Action] = change;
        }
    }

    private string DisplayBinding(string action) =>
        OS.GetKeycodeString((Key)InputAdapter.ResolveKey(SettingsService.Current, action, (Key)InputAdapter.DefaultCodeFor(action)));

    private bool IsBindingUsed(string exceptAction, int code)
    {
        foreach (var def in InputAdapter.BindingDefinitions)
        {
            if (def.Action == exceptAction) continue;
            var current = InputAdapter.ResolveKey(SettingsService.Current, def.Action, def.DefaultKey);
            if (current == code) return true;
        }
        return false;
    }

    private void StartCapture(string action, Button button)
    {
        _capturing = action;
        foreach (var change in _changeButtons.Values) change.Disabled = true;
        button.Text = "Pulsa una tecla…";
    }

    private void ResetControls()
    {
        SettingsService.ResetBindings();
        RefreshControls();
        _session.Say("Controles restablecidos.", MessageKind.Info);
    }

    private void RefreshControls()
    {
        _capturing = null;
        foreach (var def in InputAdapter.BindingDefinitions)
        {
            if (_keyLabels.TryGetValue(def.Action, out var label)) label.Text = DisplayBinding(def.Action);
            if (_changeButtons.TryGetValue(def.Action, out var button))
            {
                button.Text = "Cambiar";
                button.Disabled = false;
            }
        }
    }

    private void OnVolumeChanged(double value)
    {
        SettingsService.SetVolume((float)(value / 100.0));
        _volumeValue.Text = $"{Mathf.RoundToInt((float)value)} %";
    }

    private void SlotHostDelete(string id) => DeleteSlotConfirmed?.Invoke(id);

    private void RefreshContinueButton() => _continueButtonHolder.Disabled = !SaveManager.HasAutosave;

    private readonly Button _continueButtonHolder;
    private readonly Button _newGameButtonHolder;
    private readonly Button _loadButtonHolder;
    private readonly Button _optionsButtonHolder;
    private readonly Button _quitButtonHolder;
    private readonly Button _resumeButtonHolder;
    private readonly Button _saveButtonHolder;
    private readonly Button _pauseLoadButtonHolder;
    private readonly Button _pauseOptionsButtonHolder;
    private readonly Button _quitMenuButtonHolder;
}
