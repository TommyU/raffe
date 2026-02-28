using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Raffe.Controls;

public partial class IdleSlideshow : UserControl
{
    public static readonly DependencyProperty ImagePathsProperty =
        DependencyProperty.Register(nameof(ImagePaths), typeof(IEnumerable<string>),
            typeof(IdleSlideshow), new PropertyMetadata(null, OnImagePathsChanged));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool),
            typeof(IdleSlideshow), new PropertyMetadata(false, OnIsActiveChanged));

    public static readonly DependencyProperty IntervalSecondsProperty =
        DependencyProperty.Register(nameof(IntervalSeconds), typeof(int),
            typeof(IdleSlideshow), new PropertyMetadata(5));

    public IEnumerable<string>? ImagePaths
    {
        get => (IEnumerable<string>?)GetValue(ImagePathsProperty);
        set => SetValue(ImagePathsProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public int IntervalSeconds
    {
        get => (int)GetValue(IntervalSecondsProperty);
        set => SetValue(IntervalSecondsProperty, value);
    }

    // ── 内部状态 ──────────────────────────────────────────────────────

    private enum Trans { Fade, SlideLeft, SlideRight, SlideUp, SlideDown, BlindsH }

    private List<string> _paths = new();
    private int          _currentIndex = -1;
    private bool         _transitioning;
    private DispatcherTimer? _timer;
    private readonly Random  _rng = new();

    public IdleSlideshow()
    {
        InitializeComponent();
        Loaded += (_, _) => { if (IsActive) StartSlideshow(); };
    }

    private static void OnImagePathsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (IdleSlideshow)d;
        ctrl._paths = (e.NewValue as IEnumerable<string>)?.ToList() ?? new();
        if (ctrl.IsActive && ctrl.IsLoaded) ctrl.StartSlideshow();
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (IdleSlideshow)d;
        if ((bool)e.NewValue) { if (ctrl.IsLoaded) ctrl.StartSlideshow(); }
        else ctrl.StopSlideshow();
    }

    // ── 生命周期 ──────────────────────────────────────────────────────

    private void StartSlideshow()
    {
        _timer?.Stop();
        _timer = null;
        _transitioning = false;

        if (_paths.Count == 0) { ResetImages(); return; }

        _currentIndex = 0;
        ShowImmediate(_paths[0]);
        if (_paths.Count == 1) return;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Max(2, IntervalSeconds)) };
        _timer.Tick += (_, _) => NextSlide();
        _timer.Start();
    }

    private void StopSlideshow()
    {
        _timer?.Stop();
        _timer = null;
        _transitioning = false;
        ResetImages();
    }

    // ── 图片显示 ──────────────────────────────────────────────────────

    private void ShowImmediate(string path)
    {
        ResetImages();
        var bmp = TryLoad(path);
        if (bmp == null) return;
        ImgFront.Source  = bmp;
        ImgFront.Opacity = 1;
    }

    private void NextSlide()
    {
        if (_transitioning || _paths.Count <= 1) return;

        // 找下一张可加载的图
        for (var attempt = 0; attempt < _paths.Count; attempt++)
        {
            _currentIndex = (_currentIndex + 1) % _paths.Count;
            var bmp = TryLoad(_paths[_currentIndex]);
            if (bmp == null) continue;

            _transitioning   = true;
            ImgBack.Source   = bmp;
            ImgBack.Opacity  = 0;
            ImgBack.RenderTransform = null;
            ImgBack.Clip     = null;

            var all = (Trans[])Enum.GetValues(typeof(Trans));
            RunTransition(all[_rng.Next(all.Length)]);
            return;
        }
    }

    private void RunTransition(Trans t)
    {
        switch (t)
        {
            case Trans.Fade:    DoFade();    break;
            case Trans.BlindsH: DoBlinds();  break;
            default:            DoSlide(t);  break;
        }
    }

    // ── 过渡动画：淡入淡出 ────────────────────────────────────────────

    private void DoFade()
    {
        var dur     = new Duration(TimeSpan.FromSeconds(0.9));
        var fadeOut = new DoubleAnimation(1, 0, dur);
        var fadeIn  = new DoubleAnimation(0, 1, dur);
        fadeOut.Completed += (_, _) => FinishTransition();
        ImgFront.BeginAnimation(OpacityProperty, fadeOut);
        ImgBack.BeginAnimation(OpacityProperty, fadeIn);
    }

    // ── 过渡动画：滚动（四个方向随机）───────────────────────────────────

    private void DoSlide(Trans t)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        double outX = 0, outY = 0, inX = 0, inY = 0;
        switch (t)
        {
            case Trans.SlideLeft:  outX = -w; inX =  w; break;
            case Trans.SlideRight: outX =  w; inX = -w; break;
            case Trans.SlideUp:    outY = -h; inY =  h; break;
            case Trans.SlideDown:  outY =  h; inY = -h; break;
        }

        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
        var dur  = new Duration(TimeSpan.FromSeconds(0.7));

        ImgFront.RenderTransform = new TranslateTransform(0,   0);
        ImgBack.RenderTransform  = new TranslateTransform(inX, inY);
        ImgBack.Opacity = 1;

        var animFX = new DoubleAnimation(0,   outX, dur) { EasingFunction = ease };
        var animFY = new DoubleAnimation(0,   outY, dur) { EasingFunction = ease };
        var animBX = new DoubleAnimation(inX,    0, dur) { EasingFunction = ease };
        var animBY = new DoubleAnimation(inY,    0, dur) { EasingFunction = ease };
        animFX.Completed += (_, _) => FinishTransition();

        ((TranslateTransform)ImgFront.RenderTransform).BeginAnimation(TranslateTransform.XProperty, animFX);
        ((TranslateTransform)ImgFront.RenderTransform).BeginAnimation(TranslateTransform.YProperty, animFY);
        ((TranslateTransform)ImgBack.RenderTransform).BeginAnimation(TranslateTransform.XProperty, animBX);
        ((TranslateTransform)ImgBack.RenderTransform).BeginAnimation(TranslateTransform.YProperty, animBY);
    }

    // ── 过渡动画：百叶窗（当前图片条带逐条收缩，露出新图片）──────────────

    private void DoBlinds()
    {
        const int N     = 10;
        var w           = ActualWidth;
        var h           = ActualHeight;
        var stripH      = h / N;
        var animDur     = new Duration(TimeSpan.FromSeconds(0.45));
        var stagger     = 0.05;

        ImgBack.Opacity  = 1;
        ImgFront.Opacity = 1;

        var geo = new GeometryGroup();
        for (var i = 0; i < N; i++)
            geo.Children.Add(new RectangleGeometry(new Rect(0, i * stripH, w, stripH)));
        ImgFront.Clip = geo;

        for (var i = 0; i < N; i++)
        {
            var y    = i * stripH;
            var rg   = (RectangleGeometry)geo.Children[i];
            var anim = new RectAnimation(
                new Rect(0, y, w, stripH),
                new Rect(0, y, w, 0),
                animDur)
            {
                BeginTime    = TimeSpan.FromSeconds(i * stagger),
                FillBehavior = FillBehavior.HoldEnd,
            };
            if (i == N - 1) anim.Completed += (_, _) => FinishTransition();
            rg.BeginAnimation(RectangleGeometry.RectProperty, anim);
        }
    }

    // ── 收尾 ──────────────────────────────────────────────────────────

    private void FinishTransition()
    {
        if (!_transitioning) return;
        RemoveAllAnimations();
        ImgFront.Source          = ImgBack.Source;
        ImgFront.Opacity         = 1;
        ImgFront.RenderTransform = null;
        ImgFront.Clip            = null;
        ImgBack.Source           = null;
        ImgBack.Opacity          = 0;
        ImgBack.RenderTransform  = null;
        ImgBack.Clip             = null;
        _transitioning           = false;
    }

    private void ResetImages()
    {
        RemoveAllAnimations();
        ImgFront.Source = ImgBack.Source = null;
        ImgFront.Opacity = 1; ImgBack.Opacity = 0;
        ImgFront.RenderTransform = ImgBack.RenderTransform = null;
        ImgFront.Clip = ImgBack.Clip = null;
    }

    private void RemoveAllAnimations()
    {
        ImgFront.BeginAnimation(OpacityProperty, null);
        ImgBack.BeginAnimation(OpacityProperty,  null);
        if (ImgFront.RenderTransform is TranslateTransform ttf)
        {
            ttf.BeginAnimation(TranslateTransform.XProperty, null);
            ttf.BeginAnimation(TranslateTransform.YProperty, null);
        }
        if (ImgBack.RenderTransform is TranslateTransform ttb)
        {
            ttb.BeginAnimation(TranslateTransform.XProperty, null);
            ttb.BeginAnimation(TranslateTransform.YProperty, null);
        }
    }

    private static BitmapImage? TryLoad(string path)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource   = new Uri(path, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            return bmp;
        }
        catch { return null; }
    }
}
