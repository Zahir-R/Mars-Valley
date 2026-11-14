namespace NasaValley.Domain;

/// <summary>
/// Single source of truth for every tunable value in the game. Keeping these in
/// one place (instead of scattered magic numbers) is what lets designers retune
/// the game without touching simulation code.
/// </summary>
public sealed class BalanceConfig
{
    // World / view. The biome grid is baked from real Gale Crater data (see GeneratedBiomes).
    public int ViewWidth { get; init; } = 768;
    public int ViewHeight { get; init; } = 448;
    public int WorldWidth => GeneratedBiomes.WorldWidth;
    public int WorldHeight => GeneratedBiomes.WorldHeight;
    public int PlotSize { get; init; } = 34;
    public int PlotColumns { get; init; } = 2;
    public int PlotRows { get; init; } = 2;

    // Domes are empty shells: farms and utilities are placed inside them afterward.
    // The footprint leaves room for 12 interior slots (more than four farms).
    public int DomeWidth { get; init; } = 220;
    public int DomeHeight { get; init; } = 150;
    public int DomeSlotColumns { get; init; } = 4;
    public int DomeSlotRows { get; init; } = 3;
    public float DomeSlotGap { get; init; } = 10f;
    public float DomeSlotMarginX { get; init; } = 27f;
    public float DomeSlotMarginY { get; init; } = 14f;

    public int DomeMaxLevel { get; init; } = 3;
    public float DomeTerminalRadius { get; init; } = 30f;

    public double DomeStartEnergy { get; init; } = 40;
    public double DomeBaseEnergy { get; init; } = 60;
    public double BatteryStorage { get; init; } = 80;
    public double DomeTargetTempC { get; init; } = 22;
    public double DomePassiveSolar { get; init; } = 1.4;
    public double SolarPanelOutput { get; init; } = 2.0;
    public double DomeEnergyPerDegreePerSec { get; init; } = 0.008;
    public double RestEnergyGain { get; init; } = 60;
    public double RestCooldown { get; init; } = 15;

    public IReadOnlyDictionary<string, int> DomeCost { get; init; } = new Dictionary<string, int>
    {
        ["hierro"] = 4,
        ["silicio"] = 4,
        ["roca"] = 6,
    };

    // Interior fixtures (placed on a dome slot).
    public IReadOnlyDictionary<string, int> FarmCost { get; init; } = new Dictionary<string, int>
    {
        ["roca"] = 3,
    };

    public IReadOnlyDictionary<string, int> SolarPanelCost { get; init; } = new Dictionary<string, int>
    {
        ["silicio"] = 4,
        ["hierro"] = 2,
    };

    public IReadOnlyDictionary<string, int> BatteryCost { get; init; } = new Dictionary<string, int>
    {
        ["hierro"] = 3,
        ["silicio"] = 2,
    };

    public IReadOnlyDictionary<string, int> BunkCost { get; init; } = new Dictionary<string, int>
    {
        ["roca"] = 3,
        ["hierro"] = 2,
    };

    /// <summary>Cost to upgrade a dome from (key-1) to key. Level is capped at DomeMaxLevel.</summary>
    public IReadOnlyDictionary<int, IReadOnlyDictionary<string, int>> DomeUpgradeCost { get; init; } =
        new Dictionary<int, IReadOnlyDictionary<string, int>>
        {
            [2] = new Dictionary<string, int> { ["hierro"] = 6, ["silicio"] = 6, ["roca"] = 8 },
            [3] = new Dictionary<string, int> { ["hierro"] = 10, ["silicio"] = 10, ["roca"] = 12 },
        };

    // Developer mode (unlocked by typing "marscolonization")
    public int DevItemAmount { get; init; } = 99;
    public double DevGrowSpeedMultiplier { get; init; } = 10;

    // Environment / weather coupling
    public double StormLightFactor { get; init; } = 0.45;
    public double DayNightSwing { get; init; } = 12;
    public double StormTempDrop { get; init; } = 6;

    // Ship / greenhouse rectangles (logical world units)
    public float ShipX { get; init; } = 1330;
    public float ShipY { get; init; } = 780;
    public float ShipWidth { get; init; } = 170;
    public float ShipHeight { get; init; } = 118;

