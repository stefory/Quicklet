using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Quicklet.Services;

public static class HotkeyService
{
    public const int HotkeyId = 9000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;

    public static bool ParseHotkey(string hotkeyStr, out uint modifiers, out uint vk, out string errorMessage)
    {
        modifiers = 0;
        vk = 0;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(hotkeyStr))
        {
            errorMessage = "快捷键不能为空。";
            return false;
        }

        var parts = hotkeyStr.Split('+').Select(p => p.Trim()).ToList();
        if (parts.Count == 0)
        {
            errorMessage = "快捷键格式无效。";
            return false;
        }

        string mainKeyStr = parts.Last().ToUpper();
        parts.RemoveAt(parts.Count - 1);

        // 解析修饰键
        foreach (var mod in parts)
        {
            if (mod.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                modifiers |= MOD_ALT;
            else if (mod.Equals("Control", StringComparison.OrdinalIgnoreCase) || mod.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
                modifiers |= MOD_CONTROL;
            else if (mod.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                modifiers |= MOD_SHIFT;
            else if (mod.Equals("Win", StringComparison.OrdinalIgnoreCase) || mod.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                modifiers |= MOD_WIN;
            else
            {
                errorMessage = $"无法识别修饰键: {mod}";
                return false;
            }
        }

        // 解析主键
        if (TryParseMainKey(mainKeyStr, out vk))
        {
            return true;
        }

        errorMessage = $"无法识别主按键: {mainKeyStr}";
        return false;
    }

    private static bool TryParseMainKey(string keyStr, out uint vk)
    {
        vk = 0;

        // 单个字母或数字 (A-Z, 0-9)
        if (keyStr.Length == 1)
        {
            char c = keyStr[0];
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
            {
                vk = (uint)c;
                return true;
            }
        }

        // F1 - F12
        if (keyStr.StartsWith("F") && int.TryParse(keyStr.Substring(1), out int fNum) && fNum >= 1 && fNum <= 12)
        {
            vk = (uint)(0x70 + (fNum - 1)); // VK_F1 = 0x70
            return true;
        }

        // 常用特殊按键
        switch (keyStr)
        {
            case "SPACE": vk = 0x20; return true;
            case "TAB": vk = 0x09; return true;
            case "ENTER": case "RETURN": vk = 0x0D; return true;
            case "ESC": case "ESCAPE": vk = 0x1B; return true;
            case "BACKSPACE": case "BACK": vk = 0x08; return true;
            case "DELETE": case "DEL": vk = 0x2E; return true;
            case "INSERT": case "INS": vk = 0x2D; return true;
            case "HOME": vk = 0x24; return true;
            case "END": vk = 0x23; return true;
            case "PAGEUP": case "PGUP": vk = 0x21; return true;
            case "PAGEDOWN": case "PGDN": vk = 0x22; return true;
            case "UP": vk = 0x26; return true;
            case "DOWN": vk = 0x28; return true;
            case "LEFT": vk = 0x25; return true;
            case "RIGHT": vk = 0x27; return true;
        }

        // WPF Key Enum 兜底
        if (Enum.TryParse<Key>(keyStr, true, out var key))
        {
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            if (virtualKey > 0)
            {
                vk = (uint)virtualKey;
                return true;
            }
        }

        return false;
    }

    public static bool Register(IntPtr hWnd, string hotkeyStr, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (!ParseHotkey(hotkeyStr, out uint modifiers, out uint vk, out errorMessage))
        {
            return false;
        }

        bool success = RegisterHotKey(hWnd, HotkeyId, modifiers, vk);
        if (!success)
        {
            int errCode = Marshal.GetLastWin32Error();
            errorMessage = $"快捷键 {hotkeyStr} 注册失败 (Win32错误码: {errCode})，可能已被其他程序占用。";
            return false;
        }

        return true;
    }

    public static bool Unregister(IntPtr hWnd)
    {
        return UnregisterHotKey(hWnd, HotkeyId);
    }
}
