using System;
using System.Collections.Generic;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

public sealed class TopBar
{
    public Control Root { get; }
    public Button PauseButton { get; }
    public IReadOnlyList<Control> HideSmall => _hideSmall;

    private readonly Label _sol;
    private readonly Label _time;
    private readonly Label _temp;
    private readonly List<Control> _hideSmall = new();

    public TopBar(GameSession session, Action onPause)
    {
        var mark = new BrandMark { CustomMinimumSize = new Vector2(34, 34) };
        var title = UiTheme.Text("Mars Valley", 20, Palette.Suit, Fonts.DisplayWeight(700));
        var subtitle = UiTheme.Label(Loc.T("Misión Ares-7 · Cráter Gale"), 12, Palette.Muted);
        var titleBox = UiTheme.VBox(3);
        titleBox.AddChild(title);
        titleBox.AddChild(subtitle);

        var brand = UiTheme.HBox(12);
        brand.AddChild(mark);
        brand.AddChild(titleBox);

        var env = UiTheme.HBox(22);
        env.AddThemeConstantOverride("separation", 22);
        _sol = AddEnv(env, Loc.T("Sol"), "1", false);
        _time = AddEnv(env, Loc.T("Hora local"), "06:00", false);
        _temp = AddEnv(env, Loc.T("Exterior"), "-70 °C", false);
        AddEnv(env, Loc.T("Presión"), "6,1 mbar", true);
        AddEnv(env, Loc.T("Gravedad"), "0,38 g", true);

        PauseButton = UiTheme.GhostButton(Loc.T("Pausa"));
        PauseButton.Pressed += onPause;

        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var row = UiTheme.HBox(28);
        row.AddChild(brand);
        row.AddChild(spacer);
        row.AddChild(env);
        row.AddChild(PauseButton);

        var line = new ColorRect { Color = Palette.Line, CustomMinimumSize = new Vector2(0, 1) };
        var box = UiTheme.VBox(12);
        box.AddChild(row);
        box.AddChild(line);
        Root = box;
    }

    private Label AddEnv(HBoxContainer parent, string caption, string value, bool hideSmall)
    {
        var valueLabel = UiTheme.Text(value, 15, Palette.Suit, Fonts.MonoWeight(500));
        var box = UiTheme.VBox(3);
        box.AddChild(UiTheme.Caption(caption));
        box.AddChild(valueLabel);
        parent.AddChild(box);
        if (hideSmall) _hideSmall.Add(box);
        return valueLabel;
    }

    public void Refresh(GameSession session)
    {
        var s = session.State;
        var hour = GameClock.HourOf(s.Time, session.Balance.SolLength);
        _sol.Text = GameClock.DayNumber(s.Time, session.Balance.SolLength).ToString();
        _time.Text = GameClock.FormatTime(hour);
        _temp.Text = Mathf.Round(GameClock.TemperatureAt(hour)) + " °C";
        PauseButton.Text = s.Paused ? "Continuar" : "Pausa";
    }
}
