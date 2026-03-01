using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;
using Raffe.Models;
using Raffe.ViewModels;

namespace Raffe;

public partial class MainWindow : Window
{
    private bool _isFullscreen;
    private static readonly WindowChrome FullscreenChrome = new()
    {
        CaptionHeight = 0,
        GlassFrameThickness = new Thickness(0),
        NonClientFrameEdges = NonClientFrameEdges.None,
        ResizeBorderThickness = new Thickness(0),
        UseAeroCaptionButtons = false
    };

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TrySetTitleBarColor();
        if (DataContext is MainViewModel vm)
            vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ThemeId))
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

    private void CaptionMinimize_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
    private void CaptionMaximize_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            SystemCommands.RestoreWindow(this);
        else
            SystemCommands.MaximizeWindow(this);
    }
    private void CaptionClose_Click(object sender, RoutedEventArgs e) => Close();

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
        WindowChrome.SetWindowChrome(this, FullscreenChrome);
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Normal;
        WindowState = WindowState.Maximized;
        ResizeMode = ResizeMode.NoResize;
    }

    private void ExitFullscreen()
    {
        _isFullscreen = false;
        WindowChrome.SetWindowChrome(this, null);
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
            var color = DataContext is MainViewModel vm ? ThemeSchema.GetCaptionColorBgr(vm.ThemeId) : 0x0000081A;
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref color, sizeof(int));
        }
        catch { }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}
