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
        [FlowState.Playing] = new[] { FlowState.Paused, FlowState.Won, FlowState.Lost, FlowState.Boot },
        [FlowState.Paused] = new[] { FlowState.Playing, FlowState.Won, FlowState.Lost, FlowState.Boot },
        [FlowState.Won] = new[] { FlowState.Boot },
        [FlowState.Lost] = new[] { FlowState.Boot },
    };

    public GameSession(
        BalanceConfig? balance = null,
        ItemDatabase? items = null,
        NodeDatabase? nodes = null,
        PartDatabase? parts = null,
        RecipeDatabase? recipes = null,
        PlantDatabase? plants = null,
        MissionSet? missions = null,
        IRandom? rng = null,
        GameEvents? events = null)
    {
        Balance = balance ?? BalanceConfig.Default;
        Items = items ?? ItemDatabase.CreateDefault();
        Nodes = nodes ?? NodeDatabase.CreateDefault();
        Parts = parts ?? PartDatabase.CreateDefault();
        Recipes = recipes ?? RecipeDatabase.CreateDefault();
        Plants = plants ?? PlantDatabase.CreateDefault();
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
    public RecipeDatabase Recipes { get; }
    public PlantDatabase Plants { get; }
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
        if (State.Flow != FlowState.Playing || State.ModalOpen) return;

        State.Time += dt;
        MovementSystem.Update(this, intent, dt);
        State.ActionCooldown -= dt;

        if (State.Building)
        {
            BuildSystem.Update(this, intent);
        }
        else
        {
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
                case StationTarget when intent.InteractPressed:
                    Events.RaisePanelRequested("taller");
                    break;
                case ShipTarget when intent.InteractPressed:
                    Events.RaisePanelRequested("nave");
                    break;
                case DomeTerminalTarget dtt when intent.InteractPressed:
                    State.ActiveDomeId = dtt.Dome.Id;
                    Events.RaisePanelRequested("cupula");
                    break;
                case DomeSlotTarget slot when intent.InteractPressed:
                    var kind = State.PendingFixture ?? FixtureKind.Farm;
                    if (BuildSystem.BuildFixture(this, slot.Dome, slot.SlotIndex, kind, out var buildReason))
                        State.PendingFixture = null;
                    else Say(buildReason, MessageKind.Warn);
                    break;
                case DomeFixtureTarget fixture when intent.InteractPressed:
                    if (fixture.Fixture.Kind == FixtureKind.Bunk)
                    {
                        Rest(fixture.Dome);
                    }
                    else
                    {
                        State.ActiveDomeId = fixture.Dome.Id;
                        Events.RaisePanelRequested("cupula");
                    }
                    break;
                case PlotTarget pt when intent.InteractPressed:
                    if (pt.Plot.State == PlotState.Empty)
                    {
                        State.ModalOpen = true;
                        Events.RaisePanelRequested("plant");
                        State.ActivePlot = pt.Plot;
                        return;
                    }
                    new InteractCommand(pt).Execute(this);
                    break;
                case not null when intent.InteractPressed:
                    new InteractCommand(target).Execute(this);
                    break;
            }
        }

        NodeSystem.Update(this, dt);
        DomeSystem.Update(this, dt);
        FarmingSystem.Update(this, dt);
        WeatherSystem.Update(this, dt);
        SurvivalSystem.Update(this, dt);
        NormalizeFoodSource();
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
    public void Eat(string itemId) => new EatItemCommand(itemId).Execute(this);
    public void Repair(string partId) => new RepairPartCommand(partId).Execute(this);
    public void RefillO2() => new RefillO2Command().Execute(this);
    public void Plant(Plot plot, string plantId) => new PlantCommand(plot, plantId).Execute(this);
    public void MeltIce() => new MeltIceCommand().Execute(this);

    public void Craft(string recipeId)
    {
        if (Recipes.TryGet(recipeId, out var recipe)) new CraftCommand(recipe).Execute(this);
    }

    /// <summary>Freezes the simulation while a UI modal is open (flow stays Playing).</summary>
    public void OpenModal()
    {
        if (State.Flow == FlowState.Playing) State.ModalOpen = true;
    }

    public void CloseModal() => State.ModalOpen = false;

    public void SetCrop(string plantId)
    {
        if (Plants.TryGet(plantId, out _)) State.SelectedCrop = plantId;
    }

    /// <summary>Chooses which food item F eats first. Must be an item tagged "Comida".</summary>
    public void SetFoodSource(string itemId)
    {
        if (Items.TryGet(itemId, out var def) && def.HasTag("Comida")) State.FoodSource = itemId;
    }

    /// <summary>Keeps the displayed food source valid: swap to another food once it runs out.</summary>
    public void NormalizeFoodSource()
    {
        if (State.FoodSource is string current && Items.TryGet(current, out var chosen) &&
            chosen.HasTag("Comida") && State.Inventory[current] > 0)
        {
            return;
        }

        string? fallback = null;
        foreach (var def in Items.Items)
        {
            if (!def.HasTag("Comida") || State.Inventory[def.Id] <= 0) continue;
            fallback = def.Id;
            break;
        }
        State.FoodSource = fallback;
    }

    // ---- Dome management ------------------------------------------------------

    public Dome? ActiveDome => State.ActiveDomeId is int id ? BuildSystem.Find(State, id) : null;

    public IReadOnlyDictionary<string, int> UpgradeCostFor(Dome dome) =>
        BuildSystem.UpgradeCost(Balance, dome.Level + 1);

    public bool CanUpgradeDome(Dome dome, out string reason) =>
        BuildSystem.CanUpgrade(Balance, dome, State.Inventory, out _, out reason);

    public bool UpgradeDome(Dome dome, out string reason) => BuildSystem.Upgrade(this, dome, out reason);

    public void StoreDome(Dome dome) => BuildSystem.Remove(this, dome, store: true);
    public void DestroyDome(Dome dome) => BuildSystem.Remove(this, dome, store: false);

    /// <summary>Picks which interior fixture the next empty dome slot will place.</summary>
    public void RequestFixture(FixtureKind kind) => State.PendingFixture = kind;

    public void ClearPendingFixture() => State.PendingFixture = null;

    public void Rest(Dome dome) => new RestCommand(dome).Execute(this);

    public void StartMoveDome(Dome dome)
    {
        State.MovingDomeId = dome.Id;
        State.Building = true;
        State.ActiveDomeId = null;
    }

    /// <summary>Toggles developer mode. Enabling grants every item and speeds up crops.</summary>
    public void ToggleDevMode()
    {
        State.DevMode = !State.DevMode;
        if (!State.DevMode)
        {
            Say("Modo desarrollador desactivado.", MessageKind.Warn);
            return;
        }

        foreach (var def in Items.Items)
        {
            if (State.Inventory[def.Id] < Balance.DevItemAmount) State.Inventory[def.Id] = Balance.DevItemAmount;
        }
        NormalizeFoodSource();
        Say("Modo desarrollador activado: todos los objetos y cultivos acelerados.", MessageKind.Good);
    }

    public void StartBuild() => StartPlacement(BuildSystem.StoredDomeItem);

    public void StartPlacement(string itemId)
    {
        if (State.Flow != FlowState.Playing || State.ModalOpen) return;
        if (!Items.TryGet(itemId, out var def) || !def.HasTag("Colocable")) return;
        if (State.Inventory[itemId] < 1)
        {
            Say($"No tienes {def.Name.ToLowerInvariant()} para colocar.", MessageKind.Warn);
            return;
        }

        State.MovingDomeId = null;
        State.PendingFixture = null;
        State.PlacingItem = itemId;
        State.Building = true;
    }

    public void CancelBuild()
    {
        State.Building = false;
        State.MovingDomeId = null;
        State.PlacingItem = null;
    }

    public bool CanPlaceDome() => BuildSystem.CanPlaceAt(this, State.BuildX, State.BuildY, out _);

    public bool CanPlaceCurrentPlacement() =>
        State.PlacingItem == "parcela"
            ? BuildSystem.CanPlaceField(this, State.BuildX, State.BuildY, out _)
            : BuildSystem.CanPlaceAt(this, State.BuildX, State.BuildY, out _);

    // ---- Save / load -----------------------------------------------------------

    public SaveGameData CaptureSave()
    {
        var s = State;
        var data = new SaveGameData
        {
            SavedAtUtc = DateTime.UtcNow.ToString("O"),
            Time = s.Time,
            Px = s.Px,
            Py = s.Py,
            Face = (int)s.Face,
            FoodSource = s.FoodSource,
            SelectedCrop = s.SelectedCrop,
            RestCooldown = s.RestCooldown,
            Vitals = new VitalsData
            {
                O2 = s.Vitals.O2,
                Energy = s.Vitals.Energy,
                Food = s.Vitals.Food,
                Health = s.Vitals.Health,
            },
            Storm = new StormData { Next = s.Storm.Next, Left = s.Storm.Left },
            Stats = new StatsData { Ice = s.Stats.Ice, Planted = s.Stats.Planted, Harvested = s.Stats.Harvested },
        };

        foreach (var (id, count) in s.Inventory.Entries)
        {
            if (count > 0) data.Inventory.Add(new ItemStackData { Id = id, Count = count });
        }

        foreach (var (id, repaired) in s.Repairs)
        {
            if (repaired) data.RepairedParts.Add(id);
        }

        foreach (var node in s.Nodes)
        {
            data.Nodes.Add(new NodeData
            {
                Type = node.Type,
                X = node.X,
                Y = node.Y,
                Amount = node.Amount,
                Progress = node.Progress,
                Regen = node.Regen,
            });
        }

        foreach (var pickup in s.Pickups)
        {
            data.Pickups.Add(new PickupData
            {
                ItemId = pickup.ItemId,
                Amount = pickup.Amount,
                X = pickup.X,
                Y = pickup.Y,
                Taken = pickup.Taken,
            });
        }

        foreach (var dome in s.Domes)
        {
            var saved = new DomeData
            {
                Id = dome.Id,
                X = dome.X,
                Y = dome.Y,
                Width = dome.Width,
                Height = dome.Height,
                Level = dome.Level,
                Energy = dome.Energy,
                InternalTempC = dome.InternalTempC,
                ExternalTempC = dome.ExternalTempC,
            };
            foreach (var fixture in dome.Fixtures)
            {
                saved.Fixtures.Add(new FixtureData { Kind = fixture.Kind.ToString(), SlotIndex = fixture.SlotIndex });
            }
            foreach (var plot in dome.Plots)
            {
                saved.Plots.Add(new PlotData
                {
                    SlotIndex = plot.SlotIndex,
                    PlantId = plot.PlantId,
                    State = plot.State.ToString(),
                    Time = plot.Time,
                });
            }
            data.Domes.Add(saved);
        }

        foreach (var plot in s.Plots)
        {
            if (plot.Dome != null) continue; // already saved with its dome.
            data.Fields.Add(new FieldData
            {
                X = plot.X,
                Y = plot.Y,
                PlantId = plot.PlantId,
                State = plot.State.ToString(),
                Time = plot.Time,
            });
        }

        return data;
    }

    public void ApplySave(SaveGameData data)
    {
        var s = new GameState
        {
            Flow = FlowState.Playing,
        };

        s.Stations.Add(new Station(StationKind.Workbench, Balance.WorkbenchX, Balance.WorkbenchY));

        s.Time = Math.Max(0, data.Time);
        s.Px = Math.Clamp(data.Px, Balance.MoveMinX, Balance.MoveMaxX);
        s.Py = Math.Clamp(data.Py, Balance.MoveMinY, Balance.MoveMaxY);
        s.Face = Enum.IsDefined(typeof(Direction), data.Face) ? (Direction)data.Face : Direction.Down;

        s.Vitals.O2 = Saturate(data.Vitals.O2);
        s.Vitals.Energy = Saturate(data.Vitals.Energy);
        s.Vitals.Food = Saturate(data.Vitals.Food);
        s.Vitals.Health = Saturate(data.Vitals.Health);

        foreach (var stack in data.Inventory)
        {
            if (Items.TryGet(stack.Id, out _)) s.Inventory[stack.Id] = Math.Max(0, stack.Count);
        }

        s.FoodSource = data.FoodSource;
        s.SelectedCrop = Plants.TryGet(data.SelectedCrop, out _) ? data.SelectedCrop : "lechuga";

        s.Storm.Next = Math.Max(0, data.Storm.Next);
        s.Storm.Left = Math.Max(0, data.Storm.Left);

        s.Stats.Ice = Math.Max(0, data.Stats.Ice);
        s.Stats.Planted = Math.Max(0, data.Stats.Planted);
        s.Stats.Harvested = Math.Max(0, data.Stats.Harvested);

        foreach (var id in data.RepairedParts)
        {
            if (Parts.TryGet(id, out _)) s.Repairs[id] = true;
        }

        s.RestCooldown = Math.Max(0, data.RestCooldown);

        foreach (var node in data.Nodes)
        {
            if (!Nodes.TryGet(node.Type, out _)) continue;
            s.Nodes.Add(new ResourceNode(node.Type, node.X, node.Y, Math.Max(0, node.Amount))
            {
                Progress = Math.Max(0, node.Progress),
                Regen = Math.Max(0, node.Regen),
            });
        }

        foreach (var pickup in data.Pickups)
        {
            if (!Items.TryGet(pickup.ItemId, out _)) continue;
            s.Pickups.Add(new Pickup(pickup.ItemId, Math.Max(0, pickup.Amount), pickup.X, pickup.Y)
            {
                Taken = pickup.Taken,
            });
        }

        var domesById = new Dictionary<int, Dome>();
        foreach (var saved in data.Domes)
        {
            var dome = new Dome(saved.Id, saved.X, saved.Y, saved.Width, saved.Height)
            {
                Level = Math.Clamp(saved.Level, 1, Balance.DomeMaxLevel),
                Energy = Math.Max(0, saved.Energy),
                InternalTempC = saved.InternalTempC,
                ExternalTempC = saved.ExternalTempC,
            };
            s.Domes.Add(dome);
            domesById[dome.Id] = dome;
        }

        foreach (var saved in data.Domes)
        {
            var dome = domesById[saved.Id];

            foreach (var fixture in saved.Fixtures)
            {
                if (Enum.TryParse<FixtureKind>(fixture.Kind, out var kind))
                {
                    dome.Fixtures.Add(new Fixture(kind, fixture.SlotIndex));
                }
            }

            foreach (var plot in saved.Plots)
            {
                if (!Plants.TryGet(plot.PlantId, out _)) continue;
                var (px, py) = DomeLayout.SlotPosition(Balance, dome, plot.SlotIndex);
                var state = Enum.TryParse<PlotState>(plot.State, out var parsed) ? parsed : PlotState.Empty;
                var created = new Plot(px, py)
                {
                    Dome = dome,
                    SlotIndex = plot.SlotIndex,
                    PlantId = plot.PlantId,
                    State = state,
                    Time = plot.Time,
                };
                dome.Plots.Add(created);
                s.Plots.Add(created);
            }
        }

        foreach (var saved in data.Fields)
        {
            if (!Plants.TryGet(saved.PlantId, out _)) continue;
            var state = Enum.TryParse<PlotState>(saved.State, out var parsed) ? parsed : PlotState.Empty;
            var created = new Plot(saved.X, saved.Y)
            {
                SlotIndex = -1,
                PlantId = saved.PlantId,
                State = state,
                Time = saved.Time,
            };
            s.Plots.Add(created);
        }

        State = s;
        NormalizeFoodSource();
        Events.RaiseStateChanged();
    }

    public void ReturnToMenu()
    {
        State.ModalOpen = false;
        State.Building = false;
        State.MovingDomeId = null;
        State.PlacingItem = null;
        State.ActiveDomeId = null;
        State.PendingFixture = null;
        State.Flow = FlowState.Boot;
        Events.RaiseStateChanged();
    }

    private static double Saturate(double value) => Math.Clamp(value, 0, 100);

    // ---- IGameContext ---------------------------------------------------------

    public bool HasToolTag(string? tag)
    {
        if (string.IsNullOrEmpty(tag)) return true;
        foreach (var (id, count) in State.Inventory.Entries)
        {
            if (count <= 0) continue;
            if (Items.TryGet(id, out var def) && def.IsTool && def.HasTag(tag)) return true;
        }
        return false;
    }

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
        s.Inventory["fijaciones"] = Balance.StartFijaciones;
        s.FoodSource = "papas";

        s.Vitals.O2 = Balance.StartO2;
        s.Vitals.Energy = Balance.StartEnergy;
        s.Vitals.Food = Balance.StartFood;
        s.Vitals.Health = Balance.StartHealth;

        foreach (var spawn in WorldSetup.SpawnNodes(Rng))
        {
            s.Nodes.Add(new ResourceNode(spawn.Type, spawn.X, spawn.Y, Balance.NodeAmount));
        }

        foreach (var spawn in WorldSetup.SpawnPickups(Rng))
        {
            s.Pickups.Add(new Pickup(spawn.ItemId, spawn.Amount, spawn.X, spawn.Y));
        }

        s.Stations.Add(new Station(StationKind.Workbench, Balance.WorkbenchX, Balance.WorkbenchY));

        s.Storm.Next = Balance.FirstStormDelay;
        return s;
    }
}
