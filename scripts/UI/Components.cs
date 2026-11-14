using System;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>Thin filled bar with the original scanline overlay.</summary>
public partial class MiniBar : Control
{
    public double Value { get; set; }
    public Color FillColor { get; set; } = Palette.Dust;

    public override void _Draw()
    {
        var width = Size.X;
        var height = Size.Y;
        DrawRect(new Rect2(0, 0, width, height), Color.FromHtml("#100a08"));
        var fill = (float)Mathf.Clamp(Value, 0, 100) / 100 * width;
        DrawRect(new Rect2(0, 0, fill, height), FillColor);
        for (var x = 0f; x < width; x += 8)
        {
            DrawRect(new Rect2(x, 0, 1, height), new Color(15 / 255f, 9 / 255f, 7 / 255f, 0.7f));
        }
    }
}

/// <summary>The circular mission patch in the top bar.</summary>
public partial class BrandMark : Control
{
    public override void _Draw()
    {
        var center = new Vector2(12.2f, 11.6f);
        DrawCircle(new Vector2(17, 17), 17, Palette.DustDeep);
        DrawCircle(center, 10.5f, Palette.Dust);
        DrawCircle(center, 4.8f, Color.FromHtml("#e98a58"));
        DrawArc(new Vector2(17, 17), 16.5f, 0, Mathf.Tau, 40, Palette.Bg, 2);
        DrawArc(new Vector2(17, 17), 17, 0, Mathf.Tau, 40, Palette.DustDeep, 1);
    }
}

public sealed class ItemSlot
{
    public Control Root { get; }
    private readonly Label _count;
    private readonly ColorRect _icon;
    private readonly string _id;

    public ItemSlot(ItemDefinition item)
    {
        _id = item.Id;
        var box = UiTheme.HBox(8);
        box.CustomMinimumSize = new Vector2(92, 0);

        _icon = new ColorRect { Color = Palette.FromHex(item.Color), CustomMinimumSize = new Vector2(14, 14) };

        var name = UiTheme.Label(item.Name, 12, Palette.Muted);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        _count = UiTheme.Text("0", 16, Palette.Suit, Fonts.MonoWeight(600));

        box.AddChild(_icon);
        box.AddChild(name);
        box.AddChild(_count);

        var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", UiTheme.Box(Palette.Panel, Palette.Line, 1));
        var pad = UiTheme.Card(8);
        panel.AddThemeStyleboxOverride("panel", pad);
        panel.AddChild(box);
        Root = panel;
    }

    public void Refresh(int count)
    {
        _count.Text = count.ToString();
        var empty = count == 0;
        _count.AddThemeColorOverride("font_color", empty ? Palette.Muted : Palette.Suit);
        _count.Modulate = empty ? new Color(1, 1, 1, 0.6f) : Colors.White;
    }
}

public sealed class PartRow
{
    public Control Root { get; }
    public Button Button { get; }
    public ColorRect StateDot { get; }
    private readonly Label _name;
    private readonly Label _desc;
    private readonly HBoxContainer _costs;
    private readonly PartDefinition _part;

    public PartRow(PartDefinition part, GameSession session)
    {
        _part = part;

        StateDot = new ColorRect { Color = Palette.Health, CustomMinimumSize = new Vector2(10, 10) };
        StateDot.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;

        _name = UiTheme.Text(part.Name, 13, Palette.Suit, Fonts.SansWeight(600));
        _desc = UiTheme.Label(part.Description, 12, Palette.Muted);
        var text = UiTheme.VBox(2);
        text.AddChild(_name);
        text.AddChild(_desc);

        var head = UiTheme.HBox(10);
        head.AddChild(StateDot);
        head.AddChild(text);

        _costs = UiTheme.HBox(12);
        _costs.AddThemeConstantOverride("separation", 12);
        foreach (var (item, amount) in part.Cost)
        {
            var def = session.Items[item];
            var row = UiTheme.HBox(6);
            row.AddChild(new ColorRect { Color = Palette.FromHex(def.Color), CustomMinimumSize = new Vector2(14, 14) });
            row.AddChild(UiTheme.Label(def.Name, 12, Palette.Muted));
            var em = UiTheme.Text($"0/{amount}", 12, Palette.Health, Fonts.Mono);
            row.AddChild(em);
            row.SetMeta("item", item);
            _costs.AddChild(row);
        }

        Button = UiTheme.PrimaryButton("Reparar");
        Button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;

        var body = UiTheme.VBox(8);
        body.AddChild(head);
        body.AddChild(_costs);
        body.AddChild(Button);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.Box(Palette.Panel2, Palette.Line, 1, 0));
        var pad = UiTheme.Card(10);
        panel.AddThemeStyleboxOverride("panel", pad);
        panel.AddChild(body);
        Root = panel;
    }

    public void Refresh(GameSession session)
    {
        var state = session.State;
        var done = RepairSystem.IsRepaired(state, _part.Id);
        var locked = RepairSystem.IsLocked(state, session.Parts, _part);
        var near = InteractionSystem.NearShip(state, session.Balance);
        var pay = RepairSystem.CanPay(state, _part);

        StateDot.Color = done ? Palette.O2 : locked ? Palette.Muted : Palette.Health;
        Root.Modulate = locked && !done ? new Color(1, 1, 1, 0.7f) : Colors.White;

        foreach (var row in _costs.GetChildren())
        {
            if (row is not HBoxContainer hbox) continue;
            var item = (string)hbox.GetMeta("item");
            var needed = _part.Cost[item];
            var em = hbox.GetChild<Label>(2);
            em.Text = $"{Math.Min(state.Inventory[item], needed)}/{needed}";
            em.AddThemeColorOverride("font_color", state.Inventory[item] >= needed ? Palette.Food : Palette.Health);
        }

        var label = "Reparar";
        var enabled = true;
        if (done) { label = "Reparada"; enabled = false; }
        else if (locked) { label = "Bloqueada"; enabled = false; }
        else if (!near) { label = "Lejos de la nave"; enabled = false; }
        else if (!pay) { label = "Faltan materiales"; enabled = false; }

        Button.Text = label;
        Button.Disabled = !enabled;
        Button.Visible = !done;
    }
}

