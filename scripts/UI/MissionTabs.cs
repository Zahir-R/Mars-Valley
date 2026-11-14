using System;
using System.Collections.Generic;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>The right-hand mission column: Nave / Granja / Misiones.</summary>
public sealed class MissionTabs
{
    public Control Root { get; }

    private readonly GameSession _session;
    private readonly Dictionary<string, Button> _tabs = new();
    private readonly Dictionary<string, Control> _panels = new();
    private readonly ShipPanel _ship;
    private readonly FarmPanel _farm;
    private readonly GoalsPanel _goals;
    private string _active = "nave";

    public MissionTabs(GameSession session)
    {
        _session = session;

        var tabs = UiTheme.HBox(0);
        tabs.AddThemeConstantOverride("separation", 0);
        foreach (var (id, label) in new[] { ("nave", "Nave"), ("granja", "Granja"), ("misiones", "Misiones") })
        {
            var button = new Button { Text = label };
            button.AddThemeFontOverride("font", Fonts.Display);
            button.AddThemeFontSizeOverride("font_size", 11);
            button.Pressed += () => ShowTab(id);
            _tabs[id] = button;
            tabs.AddChild(button);
        }

        _ship = new ShipPanel(session);
        _farm = new FarmPanel(session);
        _goals = new GoalsPanel(session);
        _panels["nave"] = _ship.Root;
        _panels["granja"] = _farm.Root;
        _panels["misiones"] = _goals.Root;

        var box = UiTheme.VBox(14);
        box.AddChild(WrapTabs(tabs));
        foreach (var panel in _panels.Values) box.AddChild(panel);

        Root = box;
        ShowTab("nave");
    }

    private static PanelContainer WrapTabs(HBoxContainer tabs)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.Box(Palette.Panel, Palette.Line, 1));
        panel.AddChild(tabs);
        return panel;
    }

    public void ShowTab(string id)
    {
        if (!_panels.ContainsKey(id)) return;
        _active = id;

        foreach (var (key, panel) in _panels) panel.Visible = key == id;
        foreach (var (key, tab) in _tabs)
        {
            var selected = key == id;
            var normal = selected ? UiTheme.Box(Palette.Dust) : UiTheme.Box(new Color(0, 0, 0, 0));
            UiTheme.Pad(normal, 8, 10);
            var hover = UiTheme.Box(selected ? Palette.Dust : Palette.Panel2);
            UiTheme.Pad(hover, 8, 10);
            tab.AddThemeStyleboxOverride("normal", normal);
            tab.AddThemeStyleboxOverride("hover", hover);
            tab.AddThemeStyleboxOverride("pressed", normal);
            tab.AddThemeColorOverride("font_color", selected ? Palette.Ink : Palette.Muted);
            tab.AddThemeColorOverride("font_hover_color", selected ? Palette.Ink : Palette.Suit);
        }
    }

    public void Refresh()
    {
        _ship.Refresh();
        _farm.Refresh();
        _goals.Refresh();
    }
}

public sealed class ShipPanel
{
    public Control Root { get; }

    private readonly GameSession _session;
    private readonly Label _percent;
    private readonly MiniBar _bar;
    private readonly Label _hint;
    private readonly Button _refill;
    private readonly Button _melt;
    private readonly List<PartRow> _parts = new();

    public ShipPanel(GameSession session)
    {
        _session = session;

        _percent = UiTheme.Text("0%", 13, Palette.Suit, Fonts.MonoWeight(500));
        var percentRow = UiTheme.HBox(0);
        var percentLabel = UiTheme.Label("Reparación de la nave", 13, Palette.Suit);
        percentLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        percentRow.AddChild(percentLabel);
        percentRow.AddChild(_percent);

        _bar = new MiniBar { CustomMinimumSize = new Vector2(0, 10), FillColor = Palette.Dust };
        _hint = UiTheme.Label("Camina hasta la nave para reparar y recargar.", 12, Palette.Muted);
        _hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var overall = UiTheme.VBox(5);
        overall.AddChild(percentRow);
        overall.AddChild(_bar);
        overall.AddChild(_hint);

        var parts = UiTheme.VBox(8);
        foreach (var part in session.Parts.Parts)
        {
            var row = new PartRow(part, session);
            row.Button.Pressed += () => _session.Repair(part.Id);
            _parts.Add(row);
            parts.AddChild(row.Root);
        }

        _refill = UiTheme.SupportButton("Recargar O₂", "1 hielo → +25 %");
        _melt = UiTheme.SupportButton("Fundir hielo", "1 hielo → 2 agua");
        _refill.Pressed += () => _session.RefillO2();
        _melt.Pressed += () => _session.MeltIce();

        var supportButtons = UiTheme.HBox(8);
        supportButtons.AddChild(Support(_refill, "1 hielo → +25 %"));
        supportButtons.AddChild(Support(_melt, "1 hielo → 2 agua"));

        var support = UiTheme.VBox(8);
        support.AddChild(UiTheme.Caption("Soporte vital"));
        support.AddChild(supportButtons);

        var body = UiTheme.VBox(12);
        body.AddChild(overall);
        body.AddChild(parts);
        body.AddChild(support);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.Card(14));
        panel.AddChild(body);
        Root = panel;
    }

    private static Control Support(Button button, string subtitle)
    {
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var box = UiTheme.VBox(2);
        box.AddChild(button);
        box.AddChild(UiTheme.Label(subtitle, 11, Palette.Muted));
        return box;
    }

    public void Refresh()
    {
        var s = _session.State;
        var count = RepairSystem.Count(s);
        var total = _session.Parts.Parts.Count;
        var percent = Mathf.RoundToInt((float)count / total * 100);
        _percent.Text = percent + "%";
        _bar.Value = percent;
        _bar.QueueRedraw();

        var near = InteractionSystem.NearShip(s, _session.Balance);
        _hint.Text = near
            ? "Estás junto a la nave: puedes reparar y recargar."
            : "Camina hasta la nave para reparar y recargar.";
        _hint.AddThemeColorOverride("font_color", near ? Palette.O2 : Palette.Muted);

        foreach (var row in _parts) row.Refresh(_session);

        _refill.Disabled = !near || s.Inventory["hielo"] < 1 || s.Vitals.O2 >= 100;
        _melt.Disabled = !near || s.Inventory["hielo"] < 1;
    }
}

