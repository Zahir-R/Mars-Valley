using System.Collections.Generic;
using Godot;
using NasaValley.Domain;

namespace NasaValley.Core;

/// <summary>Slot ids used by the save system: one autosave plus manual slots.</summary>
public static class SaveSlots
{
    public const string Autosave = "autosave";
    public const string Slot1 = "slot1";
    public const string Slot2 = "slot2";
    public const string Slot3 = "slot3";

    public static readonly string[] All = { Autosave, Slot1, Slot2, Slot3 };

    public static string NameOf(string id) => id switch
    {
        Autosave => Loc.T("Autoguardado"),
        Slot1 => Loc.T("Ranura 1"),
        Slot2 => Loc.T("Ranura 2"),
        Slot3 => Loc.T("Ranura 3"),
        _ => id,
    };
}

public sealed record SlotInfo(string Id, bool Exists, string? SavedAtUtc, double Time)
{
    public string Label => SaveSlots.NameOf(Id);
}

internal static class SavePaths
{
    public const string Folder = "user://saves";

    public static string For(string slotId) => $"{Folder}/{slotId}.json";
}

/// <summary>Read/write access to the save slots, kept out of the domain library.</summary>
public static class SaveManager
{
    public static bool HasSave(string slotId) => FileAccess.FileExists(SavePaths.For(slotId));

    public static bool HasAutosave => HasSave(SaveSlots.Autosave);

    /// <summary>True when saving is allowed (a run is in progress and can be resumed).</summary>
    public static bool CanSave(GameSession session)
    {
        var flow = session.State.Flow;
        return flow == FlowState.Playing || flow == FlowState.Paused;
    }

    public static bool SaveTo(GameSession session, string slotId)
    {
        if (!CanSave(session)) return false;

        var data = session.CaptureSave();
        var json = SaveSerializer.Serialize(data);

        if (!EnsureFolder()) return false;

        var final = SavePaths.For(slotId);
        var tmp = final + ".tmp";
        var tmpFileName = System.IO.Path.GetFileName(tmp);
        var finalFileName = System.IO.Path.GetFileName(final);

        using (var file = FileAccess.Open(tmp, FileAccess.ModeFlags.Write))
        {
            if (file is null) return false;
            file.StoreString(json);
        }

        var dir = DirAccess.Open(SavePaths.Folder);
        if (dir is null) return false;

        return dir.Rename(tmpFileName, finalFileName) == Error.Ok;
    }

    public static bool TryLoad(GameSession session, string slotId, out string error)
    {
        error = string.Empty;

        if (!HasSave(slotId))
        {
            error = Loc.T("No hay una partida guardada en esa ranura.");
            return false;
        }

        string json;
        using (var file = FileAccess.Open(SavePaths.For(slotId), FileAccess.ModeFlags.Read))
        {
            if (file is null)
            {
                error = Loc.T("No se pudo abrir el guardado.");
                return false;
            }
            json = file.GetAsText();
        }

        var data = SaveSerializer.Deserialize(json);
        if (data is null)
        {
            error = Loc.T("El guardado está vacío, dañado o es de una versión incompatible.");
            return false;
        }

        session.ApplySave(data);
        return true;
    }

    public static void Delete(string slotId)
    {
        var path = ProjectSettings.GlobalizePath(SavePaths.For(slotId));
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }

    public static List<SlotInfo> ListSlots()
    {
        var slots = new List<SlotInfo>();
        foreach (var id in SaveSlots.All)
        {
            if (!HasSave(id))
            {
                slots.Add(new SlotInfo(id, false, null, 0));
                continue;
            }

            string? savedAt = null;
            double time = 0;
            using (var file = FileAccess.Open(SavePaths.For(id), FileAccess.ModeFlags.Read))
            {
                if (file is not null)
                {
                    var data = SaveSerializer.Deserialize(file.GetAsText());
                    if (data is not null) { savedAt = data.SavedAtUtc; time = data.Time; }
                }
            }

            slots.Add(new SlotInfo(id, true, savedAt, time));
        }
        return slots;
    }

    public static string DescribeSlot(SlotInfo info)
    {
        if (!info.Exists) return Loc.T("Vacía");

        string when;
        if (!string.IsNullOrWhiteSpace(info.SavedAtUtc) &&
            System.DateTime.TryParse(info.SavedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var utc))
        {
            when = utc.ToLocalTime().ToString("dd/MM · HH:mm");
        }
        else
        {
            when = Loc.T("guardada");
        }

        var day = GameClock.DayNumber(info.Time, BalanceConfig.Default.SolLength);
        return $"{when} · {Loc.T("Sol")} {day}";
    }

    private static bool EnsureFolder()
    {
        var dir = DirAccess.Open("user://");
        if (dir is null) return false;
        if (!dir.DirExists("saves")) dir.MakeDir("saves");
        return dir.DirExists("saves");
    }
}
