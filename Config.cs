using System.Collections.Generic;

namespace Quicklet;

public class Config
{
    public string DefaultSearchUrl { get; set; } = "https://www.google.com/search?q={query}";
    public string Hotkey { get; set; } = "Alt+Q";
    public string Theme { get; set; } = "Dark";
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public List<KeywordRule> Keywords { get; set; } = new();
}

public class KeywordRule
{
    public string Keyword { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SearchUrl { get; set; } = string.Empty;
}
