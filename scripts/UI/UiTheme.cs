using Godot;
using NasaValley.Rendering;

namespace NasaValley.UI;

/// <summary>
/// Factory helpers that translate the original CSS into Godot theme overrides.
/// Keeps every widget looking consistent without a hand-authored .tres theme.
/// </summary>
public static class UiTheme
{
    public static StyleBoxFlat Box(Color bg, Color? border = null, int borderWidth = 0, int radius = 0)
    {
        var box = new StyleBoxFlat { BgColor = bg };
        if (border.HasValue && borderWidth > 0)
        {
            box.BorderColor = border.Value;
            box.SetBorderWidthAll(borderWidth);
        }
        if (radius > 0) box.SetCornerRadiusAll(radius);
        return box;
    }

    public static StyleBoxFlat Card(int pad = 14)
    {
        var box = Box(Palette.Panel, Palette.Line, 1);
        box.ContentMarginLeft = pad;
        box.ContentMarginRight = pad;
        box.ContentMarginTop = pad;
        box.ContentMarginBottom = pad;
        return box;
    }

    public static PanelContainer CardPanel(int pad = 14)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", Card(pad));
        return panel;
    }

    public static Label Text(string text, int size, Color color, Font font)
    {
        var label = new Label { Text = text };
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    public static Label Label(string text, int size, Color color, Font? font = null) =>
        Text(text, size, color, font ?? Fonts.Sans);

    public static Label Caption(string text)
    {
        var label = Text(text.ToUpperInvariant(), 11, Palette.Muted, Fonts.Display);
        return label;
    }

    public static Button PrimaryButton(string text, int fontSize = 13)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.All };
        ApplyButton(button, Palette.Dust, Palette.Ink, Palette.DustDeep, fontSize);
        return button;
    }

    public static Button GhostButton(string text, int fontSize = 13)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.All };
        var normal = Box(new Color(0, 0, 0, 0), Palette.Line, 1);
        normal.ContentMarginLeft = 14;
        normal.ContentMarginRight = 14;
        normal.ContentMarginTop = 8;
        normal.ContentMarginBottom = 8;
        var hover = Box(new Color(0, 0, 0, 0), Palette.Dust, 1);
        hover.ContentMarginLeft = 14;
        hover.ContentMarginRight = 14;
        hover.ContentMarginTop = 8;
        hover.ContentMarginBottom = 8;
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.AddThemeStyleboxOverride("focus", FocusBox());
        button.AddThemeColorOverride("font_color", Palette.Suit);
        button.AddThemeColorOverride("font_hover_color", Palette.Suit);
        button.AddThemeFontOverride("font", Fonts.SansWeight(600));
        button.AddThemeFontSizeOverride("font_size", fontSize);
        return button;
    }

    public static Button SupportButton(string title, string subtitle)
    {
        var button = new Button { Text = title, FocusMode = Control.FocusModeEnum.All };
        var normal = Box(Palette.Panel2, Palette.Line, 1);
        Pad(normal, 10, 8);
        var hover = Box(Palette.Panel2, Palette.O2, 1);
        Pad(hover, 10, 8);
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.AddThemeStyleboxOverride("disabled", normal);
        button.AddThemeStyleboxOverride("focus", FocusBox());
        button.AddThemeColorOverride("font_color", Palette.Suit);
        button.AddThemeColorOverride("font_hover_color", Palette.Suit);
        button.AddThemeFontOverride("font", Fonts.SansWeight(600));
        button.AddThemeFontSizeOverride("font_size", 13);
        button.TooltipText = subtitle;
        return button;
    }

    public static void ApplyButton(Button button, Color bg, Color fg, Color shadow, int fontSize)
    {
        var normal = Box(bg);
        Pad(normal, 14, 8);
        normal.ShadowColor = shadow;
        normal.ShadowSize = 2;
        normal.ShadowOffset = new Vector2(0, 2);

        var hover = Box(bg.Lightened(0.08f));
        Pad(hover, 14, 8);
        hover.ShadowColor = shadow;
        hover.ShadowSize = 2;
        hover.ShadowOffset = new Vector2(0, 2);

        var pressed = Box(bg.Darkened(0.1f));
        Pad(pressed, 14, 8);

        var disabled = Box(Palette.Panel2);
        Pad(disabled, 14, 8);

        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("disabled", disabled);
        button.AddThemeStyleboxOverride("focus", FocusBox());
        button.AddThemeColorOverride("font_color", fg);
        button.AddThemeColorOverride("font_hover_color", fg);
        button.AddThemeColorOverride("font_pressed_color", fg);
        button.AddThemeColorOverride("font_disabled_color", Palette.Muted);
        button.AddThemeFontOverride("font", Fonts.SansWeight(600));
        button.AddThemeFontSizeOverride("font_size", fontSize);
    }

    public static StyleBoxFlat FocusBox()
    {
        var box = new StyleBoxFlat { DrawCenter = false, BorderColor = Palette.O2 };
        box.SetBorderWidthAll(2);
        return box;
    }

    public static void Pad(StyleBoxFlat box, int horizontal, int vertical)
    {
        box.ContentMarginLeft = horizontal;
        box.ContentMarginRight = horizontal;
        box.ContentMarginTop = vertical;
        box.ContentMarginBottom = vertical;
    }

    public static VBoxContainer VBox(int separation = 8)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", separation);
        return box;
    }

    public static HBoxContainer HBox(int separation = 8)
    {
        var box = new HBoxContainer();
        box.AddThemeConstantOverride("separation", separation);
        return box;
    }
}
