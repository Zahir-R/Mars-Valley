using Godot;

namespace NasaValley.UI;

/// <summary>
/// Responsive stage. Wide windows use three columns (vitals/log · game · mission),
/// narrow windows collapse to a single column, mirroring the CSS breakpoints.
/// </summary>
public partial class StageLayout : Control
{
    public enum Mode
    {
        Wide,
        Stacked,
    }

    private readonly Control _vitals;
    private readonly Control _center;
    private readonly Control _log;
    private readonly Control _right;
    private BoxContainer? _root;
    private Mode _mode = Mode.Wide;

    public StageLayout(Control vitals, Control center, Control log, Control right)
    {
        _vitals = vitals;
        _center = center;
        _log = log;
        _right = right;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        SetMode(Mode.Wide);
    }

    public void SetMode(Mode mode)
    {
        if (_root != null && mode == _mode) return;
        _mode = mode;

        Detach(_vitals);
        Detach(_center);
        Detach(_log);
        Detach(_right);

        if (_root != null)
        {
            RemoveChild(_root);
            _root.QueueFree();
            _root = null;
        }

        if (mode == Mode.Wide)
        {
            _center.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _right.CustomMinimumSize = new Vector2(316, 0);

            var left = UiTheme.VBox(14);
            left.CustomMinimumSize = new Vector2(224, 0);
            left.AddChild(_vitals);
            left.AddChild(_log);

            var row = UiTheme.HBox(14);
            row.AddChild(left);
            row.AddChild(_center);
            row.AddChild(_right);
            _root = row;
        }
        else
        {
            _center.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _right.CustomMinimumSize = new Vector2(0, 0);

            var column = UiTheme.VBox(14);
            column.AddChild(_vitals);
            column.AddChild(_center);
            column.AddChild(_log);
            column.AddChild(_right);
            _root = column;
        }

        AddChild(_root);
        _root.SetAnchorsPreset(LayoutPreset.FullRect);
    }

    private static void Detach(Node node)
    {
        node.GetParent()?.RemoveChild(node);
    }
}
