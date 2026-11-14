namespace NasaValley.Domain;

/// <summary>
/// Composition root and facade for a single run. Owns the state, the systems and
/// the flow state machine, and is the only thing the presentation layer talks to.
/// </summary>
public sealed class GameSession : IGameContext
{
    private static readonly Dictionary<FlowState, FlowState[]> AllowedTransitions = new()
    {
        [FlowState.Boot] = new[] { FlowState.Playing },
        [FlowState.Playing] = new[] { FlowState.Paused, FlowState.Won, FlowState.Lost },
        [FlowState.Paused] = new[] { FlowState.Playing, FlowState.Won, FlowState.Lost },
        [FlowState.Won] = new[] { FlowState.Boot },
        [FlowState.Lost] = new[] { FlowState.Boot },
    };

    public GameSession(
        BalanceConfig? balance = null,
        ItemDatabase? items = null,
        NodeDatabase? nodes = null,
        PartDatabase? parts = null,
        MissionSet? missions = null,
        IRandom? rng = null,
        GameEvents? events = null)
    {
        Balance = balance ?? BalanceConfig.Default;
        Items = items ?? ItemDatabase.CreateDefault();
        Nodes = nodes ?? NodeDatabase.CreateDefault();
        Parts = parts ?? PartDatabase.CreateDefault();
        Missions = missions ?? MissionSet.CreateDefault();
        Rng = rng ?? new SystemRandom();
        Events = events ?? new GameEvents();
        State = CreateInitialState();
    }

    public GameState State { get; private set; }
    public BalanceConfig Balance { get; }
    public ItemDatabase Items { get; }
    public NodeDatabase Nodes { get; }
    public PartDatabase Parts { get; }
    public MissionSet Missions { get; }
    public IRandom Rng { get; }
    public GameEvents Events { get; }

    public InteractionTarget? CurrentTarget { get; private set; }
    public string Prompt => InteractionSystem.PromptFor(CurrentTarget, this);

    // ---- Flow -----------------------------------------------------------------

    public void Start()
    {
        if (State.Flow == FlowState.Boot) SetFlow(FlowState.Playing);
        Say("Aterrizaje forzoso en el cráter Gale. Empieza por conseguir hielo.");
    }

    public void TogglePause()
    {
        if (State.Flow == FlowState.Playing)
        {
            SetFlow(FlowState.Paused);
        }
        else if (State.Flow == FlowState.Paused)
        {
            SetFlow(FlowState.Playing);
        }
    }

    public void Reset()
    {
        SetFlow(FlowState.Boot);
        State = CreateInitialState();
        Events.RaiseStateChanged();
        Start();
    }

    public void Finish(bool win)
    {
        if (State.Over) return;
        State.Win = win;
        SetFlow(win ? FlowState.Won : FlowState.Lost);
        Events.RaiseGameEnded(win);
    }

    private void SetFlow(FlowState next)
    {
        if (State.Flow == next) return;
        if (AllowedTransitions.TryGetValue(State.Flow, out var allowed) && !allowed.Contains(next))
        {
            return;
        }
        State.Flow = next;
        Events.RaiseStateChanged();
    }

    // ---- Simulation -----------------------------------------------------------

    public void Tick(in PlayerIntent intent, double dt)
    {
        if (State.Flow != FlowState.Playing) return;

        State.Time += dt;
        MovementSystem.Update(this, intent, dt);
        State.ActionCooldown -= dt;

        var target = InteractionSystem.FindTarget(State, Balance);
        CurrentTarget = target;

        switch (target)
        {
            case NodeTarget nt:
                if (intent.InteractHeld && State.ActionCooldown <= 0)
                {
                    new MineNodeCommand(nt.Node).Execute(this);
                    State.ActionCooldown = State.Vitals.Energy <= 0
                        ? Balance.MineCooldownTired
                        : Balance.MineCooldownNormal;
                }
                break;
            case ShipTarget when intent.InteractPressed:
                Events.RaisePanelRequested("nave");
                break;
            case not null when intent.InteractPressed:
                new InteractCommand(target).Execute(this);
                break;
        }

        NodeSystem.Update(this, dt);
        FarmingSystem.Update(this, dt);
        WeatherSystem.Update(this, dt);
        SurvivalSystem.Update(this, dt);
        EffectsSystem.Update(this, dt);

        if (State.WinTimer > 0)
        {
            State.WinTimer -= dt;
            if (State.WinTimer <= 0) Finish(true);
        }

        if (State.Vitals.Health <= 0 && !State.Over) Finish(false);
    }

    // ---- Actions --------------------------------------------------------------

    public void Execute(IGameCommand command) => command.Execute(this);

    public void Eat() => new EatCommand().Execute(this);
    public void Repair(string partId) => new RepairPartCommand(partId).Execute(this);
    public void RefillO2() => new RefillO2Command().Execute(this);
    public void MeltIce() => new MeltIceCommand().Execute(this);

    // ---- IGameContext ---------------------------------------------------------

    public void Say(string text, MessageKind kind = MessageKind.Info)
    {
        var hour = GameClock.HourOf(State.Time, Balance.SolLength);
        State.Log.Insert(0, new LogEntry(GameClock.FormatTime(hour), text, kind));
        if (State.Log.Count > 7) State.Log.RemoveRange(7, State.Log.Count - 7);
        Events.RaiseMessage(text, kind);
    }

    // ---- Initial state --------------------------------------------------------

    private GameState CreateInitialState()
    {
        var s = new GameState
        {
            Px = Balance.StartX,
            Py = Balance.StartY,
        };

        s.Inventory["hielo"] = Balance.StartIce;
        s.Inventory["agua"] = Balance.StartWater;
        s.Inventory["semillas"] = Balance.StartSeeds;
        s.Inventory["papas"] = Balance.StartPotatoes;

        s.Vitals.O2 = Balance.StartO2;
        s.Vitals.Energy = Balance.StartEnergy;
        s.Vitals.Food = Balance.StartFood;
        s.Vitals.Health = Balance.StartHealth;

        for (var r = 0; r < Balance.PlotRows; r++)
        for (var c = 0; c < Balance.PlotColumns; c++)
        {
            s.Plots.Add(new Plot(36 + c * 46, 326 + r * 46));
        }

        foreach (var spawn in NodeDatabase.DefaultSpawns)
        {
            s.Nodes.Add(new ResourceNode(spawn.Type, spawn.X, spawn.Y, Balance.NodeAmount));
        }

        s.Storm.Next = Balance.FirstStormDelay;
        return s;
    }
}
