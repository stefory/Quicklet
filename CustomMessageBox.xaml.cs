using System.Windows;
using Quicklet.Services;
using Application = System.Windows.Application;

namespace Quicklet;

public partial class CustomMessageBox : Window
{
    private CustomMessageBox(Window? owner, string message, string title, string icon)
    {
        InitializeComponent();
        
        if (owner != null)
        {
            this.Owner = owner;
        }

        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
        IconTextBlock.Text = icon;

        // 优先复用当前窗口已渲染的调色板资源，消灭弹窗时的重复磁盘 IO
        var sourceResources = owner?.Resources ?? Application.Current?.MainWindow?.Resources;
        if (sourceResources != null && sourceResources.Count > 0)
        {
            foreach (var key in sourceResources.Keys)
            {
                if (key != null)
                {
                    this.Resources[key] = sourceResources[key];
                }
            }
        }
        else
        {
            var config = ConfigService.LoadConfig();
            ThemeService.ApplyTheme(this.Resources, config.Theme);
        }
    }

    public static void Show(Window? owner, string message, string title = "提示", string icon = "💡")
    {
        if (Application.Current.Dispatcher.CheckAccess())
        {
            var msgBox = new CustomMessageBox(owner, message, title, icon);
            msgBox.ShowDialog();
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var msgBox = new CustomMessageBox(owner, message, title, icon);
                msgBox.ShowDialog();
            });
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
        this.Close();
    }
}
