using Godot;
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

    private GameSession _session = null!;
    private InputAdapter _input = null!;
    private Hud _hud = null!;
    private double _accumulator;

    public override void _Ready()
    {
        InputAdapter.RegisterActions();

        _session = new GameSession();
        _input = new InputAdapter();

        _hud = new Hud(_session, _input);
        AddChild(_hud);
    }

    public override void _Process(double delta)
    {

        if (_input.PausePressed())
        {
            if (_session.State.ModalOpen) _hud.CloseModal();
            else if (_session.State.Building) _session.CancelBuild();
            else _session.TogglePause();
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

        if (_session.State.ModalOpen)
        {
            _input.GetIntent();
            _accumulator = 0;
            return;
        }

        if (_session.State.Running && !_session.State.Paused && !_session.State.Over && _input.EatPressed())
        {
            _session.Eat();
        }

        var intent = _input.GetIntent();
        _accumulator += Mathf.Min(delta, 0.25);
        var pressed = intent.InteractPressed;

        while (_accumulator >= FixedStep)
        {
            _session.Tick(intent with { InteractPressed = pressed }, FixedStep);
            pressed = false;
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
        }
        else if (what == NotificationWMCloseRequest)
        {
            GetTree().Quit();
        }
    }
}