    // Astronaut
    public float NormalSpeed { get; init; } = 100f;
    public float TiredSpeed { get; init; } = 62f;
    public float TiredEnergy { get; init; } = 15f;
    public float MoveMinX { get; init; } = 14f;
    public float MoveMaxX => WorldWidth - 14f;
    public float MoveMinY { get; init; } = 24f;
    public float MoveMaxY => WorldHeight - 18f;
    public float BodyHalfWidth { get; init; } = 7f;
    public float BodyTopOffset { get; init; } = 2f;
    public float BodyHeight { get; init; } = 12f;
    public float BodyWidth { get; init; } = 14f;

    // Interaction radii
    public float NodeInteractRadius { get; init; } = 46f;
    public float PlotInteractRadius { get; init; } = 34f;
    public float PickupInteractRadius { get; init; } = 40f;
    public float StationInteractRadius { get; init; } = 40f;
    public float ShipTargetRadius { get; init; } = 40f;
    public float ShipNearRadius { get; init; } = 48f;

    // Occupied footprints for structure placement and vein respawn.
    public float NodeFootprintRadius { get; init; } = 18f;
    public float PickupFootprintRadius { get; init; } = 10f;
    public float StationFootprintRadius { get; init; } = 18f;
    public double NodeBlockedRetry { get; init; } = 5;

    // Base stations (world positions). The ship itself is the ship upgrades station.
    public float WorkbenchX { get; init; } = 1580f;
    public float WorkbenchY { get; init; } = 975f;

    // Simulation clock
    public double SolLength { get; init; } = 240;
    public double GrowTime { get; init; } = 25;
    public double WinDelay { get; init; } = 2.4;

    // Resource nodes
    public int NodeAmount { get; init; } = 4;
    public double NodeRegenTime { get; init; } = 45;
    public double NodeShakeTime { get; init; } = 0.18;
    public double MineCooldownNormal { get; init; } = 0.35;
    public double MineCooldownTired { get; init; } = 0.8;
    public double MineEnergyCost { get; init; } = 1;

    // Vitals decay
    public double O2Decay { get; init; } = 0.42;
    public double FoodDecay { get; init; } = 0.10;
    public double EnergyDecay { get; init; } = 0.06;
    public double EnergyMoveDecay { get; init; } = 0.05;
    public double StormDecayMultiplier { get; init; } = 1.6;
    public double O2HealthDrain { get; init; } = 5;
    public double FoodHealthDrain { get; init; } = 0.8;
    public double HealthRegen { get; init; } = 0.4;
    public double HealthRegenThreshold { get; init; } = 30;

    public double LowO2Threshold { get; init; } = 25;
    public double LowO2Reset { get; init; } = 40;
    public double LowFoodThreshold { get; init; } = 25;
    public double LowFoodReset { get; init; } = 40;

    // Weather
    public double FirstStormDelay { get; init; } = 85;
    public double StormDuration { get; init; } = 22;
    public double NextStormMin { get; init; } = 70;
    public double NextStormSpread { get; init; } = 50;
    public double StormWarningTime { get; init; } = 10;

    // Consumables
    public int EatFoodGain { get; init; } = 30;
    public int EatEnergyGain { get; init; } = 10;
    public int O2RefillAmount { get; init; } = 25;
    public int MeltWaterGain { get; init; } = 2;
    public int HarvestPotatoes { get; init; } = 2;
    public double HarvestSeedChance { get; init; } = 0.5;

    // Lighting
    public double NightDarkness { get; init; } = 0.55;

    // Starting state
    public int StartIce { get; init; } = 2;
    public int StartWater { get; init; } = 2;
    public int StartSeeds { get; init; } = 4;
    public int StartPotatoes { get; init; } = 1;
    public int StartFijaciones { get; init; } = 2;
    public int StartO2 { get; init; } = 100;
    public int StartEnergy { get; init; } = 80;
    public int StartFood { get; init; } = 70;
    public int StartHealth { get; init; } = 100;
    public float StartX { get; init; } = 1550;
    public float StartY { get; init; } = 975;

    public static BalanceConfig Default { get; } = new();
}
