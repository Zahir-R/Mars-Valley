using System.Collections.Generic;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

public sealed class VitalsPanel
{
    private sealed record VitalView(string Key, string Name, Color Color, Label Value, MiniBar Bar, Label RowLabel);

    public Control Root { get; }

    private readonly GridContainer _grid;
    private readonly Label _suitState;
    private readonly PanelContainer _suitBadge;
    private readonly List<VitalView> _vitals = new();

    public VitalsPanel()
    {
        _suitState = UiTheme.Text(Loc.T("SELLADO"), 10, Palette.O2, Fonts.MonoWeight(600));
        _suitBadge = new PanelContainer();
        _suitBadge.AddThemeStyleboxOverride("panel", Badge(Palette.O2));
        _suitBadge.AddChild(_suitState);

        var header = UiTheme.HBox(8);
        var headerLabel = UiTheme.Caption(Loc.T("Traje EVA"));
        headerLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(headerLabel);
        header.AddChild(_suitBadge);

        _grid = new GridContainer { Columns = 1 };
        _grid.AddThemeConstantOverride("h_separation", 14);
        _grid.AddThemeConstantOverride("v_separation", 12);
        AddVital("o2", Loc.T("Oxígeno"), Palette.O2);
        AddVital("en", Loc.T("Energía"), Palette.Energy);
        AddVital("food", Loc.T("Alimento"), Palette.Food);
        AddVital("hp", Loc.T("Salud"), Palette.Health);

        var body = UiTheme.VBox(12);
        body.AddChild(header);
        body.AddChild(_grid);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.Card(14));
        panel.AddChild(body);
        Root = panel;
    }

    private void AddVital(string key, string name, Color color)
    {
        var value = UiTheme.Text("100%", 13, Palette.Suit, Fonts.MonoWeight(500));
        var row = UiTheme.HBox(0);
        var label = UiTheme.Label(name, 13, Palette.Suit);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(label);
        row.AddChild(value);

        var bar = new MiniBar { CustomMinimumSize = new Vector2(0, 10), FillColor = color };

        var item = UiTheme.VBox(5);
        item.AddChild(row);
        item.AddChild(bar);
        _grid.AddChild(item);

        _vitals.Add(new VitalView(key, name, color, value, bar, label));
    }

    public void SetColumns(int columns) => _grid.Columns = columns;

    public void Refresh(GameSession session)
    {
        var s = session.State;
        var v = s.Vitals;
        var blink = (Time.GetTicksMsec() / 450) % 2 == 0;

        foreach (var view in _vitals)
        {
            var raw = view.Key switch
            {
                "o2" => v.O2,
                "en" => v.Energy,
                "food" => v.Food,
                _ => v.Health,
            };
            var value = Mathf.RoundToInt((float)raw);
            view.Value.Text = value + "%";
            view.Bar.Value = value;
            view.Bar.QueueRedraw();

            var low = value < 25;
            view.Value.AddThemeColorOverride("font_color", low ? Palette.Health : Palette.Suit);
            view.Bar.Modulate = low && blink ? new Color(1, 1, 1, 0.45f) : Colors.White;
        }

        string text;
        Color color;
        if (v.O2 < 20) { text = Loc.T("O₂ CRÍTICO"); color = Palette.Health; }
        else if (s.Storm.Left > 0) { text = Loc.T("TORMENTA"); color = Palette.Energy; }
        else if (v.O2 < 40 || v.Food < 25) { text = Loc.T("ATENCIÓN"); color = Palette.Energy; }
        else { text = Loc.T("SELLADO"); color = Palette.O2; }

        _suitState.Text = text;
        _suitState.AddThemeColorOverride("font_color", color);
        _suitBadge.AddThemeStyleboxOverride("panel", Badge(color));
    }

    private static StyleBoxFlat Badge(Color color)
    {
        var box = UiTheme.Box(Colors.Transparent, color, 1);
        box.ContentMarginLeft = 6;
        box.ContentMarginRight = 6;
        box.ContentMarginTop = 4;
        box.ContentMarginBottom = 4;
        return box;
    }
}
