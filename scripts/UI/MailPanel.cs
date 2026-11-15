using System;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// The Nave's inbox: interference bursts that decode into letters, the supply request,
/// the coordinates decision and the final rescue choice. Lives inside the Nave modal.
/// </summary>
public sealed class MailPanel
{
    public Control Root { get; }

    private readonly GameSession _session;
    private readonly Action _onClose;
    private readonly VBoxContainer _body = UiTheme.VBox(8);
    private string _signature = string.Empty;
    private bool _force;

    public MailPanel(GameSession session, Action onClose)
    {
        _session = session;
        _onClose = onClose;
        Root = _body;
    }

    public void Refresh()
    {
        var s = _session.State;
        var signature = $"{s.InterferenceActive}|{s.NpcPhase}|{s.ActiveNpcLetterId}|" +
                        $"{string.Join(",", s.NpcLetters)}|{string.Join(",", s.NpcUnread)}|{s.PendingFinalChoice}";
        if (!_force && signature == _signature) return;
        _signature = signature;
        _force = false;

        foreach (var child in _body.GetChildren())
        {
            _body.RemoveChild(child);
            child.QueueFree();
        }

        if (s.InterferenceActive)
        {
            BuildInterference();
            return;
        }

        if (s.NpcLetters.Count == 0)
        {
            _body.AddChild(UiTheme.Label(Loc.T("Sin mensajes."), 12, Palette.Muted));
            return;
        }

        _body.AddChild(UiTheme.Caption(Loc.T("Correo de la nave")));

        var unread = NpcQuestSystem.UnreadCount(s);
        if (unread > 0)
        {
            var badge = UiTheme.Label(Loc.F("{0} mensaje(s) sin leer", unread), 11, Palette.Energy);
            _body.AddChild(badge);
        }

        var list = UiTheme.VBox(4);
        for (var i = s.NpcLetters.Count - 1; i >= 0; i--)
        {
            var id = s.NpcLetters[i];
            var letter = NpcQuestDatabase.Get(id);
            if (letter == null) continue;

            var prefix = s.NpcUnread.Contains(id) ? "● " : string.Empty;
            var button = UiTheme.GhostButton($"{prefix}{NpcQuestSystem.SubjectFor(letter)} · {letter.Sender}", 12);
            button.Alignment = HorizontalAlignment.Left;
            var captured = id;
            button.Pressed += () =>
            {
                _session.ReadNpcLetter(captured);
                _force = true;
            };
            list.AddChild(button);
        }
        _body.AddChild(list);

        var activeId = s.ActiveNpcLetterId;
        if (activeId == null || !s.NpcLetters.Contains(activeId))
        {
            activeId = s.NpcLetters[^1];
            s.ActiveNpcLetterId = activeId;
        }

        var active = NpcQuestDatabase.Get(activeId);
        if (active == null) return;

        var subject = UiTheme.Text(NpcQuestSystem.SubjectFor(active), 14, Palette.Suit, Fonts.SansWeight(700));
        var from = UiTheme.Label(active.Sender, 11, Palette.Muted);
        var body = UiTheme.Label(NpcQuestSystem.BodyFor(active, s), 12, Palette.Suit);
        body.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var card = UiTheme.CardPanel(10);
        var cardBody = UiTheme.VBox(4);
        cardBody.AddChild(from);
        cardBody.AddChild(subject);
        cardBody.AddChild(body);
        card.AddChild(cardBody);
        _body.AddChild(card);

        var actions = new HFlowContainer();
        actions.AddThemeConstantOverride("h_separation", 6);
        actions.AddThemeConstantOverride("v_separation", 6);
        BuildActions(active, actions);
        _body.AddChild(actions);
    }

    private void BuildInterference()
    {
        var noisy = UiTheme.Text("▒░▒  ▓█▒░  █▒ ░▓  ▒░▒█", 14, Palette.Energy, Fonts.MonoWeight(700));
        noisy.HorizontalAlignment = HorizontalAlignment.Center;

        var title = UiTheme.Text(Loc.T("SEÑAL INTERFERIDA"), 14, Palette.Health, Fonts.MonoWeight(700));
        title.HorizontalAlignment = HorizontalAlignment.Center;

        var hint = UiTheme.Label(Loc.T("Hay una transmisión atrapada en la radio de la nave."), 11, Palette.Muted);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var listen = UiTheme.PrimaryButton(Loc.T("Escuchar señal"), 13);
        listen.Pressed += () =>
        {
            _session.DecodeNpcInterference();
            _force = true;
        };

        var panel = UiTheme.CardPanel(10);
        var box = UiTheme.VBox(6);
        box.AddChild(title);
        box.AddChild(noisy);
        box.AddChild(hint);
        box.AddChild(listen);
        panel.AddChild(box);
        _body.AddChild(panel);
    }

    private void BuildActions(NpcLetter letter, HFlowContainer actions)
    {
        var s = _session.State;

        switch (letter.Decision)
        {
            case NpcDecision.GoOrSkip:
                var go = UiTheme.PrimaryButton("Ir al sitio", 12);
                go.Pressed += () =>
                {
                    _session.ChooseSite(true);
                    _onClose();
                };
                var stay = UiTheme.GhostButton(Loc.T("No iré"), 12);
                stay.Pressed += () =>
                {
                    _session.ChooseSite(false);
                    _force = true;
                };
                actions.AddChild(go);
                actions.AddChild(stay);
                break;

            case NpcDecision.Final:
                var take = UiTheme.PrimaryButton(Loc.T("Llevarla en la nave"), 12);
                take.Pressed += () =>
                {
                    _session.ChooseFinal(true);
                    _session.Launch();
                    _onClose();
                };
                var leave = UiTheme.GhostButton(Loc.T("Dejarla atrás"), 12);
                leave.Pressed += () =>
                {
                    _session.ChooseFinal(false);
                    _session.Launch();
                    _onClose();
                };
                actions.AddChild(take);
                actions.AddChild(leave);
                break;

            default:
                var ack = UiTheme.GhostButton(Loc.T(s.PendingFinalChoice ? "Cerrar" : "Entendido"), 12);
                ack.Pressed += () =>
                {
                    if (s.PendingFinalChoice)
                    {
                        _session.ChooseFinal(false);
                        _onClose();
                        return;
                    }
                    _session.ReadNpcLetter(letter.Id);
                    _force = true;
                };
                actions.AddChild(ack);
                break;
        }
    }
}
