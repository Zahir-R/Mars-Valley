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

    public float X { get; }
    public float Y { get; }
    public PlotState State { get; set; } = PlotState.Empty;
    public double Time { get; set; }
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

    public Inventory Inventory { get; } = new();
    public Vitals Vitals { get; } = new();
    public Dictionary<string, bool> Repairs { get; } = new();
    public List<Plot> Plots { get; } = new();
    public List<ResourceNode> Nodes { get; } = new();
    public List<Pickup> Pickups { get; } = new();
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
