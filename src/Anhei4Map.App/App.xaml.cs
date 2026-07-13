using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace Anhei4Map.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            _mutex = new Mutex(true, @"Global\Anhei4Map_SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                _mutex.Dispose();
                _mutex = null;
                Shutdown();
                return;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or WaitHandleCannotBeOpenedException or IOException)
        {
            MessageBox.Show(
                $"无法创建应用程序互斥锁：{ex.Message}",
                "启动失败",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        string? version;
        try
        {
            version = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch (WebView2RuntimeNotFoundException)
        {
            ShowRuntimeMissingDialog();
            Shutdown();
            return;
        }
        catch (Exception ex) when (ex is not WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(
                $"WebView2 运行时检测失败：{ex.Message}",
                "启动失败",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            ShowRuntimeMissingDialog();
            Shutdown();
            return;
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _mutex?.ReleaseMutex();
        }
        catch
        {
            // 忽略——进程退出前释放尽力而为
        }

        _mutex?.Dispose();
        _mutex = null;

        base.OnExit(e);
    }

    private static void ShowRuntimeMissingDialog()
    {
        MessageBox.Show(
            "Microsoft Edge WebView2 Runtime 未安装。\n\n" +
            "请从以下链接下载 Evergreen Bootstrapper 后重试：\n\n" +
            "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
            "缺少必需组件 — Anhei4Map",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
