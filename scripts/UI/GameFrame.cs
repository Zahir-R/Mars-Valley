using System;
using System.Collections.Generic;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// The 768x448 overlay drawn on top of the world: objective chip, storm banner,
/// toasts, interaction prompt and the start/pause/end cards.
/// </summary>
public partial class GameFrame : Control
{
    public GameSession Session { get; set; } = null!;

    private readonly PanelContainer _chip;
    private readonly Label _objective;
    private readonly PanelContainer _banner;
    private readonly Label _bannerLabel;
    private readonly VBoxContainer _toasts;
    private readonly PanelContainer _prompt;
    private readonly Label _promptLabel;
    private readonly Control _cardLayer;
    private readonly PanelContainer _endCard;
    private readonly Label _endEyebrow;
    private readonly Label _endTitle;
    private readonly Label _endText;
    private readonly Button _endButton;
    private readonly List<Control> _toastList = new();

    public Action? BackToMenuRequested;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Pass;
    }

    public GameFrame()
    {
        // --- objective chip ---------------------------------------------------
        _objective = UiTheme.Label("Extrae 5 de hielo", 12, Palette.Suit);
        _objective.ClipText = true;
        var chipBody = UiTheme.VBox(2);
        chipBody.AddChild(UiTheme.Caption("Objetivo"));
        chipBody.AddChild(_objective);
        _chip = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        _chip.AddThemeStyleboxOverride("panel", UiTheme.Card(10));
        _chip.AddChild(chipBody);

        // --- storm banner -----------------------------------------------------
        _bannerLabel = UiTheme.Text(string.Empty, 12, Palette.Energy, Fonts.MonoWeight(600));
        _banner = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        _banner.AddChild(_bannerLabel);

        // --- toasts -----------------------------------------------------------
        // Top-right, up to 48% wide, growing downward; mirrors `.toasts` in the CSS.
        _toasts = UiTheme.VBox(6);
        _toasts.Alignment = BoxContainer.AlignmentMode.Begin;
        _toasts.AnchorLeft = 0.52f;
        _toasts.AnchorRight = 1f;
        _toasts.AnchorTop = 0f;
        _toasts.AnchorBottom = 0f;
        _toasts.OffsetLeft = -10;
        _toasts.OffsetRight = -10;
        _toasts.OffsetTop = 10;
        _toasts.GrowHorizontal = GrowDirection.Both;
        _toasts.GrowVertical = GrowDirection.End;
        _toasts.MouseFilter = MouseFilterEnum.Ignore;

        // --- prompt -----------------------------------------------------------
        _promptLabel = UiTheme.Label(string.Empty, 13, Palette.Suit);
        _prompt = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        var promptBody = UiTheme.HBox(8);
        promptBody.AddChild(Kbd("E"));
        promptBody.AddChild(_promptLabel);
        _prompt.AddChild(promptBody);

        // --- cards ------------------------------------------------------------
        (_endCard, _endEyebrow, _endTitle, _endText, _endButton) = BuildEndCard();
        _endButton.Pressed += () => BackToMenuRequested?.Invoke();

        _cardLayer = new Control { MouseFilter = MouseFilterEnum.Stop };
        _cardLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        center.AddChild(_endCard);
        _cardLayer.AddChild(center);
        _cardLayer.Visible = false;

        AddChild(_chip);
        AddChild(_banner);
        AddChild(_toasts);
        AddChild(_prompt);
        AddChild(_cardLayer);

        foreach (var overlay in new Control[] { _chip, _banner, _prompt, _cardLayer })
        {
            overlay.TopLevel = true;
        }
    }

    private (PanelContainer Card, Label Eyebrow, Label Title, Label Text, Button Button) BuildEndCard()
    {
        var eyebrow = UiTheme.Caption(string.Empty);
        var title = UiTheme.Text(string.Empty, 24, Palette.Suit, Fonts.DisplayWeight(700));
        var text = UiTheme.Label(string.Empty, 14, Palette.Muted);
        text.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var button = UiTheme.PrimaryButton("Volver al menú", 14);

        var card = MakeCardRaw(eyebrow, title, text, null, button);
        return (card, eyebrow, title, text, button);
    }

    private static PanelContainer MakeCardRaw(Label eyebrow, Label title, Label paragraph, Control? extra, Button button)
    {
        paragraph.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        paragraph.CustomMinimumSize = new Vector2(400, 0);

        var body = UiTheme.VBox(12);
        body.AddChild(eyebrow);
        body.AddChild(title);
        body.AddChild(paragraph);
        if (extra != null) body.AddChild(extra);
        body.AddChild(button);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(460, 0) };
        panel.AddThemeStyleboxOverride("panel", UiTheme.Box(Palette.Panel, Palette.DustDeep, 1));
        var pad = UiTheme.Card(22);
        panel.AddThemeStyleboxOverride("panel", pad);
        panel.AddChild(body);
        return panel;
    }

    private static PanelContainer Kbd(string text)
    {
        var label = UiTheme.Text(text, 11, Palette.Suit, Fonts.Mono);
        var panel = new PanelContainer();
        var box = UiTheme.Box(Palette.Panel2, Palette.Line, 1, 2);
        UiTheme.Pad(box, 6, 2);
        panel.AddThemeStyleboxOverride("panel", box);
        panel.AddChild(label);
        return panel;
    }

    public override void _Process(double delta)
    {
        var s = Session.State;
        var width = Size.X > 0 ? Size.X : 768;
        var height = Size.Y > 0 ? Size.Y : 448;

        _chip.ResetSize();
        _chip.Position = new Vector2(10, 10);
        _chip.Size = new Vector2(Mathf.Min(_chip.Size.X, width * 0.56f), _chip.Size.Y);

        _banner.ResetSize();
        _banner.Position = new Vector2((width - _banner.Size.X) / 2, 10);

        _prompt.ResetSize();
        _prompt.Position = new Vector2((width - _prompt.Size.X) / 2, height - _prompt.Size.Y - 12);

        _objective.Text = MissionSystem.CurrentObjective(s, Session.Missions, Session.Parts);

        var banner = string.Empty;
        var crit = false;
        if (s.Building)
        {
            banner = "Construcción · E colocar · Esc cancelar";
        }
        else if (s.Storm.Left > 0)
        {
            banner = "Tormenta de polvo · " + Math.Ceiling(s.Storm.Left) + " s";
            crit = true;
        }
        else if (s.Running && s.Storm.Next <= 10)
        {
            banner = "Alerta: tormenta de polvo en " + Math.Ceiling(s.Storm.Next) + " s";
        }
        _banner.Visible = banner.Length > 0;
        if (banner.Length > 0)
        {
            _bannerLabel.Text = banner;
            _bannerLabel.AddThemeColorOverride("font_color", crit ? Palette.Health : Palette.Energy);
            var style = UiTheme.Box(crit ? new Color(80 / 255f, 15 / 255f, 10 / 255f, 0.92f)
                : new Color(80 / 255f, 40 / 255f, 10 / 255f, 0.92f), crit ? Palette.Health : Palette.Energy, 1);
            UiTheme.Pad(style, 12, 6);
            _banner.AddThemeStyleboxOverride("panel", style);
        }

        var showPrompt = s.Running && !s.Paused && !s.Over && Session.Prompt.Length > 0;
        _prompt.Visible = showPrompt;
        if (showPrompt)
        {
            _promptLabel.Text = Session.Prompt;
            var style = UiTheme.Box(new Color(21 / 255f, 15 / 255f, 13 / 255f, 0.92f), Palette.O2, 1);
            UiTheme.Pad(style, 12, 6);
            _prompt.AddThemeStyleboxOverride("panel", style);
        }

        UpdateCards(s);
    }

    private void UpdateCards(GameState s)
    {
        var over = s.Over;

        _cardLayer.Visible = over;
        _endCard.Visible = over;

        if (!over) return;

        _endEyebrow.Text = s.Win ? "Misión cumplida" : "Misión fallida";
        _endTitle.Text = s.Win ? "¡Despegue!" : "Traje sin soporte";
        _endText.Text = s.Win
            ? $"Reparaste la nave en el Sol {GameClock.DayNumber(s.Time, Session.Balance.SolLength)} y vuelves a casa. El motor de ascenso respondió a la primera."
            : s.Vitals.O2 <= 0
                ? "Te quedaste sin oxígeno. Recarga el traje con hielo antes de que baje del 25 %."
                : "La falta de alimento terminó con tus fuerzas. Cultiva y come papas con regularidad.";
    }

    public void OnMessage(string text, MessageKind kind)
    {
        var label = UiTheme.Label(text, 12, Palette.Suit);
        var naturalW = label.GetMinimumSize().X;
        var vpW = GetViewportRect().Size.X;
        if (vpW <= 0) vpW = 768;
        var maxW = Mathf.Max(140f, vpW * 0.48f - 26f);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(Mathf.Min(naturalW, maxW), 0);
        var accent = kind switch
        {
            MessageKind.Warn => Palette.Energy,
            MessageKind.Good => Palette.Food,
            _ => Palette.O2,
        };
        var style = UiTheme.Box(new Color(21 / 255f, 15 / 255f, 13 / 255f, 0.92f), accent, 1);
        style.BorderWidthLeft = 3;
        UiTheme.Pad(style, 10, 6);
        var panel = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            Modulate = new Color(1, 1, 1, 0),
        };
        panel.AddThemeStyleboxOverride("panel", style);
        panel.AddChild(label);

        _toasts.AddChild(panel);
        _toastList.Add(panel);

        var tween = CreateTween();
        tween.TweenProperty(panel, "modulate:a", 1.0f, 0.2f);

        while (_toastList.Count > 3)
        {
            var oldest = _toastList[0];
            _toastList.RemoveAt(0);
            oldest.QueueFree();
        }

        var timer = GetTree().CreateTimer(3.2);
        timer.Timeout += () => { _toastList.Remove(panel); panel.QueueFree(); };
    }
}
