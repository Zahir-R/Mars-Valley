namespace NasaValley.Domain;

public readonly record struct GoalProgress(bool Done, string Progress);

/// <summary>Strategy-per-goal evaluation, ported from the original GOALS array.</summary>
public static class MissionSystem
{
    public static GoalProgress Evaluate(GameState s, MissionGoal goal, PartDatabase parts)
    {
        var repairs = RepairSystem.Count(s);
        return goal.Kind switch
        {
            GoalKind.MineIce => Build(s.Stats.Ice >= goal.Target, s.Stats.Ice, goal.Target),
            GoalKind.PlantFirst => Build(s.Stats.Planted >= goal.Target, s.Stats.Planted, goal.Target),
            GoalKind.HarvestFirst => Build(s.Stats.Harvested >= goal.Target, s.Stats.Harvested, goal.Target),
            GoalKind.RepairOne => Build(repairs >= goal.Target, repairs, goal.Target),
            GoalKind.RepairFour => Build(repairs >= goal.Target, repairs, goal.Target),
            GoalKind.RepairAll => Build(repairs >= goal.Target, repairs, goal.Target),
            _ => new GoalProgress(false, "0/0"),
        };
    }

    public static string CurrentObjective(GameState s, MissionSet missions, PartDatabase parts)
    {
        foreach (var goal in missions.Goals)
        {
            var progress = Evaluate(s, goal, parts);
            if (!progress.Done) return $"{Loc.T(goal.Text)} ({progress.Progress})";
        }
        return Loc.T("Todo listo para despegar");
    }

    private static GoalProgress Build(bool done, int value, int target) =>
        new(done, $"{Math.Min(target, value)}/{target}");
}
