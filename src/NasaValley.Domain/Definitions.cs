namespace NasaValley.Domain;

public sealed record ItemDefinition(string Id, string Name, string Color);

public sealed record NodeDefinition(string Type, string ItemId, string Color);

public sealed record PartDefinition(
    string Id,
    string Name,
    string Description,
    IReadOnlyDictionary<string, int> Cost,
    bool RequiresAllOthers = false);

public enum GoalKind
{
    MineIce,
    PlantFirst,
    HarvestFirst,
    RepairOne,
    RepairFour,
    RepairAll,
}

public sealed record MissionGoal(string Text, GoalKind Kind, int Target);

public sealed record NodeSpawn(string Type, float X, float Y);

/// <summary>Lookup table for items; acts as a flyweight so definitions are shared.</summary>
public sealed class ItemDatabase
{
    private readonly Dictionary<string, ItemDefinition> _byId;

    public ItemDatabase(IEnumerable<ItemDefinition> items)
    {
        Items = items.ToList();
        _byId = Items.ToDictionary(i => i.Id);
        Order = Items.Select(i => i.Id).ToList();
    }

    public IReadOnlyList<ItemDefinition> Items { get; }
    public IReadOnlyList<string> Order { get; }
    public ItemDefinition this[string id] => _byId[id];
    public bool TryGet(string id, out ItemDefinition def) => _byId.TryGetValue(id, out def!);

    public static ItemDatabase CreateDefault() => new(new[]
    {
        new ItemDefinition("hielo", "Hielo", "#8fd3f4"),
        new ItemDefinition("agua", "Agua", "#4aa3e0"),
        new ItemDefinition("hierro", "Hierro", "#a6806f"),
        new ItemDefinition("silicio", "Silicio", "#e0d2a8"),
        new ItemDefinition("semillas", "Semillas", "#c9a45a"),
        new ItemDefinition("papas", "Papas", "#d9a05b"),
    });
}

public sealed class NodeDatabase
{
    private readonly Dictionary<string, NodeDefinition> _byType;

    public NodeDatabase(IEnumerable<NodeDefinition> nodes)
    {
        _byType = nodes.ToDictionary(n => n.Type);
    }

    public NodeDefinition this[string type] => _byType[type];
    public bool TryGet(string type, out NodeDefinition def) => _byType.TryGetValue(type, out def!);

    public static NodeDatabase CreateDefault() => new(new[]
    {
        new NodeDefinition("ice", "hielo", "#8fd3f4"),
        new NodeDefinition("iron", "hierro", "#a6806f"),
        new NodeDefinition("si", "silicio", "#e0d2a8"),
    });

    /// <summary>Fixed world spawns, ported verbatim from the original NODES table.</summary>
    public static IReadOnlyList<NodeSpawn> DefaultSpawns { get; } = new[]
    {
        new NodeSpawn("ice", 350, 70),
        new NodeSpawn("ice", 398, 98),
        new NodeSpawn("ice", 655, 365),
        new NodeSpawn("ice", 700, 330),
        new NodeSpawn("ice", 520, 400),
        new NodeSpawn("iron", 470, 200),
        new NodeSpawn("iron", 522, 228),
        new NodeSpawn("iron", 690, 84),
        new NodeSpawn("iron", 600, 112),
        new NodeSpawn("iron", 330, 350),
        new NodeSpawn("iron", 300, 235),
        new NodeSpawn("si", 272, 150),
        new NodeSpawn("si", 560, 320),
        new NodeSpawn("si", 420, 300),
        new NodeSpawn("si", 712, 214),
        new NodeSpawn("si", 610, 200),
    };
}

public sealed class PartDatabase
{
    private readonly Dictionary<string, PartDefinition> _byId;

    public PartDatabase(IEnumerable<PartDefinition> parts)
    {
        Parts = parts.ToList();
        _byId = Parts.ToDictionary(p => p.Id);
    }

    public IReadOnlyList<PartDefinition> Parts { get; }
    public PartDefinition this[string id] => _byId[id];
    public bool TryGet(string id, out PartDefinition def) => _byId.TryGetValue(id, out def!);

    public static PartDatabase CreateDefault() => new(new[]
    {
        new PartDefinition("casco", "Sellado del casco", "Parches de aleación y sellador",
            new Dictionary<string, int> { ["hierro"] = 5, ["silicio"] = 2 }),
        new PartDefinition("panel", "Paneles solares", "Celdas de silicio sobre bastidor",
            new Dictionary<string, int> { ["silicio"] = 7, ["hierro"] = 2 }),
        new PartDefinition("antena", "Antena de alta ganancia", "Para hablar con la Tierra",
            new Dictionary<string, int> { ["hierro"] = 3, ["silicio"] = 5 }),
        new PartDefinition("combustible", "Tanque de combustible", "Propelente hecho con agua y hierro",
            new Dictionary<string, int> { ["hielo"] = 6, ["hierro"] = 4 }),
        new PartDefinition("motor", "Motor de ascenso", "Toberas y turbobomba. Exige las otras cuatro piezas.",
            new Dictionary<string, int> { ["hierro"] = 8, ["silicio"] = 4 }, RequiresAllOthers: true),
    });
}

public sealed class MissionSet
{
    public MissionSet(IEnumerable<MissionGoal> goals) => Goals = goals.ToList();

    public IReadOnlyList<MissionGoal> Goals { get; }

    public static MissionSet CreateDefault() => new(new[]
    {
        new MissionGoal("Extrae 5 de hielo", GoalKind.MineIce, 5),
        new MissionGoal("Planta tu primera papa", GoalKind.PlantFirst, 1),
        new MissionGoal("Cosecha tus primeras papas", GoalKind.HarvestFirst, 1),
        new MissionGoal("Repara una pieza de la nave", GoalKind.RepairOne, 1),
        new MissionGoal("Repara cuatro piezas", GoalKind.RepairFour, 4),
        new MissionGoal("Repara el motor y despega", GoalKind.RepairAll, 5),
    });
}
