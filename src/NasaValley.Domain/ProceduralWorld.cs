namespace NasaValley.Domain;

/// <summary>
/// Procedural spawn generator for a run's world. Node placements are weighted by biome
/// (rocks in the rocky basin, regolith in the plains, basalt in the highlands…) and
/// seeded by the session RNG so save/load is deterministic.
/// </summary>
public static class WorldSetup
{
    private static readonly (string Type, string ItemId)[] PickupPool =
    {
        ("rock", "roca"),
        ("regolith", "regolito"),
        ("basalt", "basalto"),
        ("iron", "hierro"),
        ("si", "silicio"),
        ("ice", "hielo"),
        ("fijaciones", "fijaciones"),
    };

    public static List<NodeSpawn> SpawnNodes(IRandom rng)
    {
        var cells = FreeCells().ToList();
        var spawns = new List<NodeSpawn>();
        var totalCells = cells.Count;
        var target = 130;

        // Keep every node at least one tile from the ship footprint.
        var ship = new List<int>();
        for (var y = 0; y < GeneratedBiomes.Height; y++)
        for (var x = 0; x < GeneratedBiomes.Width; x++)
        {
            var wx = x * GeneratedBiomes.TileSize;
            var wy = y * GeneratedBiomes.TileSize;
            if (wx + GeneratedBiomes.TileSize < BalanceConfig.Default.ShipX - 8 ||
                wx > BalanceConfig.Default.ShipX + BalanceConfig.Default.ShipWidth + 8 ||
                wy + GeneratedBiomes.TileSize < BalanceConfig.Default.ShipY - 8 ||
                wy > BalanceConfig.Default.ShipY + BalanceConfig.Default.ShipHeight + 8)
            {
                continue;
            }
            ship.Add(y * GeneratedBiomes.Width + x);
        }

        var shuffled = cells.OrderBy(_ => rng.NextDouble()).ToList();

        foreach (var cell in shuffled)
        {
            if (spawns.Count >= target) break;
            if (ship.Contains(cell)) continue;

            var x = cell % GeneratedBiomes.Width;
            var y = cell / GeneratedBiomes.Width;
            var biome = GeneratedBiomes.At(x, y);

            var roll = rng.NextDouble();
            var density = 0.024; // ~130 nodes over 5376 cells
            if (roll > density) continue;

            // Weighted node-type pick for this biome.
            var total = 0.0;
            foreach (var (_, weight) in biome.NodeWeights)
            {
                total += weight;
            }

            if (total <= 0) continue;
            var pick = rng.NextDouble() * total;
            double acc = 0;
            string? chosen = null;
            foreach (var (nodeType, weight) in biome.NodeWeights)
            {
                acc += weight;
                if (acc >= pick)
                {
                    chosen = nodeType;
                    break;
                }
            }

            if (chosen == null) continue;

            var px = x * GeneratedBiomes.TileSize + GeneratedBiomes.TileSize / 2f +
                     (float)(rng.NextDouble() - 0.5) * 20f;
            var py = y * GeneratedBiomes.TileSize + GeneratedBiomes.TileSize / 2f +
                     (float)(rng.NextDouble() - 0.5) * 20f;
            spawns.Add(new NodeSpawn(chosen, px, py));
        }

        return spawns;
    }

    public static List<PickupSpawn> SpawnPickups(IRandom rng)
    {
        var spawns = new List<PickupSpawn>();

        // Loose parts near the landing site, scattered debris further out.
        for (var i = 0; i < 12; i++)
        {
            var angle = rng.NextDouble() * Math.Tau;
            var distance = 90 + rng.NextDouble() * 260;
            var x = (float)(BalanceConfig.Default.ShipX + BalanceConfig.Default.ShipWidth / 2f + Math.Cos(angle) * distance);
            var y = (float)(BalanceConfig.Default.ShipY + BalanceConfig.Default.ShipHeight / 2f + Math.Sin(angle) * distance);
            string itemId;
            if (i < 8) itemId = "fijaciones";
            else if (i < 11) itemId = "mango";
            else itemId = "roca";
            spawns.Add(new PickupSpawn(itemId, 1, x, y));
        }

        for (var i = 0; i < 18; i++)
        {
            var x = 60 + (float)rng.NextDouble() * (GeneratedBiomes.WorldWidth - 120);
            var y = 60 + (float)rng.NextDouble() * (GeneratedBiomes.WorldHeight - 120);
            if (OverlapsShip(x, y)) continue;

            // Weighted toward biome contents.
            var tx = (int)(x / GeneratedBiomes.TileSize);
            var ty = (int)(y / GeneratedBiomes.TileSize);
            var biome = GeneratedBiomes.At(tx, ty);
            var total = 0.0;
            foreach (var (_, weight) in biome.NodeWeights)
            {
                total += weight;
            }

            if (total <= 0) continue;
            var pick = rng.NextDouble() * total;
            double acc = 0;
            string? itemId = null;
            foreach (var (nodeType, weight) in biome.NodeWeights)
            {
                acc += weight;
                if (acc >= pick)
                {
                    itemId = nodeType switch
                    {
                        "ice" => "hielo",
                        "regolith" => "roca",
                        "basalt" => "roca",
                        "si" => "silicio",
                        "iron" => "hierro",
                        "rock" => "roca",
                        _ => null,
                    };
                    break;
                }
            }

            if (itemId != null) spawns.Add(new PickupSpawn(itemId, 1, x, y));
        }

        return spawns;
    }

    private static bool OverlapsShip(float x, float y)
    {
        var b = BalanceConfig.Default;
        return x >= b.ShipX - 16 && x <= b.ShipX + b.ShipWidth + 16 &&
               y >= b.ShipY - 16 && y <= b.ShipY + b.ShipHeight + 16;
    }

    private static IEnumerable<int> FreeCells()
    {
        for (var y = 0; y < GeneratedBiomes.Height; y++)
        for (var x = 0; x < GeneratedBiomes.Width; x++)
        {
            yield return y * GeneratedBiomes.Width + x;
        }
    }
}
