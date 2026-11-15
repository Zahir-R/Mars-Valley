namespace NasaValley.Domain;

public sealed class Inventory
{
    private readonly Dictionary<string, int> _items = new();

    public int this[string id]
    {
        get => _items.TryGetValue(id, out var v) ? v : 0;
        set => _items[id] = value;
    }

    public bool Has(string id, int amount) => this[id] >= amount;

    public void Add(string id, int amount) => this[id] += amount;

    public void Remove(string id, int amount) => this[id] = Math.Max(0, this[id] - amount);

    public IEnumerable<KeyValuePair<string, int>> Entries => _items;
}

public sealed class Vitals
{
    public double O2 { get; set; } = 100;
    public double Energy { get; set; } = 80;
    public double Food { get; set; } = 70;
    public double Health { get; set; } = 100;
}

public sealed class Plot
{
    public Plot(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; private set; }
    public float Y { get; private set; }
    public PlotState State { get; set; } = PlotState.Empty;
    public string PlantId { get; set; } = "papa";
    public double Time { get; set; }
    public Dome? Dome { get; set; }
    public int SlotIndex { get; init; } = -1;

    public void MoveTo(float x, float y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>
/// A built greenhouse shell. Its footprint is a breathable safe zone and it exposes a
/// grid of empty interior slots where farms and utilities can be placed afterwards.
/// </summary>
public sealed class Dome
{
    public Dome(int id, float x, float y, float width, float height)
    {
        Id = id;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int Id { get; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public int Level { get; set; } = 1;
    public double Energy { get; set; }
    public double InternalTempC { get; set; }
    public double ExternalTempC { get; set; }
    public List<Plot> Plots { get; } = new();
    public List<Fixture> Fixtures { get; } = new();

    public float CenterX => X + Width / 2f;
    public float CenterY => Y + Height / 2f;

    /// <summary>Console below the dome that opens its management terminal.</summary>
    public float TerminalX => CenterX;
    public float TerminalY => Y + Height + 16f;

    public int PanelCount => CountFixtures(FixtureKind.SolarPanel);
    public int BatteryCount => CountFixtures(FixtureKind.Battery);
    public int BunkCount => CountFixtures(FixtureKind.Bunk);

    public bool HasBunk => BunkCount > 0;

    public bool IsSlotOccupied(int slotIndex)
    {
        foreach (var plot in Plots)
        {
            if (plot.SlotIndex == slotIndex) return true;
        }
        foreach (var fixture in Fixtures)
        {
            if (fixture.SlotIndex == slotIndex) return true;
        }
        return false;
    }

    public bool HasFreeSlot(int slotCount)
    {
        for (var i = 0; i < slotCount; i++)
        {
            if (!IsSlotOccupied(i)) return true;
        }
        return false;
    }

    public double EnergyCapacity(BalanceConfig b) =>
        (b.DomeBaseEnergy + BatteryCount * b.BatteryStorage) * Level;

    private int CountFixtures(FixtureKind kind)
    {
        var count = 0;
        foreach (var fixture in Fixtures)
        {
            if (fixture.Kind == kind) count++;
        }
        return count;
    }
}

public enum FixtureKind
{
    Farm,
    SolarPanel,
    Battery,
    Bunk,
}

/// <summary>A non-farm utility placed on one of a dome's interior slots.</summary>
public sealed class Fixture
{
    public Fixture(FixtureKind kind, int slotIndex)
    {
        Kind = kind;
        SlotIndex = slotIndex;
    }

    public FixtureKind Kind { get; }
    public int SlotIndex { get; }
}

public sealed class ResourceNode
{
    public ResourceNode(string type, float x, float y, int amount)
    {
        Type = type;
        X = x;
        Y = y;
        Amount = amount;
    }

    public string Type { get; }
    public float X { get; }
    public float Y { get; }
    public int Amount { get; set; }
    public int Progress { get; set; }
    public double Regen { get; set; }
    public double Shake { get; set; }
}

/// <summary>An interactable base station (workbench) that opens a modal.</summary>
public sealed class Station
{
    public Station(StationKind kind, float x, float y)
    {
        Kind = kind;
        X = x;
        Y = y;
    }

    public StationKind Kind { get; }
    public float X { get; }
    public float Y { get; }
}

/// <summary>A loose item on the ground that is collected directly on interaction.</summary>
public sealed class Pickup
{
    public Pickup(string itemId, int amount, float x, float y)
    {
        ItemId = itemId;
        Amount = amount;
        X = x;
        Y = y;
    }

    public string ItemId { get; }
    public int Amount { get; }
    public float X { get; }
    public float Y { get; }
    public bool Taken { get; set; }
    public double Bob { get; set; }
}

public sealed class Storm
{
    public double Next { get; set; } = 85;
    public double Left { get; set; }
}

public sealed class RunStats
{
    public int Ice { get; set; }
    public int Planted { get; set; }
    public int Harvested { get; set; }
}

public sealed class WarningFlags
{
    public bool O2 { get; set; }
    public bool Food { get; set; }
}

public sealed class Particle
{
    public float X;
    public float Y;
    public float Vx;
    public float Vy;
    public double Life;
    public string Color = "#ffffff";
}

public sealed class FloatingText
{
    public float X;
    public float Y;
    public string Text = string.Empty;
    public string Color = "#ffffff";
    public double Life;
}

public sealed class LogEntry
{
    public LogEntry(string time, string text, MessageKind kind)
    {
        Time = time;
        Text = text;
        Kind = kind;
    }

    public string Time { get; }
    public string Text { get; }
    public MessageKind Kind { get; }
}

/// <summary>
/// Mutable snapshot of a whole session. It is the single source of truth; the UI
/// and renderer only ever read from it (or from events it triggers).
/// </summary>
public sealed class GameState
{
    public FlowState Flow { get; set; } = FlowState.Boot;
    public double Time { get; set; }
    public double WinTimer { get; set; }
    public bool Win { get; set; }

    /// <summary>Oxygen refills the ship has left (each one refills to 100%).</summary>
    public int ShipO2Charges { get; set; } = 5;

    public Inventory Inventory { get; } = new();
    public Vitals Vitals { get; } = new();
    public Dictionary<string, bool> Repairs { get; } = new();
    public List<Plot> Plots { get; } = new();
    public List<ResourceNode> Nodes { get; } = new();
    public List<Pickup> Pickups { get; } = new();
    public List<Station> Stations { get; } = new();
    public List<Dome> Domes { get; } = new();

    /// <summary>True while a UI modal is open; the simulation is frozen but the flow stays Playing.</summary>
    public bool ModalOpen { get; set; }

    /// <summary>True while placing a dome; the ghost follows the player and E confirms.</summary>
    public bool Building { get; set; }

    /// <summary>Item currently being placed (e.g. "cupula" or "parcela"), or null.</summary>
    public string? PlacingItem { get; set; }

    public float BuildX { get; set; }
    public float BuildY { get; set; }

    /// <summary>When set, confirming placement relocates this dome instead of building a new one.</summary>
    public int? MovingDomeId { get; set; }

    /// <summary>Dome whose terminal modal is currently open.</summary>
    public int? ActiveDomeId { get; set; }

    /// <summary>Dev mode (typed combo): full inventory and accelerated crops.</summary>
    public bool DevMode { get; set; }

    /// <summary>Fixture chosen in a dome terminal, placed on the next empty slot pressed with E.</summary>
    public FixtureKind? PendingFixture { get; set; }

    /// <summary>Remaining rest cooldown (seconds) before a bunk can be used again.</summary>
    public double RestCooldown { get; set; }

    public string SelectedCrop { get; set; } = "lechuga";

    /// <summary>Item id currently chosen as the food source (eaten first with F).</summary>
    public Plot? ActivePlot { get; set; }

    public string? FoodSource { get; set; }
    public Storm Storm { get; } = new();
    public RunStats Stats { get; } = new();
    public WarningFlags Flags { get; } = new();
    public List<Particle> Particles { get; } = new();
    public List<FloatingText> Floats { get; } = new();
    public List<LogEntry> Log { get; } = new();

    public float Px { get; set; }
    public float Py { get; set; }
    public Direction Face { get; set; } = Direction.Down;
    public double Walk { get; set; }
    public bool Moving { get; set; }
    public double ActionCooldown { get; set; }

    public bool Running => Flow == FlowState.Playing;
    public bool Paused => Flow == FlowState.Paused;
    public bool Over => Flow is FlowState.Won or FlowState.Lost;
}
