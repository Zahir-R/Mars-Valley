using System;
using System.Collections.Generic;
using Godot;
using NasaValley.Core;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// Renders the save slots with live metadata. In load mode each row offers Load and Delete;
/// in save mode each row offers Save and Delete. Destructive clicks need a second press to confirm.
/// </summary>
public sealed class SlotListView
{
    public Control Root { get; }

    /// <summary>Raised when the user confirms the slot action (load in load mode, save in save mode).</summary>
    public event Action<string>? ActionConfirmed;

    /// <summary>Raised when the user confirms deleting a slot.</summary>
    public event Action<string>? DeleteConfirmed;

    private readonly bool _saveMode;
    private readonly VBoxContainer _rows;
    private readonly Dictionary<string, Button> _actionButtons = new();
    private readonly Dictionary<string, Button> _deleteButtons = new();
    private readonly Dictionary<string, Label> _subLabels = new();
    private readonly Dictionary<string, bool> _saveArmed = new();
    private readonly Dictionary<string, bool> _deleteArmed = new();

    public SlotListView(bool saveMode)
    {
        _saveMode = saveMode;
        _rows = UiTheme.VBox(6);

        foreach (var id in SaveSlots.All) BuildRow(id);

        Root = new PanelContainer();
        ((PanelContainer)Root).AddThemeStyleboxOverride("panel", UiTheme.Card(12));
        Root.AddChild(_rows);
    }

    public void Rebuild()
    {
        var slots = SaveManager.ListSlots();
        foreach (var info in slots)
        {
            _saveArmed[info.Id] = false;
            _deleteArmed[info.Id] = false;
            _subLabels[info.Id].Text = SaveManager.DescribeSlot(info);

            var action = _actionButtons[info.Id];
            action.Disabled = !_saveMode && !info.Exists;
            action.Text = _saveMode
                ? (info.Exists ? Loc.T("Sobrescribir") : Loc.T("Guardar"))
                : Loc.T("Cargar");

            _deleteButtons[info.Id].Text = Loc.T("Borrar");
        }
    }

    private void BuildRow(string id)
    {
        var name = UiTheme.Text(SaveSlots.NameOf(id), 14, Palette.Suit, Fonts.SansWeight(700));
        var sub = UiTheme.Label(Loc.T("Vacía"), 11, Palette.Muted);

        var texts = UiTheme.VBox(2);
        texts.AddChild(name);
        texts.AddChild(sub);
        texts.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var action = UiTheme.PrimaryButton(_saveMode ? Loc.T("Guardar") : Loc.T("Cargar"), 12);
        action.CustomMinimumSize = new Vector2(150, 0);
        action.Pressed += () => TriggerAction(id);

        var delete = UiTheme.GhostButton(Loc.T("Borrar"), 12);
        delete.CustomMinimumSize = new Vector2(130, 0);
        delete.Visible = id != SaveSlots.Autosave;
        delete.Pressed += () => TriggerDelete(id);

        var row = UiTheme.HBox(8);
        row.AddChild(texts);
        row.AddChild(action);
        row.AddChild(delete);

        _rows.AddChild(row);

        _actionButtons[id] = action;
        _deleteButtons[id] = delete;
        _subLabels[id] = sub;
    }

    private void TriggerAction(string id)
    {
        if (_saveMode && SaveManager.HasSave(id) && !_saveArmed.GetValueOrDefault(id))
        {
            _saveArmed[id] = true;
            _actionButtons[id].Text = Loc.T("Confirmar");
            _deleteArmed[id] = false;
            _deleteButtons[id].Text = Loc.T("Borrar");
            return;
        }

        _saveArmed[id] = false;
        _actionButtons[id].Text = _saveMode ? (SaveManager.HasSave(id) ? Loc.T("Sobrescribir") : Loc.T("Guardar")) : Loc.T("Cargar");
        ActionConfirmed?.Invoke(id);
    }

    private void TriggerDelete(string id)
    {
        if (!_deleteArmed.GetValueOrDefault(id))
        {
            _deleteArmed[id] = true;
            _deleteButtons[id].Text = Loc.T("¿Borrar?");
            _saveArmed[id] = false;
            _actionButtons[id].Text = _saveMode ? (SaveManager.HasSave(id) ? Loc.T("Sobrescribir") : Loc.T("Guardar")) : Loc.T("Cargar");
            return;
        }

        _deleteArmed[id] = false;
        _deleteButtons[id].Text = Loc.T("Borrar");
        DeleteConfirmed?.Invoke(id);
    }
}
