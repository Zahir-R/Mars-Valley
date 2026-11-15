using System;
using System.Collections.Generic;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

public sealed class WorkshopPanel
{
    public Control Root { get; }

    private readonly GameSession _session;
    private readonly List<RecipeRow> _rows = new();

    public WorkshopPanel(GameSession session)
    {
        _session = session;

        var list = UiTheme.VBox(8);
        foreach (var recipe in session.Recipes.Recipes)
        {
            var row = new RecipeRow(recipe, session);
            _rows.Add(row);
            list.AddChild(row.Root);
        }

        var hint = UiTheme.Label(
            Loc.T("Extrae roca a mano. El regolito necesita una pala y el basalto un martillo o un pico. Recoge objetos sueltos junto a la nave."),
            12, Palette.Muted);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var body = UiTheme.VBox(12);
        body.AddChild(UiTheme.Caption(Loc.T("Taller manual")));
        body.AddChild(hint);
        body.AddChild(list);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.Card(14));
        panel.AddChild(body);
        Root = panel;
    }

    public void Refresh()
    {
        foreach (var row in _rows) row.Refresh(_session);
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
    private readonly Label _refillSub;
    private readonly Button _launch;
    private readonly Label _requirements;
    private readonly Label _ready;
    private readonly List<PartRow> _parts = new();
    private readonly MailPanel _mail;
    private readonly Action _onClose;

    public ShipPanel(GameSession session, Action? onClose = null)
    {
        _session = session;
        _onClose = onClose ?? (() => { });
        _mail = new MailPanel(session, _onClose);

        _percent = UiTheme.Text("0%", 13, Palette.Suit, Fonts.MonoWeight(500));
        var percentRow = UiTheme.HBox(0);
        var percentLabel = UiTheme.Label(Loc.T("Reparación de la nave"), 13, Palette.Suit);
        percentLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        percentRow.AddChild(percentLabel);
        percentRow.AddChild(_percent);

        _bar = new MiniBar { CustomMinimumSize = new Vector2(0, 10), FillColor = Palette.Dust };
        _hint = UiTheme.Label(Loc.T("Camina hasta la nave para reparar y recargar."), 12, Palette.Muted);
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

        _refill = UiTheme.SupportButton(Loc.T("Recargar O₂"), Loc.T("1 hielo → O₂ a 100 % (máx. 5 cargas)"));
        _melt = UiTheme.SupportButton(Loc.T("Fundir hielo"), Loc.T("1 hielo → 2 agua"));
        _refill.Pressed += () => _session.RefillO2();
        _melt.Pressed += () => _session.MeltIce();

        _refillSub = UiTheme.Label(string.Empty, 11, Palette.Muted);
        var supportButtons = UiTheme.HBox(8);
        var refillBox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        refillBox.AddChild(_refill);
        refillBox.AddChild(_refillSub);
        supportButtons.AddChild(refillBox);
        supportButtons.AddChild(Support(_melt, Loc.T("1 hielo → 2 agua")));

        var support = UiTheme.VBox(8);
        support.AddChild(UiTheme.Caption(Loc.T("Soporte vital")));
        support.AddChild(supportButtons);

        _ready = UiTheme.Label(string.Empty, 13, Palette.O2);
        _requirements = UiTheme.Label(string.Empty, 11, Palette.Muted);
        _requirements.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _launch = UiTheme.PrimaryButton(Loc.T("Despegar"), 14);
        _launch.Pressed += () =>
        {
            if (_session.Launch()) _onClose();
        };

        var launchBox = UiTheme.VBox(6);
        launchBox.AddChild(_ready);
        launchBox.AddChild(_requirements);
        launchBox.AddChild(_launch);

        var body = UiTheme.VBox(12);
        body.AddChild(overall);
        body.AddChild(parts);
        body.AddChild(launchBox);
        body.AddChild(support);
        body.AddChild(_mail.Root);

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
            ? Loc.T("Estás junto a la nave: puedes reparar y recargar.")
            : Loc.T("Camina hasta la nave para reparar y recargar.");
        _hint.AddThemeColorOverride("font_color", near ? Palette.O2 : Palette.Muted);

        foreach (var row in _parts) row.Refresh(_session);

        _refillSub.Text = Loc.F("1 hielo → O₂ a 100 % · {0}/{1} cargas",
            s.ShipO2Charges, _session.Balance.ShipO2ChargesMax);
        _refillSub.AddThemeColorOverride("font_color", s.ShipO2Charges > 0 ? Palette.Muted : Palette.Health);
        _refill.Disabled = !near || s.Inventory["hielo"] < 1 || s.Vitals.O2 >= 100 || s.ShipO2Charges <= 0;
        _melt.Disabled = !near || s.Inventory["hielo"] < 1;

        _launch.Text = Loc.T("Despegar");
        _launch.Disabled = !_session.CanLaunch;
        if (_session.CanLaunch)
        {
            _ready.Text = Loc.T("Nave lista para despegar");
            _requirements.Text = string.Empty;
        }
        else
        {
            _ready.Text = string.Empty;
            var lines = new List<string> { Loc.T("Completa estos requisitos para despegar:") };
            foreach (var (key, done) in _session.LaunchRequirements())
            {
                lines.Add((done ? "✓ " : "• ") + Loc.T(key));
            }
            _requirements.Text = string.Join("\n", lines);
        }

        _mail.Refresh();
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
            var row = new GoalRow(Loc.T(goal.Text));
            _rows.Add(row);
            list.AddChild(row.Root);
        }

        var body = UiTheme.VBox(12);
        body.AddChild(UiTheme.Caption(Loc.T("Objetivos")));
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
