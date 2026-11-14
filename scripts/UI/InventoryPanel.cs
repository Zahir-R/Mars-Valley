using System;
using System.Collections.Generic;
using Godot;
using NasaValley.Domain;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// Categorized inventory modal: category tabs, an item list, and a bottom description
/// area whose action buttons depend on the selected item's category.
/// </summary>
public sealed class InventoryPanel
{
    private static readonly (string Id, string Label)[] Categories =
    {
        ("comida", "Comida"),
        ("herramientas", "Herramientas"),
        ("minerales", "Minerales"),
        ("componentes", "Componentes"),
        ("semillas", "Semillas"),
        ("estructuras", "Estructuras"),
        ("otros", "Otros"),
    };

    public Control Root { get; }

    private readonly GameSession _session;
    private readonly Action? _onPlaceDome;
    private readonly Dictionary<string, Button> _tabs = new();
    private readonly Dictionary<string, Button> _rows = new();
    private readonly VBoxContainer _list;
    private readonly Label _descName;
    private readonly Label _descMeta;
    private readonly Label _descText;
    private readonly HBoxContainer _actions;

    private string _category = "comida";
    private string? _selected;
    private string? _builtCategory;
    private string? _actionsFor;

    public InventoryPanel(GameSession session, Action? onPlaceDome = null)
    {
        _session = session;
        _onPlaceDome = onPlaceDome;

        var tabBar = new HFlowContainer();
        tabBar.AddThemeConstantOverride("h_separation", 4);
        tabBar.AddThemeConstantOverride("v_separation", 4);
        foreach (var (id, label) in Categories)
        {
            var button = UiTheme.GhostButton(label, 12);
            button.Pressed += () => ShowCategory(id);
            _tabs[id] = button;
            tabBar.AddChild(button);
        }

        _list = UiTheme.VBox(6);
        _list.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 190),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        scroll.AddChild(_list);

        _descName = UiTheme.Text("Selecciona un objeto", 15, Palette.Suit, Fonts.SansWeight(700));
        _descMeta = UiTheme.Label(string.Empty, 11, Palette.Muted);
        _descText = UiTheme.Label("Elige una categoría y pulsa un objeto para ver su descripción.", 12, Palette.Muted);
        _descText.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var descBox = UiTheme.VBox(4);
        descBox.AddChild(_descName);
        descBox.AddChild(_descMeta);
        descBox.AddChild(_descText);
        var descCard = new PanelContainer { CustomMinimumSize = new Vector2(0, 96) };
        var descPad = UiTheme.Card(12);
        descCard.AddThemeStyleboxOverride("panel", descPad);
        descCard.AddChild(descBox);

        _actions = UiTheme.HBox(8);

