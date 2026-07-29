using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Brush = System.Windows.Media.Brush;

namespace Quicklet.Services;

public static class ThemeService
{
    public static bool IsSystemDarkMode()
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value != null)
                    {
                        return (int)value == 0;
                    }
                }
            }
        }
        catch { }
        return false;
    }

    public static bool IsDarkTheme(string themeSetting)
    {
        if ("Light".Equals(themeSetting, StringComparison.OrdinalIgnoreCase))
            return false;
        if ("Dark".Equals(themeSetting, StringComparison.OrdinalIgnoreCase))
            return true;
        return IsSystemDarkMode(); // Auto
    }

    public static void ApplyTheme(ResourceDictionary resources, string themeSetting)
    {
        bool isDark = IsDarkTheme(themeSetting);

        // 统一为精美 Google Material Design 3 调色板
        string bg = isDark ? "#1C1B1F" : "#FBFCFF";          // M3 Window Background
        string border = isDark ? "#49454F" : "#C4C7C5";      // M3 Border Outline
        string subBg = isDark ? "#25242A" : "#F3F4F9";       // M3 Surface Container
        string text = isDark ? "#E6E1E5" : "#1C1B1F";        // M3 On Surface Text
        string mutedText = isDark ? "#CAC4D0" : "#49454F";   // M3 On Surface Variant
        string placeholder = isDark ? "#938F99" : "#79747E"; // M3 Muted Outline
        string selectedBg = isDark ? "#49454F" : "#E3E3E3";   // M3 Tonal Highlight
        string mouseOverBg = isDark ? "#323035" : "#F1F3F4";  // M3 Hover Surface
        string footerBg = isDark ? "#1C1B1F" : "#FBFCFF";    // M3 Footer Surface
        string accent = isDark ? "#A8C7FA" : "#1A73E8";      // M3 Primary Accent (Google Blue)
        string segmentedSelected = isDark ? "#3A3A3C" : "#FFFFFF";

        var converter = new BrushConverter();
        SetResource(resources, "WindowBackgroundBrush", bg, converter);
        SetResource(resources, "BorderBrush", border, converter);
        SetResource(resources, "SubBackgroundBrush", subBg, converter);
        SetResource(resources, "TextBrush", text, converter);
        SetResource(resources, "MutedTextBrush", mutedText, converter);
        SetResource(resources, "PlaceholderBrush", placeholder, converter);
        SetResource(resources, "SelectedItemBackgroundBrush", selectedBg, converter);
        SetResource(resources, "MouseOverItemBackgroundBrush", mouseOverBg, converter);
        SetResource(resources, "FooterBackgroundBrush", footerBg, converter);
        SetResource(resources, "AccentBrush", accent, converter);
        SetResource(resources, "SegmentedSelectedBrush", segmentedSelected, converter);
    }

    private static void SetResource(ResourceDictionary resources, string key, string hexColor, BrushConverter converter)
    {
        if (converter.ConvertFromString(hexColor) is Brush brush)
        {
            // 冻结 Brush 以提高 WPF 渲染性能
            if (brush.CanFreeze) brush.Freeze();
            resources[key] = brush;
        }
    }
}
