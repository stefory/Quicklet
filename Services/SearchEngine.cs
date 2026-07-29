using System;
using System.Collections.Generic;
using System.Linq;

namespace Quicklet.Services;

public static class SearchEngine
{
    public static List<SuggestionItem> GetSuggestions(string rawInput, Config config)
    {
        var list = new List<SuggestionItem>();
        string input = rawInput.Trim();
        if (string.IsNullOrEmpty(input) || config == null)
        {
            return list;
        }

        // 拆分出关键字与参数，例如 "g react"
        string[] parts = input.Split(new[] { ' ' }, 2);
        string firstWord = parts[0].ToLower();
        string query = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        // 1. 精确匹配第一词是否为关键字
        var exactRule = config.Keywords.FirstOrDefault(k => 
            !string.IsNullOrEmpty(k.Keyword) && k.Keyword.ToLower() == firstWord);

        if (exactRule != null)
        {
            if (string.IsNullOrEmpty(query))
            {
                // 仅输入了关键字本身，例如 "g" -> 打开 Google 首页
                list.Add(new SuggestionItem
                {
                    DisplayName = $"打开 {exactRule.Name}",
                    TargetUrl = exactRule.Url,
                    BadgeText = exactRule.Keyword,
                    IconData = SuggestionItem.LinkIconData,
                    IconColor = "#007AFF"
                });
            }
            else
            {
                // 输入了关键字 + 搜索词，例如 "g react" -> 用 Google 搜索 react
                string searchUrl = string.IsNullOrEmpty(exactRule.SearchUrl) ? exactRule.Url : exactRule.SearchUrl;
                string targetUrl = searchUrl.Replace("{query}", Uri.EscapeDataString(query));
                list.Add(new SuggestionItem
                {
                    DisplayName = $"在 {exactRule.Name} 搜索 \"{query}\"",
                    TargetUrl = targetUrl,
                    BadgeText = exactRule.Keyword,
                    IconData = SuggestionItem.SearchIconData,
                    IconColor = "#34C759"
                });
            }
        }

        // 2. 模糊匹配其他可能的名字/关键字
        var otherRules = config.Keywords
            .Where(k => (exactRule == null || k != exactRule) &&
                        !string.IsNullOrEmpty(k.Keyword) &&
                        (k.Keyword.StartsWith(input, StringComparison.OrdinalIgnoreCase) ||
                         (!string.IsNullOrEmpty(k.Name) && k.Name.Contains(input, StringComparison.OrdinalIgnoreCase))));

        foreach (var rule in otherRules)
        {
            list.Add(new SuggestionItem
            {
                DisplayName = $"打开 {rule.Name}",
                TargetUrl = rule.Url,
                BadgeText = rule.Keyword,
                IconData = SuggestionItem.LinkIconData,
                IconColor = "#007AFF"
            });
        }

        // 3. 兜底默认搜索引擎搜索
        if (exactRule == null)
        {
            string defaultUrlPattern = string.IsNullOrEmpty(config.DefaultSearchUrl) 
                ? "https://www.google.com/search?q={query}" 
                : config.DefaultSearchUrl;
                
            string targetUrl = defaultUrlPattern.Replace("{query}", Uri.EscapeDataString(input));
            list.Add(new SuggestionItem
            {
                DisplayName = $"直接搜索 \"{input}\"",
                TargetUrl = targetUrl,
                BadgeText = "默认搜索",
                IconData = SuggestionItem.SearchIconData,
                IconColor = "#A1A1AA"
            });
        }

        return list;
    }
}
