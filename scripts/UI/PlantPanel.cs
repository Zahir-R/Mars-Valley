using System;
using System.Collections.Generic;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// Crop picker opened when interacting with an empty plot. Each row shows the crop's
/// requirements and whether it would grow right now in this plot's environment.
/// </summary>
public sealed class PlantPanel
{
    public Control Root { get; }

    private readonly GameSession _session;
    private readonly VBoxContainer _rows;
    private readonly Label _title;
    private readonly Label _subtitle;
    private readonly System.Action _onClose;
    private readonly Dictionary<string, (Button Button, Label Status)> _rowControls = new();
    private bool _rowsBuilt;
    private Plot? _plot;

    public PlantPanel(GameSession session, System.Action onClose)
    {
        _session = session;
        _onClose = onClose;

        _title = UiTheme.Text(Loc.T("Plantar"), 16, Palette.Suit, Fonts.SansWeight(700));
        _subtitle = UiTheme.Label(Loc.T("Elige un cultivo para este hueco."), 12, Palette.Muted);

        var body = UiTheme.VBox(10);
        body.AddChild(_title);
        body.AddChild(_subtitle);
        body.AddChild(UiTheme.Caption(Loc.T("Cultivos")));
        _rows = UiTheme.VBox(6);
        body.AddChild(_rows);

        Root = new PanelContainer { CustomMinimumSize = new Vector2(460, 0) };
        ((PanelContainer)Root).AddThemeStyleboxOverride("panel", UiTheme.Card(16));
        Root.AddChild(body);
    }

    public void SetPlot(Plot? plot)
    {
        _plot = plot;
        _title.Text = plot?.Dome == null
            ? Loc.T("Plantar en bandeja abierta")
            : Loc.F("Plantar en Cúpula {0}", plot.Dome.Id);
        _subtitle.Text = plot?.Dome == null
            ? Loc.T("El clima marciano manda; no todo sobrevive ahí fuera.")
            : Loc.T("Interior estable; casi todo crece bien aquí.");
        Refresh();
    }

    public void Refresh()
    {
        if (_plot == null) return;
        if (!Root.IsVisibleInTree()) return;

        if (!_rowsBuilt)
        {
            foreach (var id in _session.Plants.Order)
            {
                var plant = _session.Plants[id];
                var (row, button, status) = BuildRow(plant);
                _rowControls[id] = (button, status);
                _rows.AddChild(row);
            }
            _rowsBuilt = true;
        }

        foreach (var (id, controls) in _rowControls)
        {
            var blocker = FarmingSystem.GrowthBlockerNow(_session, _plot, id);
            controls.Status.Text = blocker.Length > 0 ? Loc.T(blocker) : Loc.T("Crecerá aquí");
            controls.Status.AddThemeColorOverride("font_color", blocker.Length > 0 ? Palette.Health : Palette.Food);
            controls.Button.Disabled = _session.State.Inventory[_session.Plants[id].SeedId] < 1;
        }
    }

    private (Control, Button, Label) BuildRow(PlantDefinition plant)
    {
        var row = new PanelContainer();
        var pad = UiTheme.Card(10);
        row.AddThemeStyleboxOverride("panel", pad);

        var icon = new ColorRect { Color = Palette.FromHex(plant.Color), CustomMinimumSize = new Vector2(34, 34) };

        var name = UiTheme.Text(Loc.T(plant.Name), 13, Palette.Suit, Fonts.SansWeight(600));
        var detail = UiTheme.Label(
            Loc.F("{0} s · {1} agua · cosecha {2}",
                Mathf.RoundToInt((float)plant.GrowTime), plant.WaterPerGrowth, plant.HarvestYield),
            11, Palette.Muted);
        var status = UiTheme.Label(string.Empty, 11, Palette.Food);
        status.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var texts = UiTheme.VBox(2);
        texts.AddChild(name);
        texts.AddChild(detail);
        texts.AddChild(status);
        texts.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var button = UiTheme.PrimaryButton(Loc.T("Plantar"), 12);
        button.CustomMinimumSize = new Vector2(120, 38);
        button.Pressed += () => Plant(plant.Id);

        var content = UiTheme.HBox(10);
        content.AddChild(icon);
        content.AddChild(texts);
        content.AddChild(button);
        row.AddChild(content);
        return (row, button, status);
    }

    private void Plant(string plantId)
    {
        if (_plot == null) return;
        _session.Plant(_plot, plantId);
        _plot = null;
        _onClose();
        Refresh();
    }
}
