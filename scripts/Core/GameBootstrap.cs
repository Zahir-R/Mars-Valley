using Godot;
using NasaValley.Core;
using NasaValley.Domain;
using NasaValley.Input;
using NasaValley.UI;

namespace NasaValley.Core;

/// <summary>
/// Composition root. Builds the session, the input adapter and the HUD, then runs
/// the fixed-step simulation loop and forwards discrete input.
/// </summary>
public partial class GameBootstrap : Node
{
    private const double FixedStep = 1.0 / 60.0;
    private const string DevPhrase = "marscolonization";
    private const double AutosaveSeconds = 60.0;

    private GameSession _session = null!;
    private InputAdapter _input = null!;
    private Hud _hud = null!;
    private double _accumulator;
    private double _autosaveTimer;
    private string _typed = string.Empty;
    private bool _pendingInteract;

    public override void _Ready()
    {
        InputAdapter.RegisterActions(SettingsService.Current);
        SettingsService.ApplyAudio();

        _session = new GameSession();
        _input = new InputAdapter();

        var audio = new AudioDirector { Session = _session };
        AddChild(audio);

        _hud = new Hud(_session, _input);
        AddChild(_hud);
    }

    /// <summary>Listens for the dev-mode phrase anywhere during a run (except in menus).</summary>
    public override void _Input(InputEvent @event)
    {
        if (_hud?.MenuOpen == true) return;
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;

        char ch;
        if (key.Unicode != 0)
        {
            ch = (char)key.Unicode;
        }
        else if (key.Keycode >= Key.A && key.Keycode <= Key.Z)
        {
            ch = (char)('a' + ((int)key.Keycode - (int)Key.A));
        }
        else
        {
            return;
        }

        ch = char.ToLowerInvariant(ch);
        if (ch < 'a' || ch > 'z') return;

        _typed += ch;
        if (_typed.Length > DevPhrase.Length) _typed = _typed.Substring(_typed.Length - DevPhrase.Length);

        if (_typed != DevPhrase) return;

        _typed = string.Empty;
        _session.ToggleDevMode();
    }

    public override void _Process(double delta)
    {
        // Autosave while the run is active; a brand-new run is saved on first pause/exit.
        if (_session.State.Running || _session.State.Paused)
        {
            _autosaveTimer += delta;
            if (_autosaveTimer >= AutosaveSeconds)
            {
                _autosaveTimer = 0;
                SaveManager.SaveTo(_session, SaveSlots.Autosave);
            }
        }
        else
        {
            _autosaveTimer = 0;
        }

        if (_input.PausePressed())
        {
            if (_session.State.ModalOpen) _hud.CloseModal();
            else if (_session.State.Building) _session.CancelBuild();
            else _session.TogglePause();

            if (_session.State.Paused) SaveManager.SaveTo(_session, SaveSlots.Autosave);
            return;
        }

        if (_hud.MenuOpen)
        {
            // A menu is open: the world is frozen; only the menu sees input.
            _input.GetIntent();
            _accumulator = 0;
            _pendingInteract = false;
            return;
        }

        if (_input.BuildPressed() && _session.State.Running &&
            !_session.State.ModalOpen && !_session.State.Building)
        {
            _session.StartBuild();
        }

        if (_input.StatsPressed() && _session.State.Running && !_session.State.ModalOpen)
        {
            _hud.ToggleStats();
        }

        if (_input.MinimapPressed())
        {
            _hud.ToggleMinimap();
        }

        if (_session.State.ModalOpen)
        {
            _input.GetIntent();
            _accumulator = 0;
            _pendingInteract = false;
            return;
        }

        if (_session.State.Running && !_session.State.Paused && !_session.State.Over && _input.EatPressed())
        {
            _session.Eat();
        }

        var intent = _input.GetIntent();
        // Latch the single-frame press; it survives frames that produce no fixed-step tick.
        _pendingInteract |= intent.InteractPressed;
        _accumulator += Mathf.Min(delta, 0.25);

        while (_accumulator >= FixedStep)
        {
            var pressedThisTick = _pendingInteract;
            _pendingInteract = false;
            _session.Tick(intent with { InteractPressed = pressedThisTick }, FixedStep);
            _accumulator -= FixedStep;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut)
        {
            _input.Clear();
            if (_session.State.ModalOpen) return;
            if (_session.State.Running && !_session.State.Paused)
            {
                _session.TogglePause();
            }

            if (_session.State.Paused)
            {
                SaveManager.SaveTo(_session, SaveSlots.Autosave);
            }
        }
        else if (what == NotificationWMCloseRequest)
        {
            if (_session.State.Running || _session.State.Paused)
            {
                SaveManager.SaveTo(_session, SaveSlots.Autosave);
            }
            GetTree().Quit();
        }
    }
}
