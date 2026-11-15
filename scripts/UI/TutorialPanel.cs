using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>Compact 5-step onboarding card. Auto-advances; can be skipped.</summary>
public sealed class TutorialPanel
{
    public Control Root { get; }

    private readonly GameSession _session;
    private readonly Label _counter;
    private readonly Label _title;
    private readonly Label _hint;
    private readonly Button _skip;

    public TutorialPanel(GameSession session)
    {
        _session = session;

        _counter = UiTheme.Label(string.Empty, 11, Palette.Muted);
        _title = UiTheme.Text(string.Empty, 13, Palette.Suit, Fonts.SansWeight(700));
        _hint = UiTheme.Label(string.Empty, 11, Palette.Muted);
        _hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _hint.CustomMinimumSize = new Vector2(260, 0);

        _skip = UiTheme.GhostButton(Loc.T("Saltar"), 11);
        _skip.Pressed += () => _session.SkipTutorial();

        var box = UiTheme.VBox(3);
        box.AddChild(_counter);
        box.AddChild(_title);
        box.AddChild(_hint);
        box.AddChild(_skip);

        var card = UiTheme.CardPanel(10);
        card.AddChild(box);
        Root = card;
    }

    public void Refresh()
    {
        if (!_session.State.Running)
        {
            Root.Visible = false;
            return;
        }

        var step = _session.CurrentTutorialStep;
        if (step == null)
        {
            Root.Visible = false;
            return;
        }

        Root.Visible = true;
        _counter.Text = Loc.F("Tutorial · paso {0}/{1}", _session.TutorialIndex + 1, _session.TutorialSteps.Count);
        _title.Text = Loc.T(step.Title);
        _hint.Text = Loc.T(step.Hint);
        _skip.Text = Loc.T("Saltar");
    }
}
