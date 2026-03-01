using System.Collections.Generic;
using System.Windows.Media;

namespace Raffe.Models;

public class ThemeSchema
{
    public string Id { get; }
    public string Name { get; }
    public string CenterColor { get; }
    public string EdgeColor { get; }
    public string DarkEdgeColor { get; }

    private ThemeSchema(string id, string name, string center, string edge, string? darkEdge = null)
    {
        Id = id;
        Name = name;
        CenterColor = center;
        EdgeColor = edge;
        DarkEdgeColor = darkEdge ?? edge;
    }

    public static readonly IReadOnlyList<ThemeSchema> All = new[]
    {
        new ThemeSchema("R1", "中国红", "#B83020", "#6B1818", "#2A0808"),
        new ThemeSchema("R2", "朱砂红", "#CC4030", "#8B2020", "#350C0C"),
        new ThemeSchema("R3", "橙红（推荐）", "#D85C28", "#8B3018", "#4A180C"),
        new ThemeSchema("B1", "深蓝", "#2060A0", "#102040", "#081828"),
        new ThemeSchema("B2", "宝石蓝", "#2870C0", "#103050", "#0A1830"),
        new ThemeSchema("B3", "青蓝", "#3088C8", "#184060", "#0C2040"),
        new ThemeSchema("Y1", "金棕", "#C8A030", "#604818", "#302408"),
        new ThemeSchema("Y2", "琥珀", "#D89038", "#705020", "#382810"),
        new ThemeSchema("Y3", "暗金", "#A07828", "#503818", "#281808"),
        new ThemeSchema("P1", "深紫", "#6040A0", "#281840", "#140C20"),
    };

    public static ThemeSchema Get(string id)
    {
        foreach (var t in All)
            if (t.Id == id) return t;
        return Get("R3");
    }

    /// <summary>DWM caption color in 0x00BBGGRR format.</summary>
    public static int GetCaptionColorBgr(string themeId)
    {
        var c = (Color)ColorConverter.ConvertFromString(Get(themeId).EdgeColor);
        return (c.B << 16) | (c.G << 8) | c.R;
    }

    public static void Apply(string themeId)
    {
        var theme = Get(themeId);
        var center = (Color)ColorConverter.ConvertFromString(theme.CenterColor);
        var edge = (Color)ColorConverter.ConvertFromString(theme.EdgeColor);
        var darkEdge = (Color)ColorConverter.ConvertFromString(theme.DarkEdgeColor);

        var app = System.Windows.Application.Current;
        if (app == null) return;

        app.Resources["ThemeMainBackground"] = new RadialGradientBrush(center, edge)
        {
            Center = new System.Windows.Point(0.5, 0.3),
            RadiusX = 0.8,
            RadiusY = 0.7
        };
        app.Resources["ThemeWindowBackground"] = new SolidColorBrush(edge);
        app.Resources["ThemeResultsBackground"] = new RadialGradientBrush(center, darkEdge)
        {
            Center = new System.Windows.Point(0.5, 0.3),
            RadiusX = 0.9,
            RadiusY = 0.8
        };
    }
}