        var body = UiTheme.VBox(10);
        body.AddChild(tabBar);
        body.AddChild(scroll);
        body.AddChild(descCard);
        body.AddChild(_actions);
        Root = body;
    }

    public void ShowCategory(string id)
    {
        _category = id;
        _selected = null;
        _builtCategory = null;
        _actionsFor = null;
        Refresh();
    }

    public void Refresh()
    {
        if (!Root.IsVisibleInTree()) return;

        EnsureRows();
        UpdateTabStyles();

        foreach (var (id, row) in _rows)
        {
            var def = _session.Items[id];
            var count = _session.State.Inventory[id];
            var selected = id == _selected;
            row.Text = (selected ? "▶  " : string.Empty) + def.Name + "      x" + count;
            row.Modulate = selected ? Colors.White : new Color(1, 1, 1, 0.82f);
            row.AddThemeColorOverride("font_color", count > 0 ? Palette.Suit : Palette.Muted);
        }

        RefreshDescription();
    }

    private void EnsureRows()
    {
        if (_builtCategory == _category) return;
        _builtCategory = _category;

        foreach (var child in _list.GetChildren()) child.QueueFree();
        _rows.Clear();

        foreach (var def in _session.Items.Items)
        {
            if (GroupOf(def) != _category) continue;

            var row = new Button
            {
                Alignment = HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(0, 34),
                Icon = UiTheme.ColorTexture(Palette.FromHex(def.Color), 14),
                FocusMode = Control.FocusModeEnum.None,
            };
            row.AddThemeFontOverride("font", Fonts.Sans);
            row.AddThemeFontSizeOverride("font_size", 13);
            row.AddThemeColorOverride("font_color", Palette.Suit);
            row.AddThemeColorOverride("font_hover_color", Palette.Suit);
            var normal = UiTheme.Box(Palette.Panel2, Palette.Line, 1);
            UiTheme.Pad(normal, 10, 6);
            var hover = UiTheme.Box(Palette.Panel, Palette.Dust, 1);
            UiTheme.Pad(hover, 10, 6);
            row.AddThemeStyleboxOverride("normal", normal);
            row.AddThemeStyleboxOverride("hover", hover);
            row.AddThemeStyleboxOverride("pressed", hover);

            var id = def.Id;
            row.Pressed += () => Select(id);
            _list.AddChild(row);
            _rows[id] = row;
        }
    }

    private void Select(string id)
    {
        _selected = id;
        Refresh();
    }

    private void UpdateTabStyles()
    {
        foreach (var (id, button) in _tabs)
        {
            var selected = id == _category;
            button.Modulate = selected ? Colors.White : new Color(1, 1, 1, 0.55f);
        }
    }

    private void RefreshDescription()
    {
        if (_selected == null || !_session.Items.TryGet(_selected, out var def))
        {
            _descName.Text = "Selecciona un objeto";
            _descMeta.Text = string.Empty;
            _descText.Text = "Elige una categoría y pulsa un objeto para ver su descripción.";
            if (_actionsFor != null)
            {
                foreach (var child in _actions.GetChildren()) child.QueueFree();
                _actionsFor = null;
            }
            return;
        }

        _descName.Text = def.Name;
        _descMeta.Text = $"{CategoryLabel(GroupOf(def))} · x{_session.State.Inventory[def.Id]}";
        _descText.Text = def.Description;

        if (_actionsFor == ActionKey(def)) return;
        _actionsFor = ActionKey(def);

        foreach (var child in _actions.GetChildren()) child.QueueFree();

        if (def.HasTag("Comida"))
        {
            var use = UiTheme.PrimaryButton("Utilizar", 13);
            use.Pressed += () => { _session.Eat(def.Id); Refresh(); };
            _actions.AddChild(use);

            var source = UiTheme.GhostButton("Definir fuente de comida", 12);
            source.Pressed += () => { _session.SetFoodSource(def.Id); Refresh(); };
            _actions.AddChild(source);
        }
        else if (def.Id == BuildSystem.StoredDomeItem && _session.State.Inventory[def.Id] > 0)
        {
            var place = UiTheme.PrimaryButton("Colocar cúpula", 13);
            place.Pressed += () => _onPlaceDome?.Invoke();
            _actions.AddChild(place);
        }
        else if (def.Category == ItemCategory.Tool)
        {
            var use = UiTheme.GhostButton("Usar", 12);
            use.Pressed += () => _session.Say("Usar herramientas llegará pronto.", MessageKind.Warn);
            _actions.AddChild(use);
        }
    }

    private string ActionKey(ItemDefinition def) =>
        def.Id == BuildSystem.StoredDomeItem
            ? def.Id + ":" + (_session.State.Inventory[def.Id] > 0)
            : def.Id;

    private static string GroupOf(ItemDefinition def) => def.Category switch
    {
        ItemCategory.Consumable => "comida",
        ItemCategory.Tool => "herramientas",
        ItemCategory.NaturalResource => "minerales",
        ItemCategory.Component or ItemCategory.Processed => "componentes",
        ItemCategory.Seed => "semillas",
        ItemCategory.Structure => "estructuras",
        _ => "otros",
    };

    private static string CategoryLabel(string group) =>
        group switch
        {
            "comida" => "Comida",
            "herramientas" => "Herramienta",
            "minerales" => "Mineral",
            "componentes" => "Componente",
            "semillas" => "Semilla",
            "estructuras" => "Estructura",
            _ => "Objeto",
        };
}
