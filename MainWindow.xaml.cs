using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Quicklet.Services;
using Application = System.Windows.Application;

namespace Quicklet;

public partial class MainWindow : Window
{
    private HwndSource? _hwndSource;
    private Config _config = new();
    private string _currentRegisteredHotkey = string.Empty;
    private readonly DispatcherTimer _debounceTimer;

    public MainWindow()
    {
        InitializeComponent();
        
        // 初始隐藏窗口，不显示在任务栏
        this.Visibility = Visibility.Hidden;

        // 初始化搜索防抖定时器 (100ms)
        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _debounceTimer.Tick += (s, e) =>
        {
            _debounceTimer.Stop();
            PerformUpdateSuggestions();
        };
        
        // 加载配置并应用主题
        LoadConfig();

        // 强行创建 HWND 句柄，解决冷启动第一次按热键无效的 Bug
        new WindowInteropHelper(this).EnsureHandle();

        // 绑定全局热键
        RegisterOrUpdateHotkey();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 初始加载由 ShowAndFocus 或构造处理
    }

    private void PositionWindow()
    {
        // 1. 如果用户曾经拖动过窗口且位置有效，优先还原记忆的位置
        if (_config.WindowLeft.HasValue && _config.WindowTop.HasValue)
        {
            double left = _config.WindowLeft.Value;
            double top = _config.WindowTop.Value;
            if (IsPositionOnAnyScreen(left, top))
            {
                this.Left = left;
                this.Top = top;
                return;
            }
        }

        // 2. 未拖动过或记忆的位置超屏时，默认在当前鼠标光标所在显示器上方 15% 处居中显示
        try
        {
            var mousePos = System.Windows.Forms.Cursor.Position;
            var currentScreen = System.Windows.Forms.Screen.FromPoint(mousePos);
            var bounds = currentScreen.WorkingArea;

            // 转换设备无关单位 (DPI)
            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                dpiScaleX = source.CompositionTarget.TransformFromDevice.M11;
                dpiScaleY = source.CompositionTarget.TransformFromDevice.M22;
            }

            double screenLeft = bounds.Left * dpiScaleX;
            double screenTop = bounds.Top * dpiScaleY;
            double screenWidth = bounds.Width * dpiScaleX;
            double screenHeight = bounds.Height * dpiScaleY;

            this.Left = screenLeft + (screenWidth - this.Width) / 2;
            this.Top = screenTop + screenHeight * 0.15; // 屏幕上方 15% 处
        }
        catch
        {
            // 兜底方案
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            this.Left = (screenWidth - this.Width) / 2;
            this.Top = screenHeight * 0.15;
        }
    }

    private bool IsPositionOnAnyScreen(double left, double top)
    {
        try
        {
            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;

            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }
            else
            {
                using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                {
                    dpiScaleX = g.DpiX / 96.0;
                    dpiScaleY = g.DpiY / 96.0;
                }
            }

            // 将 WPF 设备无关坐标 (DPI) 精准转换为物理像素 (Pixel)
            int pixelX = (int)(left * dpiScaleX);
            int pixelY = (int)(top * dpiScaleY);
            var pt = new System.Drawing.Point(pixelX, pixelY);

            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                if (screen.WorkingArea.Contains(pt))
                {
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    public void ShowAndFocus()
    {
        // 1. 载入最新磁盘配置
        LoadConfig();

        // 2. 显示并激活窗口
        this.Show();
        this.Visibility = Visibility.Visible;
        this.WindowState = WindowState.Normal;

        // 3. 精准应用拖动保存的坐标或居中定位
        PositionWindow();

        // 强行获取焦点
        this.Activate();
        SearchInput.Focus();
        SearchInput.SelectAll();
    }

    public void HideWindow()
    {
        _debounceTimer.Stop();
        this.Visibility = Visibility.Hidden;
        SearchInput.Text = string.Empty;

        // 彻底复位 UI 组件状态
        SuggestionList.ItemsSource = null;
        SuggestionList.Visibility = Visibility.Collapsed;
        Divider.Visibility = Visibility.Collapsed;
        Footer.Visibility = Visibility.Collapsed;
        PlaceholderText.Visibility = Visibility.Visible;
    }

    private void ToggleWindow()
    {
        if (this.Visibility == Visibility.Visible && this.IsActive)
        {
            HideWindow();
        }
        else
        {
            ShowAndFocus();
        }
    }

    public void LoadConfig()
    {
        _config = ConfigService.LoadConfig();

        // 应用主题色
        ThemeService.ApplyTheme(this.Resources, _config.Theme);

        // 重新注册全局快捷键
        RegisterOrUpdateHotkey();

        // 同步更新托盘菜单显示的快捷键文本及主题色
        if (Application.Current is App app)
        {
            app.UpdateTrayText(_config.Hotkey);
            app.UpdateTrayTheme(_config.Theme);
        }
    }

    private void RegisterOrUpdateHotkey()
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle == IntPtr.Zero)
            return;

        if (_currentRegisteredHotkey == _config.Hotkey)
            return;
        
        // 先解绑已有热键
        if (!string.IsNullOrEmpty(_currentRegisteredHotkey))
        {
            HotkeyService.Unregister(helper.Handle);
            _currentRegisteredHotkey = string.Empty;
        }

        if (_hwndSource == null)
        {
            _hwndSource = HwndSource.FromHwnd(helper.Handle);
            _hwndSource.AddHook(HwndHook);
        }

        if (HotkeyService.Register(helper.Handle, _config.Hotkey, out string errorMsg))
        {
            _currentRegisteredHotkey = _config.Hotkey;
        }
        else
        {
            CustomMessageBox.Show(this, errorMsg, "Quicklet 热键提示", "⚠️");
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmHotkey = 0x0312;
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyService.HotkeyId)
        {
            ToggleWindow();
            handled = true;
        }
        return IntPtr.Zero;
    }

    // 搜索输入框内容改变，触发表格防抖
    private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        string input = SearchInput.Text.Trim();
        if (string.IsNullOrEmpty(input))
        {
            // 输入框为空时，0ms 立即同步收起，防止防抖异步延迟导致 Divider 和 Footer 误残留显示
            _debounceTimer.Stop();
            SuggestionList.ItemsSource = null;
            SuggestionList.Visibility = Visibility.Collapsed;
            Divider.Visibility = Visibility.Collapsed;
            Footer.Visibility = Visibility.Collapsed;
            PlaceholderText.Visibility = Visibility.Visible;
            return;
        }

        PlaceholderText.Visibility = Visibility.Collapsed;

        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void PerformUpdateSuggestions()
    {
        string input = SearchInput.Text.Trim();
        if (string.IsNullOrEmpty(input))
        {
            SuggestionList.Visibility = Visibility.Collapsed;
            Divider.Visibility = Visibility.Collapsed;
            Footer.Visibility = Visibility.Collapsed;
            PlaceholderText.Visibility = Visibility.Visible;
            return;
        }

        PlaceholderText.Visibility = Visibility.Collapsed;

        var list = SearchEngine.GetSuggestions(input, _config);
        SuggestionList.ItemsSource = list;

        if (list.Count > 0)
        {
            SuggestionList.Visibility = Visibility.Visible;
            Divider.Visibility = Visibility.Visible;
            Footer.Visibility = Visibility.Visible;
            SuggestionList.SelectedIndex = 0; // 默认选中首项
        }
        else
        {
            SuggestionList.Visibility = Visibility.Collapsed;
            Divider.Visibility = Visibility.Collapsed;
            Footer.Visibility = Visibility.Collapsed;
        }
    }

    private void ExecuteSelectedSuggestion()
    {
        if (SuggestionList.SelectedItem is SuggestionItem selected)
        {
            string url = selected.TargetUrl;
            if (!string.IsNullOrEmpty(url))
            {
                OpenBrowser(url);
                HideWindow();
            }
        }
        else if (!string.IsNullOrEmpty(SearchInput.Text))
        {
            string defaultPattern = string.IsNullOrEmpty(_config.DefaultSearchUrl) 
                ? "https://www.google.com/search?q={query}" 
                : _config.DefaultSearchUrl;
            string targetUrl = defaultPattern.Replace("{query}", Uri.EscapeDataString(SearchInput.Text.Trim()));
            OpenBrowser(targetUrl);
            HideWindow();
        }
    }

    private void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show(this, $"无法打开网页: {ex.Message}", "Quicklet 错误", "❌");
        }
    }

    // 键盘操作拦截
    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            HideWindow();
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            if (SuggestionList.Visibility == Visibility.Visible)
            {
                int newIndex = SuggestionList.SelectedIndex + 1;
                if (newIndex < SuggestionList.Items.Count)
                {
                    SuggestionList.SelectedIndex = newIndex;
                    SuggestionList.ScrollIntoView(SuggestionList.SelectedItem);
                }
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Up)
        {
            if (SuggestionList.Visibility == Visibility.Visible)
            {
                int newIndex = SuggestionList.SelectedIndex - 1;
                if (newIndex >= 0)
                {
                    SuggestionList.SelectedIndex = newIndex;
                    SuggestionList.ScrollIntoView(SuggestionList.SelectedItem);
                }
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Enter)
        {
            ExecuteSelectedSuggestion();
            e.Handled = true;
        }
        else if (e.Key == Key.Tab)
        {
            // Tab 自动补全关键字加空格
            if (SuggestionList.Visibility == Visibility.Visible && SuggestionList.SelectedItem is SuggestionItem selected)
            {
                string currentText = SearchInput.Text.Trim();
                if (currentText != selected.BadgeText && !string.IsNullOrEmpty(selected.BadgeText) && selected.BadgeText != "默认搜索")
                {
                    SearchInput.Text = selected.BadgeText + " ";
                    SearchInput.CaretIndex = SearchInput.Text.Length;
                    e.Handled = true;
                }
            }
        }
    }

    private void SuggestionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ExecuteSelectedSuggestion();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        HideWindow();
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            this.DragMove();

            // 拖动结束后自动记忆保存用户自定义的 Left/Top 屏幕坐标
            _config.WindowLeft = this.Left;
            _config.WindowTop = this.Top;
            ConfigService.SaveConfig(_config);
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        HotkeyService.Unregister(helper.Handle);
        
        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(HwndHook);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
    }
}