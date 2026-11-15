using System;
using System.Text;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// Management terminal for a built dome: place interior fixtures (farms, solar panels,
/// batteries, bunks), build another dome, upgrade, move, store or dismantle it.
/// </summary>
public sealed class DomePanel
{
    public Control Root { get; }

    private readonly GameSession _session;
    private readonly Action? _onClose;
    private readonly Label _title;
    private readonly Label _stats;
    private readonly Label _detail;
    private readonly Label _interior;
    private readonly Label _cost;
    private readonly Button _upgrade;
    private readonly Button _newDome;
    private readonly Button _move;
    private readonly Button _store;
    private readonly Button _destroy;
    private readonly Button _farm;
    private readonly Button _solar;
    private readonly Button _battery;
    private readonly VBoxContainer _batteryList = UiTheme.VBox(4);
    private readonly System.Collections.Generic.List<(MiniBar Bar, Label Label)> _batteryBars = new();
    private int _batteryBuilt = -1;
    private int _confirmDomeId = -1;

    public DomePanel(GameSession session, Action? onClose)
    {
        _session = session;
        _onClose = onClose;

        _title = UiTheme.Text(Loc.T("Cúpula"), 16, Palette.Suit, Fonts.SansWeight(700));
        _stats = UiTheme.Label(string.Empty, 12, Palette.Muted);
        _stats.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detail = UiTheme.Label(string.Empty, 12, Palette.Muted);
        _detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _interior = UiTheme.Label(string.Empty, 12, Palette.Muted);
        _interior.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _cost = UiTheme.Label(string.Empty, 12, Palette.Muted);

        _upgrade = UiTheme.PrimaryButton(Loc.T("Actualizar"), 13);
        _upgrade.Pressed += Upgrade;

        _newDome = UiTheme.GhostButton(Loc.T("Construir cúpula"), 12);
        _newDome.TooltipText = Loc.T("Coloca otra cúpula; no requiere semillas ni cultivos.");
        _newDome.Pressed += BuildAnother;

        _move = UiTheme.GhostButton(Loc.T("Mover"), 12);
        _move.Pressed += Move;

        _store = UiTheme.GhostButton(Loc.T("Guardar"), 12);
        _store.TooltipText = Loc.T("Se guarda en el inventario; lo que haya dentro se pierde.");
        _store.Pressed += Store;

        _destroy = UiTheme.GhostButton(Loc.T("Destruir"), 12);
        _destroy.Pressed += Destroy;

        _farm = FixtureButton(Loc.T("Parcela"), FixtureKind.Farm);
        _solar = FixtureButton(Loc.T("Panel solar"), FixtureKind.SolarPanel);
        _battery = FixtureButton(Loc.T("Batería"), FixtureKind.Battery);

        var management = new HFlowContainer();
        management.AddThemeConstantOverride("h_separation", 6);
        management.AddThemeConstantOverride("v_separation", 6);
        management.AddChild(_upgrade);
        management.AddChild(_newDome);
        management.AddChild(_move);
        management.AddChild(_store);
        management.AddChild(_destroy);

        var fixtures = new HFlowContainer();
        fixtures.AddThemeConstantOverride("h_separation", 6);
        fixtures.AddThemeConstantOverride("v_separation", 6);
        fixtures.AddChild(_farm);
        fixtures.AddChild(_solar);
        fixtures.AddChild(_battery);

        var hint = UiTheme.Label(
            Loc.T("Elige una instalación y colócala en un hueco libre con E. La batería recarga tu traje; los paneles se pueden retraer para protegerlos de las tormentas."),
            11, Palette.Muted);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var body = UiTheme.VBox(8);
        body.AddChild(UiTheme.Caption(Loc.T("Terminal de cúpula")));
        body.AddChild(_title);
        body.AddChild(_stats);
        body.AddChild(_detail);
        body.AddChild(_interior);
        body.AddChild(UiTheme.Label(Loc.T("Baterías"), 12, Palette.Muted));
        body.AddChild(_batteryList);
        body.AddChild(_cost);
        body.AddChild(UiTheme.Label(Loc.T("Construir en el interior"), 12, Palette.Muted));
        body.AddChild(fixtures);
        body.AddChild(UiTheme.Label(Loc.T("Gestión"), 12, Palette.Muted));
        body.AddChild(management);
        body.AddChild(hint);

        var card = UiTheme.CardPanel(14);
        card.AddChild(body);
        Root = card;
    }

    private Button FixtureButton(string text, FixtureKind kind)
    {
        var button = UiTheme.GhostButton(text, 12);
        button.TooltipText = FormatCost(BuildSystem.FixtureCost(_session.Balance, kind));
        button.Pressed += () => SelectFixture(kind);
        return button;
    }

