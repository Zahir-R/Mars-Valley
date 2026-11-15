using System.Text.Json;
using System.Text.Json.Serialization;

namespace NasaValley.Domain;

public sealed class SaveGameData
{
    public int Version { get; set; } = SaveSerializer.CurrentVersion;
    public string SavedAtUtc { get; set; } = string.Empty;

    public double Time { get; set; }
    public float Px { get; set; }
    public float Py { get; set; }
    public int Face { get; set; }

    public VitalsData Vitals { get; set; } = new();
    public List<ItemStackData> Inventory { get; set; } = new();
    public string? FoodSource { get; set; }
    public string SelectedCrop { get; set; } = "lechuga";

    public StormData Storm { get; set; } = new();
    public StatsData Stats { get; set; } = new();
    public List<string> RepairedParts { get; set; } = new();
    public List<NodeData> Nodes { get; set; } = new();
    public List<PickupData> Pickups { get; set; } = new();
    public List<DomeData> Domes { get; set; } = new();
    public List<FieldData> Fields { get; set; } = new();
    public double RestCooldown { get; set; }

    /// <summary>O2 refills the ship has left; old saves default to the full amount.</summary>
    public int ShipO2Charges { get; set; } = 5;

    /// <summary>Stranded-survivor quest state; null on saves made before the quest existed.</summary>
    public NpcSaveData? Npc { get; set; }
}

public sealed class NpcSaveData
{
    public string Phase { get; set; } = "None";
    public bool InterferenceActive { get; set; }
    public double InterferenceTimer { get; set; }
    public List<string>? Letters { get; set; } = new();
    public List<string>? Unread { get; set; } = new();
    public string? ActiveLetterId { get; set; }
    public float CacheX { get; set; }
    public float CacheY { get; set; }
    public string CacheItemId { get; set; } = "agua";
    public int CacheAmount { get; set; } = 3;
    public bool CacheFilled { get; set; }
    public float SurvivorX { get; set; }
    public float SurvivorY { get; set; }
    public bool SiteUnlocked { get; set; }
    public bool SurvivorFound { get; set; }
    public bool SiteSkipped { get; set; }
    public bool RewardGranted { get; set; }
    public bool PendingFinalChoice { get; set; }
    public string Outcome { get; set; } = "None";
}

public sealed class VitalsData
{
    public double O2 { get; set; }
    public double Energy { get; set; }
    public double Food { get; set; }
    public double Health { get; set; }
}

public sealed class ItemStackData
{
    public string Id { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class StormData
{
    public double Next { get; set; }
    public double Left { get; set; }
}

public sealed class StatsData
{
    public int Ice { get; set; }
    public int Planted { get; set; }
    public int Harvested { get; set; }
}

public sealed class NodeData
{
    public string Type { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public int Amount { get; set; }
    public int Progress { get; set; }
    public double Regen { get; set; }
}

public sealed class PickupData
{
    public string ItemId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public bool Taken { get; set; }
}

public sealed class FixtureData
{
    public string Kind { get; set; } = string.Empty;
    public int SlotIndex { get; set; }
}

public sealed class PlotData
{
    public int SlotIndex { get; set; }
    public string PlantId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public double Time { get; set; }
}

public sealed class FieldData
{
    public float X { get; set; }
    public float Y { get; set; }
    public string PlantId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public double Time { get; set; }
}

public sealed class DomeData
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public int Level { get; set; }
    public double Energy { get; set; }
    public double InternalTempC { get; set; }
    public double ExternalTempC { get; set; }
    public List<FixtureData> Fixtures { get; set; } = new();
    public List<PlotData> Plots { get; set; } = new();
}

/// <summary>JSON (de)serialization for saves; no engine calls, fully testable.</summary>
public static class SaveSerializer
{
    public const int CurrentVersion = 3;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(SaveGameData data) => JsonSerializer.Serialize(data, Options);

    public static SaveGameData? Deserialize(string json)
    {
        try
        {
            var data = JsonSerializer.Deserialize<SaveGameData>(json, Options);
            if (data == null) return null;
            if (data.Version == CurrentVersion) return data;
            if (data.Version is 1 or 2)
            {
                // Migrate older saves; the survivor quest block is created by ApplySave.
                data.Version = CurrentVersion;
                data.Fields ??= new List<FieldData>();
                return data;
            }
            return null;
        }
        catch (System.Exception)
        {
            return null;
        }
    }
}
