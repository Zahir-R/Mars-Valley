namespace NasaValley.Domain;

/// <summary>
/// A crop that can be grown in a dome. Requirements are gameplay simplifications
/// inspired by NASA's space-crop research (Veggie/CIP), not a scientific simulation.
/// </summary>
public sealed record PlantDefinition(
    string Id,
    string Name,
    string Color,
    string HarvestItemId,
    double GrowTime,
    int WaterPerGrowth,
    float MinTempC,
    float MaxTempC,
    float MinLight,
    int HarvestYield,
    string Description);

public sealed class PlantDatabase
{
    private readonly Dictionary<string, PlantDefinition> _byId;

    public PlantDatabase(IEnumerable<PlantDefinition> plants)
    {
        Plants = plants.ToList();
        _byId = Plants.ToDictionary(p => p.Id);
        Order = Plants.Select(p => p.Id).ToList();
    }

    public IReadOnlyList<PlantDefinition> Plants { get; }
    public IReadOnlyList<string> Order { get; }
    public PlantDefinition this[string id] => _byId[id];
    public bool TryGet(string id, out PlantDefinition def) => _byId.TryGetValue(id, out def!);

    /// <summary>
    /// Curated catalog of crops studied for spaceflight. Temperatures are dome
    /// setpoints (°C); light is a fraction of full daylight; water is units per cycle.
    /// </summary>
    public static PlantDatabase CreateDefault() => new(new[]
    {
        new PlantDefinition("lechuga", "Lechuga", "#7bd06a", "lechuga", 20, 1, 15, 24, 0.45f, 2,
            "Cultivo rápido y tolerante; el favorito de la estación Veggie."),
        new PlantDefinition("rabano", "Rábano", "#d6537a", "rabano", 16, 1, 14, 22, 0.40f, 2,
            "Raíz de ciclo muy corto, ideal para validar el sistema."),
        new PlantDefinition("papa", "Papa", "#d9a05b", "papas", 28, 1, 14, 22, 0.40f, 3,
            "Alto rendimiento y calorías; probada en el proyecto CIP."),
        new PlantDefinition("tomate", "Tomate", "#e0563d", "tomate", 32, 2, 20, 27, 0.60f, 2,
            "Necesita más luz y calor; variedad enana para espacio."),
        new PlantDefinition("trigo", "Trigo enano", "#e3c46b", "trigo", 36, 1, 15, 24, 0.55f, 4,
            "Grano básico; variedad de tallo corto para cámaras cerradas."),
    });
}
