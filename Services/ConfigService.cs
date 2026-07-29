using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace Quicklet.Services;

public static class ConfigService
{
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    public static Config LoadConfig()
    {
        bool fileExists = File.Exists(ConfigPath);
        if (fileExists)
        {
            try
            {
                string json = File.ReadAllText(ConfigPath);
                var parsed = JsonSerializer.Deserialize<Config>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (parsed != null)
                {
                    parsed.Keywords ??= new();
                    return parsed;
                }
            }
            catch (Exception ex)
            {
                // 解析失败时，仅记录日志，保留磁盘上的损坏原文件以防抹除，仅在内存中返回默认配置
                Debug.WriteLine($"Failed to load config: {ex.Message}");
                var fallbackConfig = new Config();
                return fallbackConfig;
            }
        }

        // 仅在配置文件完全不存在时，生成默认配置并保存到磁盘
        var defaultConfig = new Config();
        SaveConfig(defaultConfig);
        return defaultConfig;
    }

    public static bool SaveConfig(Config config)
    {
        string tmpPath = ConfigPath + ".tmp";
        try
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            // 先写临时文件，再原子替换，防止写入中途崩溃损坏 json
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, ConfigPath, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save config: {ex.Message}");
            if (File.Exists(tmpPath))
            {
                try { File.Delete(tmpPath); } catch { }
            }
            return false;
        }
    }

    public static bool SetStartup(bool enable)
    {
        try
        {
            const string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            using (var key = Registry.CurrentUser.OpenSubKey(runKey, true))
            {
                if (key != null)
                {
                    if (enable)
                    {
                        string path = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                        if (!string.IsNullOrEmpty(path))
                        {
                            key.SetValue("Quicklet", $"\"{path}\"");
                        }
                    }
                    else
                    {
                        key.DeleteValue("Quicklet", false);
                    }
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to set startup registry: {ex.Message}");
        }
        return false;
    }

    public static bool IsStartupEnabled()
    {
        try
        {
            const string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            using (var key = Registry.CurrentUser.OpenSubKey(runKey, false))
            {
                if (key != null)
                {
                    return key.GetValue("Quicklet") != null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read startup registry: {ex.Message}");
        }
        return false;
    }

    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeFile);
    }
}
