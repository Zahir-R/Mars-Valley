namespace NasaValley.Domain;

/// <summary>Broad class of an item, used to drive behaviour without per-item classes.</summary>
public enum ItemCategory
{
    NaturalResource,
    Processed,
    Component,
    Tool,
    Consumable,
    Equipment,
    Seed,
    Structure,
    MissionItem,
}

/// <summary>
/// Data-driven item definition. Behaviour is derived from <see cref="Category"/> and
/// <see cref="Tags"/> so new materials never need their own class.
/// </summary>
public sealed record ItemDefinition(
    string Id,
    string Name,
    string Color,
    ItemCategory Category = ItemCategory.NaturalResource,
    string Description = "",
    bool Stackable = true,
    int MaxStack = 99,
    IReadOnlyList<string>? Tags = null)
{
    public IReadOnlyList<string> TagList => Tags ?? Array.Empty<string>();

    public bool IsTool => Category == ItemCategory.Tool;

    public bool HasTag(string tag) => TagList.Contains(tag);
}

/// <summary>
/// A resource node type. <see cref="RequiredToolTag"/> is the capability needed to
/// harvest it (null means bare hands). <see cref="HitsPerUnit"/> is the number of
/// hits required to yield a single unit.
/// </summary>
public sealed record NodeDefinition(
    string Type,
    string ItemId,
    string Color,
    string? RequiredToolTag = null,
    int HitsPerUnit = 1);

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

/// <summary>A loose item lying in the world that can be picked up directly.</summary>
public sealed record PickupSpawn(string ItemId, int Amount, float X, float Y);

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

    public IEnumerable<ItemDefinition> Tools => Items.Where(i => i.IsTool);

    public static ItemDatabase CreateDefault() => new(new[]
    {
        // --- existing survival resources -----------------------------------
        new ItemDefinition("hielo", "Hielo", "#8fd3f4", ItemCategory.NaturalResource,
            "Agua congelada bajo la superficie; fuente de agua y oxígeno.", Tags: new[] { "Mineral", "Agua" }),
        new ItemDefinition("agua", "Agua", "#4aa3e0", ItemCategory.Processed,
            "Agua fundida y tratada para cultivos y soporte vital.", Tags: new[] { "Agua" }),
        new ItemDefinition("hierro", "Hierro", "#a6806f", ItemCategory.NaturalResource,
            "Mineral de óxido de hierro para piezas resistentes.", Tags: new[] { "Mineral", "Metal" }),
        new ItemDefinition("silicio", "Silicio", "#e0d2a8", ItemCategory.NaturalResource,
            "Compuesto silicatado usado en vidrio y electrónica.", Tags: new[] { "Mineral" }),
        new ItemDefinition("semillas", "Semillas", "#c9a45a", ItemCategory.Seed,
            "Semillas de papa para el invernadero.", Tags: new[] { "Cultivo" }),
        new ItemDefinition("papas", "Papas", "#d9a05b", ItemCategory.Consumable,
            "Alimento cultivado en el invernadero.", Tags: new[] { "Comida" }),
        new ItemDefinition("lechuga", "Lechuga", "#7bd06a", ItemCategory.Consumable,
            "Hoja verde de ciclo corto cultivada en cúpula.", Tags: new[] { "Comida" }),
        new ItemDefinition("rabano", "Rábano", "#d6537a", ItemCategory.Consumable,
            "Raíz comestible de crecimiento rápido.", Tags: new[] { "Comida" }),
        new ItemDefinition("tomate", "Tomate", "#e0563d", ItemCategory.Consumable,
            "Fruto de cúpula que exige más luz y calor.", Tags: new[] { "Comida" }),
        new ItemDefinition("trigo", "Trigo enano", "#e3c46b", ItemCategory.Consumable,
            "Grano básico cultivado en cámara cerrada.", Tags: new[] { "Comida" }),

        // --- raw geology (Fase 2/3) ----------------------------------------
        new ItemDefinition("roca", "Fragmentos de roca", "#9a8f86", ItemCategory.NaturalResource,
            "Trozos sueltos de roca marciana. Base de la fabricación manual.", Tags: new[] { "Mineral", "Roca" }),
        new ItemDefinition("regolito", "Regolito", "#b98c6a", ItemCategory.NaturalResource,
            "Polvo y arena de la superficie. No es tierra fértil.", Tags: new[] { "Mineral", "Superficie" }),
        new ItemDefinition("basalto", "Basalto", "#5b5f66", ItemCategory.NaturalResource,
            "Roca volcánica dura rica en silicatos.", Tags: new[] { "Mineral", "Roca" }),

        // --- recovered / processed components ------------------------------
        new ItemDefinition("fijaciones", "Fijaciones recuperadas", "#c7b299", ItemCategory.Component,
            "Tornillos y remaches rescatados de los restos de la nave.", Tags: new[] { "Componente" }),
        new ItemDefinition("mango", "Mango", "#a9773f", ItemCategory.Component,
            "Agarre de madera o compuesto para herramientas.", Tags: new[] { "Componente" }),

        // --- tools (Fase 5) -------------------------------------------------
        new ItemDefinition("martillo", "Martillo improvisado", "#d0a24c", ItemCategory.Tool,
            "Rompe roca compacta como el basalto.", Tags: new[] { "Herramienta", "Mineria" }),
        new ItemDefinition("pala", "Pala improvisada", "#b8b8b8", ItemCategory.Tool,
            "Retira regolito y material suelto.", Tags: new[] { "Herramienta", "Excavacion" }),
        new ItemDefinition("pico", "Pico básico", "#8a8f96", ItemCategory.Tool,
            "Extrae depósitos de roca con eficacia.", Tags: new[] { "Herramienta", "Mineria" }),
        new ItemDefinition("pico_reforzado", "Pico reforzado", "#c9a13a", ItemCategory.Tool,
            "Pico con refuerzo de basalto; mina y excava.", Tags: new[] { "Herramienta", "Mineria", "Excavacion" }),

        // --- structures -----------------------------------------------------
        new ItemDefinition("cupula", "Cúpula almacenada", "#8fd3f4", ItemCategory.Structure,
            "Cúpula de invernadero plegada. Colócala desde el inventario sin gastar materiales; al guardarla se pierde lo que hubiera dentro.",
            Tags: new[] { "Estructura", "Dome", "Colocable" }),
        new ItemDefinition("parcela", "Bandeja de cultivo", "#c9a45a", ItemCategory.Structure,
            "Bandeja abierta para cultivar al descubierto. Solo las plantas resistentes cosechan en un bioma cálido; la cúpula es más segura.",
            Tags: new[] { "Estructura", "Colocable", "Agricultura" }),
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
        // Existing survival nodes stay hand-harvestable to keep the early loop intact.
        new NodeDefinition("ice", "hielo", "#8fd3f4"),
        new NodeDefinition("iron", "hierro", "#a6806f"),
        new NodeDefinition("si", "silicio", "#e0d2a8"),

        // Milestone nodes with tool requirements.
        new NodeDefinition("rock", "roca", "#9a8f86", RequiredToolTag: null, HitsPerUnit: 1),
        new NodeDefinition("regolith", "regolito", "#b98c6a", RequiredToolTag: "Excavacion", HitsPerUnit: 1),
        new NodeDefinition("basalt", "basalto", "#5b5f66", RequiredToolTag: "Mineria", HitsPerUnit: 2),
    });
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
