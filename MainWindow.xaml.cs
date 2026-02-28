using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Raffe.ViewModels;

namespace Raffe;

public partial class MainWindow : Window
{
    private bool _isFullscreen;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TrySetTitleBarColor();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            e.Handled = true;
            if (DataContext is MainViewModel vm)
                vm.ToggleLottery();
        }
        else if (e.Key == Key.F11)
        {
            e.Handled = true;
            if (_isFullscreen) ExitFullscreen(); else EnterFullscreen();
        }
        else if (e.Key == Key.Escape && _isFullscreen)
        {
            e.Handled = true;
            ExitFullscreen();
        }
    }

    // Double-click on the header to toggle fullscreen
    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            if (_isFullscreen) ExitFullscreen(); else EnterFullscreen();
        }
    }

    private void EnterFullscreen()
    {
        _isFullscreen = true;
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Normal;   // must reset first
        WindowState = WindowState.Maximized;
        ResizeMode = ResizeMode.NoResize;
    }

    private void ExitFullscreen()
    {
        _isFullscreen = false;
        WindowStyle = WindowStyle.SingleBorderWindow;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        WindowState = WindowState.Maximized;
        TrySetTitleBarColor();
    }

    private void TrySetTitleBarColor()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;
            const int DWMWA_CAPTION_COLOR = 35;
            // #1A0800 → DWM wants 0x00BBGGRR → B=0x00 G=0x08 R=0x1A → 0x0000081A
            var color = 0x0000081A;
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref color, sizeof(int));
        }
        catch { }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}
