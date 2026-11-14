namespace NasaValley.Domain;

/// <summary>Repair affordability / locking rules, ported from canPay/pay/locked.</summary>
public static class RepairSystem
{
	public static int Count(GameState s) => s.Repairs.Count(kv => kv.Value);

	public static bool CanPay(GameState s, PartDefinition part)
	{
		foreach (var (item, amount) in part.Cost)
		{
			if (s.Inventory[item] < amount) return false;
		}
		return true;
	}

	public static void Pay(GameState s, PartDefinition part)
	{
		foreach (var (item, amount) in part.Cost)
		{
			s.Inventory.Remove(item, amount);
		}
	}

	public static bool IsLocked(GameState s, PartDatabase parts, PartDefinition part)
	{
		if (!part.RequiresAllOthers) return false;
		return parts.Parts.Any(p => p.Id != part.Id && !s.Repairs.GetValueOrDefault(p.Id));
	}

	public static bool IsRepaired(GameState s, string id) => s.Repairs.GetValueOrDefault(id);
}
