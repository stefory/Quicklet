using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Quicklet.Services;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Pen = System.Windows.Media.Pen;

namespace Quicklet;

public partial class SettingsWindow : Window
{
    private Config _config = new();
    private ObservableCollection<KeywordRule> _rules = new();
    private string _tempHotkey = string.Empty;
    private bool _isRecordingHotkey = false;

    public SettingsWindow()
    {
        InitializeComponent();
        try
        {
            this.Icon = CreateAppIconSource();
        }
        catch
        {
            // 忽略图标绘制失败
        }
    }

    private ImageSource CreateAppIconSource()
    {
        DrawingVisual drawingVisual = new DrawingVisual();
        using (DrawingContext drawingContext = drawingVisual.RenderOpen())
        {
            System.Windows.Media.Brush bgBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 32));
            drawingContext.DrawEllipse(bgBrush, null, new System.Windows.Point(16, 16), 16, 16);

            Pen bluePen = new Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 255)), 4);
            drawingContext.DrawEllipse(null, bluePen, new System.Windows.Point(16, 16), 10, 10);
        }

        RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(drawingVisual);
        return renderTargetBitmap;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadConfig();

        // 绑定规则表格
        _rules = new ObservableCollection<KeywordRule>(_config.Keywords);
        RulesDataGrid.ItemsSource = _rules;

        // 填充通用设置
        HotkeyTextBox.Text = _config.Hotkey;
        SelectDefaultEngineRadio();

        // 初始化主题单选按钮状态
        if (_config.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase))
        {
            RadioLightTheme.IsChecked = true;
        }
        else
        {
            RadioDarkTheme.IsChecked = true;
        }

        // 应用统一主题
        ApplyTheme(_config.Theme);

        // 渲染按键键帽
        UpdateHotkeyKeycaps(HotkeyTextBox.Text);

        // 初始化开机自启开关状态
        StartupCheckBox.IsChecked = ConfigService.IsStartupEnabled();
    }

    private void LoadConfig()
    {
        _config = ConfigService.LoadConfig();
    }

    private void SelectDefaultEngineRadio()
    {
        string url = _config.DefaultSearchUrl.Trim();
        if (url == "https://www.google.com/search?q={query}")
        {
            RadioGoogle.IsChecked = true;
        }
        else if (url == "https://www.baidu.com/s?wd={query}")
        {
            RadioBaidu.IsChecked = true;
        }
        else if (url == "https://www.bing.com/search?q={query}")
        {
            RadioBing.IsChecked = true;
        }
        else
        {
            RadioCustom.IsChecked = true;
            DefaultSearchTextBox.Text = url;
        }
    }

    private void SearchEngine_Checked(object sender, RoutedEventArgs e)
    {
        if (CustomUrlContainer == null || DefaultSearchTextBox == null || 
            RadioGoogle == null || RadioBaidu == null || RadioBing == null || RadioCustom == null)
            return;

        if (RadioGoogle.IsChecked == true)
        {
            DefaultSearchTextBox.Text = "https://www.google.com/search?q={query}";
            ToggleCustomUrlVisibility(false);
        }
        else if (RadioBaidu.IsChecked == true)
        {
            DefaultSearchTextBox.Text = "https://www.baidu.com/s?wd={query}";
            ToggleCustomUrlVisibility(false);
        }
        else if (RadioBing.IsChecked == true)
        {
            DefaultSearchTextBox.Text = "https://www.bing.com/search?q={query}";
            ToggleCustomUrlVisibility(false);
        }
        else if (RadioCustom.IsChecked == true)
        {
            ToggleCustomUrlVisibility(true);
        }
    }

    private void ToggleCustomUrlVisibility(bool visible)
    {
        if (CustomUrlContainer != null)
        {
            CustomUrlContainer.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_isRecordingHotkey) return;

        e.Handled = true;

        var modifiers = Keyboard.Modifiers;
        Key key = e.Key;

        if (key == Key.System)
        {
            key = e.SystemKey;
        }

        // 排除单独按修饰键的情况
        if (key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        var sb = new StringBuilder();
        if (modifiers.HasFlag(ModifierKeys.Control)) sb.Append("Ctrl+");
        if (modifiers.HasFlag(ModifierKeys.Alt)) sb.Append("Alt+");
        if (modifiers.HasFlag(ModifierKeys.Shift)) sb.Append("Shift+");
        if (modifiers.HasFlag(ModifierKeys.Windows)) sb.Append("Win+");

        string keyName = key.ToString();
        if (key == Key.Space) keyName = "Space";

        sb.Append(keyName);
        _tempHotkey = sb.ToString();
        
        HotkeyTextBox.Text = _tempHotkey;
        _isRecordingHotkey = false;
        UpdateHotkeyKeycaps(_tempHotkey);
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_isRecordingHotkey)
        {
            var pos = e.GetPosition(HotkeyKeycapsPanel);
            if (pos.X < 0 || pos.Y < 0 || pos.X > HotkeyKeycapsPanel.ActualWidth || pos.Y > HotkeyKeycapsPanel.ActualHeight)
            {
                _isRecordingHotkey = false;
                UpdateHotkeyKeycaps(HotkeyTextBox.Text);
            }
        }
    }

    private void Theme_Checked(object sender, RoutedEventArgs e)
    {
        if (RadioLightTheme == null || RadioDarkTheme == null || HotkeyTextBox == null)
            return;

        string theme = RadioLightTheme.IsChecked == true ? "Light" : "Dark";
        ApplyTheme(theme);
        UpdateHotkeyKeycaps(HotkeyTextBox.Text);
    }

    private void ApplyTheme(string themeName)
    {
        ThemeService.ApplyTheme(this.Resources, themeName);
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        var newRule = new KeywordRule
        {
            Keyword = "new",
            Name = "新网页",
            Url = "https://",
            SearchUrl = ""
        };
        _rules.Add(newRule);

        RulesDataGrid.SelectedItem = newRule;
        RulesDataGrid.ScrollIntoView(newRule);
    }

    private void DeleteRow_Click(object sender, RoutedEventArgs e)
    {
        if (RulesDataGrid.SelectedItems != null && RulesDataGrid.SelectedItems.Count > 0)
        {
            var selectedRules = RulesDataGrid.SelectedItems.Cast<KeywordRule>().ToList();
            foreach (var rule in selectedRules)
            {
                _rules.Remove(rule);
            }
        }
        else
        {
            CustomMessageBox.Show(this, "请先选择需要删除的行！", "提示", "💡");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string hotkey = HotkeyTextBox.Text.Trim();
        if (string.IsNullOrEmpty(hotkey) || hotkey == "请直接按下组合键...")
        {
            CustomMessageBox.Show(this, "快捷键不能为空！", "错误", "❌");
            return;
        }

        // 验证快捷键是否能正常解析
        if (!HotkeyService.ParseHotkey(hotkey, out _, out _, out string hotkeyErr))
        {
            CustomMessageBox.Show(this, $"快捷键格式无效: {hotkeyErr}", "错误", "❌");
            return;
        }

        string defaultSearch = DefaultSearchTextBox.Text.Trim();
        if (string.IsNullOrEmpty(defaultSearch))
        {
            CustomMessageBox.Show(this, "默认搜索引擎 URL 不能为空！", "错误", "❌");
            return;
        }

        // 保存到 config 实体
        _config.Hotkey = hotkey;
        _config.DefaultSearchUrl = defaultSearch;
        _config.Theme = RadioLightTheme.IsChecked == true ? "Light" : "Dark";
        
        // 过滤空关键字并保存列表
        _config.Keywords = _rules.Where(r => !string.IsNullOrWhiteSpace(r.Keyword)).ToList();

        // 写入注册表自启动
        ConfigService.SetStartup(StartupCheckBox.IsChecked == true);

        // 保存文件
        if (ConfigService.SaveConfig(_config))
        {
            // 通知主窗口更新配置（热重载）
            if (Application.Current.MainWindow is MainWindow mainWin)
            {
                mainWin.LoadConfig();
            }

            CustomMessageBox.Show(this, "配置保存成功，并且已经即时应用生效！", "Quicklet", "💡");
            this.Close();
        }
        else
        {
            CustomMessageBox.Show(this, "写入配置文件失败，请检查文件权限。", "错误", "❌");
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            this.DragMove();
        }
    }

    private void UpdateHotkeyKeycaps(string hotkey)
    {
        if (HotkeyKeycapsPanel == null) return;
        HotkeyKeycapsPanel.Children.Clear();
        var parts = hotkey.Split('+');
        
        string themeSetting = RadioLightTheme.IsChecked == true ? "Light" : "Dark";
        bool isDark = ThemeService.IsDarkTheme(themeSetting);
        
        var converter = new BrushConverter();
        var keyBg = (Brush)converter.ConvertFromString(isDark ? "#3A3A3C" : "#FFFFFF")!;
        var keyBorder = (Brush)converter.ConvertFromString(isDark ? "#48484A" : "#D1D1D6")!;
        var textBrush = (Brush)converter.ConvertFromString(isDark ? "#FFFFFF" : "#1C1C1E")!;
        var plusBrush = (Brush)converter.ConvertFromString(isDark ? "#AEAEB2" : "#8E8E93")!;

        for (int i = 0; i < parts.Length; i++)
        {
            if (i > 0)
            {
                HotkeyKeycapsPanel.Children.Add(new TextBlock 
                { 
                    Text = " + ", 
                    VerticalAlignment = VerticalAlignment.Center, 
                    Foreground = plusBrush,
                    FontWeight = FontWeights.Medium,
                    FontSize = 14,
                    Margin = new Thickness(6, 0, 6, 0)
                });
            }
            
            var border = new Border
            {
                Background = keyBg,
                BorderBrush = keyBorder,
                BorderThickness = new Thickness(1, 1, 1, 2),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 6, 12, 6),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            border.Child = new TextBlock
            {
                Text = parts[i],
                FontWeight = FontWeights.Medium,
                FontSize = 13,
                Foreground = textBrush
            };
            
            HotkeyKeycapsPanel.Children.Add(border);
        }
    }

    private void HotkeyKeycapsPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _isRecordingHotkey = true;
        _tempHotkey = HotkeyTextBox.Text;
        
        ShowRecordingState();
        this.Focus();
    }

    private void ShowRecordingState()
    {
        HotkeyKeycapsPanel.Children.Clear();
        
        var border = new Border
        {
            Background = (Brush)this.Resources["SelectedItemBackgroundBrush"],
            BorderBrush = (Brush)this.Resources["AccentBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 4, 12, 4),
            Margin = new Thickness(4, 0, 4, 0)
        };

        var textBlock = new TextBlock
        {
            Text = "⌨️ 请直接按下组合键...",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)this.Resources["AccentBrush"]
        };

        border.Child = textBlock;
        HotkeyKeycapsPanel.Children.Add(border);
    }
}