public sealed class FarmPanel
{
    public Control Root { get; }

    private readonly GameSession _session;
    private readonly Label _seeds;
    private readonly Label _potatoes;
    private readonly Label _water;
    private readonly List<PlotCard> _plots = new();

    public FarmPanel(GameSession session)
    {
        _session = session;

        _seeds = UiTheme.Text("0", 13, Palette.Suit, Fonts.Mono);
        _potatoes = UiTheme.Text("0", 13, Palette.Suit, Fonts.Mono);
        _water = UiTheme.Text("0", 13, Palette.Suit, Fonts.Mono);

        var seedline = UiTheme.HBox(16);
        seedline.AddChild(Stat("Semillas", _seeds));
        seedline.AddChild(Stat("Papas", _potatoes));
        seedline.AddChild(Stat("Agua", _water));

        var grid = new GridContainer { Columns = 4 };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        for (var i = 0; i < session.State.Plots.Count; i++)
        {
            var card = new PlotCard(i);
            _plots.Add(card);
            grid.AddChild(card.Root);
        }

        var body = UiTheme.VBox(12);
        body.AddChild(UiTheme.Caption("Invernadero"));
        body.AddChild(seedline);
        body.AddChild(grid);
        body.AddChild(UiTheme.Label(
            "Cada papa necesita 1 de agua y unos 25 s para madurar. Una cosecha da 2 papas y, a veces, una semilla nueva. Funde hielo en la nave para conseguir agua.",
            12, Palette.Muted));
        ((Label)body.GetChild(3)).AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.Card(14));
        panel.AddChild(body);
        Root = panel;
    }

    private static Control Stat(string name, Label value)
    {
        var row = UiTheme.HBox(6);
        row.AddChild(UiTheme.Label(name, 13, Palette.Muted));
        row.AddChild(value);
        return row;
    }

    public void Refresh()
    {
        var s = _session.State;
        _seeds.Text = s.Inventory["semillas"].ToString();
        _potatoes.Text = s.Inventory["papas"].ToString();
        _water.Text = s.Inventory["agua"].ToString();
        for (var i = 0; i < _plots.Count; i++) _plots[i].Refresh(s.Plots[i], _session.Balance);
    }
}

public sealed class GoalsPanel
{
    public Control Root { get; }

    private readonly GameSession _session;
    private readonly List<GoalRow> _rows = new();

    public GoalsPanel(GameSession session)
    {
        _session = session;

        var list = UiTheme.VBox(8);
        foreach (var goal in session.Missions.Goals)
        {
            var row = new GoalRow(goal.Text);
            _rows.Add(row);
            list.AddChild(row.Root);
        }

        var body = UiTheme.VBox(12);
        body.AddChild(UiTheme.Caption("Objetivos"));
        body.AddChild(list);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.Card(14));
        panel.AddChild(body);
        Root = panel;
    }

    public void Refresh()
    {
        var firstOpen = -1;
        for (var i = 0; i < _rows.Count; i++)
        {
            var progress = MissionSystem.Evaluate(_session.State, _session.Missions.Goals[i], _session.Parts);
            if (!progress.Done && firstOpen < 0) firstOpen = i;
        }

        for (var i = 0; i < _rows.Count; i++)
        {
            var progress = MissionSystem.Evaluate(_session.State, _session.Missions.Goals[i], _session.Parts);
            _rows[i].Refresh(progress, i == firstOpen && !progress.Done);
        }
    }
}