public sealed class PlotCard
{
    public Control Root { get; }
    private readonly Label _number;
    private readonly Label _state;
    private readonly MiniBar _bar;

    public PlotCard(int index)
    {
        _number = UiTheme.Text((index + 1).ToString(), 10, Palette.Muted, Fonts.Display);
        _state = UiTheme.Label("Vacío", 11, Palette.Suit);
        _bar = new MiniBar { CustomMinimumSize = new Vector2(0, 4), FillColor = Palette.Food };

        var body = UiTheme.VBox(3);
        body.AddChild(_number);
        body.AddChild(_state);
        body.AddChild(_bar);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(0, 64) };
        var pad = UiTheme.Card(6);
        panel.AddThemeStyleboxOverride("panel", pad);
        panel.AddChild(body);
        Root = panel;
    }

    public void Refresh(Plot plot, BalanceConfig balance)
    {
        _state.Text = plot.State switch
        {
            PlotState.Empty => "Vacío",
            PlotState.Dry => "Sin regar",
            PlotState.Grow => "Crece " + Math.Ceiling(plot.Time) + " s",
            _ => "Lista",
        };

        _bar.Value = plot.State == PlotState.Grow
            ? (1 - plot.Time / balance.GrowTime) * 100
            : plot.State == PlotState.Ready ? 100 : 0;

        var style = plot.State switch
        {
            PlotState.Dry => UiTheme.Box(Color.FromHtml("#2b1a12"), Palette.Water, 1),
            PlotState.Ready => UiTheme.Box(Color.FromHtml("#26301a"), Palette.Food, 1),
            _ => UiTheme.Box(Color.FromHtml("#2b1a12"), Palette.Line, 1),
        };
        var pad = UiTheme.Card(6);
        style.ContentMarginLeft = pad.ContentMarginLeft;
        style.ContentMarginRight = pad.ContentMarginRight;
        style.ContentMarginTop = pad.ContentMarginTop;
        style.ContentMarginBottom = pad.ContentMarginBottom;
        ((PanelContainer)Root).AddThemeStyleboxOverride("panel", style);

        _state.AddThemeColorOverride("font_color", plot.State switch
        {
            PlotState.Dry => Palette.Water,
            PlotState.Ready => Palette.Food,
            _ => Palette.Suit,
        });
    }
}

public sealed class GoalRow
{
    public Control Root { get; }
    private readonly Label _text;
    private readonly Label _progress;
    private readonly ColorRect _check;

    public GoalRow(string text)
    {
        _check = new ColorRect { Color = Colors.Transparent, CustomMinimumSize = new Vector2(12, 12) };
        _text = UiTheme.Label(text, 13, Palette.Suit);
        _text.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _progress = UiTheme.Text("0", 12, Palette.Muted, Fonts.Mono);

        var row = UiTheme.HBox(10);
        row.AddChild(_check);
        row.AddChild(_text);
        row.AddChild(_progress);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.Box(Palette.Panel2, Palette.Line, 1));
        var pad = UiTheme.Card(9);
        panel.AddThemeStyleboxOverride("panel", pad);
        panel.AddChild(row);
        Root = panel;
    }

    public void Refresh(GoalProgress progress, bool isCurrent)
    {
        _progress.Text = progress.Done ? "Hecho" : progress.Progress;
        _text.AddThemeColorOverride("font_color", progress.Done ? Palette.Muted : Palette.Suit);
        _text.AddThemeConstantOverride("outline_size", progress.Done ? 0 : 0);
        _check.Color = progress.Done ? Palette.O2 : Colors.Transparent;

        var style = progress.Done
            ? UiTheme.Box(Palette.Panel2, Palette.Line, 1)
            : isCurrent
                ? UiTheme.Box(Palette.Panel2, Palette.Dust, 1)
                : UiTheme.Box(Palette.Panel2, Palette.Line, 1);
        var pad = UiTheme.Card(9);
        style.ContentMarginLeft = pad.ContentMarginLeft;
        style.ContentMarginRight = pad.ContentMarginRight;
        style.ContentMarginTop = pad.ContentMarginTop;
        style.ContentMarginBottom = pad.ContentMarginBottom;
        ((PanelContainer)Root).AddThemeStyleboxOverride("panel", style);
    }
}
