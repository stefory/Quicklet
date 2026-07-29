using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace Quicklet;

public partial class App : Application
{
    private static Mutex? _mutex;
    private NotifyIcon? _notifyIcon;

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. 全局未捕获异常处理
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            ReleaseMutexSafely();
        };

        // 2. 单例检测
        bool isNewInstance = false;
        try
        {
            _mutex = new Mutex(true, "Quicklet_SingleInstanceMutex", out isNewInstance);
        }
        catch (AbandonedMutexException)
        {
            isNewInstance = true;
        }

        if (!isNewInstance)
        {
            // 已经有一个实例在运行，直接退出
            ReleaseMutexSafely();
            Shutdown();
            return;
        }

        base.OnStartup(e);
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            // 3. 初始化托盘图标
            CreateTrayIcon();

            // 4. 实例化主窗口但不显示（后台运行）
            MainWindow = new MainWindow();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"App OnStartup Exception: {ex.Message}");
        }
    }

    private void ReleaseMutexSafely()
    {
        if (_mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch { }
            _mutex.Dispose();
            _mutex = null;
        }
    }

    private void CreateTrayIcon()
    {
        _notifyIcon = new NotifyIcon();
        _notifyIcon.Text = "Quicklet - 网页快捷启动器 (Alt+Q)";

        // 优先获取可执行文件自身内嵌的 app.ico 主图标
        try
        {
            string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                _notifyIcon.Icon = Icon.ExtractAssociatedIcon(exePath);
            }
        }
        catch { }

        if (_notifyIcon.Icon == null)
        {
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoPath))
                {
                    _notifyIcon.Icon = new Icon(icoPath);
                }
                else
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }
        }

        // 托盘右键菜单
        var contextMenu = new ContextMenuStrip();
        contextMenu.ShowCheckMargin = false;
        contextMenu.ShowImageMargin = false; // 完全隐藏左侧空白图标区域

        var showItem = new ToolStripMenuItem("显示搜索框 (Alt+Q)", null, (s, e) => ShowMainWindow());
        var settingsItem = new ToolStripMenuItem("设置中心", null, (s, e) => OpenSettingsWindow());
        var exitItem = new ToolStripMenuItem("退出", null, (s, e) => ShutdownApp());

        // 增加内边距，呈现现代宽大舒适的交互质感
        showItem.Padding = new Padding(16, 6, 16, 6);
        settingsItem.Padding = new Padding(16, 6, 16, 6);
        exitItem.Padding = new Padding(16, 6, 16, 6);

        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.Visible = true;

        // 双击托盘图标显示窗口
        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
    }

    public void UpdateTrayTheme(string themeName)
    {
        if (_notifyIcon != null && _notifyIcon.ContextMenuStrip != null)
        {
            bool isDark = Quicklet.Services.ThemeService.IsDarkTheme(themeName);
            
            // 应用自定义渲染器
            _notifyIcon.ContextMenuStrip.Renderer = new QuickletMenuRenderer(isDark);
            
            // 设值基本颜色以兼容部分系统主题边缘重绘
            Color backColor = isDark ? Color.FromArgb(30, 30, 32) : Color.FromArgb(245, 245, 247);
            Color foreColor = isDark ? Color.FromArgb(228, 228, 231) : Color.FromArgb(28, 28, 30);
            
            _notifyIcon.ContextMenuStrip.BackColor = backColor;
            _notifyIcon.ContextMenuStrip.ForeColor = foreColor;
        }
    }

    public void ShowMainWindow()
    {
        var mainWindow = MainWindow as MainWindow;
        if (mainWindow != null)
        {
            mainWindow.ShowAndFocus();
        }
    }

    public void UpdateTrayText(string hotkey)
    {
        if (_notifyIcon != null)
        {
            // Windows 托盘 Tooltip 限制为 63 字符
            string text = $"Quicklet - 网页快捷启动器 ({hotkey})";
            if (text.Length > 63)
            {
                text = text.Substring(0, 60) + "...";
            }
            _notifyIcon.Text = text;

            if (_notifyIcon.ContextMenuStrip != null && _notifyIcon.ContextMenuStrip.Items.Count > 0)
            {
                _notifyIcon.ContextMenuStrip.Items[0].Text = $"显示搜索框 ({hotkey})";
            }
        }
    }

    private void OpenSettingsWindow()
    {
        var openWindow = Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
        if (openWindow != null)
        {
            openWindow.Activate();
        }
        else
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Show();
        }
    }

    private void ShutdownApp()
    {
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        ReleaseMutexSafely();

        base.OnExit(e);
    }
}

public class QuickletColorTable : ProfessionalColorTable
{
    private readonly bool _isDark;

    public QuickletColorTable(bool isDark)
    {
        _isDark = isDark;
    }

    public override Color ToolStripDropDownBackground => _isDark ? Color.FromArgb(30, 30, 32) : Color.FromArgb(245, 245, 247);
    public override Color MenuBorder => _isDark ? Color.FromArgb(46, 46, 50) : Color.FromArgb(229, 229, 234);
    public override Color MenuItemSelected => _isDark ? Color.FromArgb(42, 42, 45) : Color.FromArgb(232, 240, 254);
    public override Color MenuItemSelectedGradientBegin => _isDark ? Color.FromArgb(42, 42, 45) : Color.FromArgb(232, 240, 254);
    public override Color MenuItemSelectedGradientEnd => _isDark ? Color.FromArgb(42, 42, 45) : Color.FromArgb(232, 240, 254);
    public override Color MenuItemBorder => Color.Transparent;
    
    public override Color ImageMarginGradientBegin => _isDark ? Color.FromArgb(30, 30, 32) : Color.FromArgb(245, 245, 247);
    public override Color ImageMarginGradientMiddle => _isDark ? Color.FromArgb(30, 30, 32) : Color.FromArgb(245, 245, 247);
    public override Color ImageMarginGradientEnd => _isDark ? Color.FromArgb(30, 30, 32) : Color.FromArgb(245, 245, 247);

    public override Color SeparatorDark => _isDark ? Color.FromArgb(46, 46, 50) : Color.FromArgb(229, 229, 234);
    public override Color SeparatorLight => Color.Transparent;
}

public class QuickletMenuRenderer : ToolStripProfessionalRenderer
{
    private readonly bool _isDark;

    public QuickletMenuRenderer(bool isDark) : base(new QuickletColorTable(isDark))
    {
        _isDark = isDark;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (e.Item == null) return;

        e.TextColor = _isDark ? Color.FromArgb(228, 228, 231) : Color.FromArgb(28, 28, 30);
        e.TextFont = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular);
        e.TextFormat |= TextFormatFlags.VerticalCenter;

        // 计算居中的 Y 坐标偏移以实现完美的上下垂直居中
        int textHeight = TextRenderer.MeasureText(e.Text, e.TextFont).Height;
        int verticalOffset = (e.Item.Height - textHeight) / 2;

        Rectangle rect = e.TextRectangle;
        rect.Y = verticalOffset;
        e.TextRectangle = rect;

        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        if (e.Item == null || e.ToolStrip == null) return;
        Color sepColor = _isDark ? Color.FromArgb(46, 46, 50) : Color.FromArgb(229, 229, 234);
        using (var pen = new Pen(sepColor, 1))
        {
            int y = e.Item.ContentRectangle.Top + e.Item.ContentRectangle.Height / 2;
            e.Graphics.DrawLine(pen, 10, y, e.ToolStrip.Width - 10, y);
        }
    }
}
