using System;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// Left-edge drawer toggled with TAB (or the edge icon). It slides in the vitals,
/// suit status and the currently selected food source.
/// </summary>
public sealed class StatsDrawer
{
    public Control Root { get; }
    public Button Icon { get; }
    public bool IsOpen { get; private set; }

    private readonly GameSession _session;
    private readonly VitalsPanel _vitals = new();
    private readonly IconSwatch _foodIcon;
    private readonly Label _foodName;
    private readonly Label _foodCount;
    private readonly Label _energy;
    private float _slide;

    public float Slide => _slide;

    public StatsDrawer(GameSession session, Action onToggle)
    {
        _session = session;
        _vitals.SetColumns(1);

        _foodIcon = new IconSwatch(44);
        _foodName = UiTheme.Text("Sin fuente", 13, Palette.Suit, Fonts.SansWeight(600));
        _foodCount = UiTheme.Label(string.Empty, 11, Palette.Muted);

        var foodText = UiTheme.VBox(2);
        foodText.AddChild(UiTheme.Label("Fuente de comida", 11, Palette.Muted));
        foodText.AddChild(_foodName);
        foodText.AddChild(_foodCount);

        var foodRow = UiTheme.HBox(10);
        foodRow.AddChild(_foodIcon);
        foodRow.AddChild(foodText);

        var foodCard = UiTheme.CardPanel(10);
        foodCard.AddChild(foodRow);

        _energy = UiTheme.Label("Sin cúpulas", 12, Palette.Muted);

        var energyCard = UiTheme.CardPanel(10);
        var energyBox = UiTheme.VBox(2);
        energyBox.AddChild(UiTheme.Label("Energía de la base", 11, Palette.Muted));
        energyBox.AddChild(_energy);
        energyCard.AddChild(energyBox);

        var body = UiTheme.VBox(10);
        body.AddChild(_vitals.Root);
        body.AddChild(foodCard);
        body.AddChild(energyCard);
        Root = body;

        Icon = UiTheme.GhostButton("III", 12);
        Icon.CustomMinimumSize = new Vector2(34, 62);
        Icon.TooltipText = "Estadísticas (Tab)";
        Icon.Pressed += onToggle;
    }

    public void Toggle() => IsOpen = !IsOpen;

    public void Tick(double delta)
    {
        var target = IsOpen ? 1f : 0f;
        _slide = Mathf.MoveToward(_slide, target, (float)delta * 6f);
        Root.Visible = _slide > 0.001f;

        // The edge icon is only useful while the drawer is closed.
        Icon.Visible = !IsOpen;
    }

    public void Refresh()
    {
        _vitals.Refresh(_session);

        var energy = 0.0;
        var capacity = 0.0;
        var panels = 0;
        foreach (var dome in _session.State.Domes)
        {
            energy += dome.Energy;
            capacity += dome.EnergyCapacity(_session.Balance);
            panels += dome.PanelCount;
        }
        if (_session.State.Domes.Count == 0)
        {
            _energy.Text = "Sin cúpulas";
            _energy.AddThemeColorOverride("font_color", Palette.Muted);
        }
        else
        {
            _energy.Text = $"{energy:0}/{capacity:0} · paneles {panels}";
            _energy.AddThemeColorOverride("font_color", capacity > 0 && energy <= 0.5 ? Palette.Health : Palette.Muted);
        }

        var src = _session.State.FoodSource;
        if (src == null || !_session.Items.TryGet(src, out var def))
        {
            _foodIcon.Set(Palette.Panel2, "—");
            _foodName.Text = "Sin fuente";
            _foodCount.Text = string.Empty;
            return;
        }

        var count = _session.State.Inventory[src];
        _foodIcon.Set(Palette.FromHex(def.Color), Initials(def.Name));
        _foodName.Text = def.Name;
        _foodCount.Text = count > 0 ? $"x{count}" : "sin existencias";
        _foodCount.AddThemeColorOverride("font_color", count > 0 ? Palette.Muted : Palette.Health);
    }

    private static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return (parts[0][0].ToString() + parts[1][0]).ToUpperInvariant();
    }
}
