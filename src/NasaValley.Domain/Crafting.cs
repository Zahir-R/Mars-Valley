namespace NasaValley.Domain;

/// <summary>
/// Data-driven recipe. For the prototype every recipe is available at the manual
/// workbench; later phases can add station/research gating without touching commands.
/// </summary>
public sealed record RecipeDefinition(
    string Id,
    string Name,
    string Description,
    string OutputItemId,
    int OutputAmount,
    IReadOnlyDictionary<string, int> Ingredients,
    string Station = "manual");

public sealed class RecipeDatabase
{
    private readonly Dictionary<string, RecipeDefinition> _byId;

    public RecipeDatabase(IEnumerable<RecipeDefinition> recipes)
    {
        Recipes = recipes.ToList();
        _byId = Recipes.ToDictionary(r => r.Id);
    }

    public IReadOnlyList<RecipeDefinition> Recipes { get; }
    public RecipeDefinition this[string id] => _byId[id];
    public bool TryGet(string id, out RecipeDefinition def) => _byId.TryGetValue(id, out def!);

    public static RecipeDatabase CreateDefault() => new(new[]
    {
        new RecipeDefinition("martillo", "Martillo improvisado", "Rompe roca compacta.",
            "martillo", 1, new Dictionary<string, int> { ["roca"] = 3, ["fijaciones"] = 2 }),
        new RecipeDefinition("pala", "Pala improvisada", "Retira regolito suelto.",
            "pala", 1, new Dictionary<string, int> { ["roca"] = 4, ["fijaciones"] = 2 }),
        new RecipeDefinition("pico", "Pico básico", "Extrae depósitos de roca.",
            "pico", 1, new Dictionary<string, int> { ["roca"] = 5, ["fijaciones"] = 3, ["mango"] = 1 }),
        new RecipeDefinition("pico_reforzado", "Pico reforzado", "Refuerza un martillo con basalto.",
            "pico_reforzado", 1, new Dictionary<string, int> { ["martillo"] = 1, ["basalto"] = 2, ["fijaciones"] = 1 }),
    });
}
