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
    private readonly Button _bunk;
    private int _confirmDomeId = -1;

    public DomePanel(GameSession session, Action? onClose)
    {
        _session = session;
        _onClose = onClose;

        _title = UiTheme.Text("Cúpula", 16, Palette.Suit, Fonts.SansWeight(700));
        _stats = UiTheme.Label(string.Empty, 12, Palette.Muted);
        _stats.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detail = UiTheme.Label(string.Empty, 12, Palette.Muted);
        _detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _interior = UiTheme.Label(string.Empty, 12, Palette.Muted);
        _interior.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _cost = UiTheme.Label(string.Empty, 12, Palette.Muted);

        _upgrade = UiTheme.PrimaryButton("Actualizar", 13);
        _upgrade.Pressed += Upgrade;

        _newDome = UiTheme.GhostButton("Construir cúpula", 12);
        _newDome.TooltipText = "Coloca otra cúpula; no requiere semillas ni cultivos.";
        _newDome.Pressed += BuildAnother;

        _move = UiTheme.GhostButton("Mover", 12);
        _move.Pressed += Move;

        _store = UiTheme.GhostButton("Guardar", 12);
        _store.TooltipText = "Se guarda en el inventario; lo que haya dentro se pierde.";
        _store.Pressed += Store;

        _destroy = UiTheme.GhostButton("Destruir", 12);
        _destroy.Pressed += Destroy;

        _farm = FixtureButton("Parcela", FixtureKind.Farm);
        _solar = FixtureButton("Panel solar", FixtureKind.SolarPanel);
        _battery = FixtureButton("Batería", FixtureKind.Battery);
        _bunk = FixtureButton("Litera", FixtureKind.Bunk);

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
        fixtures.AddChild(_bunk);

        var hint = UiTheme.Label(
            "Elige una instalación y colócala en un hueco libre de la cúpula con E. La litera recupera energía al interactuar con ella.",
            11, Palette.Muted);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var body = UiTheme.VBox(8);
        body.AddChild(UiTheme.Caption("Terminal de cúpula"));
        body.AddChild(_title);
        body.AddChild(_stats);
        body.AddChild(_detail);
        body.AddChild(_interior);
        body.AddChild(_cost);
        body.AddChild(UiTheme.Label("Construir en el interior", 12, Palette.Muted));
        body.AddChild(fixtures);
        body.AddChild(UiTheme.Label("Gestión", 12, Palette.Muted));
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
            _title.Text = "Sin cúpula seleccionada";
            _stats.Text = "Acércate al terminal de una cúpula y pulsa E.";
            _detail.Text = string.Empty;
            _interior.Text = string.Empty;
            _cost.Text = string.Empty;
            SetEnabled(false);
            return;
        }

        if (_confirmDomeId != -1 && _confirmDomeId != dome.Id) _confirmDomeId = -1;

        var b = _session.Balance;
        var area = _session.Areas.At(dome.CenterX, dome.CenterY);
        var light = EnvironmentSystem.Light(_session.State, b);
        var lightPct = Mathf.RoundToInt((float)light * 100);
        var generation = (b.DomePassiveSolar + dome.PanelCount * b.SolarPanelOutput) * light * area.SolarFactor;
        var slots = DomeLayout.SlotCount(b);
        var free = slots - dome.Plots.Count - dome.Fixtures.Count;

        _title.Text = $"Cúpula {dome.Id} · nivel {dome.Level} · {area.Name}";
        _stats.Text = $"Interior {dome.InternalTempC:0}°C · Exterior {dome.ExternalTempC:0}°C · Luz {lightPct}%";
        _detail.Text =
            $"Energía {dome.Energy:0}/{dome.EnergyCapacity(b):0} · Generación +{generation:0.0}/s · Huecos libres {free}";
        _interior.Text =
            $"Parcelas {dome.Plots.Count} · Paneles {dome.PanelCount} · Baterías {dome.BatteryCount} · Literas {dome.BunkCount}";

        SetEnabled(true);

        if (dome.Level >= b.DomeMaxLevel)
        {
            _cost.Text = "Nivel máximo alcanzado.";
            _upgrade.Disabled = true;
        }
        else
        {
            _cost.Text = "Mejora: " + FormatCost(_session.UpgradeCostFor(dome));
            _upgrade.Disabled = !_session.CanUpgradeDome(dome, out _);
        }

        var canPlace = free > 0;
        _farm.Disabled = !canPlace;
        _solar.Disabled = !canPlace;
        _battery.Disabled = !canPlace;
        _bunk.Disabled = !canPlace;
        _destroy.Text = _confirmDomeId == dome.Id ? "Confirmar destrucción" : "Destruir";
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
        _bunk.Disabled = !enabled;
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
        if (cost.Count == 0) return "gratis";

        var builder = new StringBuilder();
        foreach (var (item, amount) in cost)
        {
            if (builder.Length > 0) builder.Append(" · ");
            builder.Append(_session.Items[item].Name).Append(" x").Append(amount);
        }
        return builder.ToString();
    }
}
