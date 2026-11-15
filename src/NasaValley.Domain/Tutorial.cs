namespace NasaValley.Domain;

public enum TutorialGoal
{
    Move,
    MineIce,
    RefillOxygen,
    CraftTool,
    BuildDomeAndPlant,
}

public sealed record TutorialStep(string Id, TutorialGoal Goal, string Title, string Hint);

/// <summary>Short 5-step onboarding for new players. Steps advance automatically.</summary>
public static class TutorialDatabase
{
    public static readonly IReadOnlyList<TutorialStep> Steps = new[]
    {
        new TutorialStep("move", TutorialGoal.Move,
            "Muévete",
            "Usa W, A, S y D para caminar y explorar el cráter."),
        new TutorialStep("ice", TutorialGoal.MineIce,
            "Extrae hielo",
            "Acércate a una veta de hielo y mantén E para extraer."),
        new TutorialStep("oxygen", TutorialGoal.RefillOxygen,
            "Recarga oxígeno",
            "Vuelve a la nave, ábrela con E y recarga el traje con hielo."),
        new TutorialStep("craft", TutorialGoal.CraftTool,
            "Fabrica una herramienta",
            "Ve al Taller y fabrica un martillo para minar mejor."),
        new TutorialStep("dome", TutorialGoal.BuildDomeAndPlant,
            "Construye tu base",
            "Pulsa B, construye una cúpula, coloca una parcela en su terminal y planta."),
    };
}

/// <summary>Advances the tutorial when the current step's condition is met.</summary>
public static class TutorialSystem
{
    public static void Update(IGameContext ctx)
    {
        var s = ctx.State;
        if (s.TutorialDone || s.TutorialSkipped) return;
        if (s.TutorialStep < 0) s.TutorialStep = 0;
        if (s.TutorialStep >= TutorialDatabase.Steps.Count)
        {
            s.TutorialDone = true;
            return;
        }

        var step = TutorialDatabase.Steps[s.TutorialStep];
        if (!IsComplete(ctx, step.Goal)) return;

        s.TutorialStep++;
        if (s.TutorialStep >= TutorialDatabase.Steps.Count)
        {
            s.TutorialDone = true;
            ctx.Say("¡Tutorial completado!", MessageKind.Good);
        }
        else
        {
            ctx.Say(Loc.F("Paso completado: {0}", Loc.T(step.Title)), MessageKind.Good);
        }
    }

    public static void Skip(GameState s)
    {
        s.TutorialSkipped = true;
        s.TutorialDone = true;
    }

    private static bool IsComplete(IGameContext ctx, TutorialGoal goal)
    {
        var s = ctx.State;
        switch (goal)
        {
            case TutorialGoal.Move:
                return s.Moving;
            case TutorialGoal.MineIce:
                return s.Stats.Ice >= 1;
            case TutorialGoal.RefillOxygen:
                return s.ShipO2Charges < ctx.Balance.ShipO2ChargesMax;
            case TutorialGoal.CraftTool:
                foreach (var def in ctx.Items.Items)
                {
                    if (def.IsTool && s.Inventory[def.Id] > 0) return true;
                }
                return false;
            case TutorialGoal.BuildDomeAndPlant:
                return s.Domes.Count > 0 && s.Stats.Planted > 0;
            default:
                return false;
        }
    }
}
