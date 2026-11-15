using System;
using System.Text;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// B opens this menu. Structures are built directly from materials here; the dome
/// terminal is reserved for interior fixtures (farm, panel, battery).
/// </summary>
public sealed class BuildMenuPanel
{
    public Control Root { get; }

    private readonly GameSession _session;
    private readonly Action _onClose;

    public BuildMenuPanel(GameSession session, Action onClose)
    {
        _session = session;
        _onClose = onClose;

        var beacon = Option(Loc.T("Baliza"), Loc.T("Da luz y calor a las parcelas cercanas. Consume energía de una cúpula próxima."),
            () => _session.StartBeaconBuild(), session.Balance.BeaconCost);
        var dome = Option(Loc.T("Cúpula"), Loc.T("Zona segura presurizada. Dentro se colocan parcelas, paneles y baterías."),
            () => { _session.StartBuild(); return true; }, session.Balance.DomeCost);
        var farm = Option(Loc.T("Parcela"), Loc.T("Bandeja de cultivo al descubierto. Dentro de una baliza crece como en una cúpula."),
            () => _session.StartFieldBuild(), session.Balance.FieldCost);

        var body = UiTheme.VBox(10);
        body.AddChild(UiTheme.Caption(Loc.T("Construir")));
        body.AddChild(beacon);
        body.AddChild(dome);
        body.AddChild(farm);

        Root = new PanelContainer { CustomMinimumSize = new Vector2(460, 0) };
        ((PanelContainer)Root).AddThemeStyleboxOverride("panel", UiTheme.Card(16));
        Root.AddChild(body);
    }

    private Control Option(string title, string description, Func<bool> start, System.Collections.Generic.IReadOnlyDictionary<string, int> cost)
    {
        var name = UiTheme.Text(title, 14, Palette.Suit, Fonts.SansWeight(700));
        var desc = UiTheme.Label(description, 12, Palette.Muted);
        desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var button = UiTheme.PrimaryButton(Loc.T("Construir"), 12);
        button.CustomMinimumSize = new Vector2(120, 0);
        button.Pressed += () =>
        {
            // Close the modal first: placement methods reject input while it is open.
            _onClose();
            start();
        };

        var texts = UiTheme.VBox(3);
        texts.AddChild(name);
        texts.AddChild(desc);
        texts.AddChild(UiTheme.Label(Loc.F("Coste: {0}", FormatCost(cost)), 11, Palette.Muted));
        texts.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var row = UiTheme.HBox(10);
        row.AddChild(texts);
        row.AddChild(button);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.Card(10));
        panel.AddChild(row);
        return panel;
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