    public void Refresh()
    {
        if (!Root.IsVisibleInTree())
        {
            _confirmDomeId = -1;
            return;
        }

        var dome = _session.ActiveDome;
        if (dome == null)
        {
            _title.Text = Loc.T("Sin cúpula seleccionada");
            _stats.Text = Loc.T("Acércate al terminal de una cúpula y pulsa E.");
            _detail.Text = string.Empty;
            _interior.Text = string.Empty;
            _cost.Text = string.Empty;
            SetEnabled(false);
            return;
        }

        if (_confirmDomeId != -1 && _confirmDomeId != dome.Id) _confirmDomeId = -1;

        var b = _session.Balance;
        var biome = GeneratedBiomes.AtWorld(dome.CenterX, dome.CenterY);
        var light = EnvironmentSystem.Light(_session.State, b);
        var lightPct = Mathf.RoundToInt((float)light * 100);
        var generation = dome.DeployedPanelCount * b.SolarPanelOutput * light * biome.SolarFactor;
        var slots = DomeLayout.SlotCount(b);
        var free = slots - dome.Plots.Count - dome.Fixtures.Count;

        _title.Text = Loc.F("Cúpula {0} · nivel {1} · {2}", dome.Id, dome.Level, biome.Name);
        _stats.Text = Loc.F("Interior {0}°C · Exterior {1}°C · Luz {2}%",
            Mathf.RoundToInt((float)dome.InternalTempC), Mathf.RoundToInt((float)dome.ExternalTempC), lightPct);
        _detail.Text = Loc.F("Energía {0}/{1} · Generación +{2}/s · Huecos libres {3}",
            Mathf.RoundToInt((float)dome.Energy), Mathf.RoundToInt((float)dome.EnergyCapacity(b)), generation, free);
        _interior.Text = Loc.F("Parcelas {0} · Paneles {1} ({2} desplegados) · Baterías {3}",
            dome.Plots.Count, dome.PanelCount, dome.DeployedPanelCount, dome.BatteryCount);
        RefreshBatteries(dome);

        SetEnabled(true);

        if (dome.Level >= b.DomeMaxLevel)
        {
            _cost.Text = Loc.T("Nivel máximo alcanzado.");
            _upgrade.Disabled = true;
        }
        else
        {
            _cost.Text = Loc.T("Mejora: ") + FormatCost(_session.UpgradeCostFor(dome));
            _upgrade.Disabled = !_session.CanUpgradeDome(dome, out _);
        }

        var canPlace = free > 0;
        _farm.Disabled = !canPlace;
        _solar.Disabled = !canPlace;
        _battery.Disabled = !canPlace;
        _destroy.Text = _confirmDomeId == dome.Id ? Loc.T("Confirmar destrucción") : Loc.T("Destruir");
    }

    private void SetEnabled(bool enabled)
    {
        _upgrade.Disabled = !enabled;
        _newDome.Disabled = !enabled;
        _move.Disabled = !enabled;
        _store.Disabled = !enabled;
        _destroy.Disabled = !enabled;
        _farm.Disabled = !enabled;
        _solar.Disabled = !enabled;
        _battery.Disabled = !enabled;
    }

    private void RefreshBatteries(Dome dome)
    {
        var count = 0;
        foreach (var fixture in dome.Fixtures)
        {
            if (fixture.Kind == FixtureKind.Battery) count++;
        }

        if (count != _batteryBuilt)
        {
            foreach (var child in _batteryList.GetChildren())
            {
                _batteryList.RemoveChild(child);
                child.QueueFree();
            }
            _batteryBars.Clear();

            for (var i = 0; i < count; i++)
            {
                var label = UiTheme.Label(Loc.F("Batería {0}", i + 1), 11, Palette.Muted);
                var bar = new MiniBar { CustomMinimumSize = new Vector2(140, 10), FillColor = Palette.Energy };
                bar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                var value = UiTheme.Text("0/40", 11, Palette.Suit, Fonts.Mono);
                var row = UiTheme.HBox(6);
                row.AddChild(label);
                row.AddChild(bar);
                row.AddChild(value);
                _batteryList.AddChild(row);
                _batteryBars.Add((bar, value));
            }
            _batteryBuilt = count;
        }

        var index = 0;
        foreach (var fixture in dome.Fixtures)
        {
            if (fixture.Kind != FixtureKind.Battery) continue;
            if (index >= _batteryBars.Count) break;
            var pct = Mathf.Clamp((float)(fixture.Stored / _session.Balance.BatteryStorage) * 100f, 0, 100);
            var (bar, value) = _batteryBars[index];
            bar.Value = pct;
            bar.QueueRedraw();
            value.Text = $"{fixture.Stored:0}/{_session.Balance.BatteryStorage:0}";
            index++;
        }
    }

    private void SelectFixture(FixtureKind kind)
    {
        var dome = _session.ActiveDome;
        if (dome == null) return;

        _session.RequestFixture(kind);
        _onClose?.Invoke();
    }

    private void BuildAnother()
    {
        _confirmDomeId = -1;
        _onClose?.Invoke();
        _session.StartBuild();
    }

    private void Upgrade()
    {
        var dome = _session.ActiveDome;
        if (dome == null) return;

        _confirmDomeId = -1;
        if (!_session.UpgradeDome(dome, out var reason)) _session.Say(reason, MessageKind.Warn);
        Refresh();
    }

    private void Move()
    {
        var dome = _session.ActiveDome;
        if (dome == null) return;

        _confirmDomeId = -1;
        _session.StartMoveDome(dome);
        _onClose?.Invoke();
    }

    private void Store()
    {
        var dome = _session.ActiveDome;
        if (dome == null) return;

        _confirmDomeId = -1;
        _session.StoreDome(dome);
        _onClose?.Invoke();
    }

    private void Destroy()
    {
        var dome = _session.ActiveDome;
        if (dome == null) return;

        if (_confirmDomeId != dome.Id)
        {
            _confirmDomeId = dome.Id;
            Refresh();
            return;
        }

        _confirmDomeId = -1;
        _session.DestroyDome(dome);
        _onClose?.Invoke();
    }

    private string FormatCost(System.Collections.Generic.IReadOnlyDictionary<string, int> cost)
    {
        if (cost.Count == 0) return Loc.T("gratis");

        var builder = new StringBuilder();
        foreach (var (item, amount) in cost)
        {
            if (builder.Length > 0) builder.Append(" · ");
            builder.Append(Loc.T(_session.Items[item].Name)).Append(" x").Append(amount);
        }
        return builder.ToString();
    }
}
