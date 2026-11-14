namespace NasaValley.Domain;

/// <summary>
/// Single source of truth for every tunable value in the game. Keeping these in
/// one place (instead of scattered magic numbers) is what lets designers retune
/// the game without touching simulation code.
/// </summary>
public sealed class BalanceConfig
{
    // World / grid
    public int TileSize { get; init; } = 32;
    public int GridWidth { get; init; } = 24;
    public int GridHeight { get; init; } = 14;
    public int ViewWidth => GridWidth * TileSize;   // 768
    public int ViewHeight => GridHeight * TileSize; // 448
    public int PlotSize { get; init; } = 34;
    public int PlotColumns { get; init; } = 2;
    public int PlotRows { get; init; } = 2;

    // Domes (player-built greenhouses with plot slots)
    public int DomeWidth { get; init; } = 140;
    public int DomeHeight { get; init; } = 104;
    public int DomeSlotColumns { get; init; } = 2;
    public int DomeSlotRows { get; init; } = 2;
    public float DomeSlotGap { get; init; } = 10f;
    public float DomeSlotMarginX { get; init; } = 18f;
    public float DomeSlotMarginY { get; init; } = 18f;

    public double DomeStartEnergy { get; init; } = 40;
    public double DomeMaxEnergy { get; init; } = 100;
    public double DomeTargetTempC { get; init; } = 22;
    public double DomeSolarRate { get; init; } = 2.2;
    public double DomeEnergyPerDegreePerSec { get; init; } = 0.02;

    public IReadOnlyDictionary<string, int> DomeCost { get; init; } = new Dictionary<string, int>
    {
        ["hierro"] = 4,
        ["silicio"] = 4,
        ["roca"] = 6,
    };

    // Environment / weather coupling
    public double StormLightFactor { get; init; } = 0.45;
    public double DayNightSwing { get; init; } = 12;
    public double StormTempDrop { get; init; } = 6;

    // Ship / greenhouse rectangles (logical world units)
    public float ShipX { get; init; } = 36;
    public float ShipY { get; init; } = 30;
    public float ShipWidth { get; init; } = 170;
    public float ShipHeight { get; init; } = 118;

    public float FarmX { get; init; } = 26;
    public float FarmY { get; init; } = 314;
    public float FarmWidth { get; init; } = 198;
    public float FarmHeight { get; init; } = 108;

    // Astronaut
    public float NormalSpeed { get; init; } = 100f;
    public float TiredSpeed { get; init; } = 62f;
    public float TiredEnergy { get; init; } = 15f;
    public float MoveMinX { get; init; } = 14f;
    public float MoveMaxX => ViewWidth - 14f;
    public float MoveMinY { get; init; } = 24f;
    public float MoveMaxY => ViewHeight - 18f;
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

    // Base stations (world positions). The ship itself is the ship upgrades station.
    public float WorkbenchX { get; init; } = 240f;
    public float WorkbenchY { get; init; } = 250f;
    public float FarmConsoleX { get; init; } = 250f;
    public float FarmConsoleY { get; init; } = 320f;

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
    public float StartX { get; init; } = 250;
    public float StartY { get; init; } = 215;

    public static BalanceConfig Default { get; } = new();
}
