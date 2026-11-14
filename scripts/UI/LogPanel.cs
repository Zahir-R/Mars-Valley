using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

public sealed class LogPanel
{
    public Control Root { get; }

    private readonly VBoxContainer _list;
    private int _lastCount = -1;
    private string _lastTop = string.Empty;

    public LogPanel()
    {
        _list = UiTheme.VBox(7);
        _list.CustomMinimumSize = new Vector2(0, 96);

        var body = UiTheme.VBox(12);
        body.AddChild(UiTheme.Caption("Bitácora"));
        body.AddChild(_list);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.Card(14));
        panel.AddChild(body);
        Root = panel;
    }

    public void Rebuild(GameSession session)
    {
        var log = session.State.Log;
        var top = log.Count > 0 ? log[0].Time + log[0].Text : string.Empty;
        if (log.Count == _lastCount && top == _lastTop) return;
        _lastCount = log.Count;
        _lastTop = top;

        foreach (var child in _list.GetChildren()) child.QueueFree();

        if (log.Count == 0)
        {
            _list.AddChild(UiTheme.Label("Sin registros todavía.", 12, Palette.Muted));
            return;
        }

        foreach (var entry in log)
        {
            var time = UiTheme.Text(entry.Time, 11, Palette.Muted, Fonts.Mono);
            time.CustomMinimumSize = new Vector2(44, 0);

            var color = entry.Kind switch
            {
                MessageKind.Warn => Palette.Energy,
                MessageKind.Good => Palette.Food,
                _ => Palette.Suit,
            };
            var text = UiTheme.Label(entry.Text, 12, color);
            text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            text.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            var row = UiTheme.HBox(8);
            row.AddChild(time);
            row.AddChild(text);
            _list.AddChild(row);
        }
    }
}
