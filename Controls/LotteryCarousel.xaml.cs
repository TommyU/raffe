using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Raffe.Models;

namespace Raffe.Controls;

public partial class LotteryCarousel : UserControl
{
    public static readonly DependencyProperty ParticipantsProperty =
        DependencyProperty.Register(nameof(Participants), typeof(IEnumerable),
            typeof(LotteryCarousel), new PropertyMetadata(null, OnParticipantsChanged));

    public static readonly DependencyProperty IsSpinningProperty =
        DependencyProperty.Register(nameof(IsSpinning), typeof(bool),
            typeof(LotteryCarousel), new PropertyMetadata(false, OnIsSpinningChanged));

    public static readonly DependencyProperty StoppedParticipantsProperty =
        DependencyProperty.Register(nameof(StoppedParticipants), typeof(IEnumerable),
            typeof(LotteryCarousel), new PropertyMetadata(null));

    public static readonly DependencyProperty BatchSizeProperty =
        DependencyProperty.Register(nameof(BatchSize), typeof(int),
            typeof(LotteryCarousel), new PropertyMetadata(1));

    public IEnumerable? Participants
    {
        get => (IEnumerable?)GetValue(ParticipantsProperty);
        set => SetValue(ParticipantsProperty, value);
    }

    public bool IsSpinning
    {
        get => (bool)GetValue(IsSpinningProperty);
        set => SetValue(IsSpinningProperty, value);
    }

    public IEnumerable? StoppedParticipants
    {
        get => (IEnumerable?)GetValue(StoppedParticipantsProperty);
        set => SetValue(StoppedParticipantsProperty, value);
    }

    public int BatchSize
    {
        get => (int)GetValue(BatchSizeProperty);
        set => SetValue(BatchSizeProperty, value);
    }

    // ── 3D 陨石带数据 ──────────────────────────────────────────────
    private class AsteroidItem
    {
        public Participant Participant = null!;
        public double X3d;   // 3D 空间横向偏移
        public double Y3d;   // 3D 空间纵向偏移
        public double Z;     // 深度：大 = 远处，小 = 近处
        public Color BaseColor;
        public Border  Border  = null!;
        public TextBlock TextBlock = null!;
    }

    private static readonly Color[] BallColors =
    {
        Color.FromRgb(255,  80,  80),
        Color.FromRgb(255, 160,  40),
        Color.FromRgb(255, 220,   0),
        Color.FromRgb( 80, 220, 100),
        Color.FromRgb( 40, 180, 255),
        Color.FromRgb(180,  80, 255),
        Color.FromRgb(255, 100, 180),
    };

    // 透视参数
    private const double FocalLength  = 400.0;
    private const double MaxZ         = 2000.0;
    private const double MinZ         = 180.0;
    private const double MaxSpread    = 310.0;  // x3d/y3d 最大半径
    private const int    AsteroidCount = 40;    // 同时在场的名字数
    private const double SpinSpeed    = 28.0;   // 旋转时 z 每帧减少量

    private List<Participant> _participantList = new();
    private List<AsteroidItem> _asteroids      = new();
    private int    _nextParticipantIndex;
    private int    _colorIndex;
    private double _speed;
    private DispatcherTimer? _timer;
    private readonly Random _rng = new();

    public LotteryCarousel()
    {
        InitializeComponent();
        Loaded      += (_, _) => RefreshParticipants();
        SizeChanged += (_, _) => UpdatePositions();
    }

    private static void OnParticipantsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (LotteryCarousel)d;
        if (!ctrl.IsSpinning) ctrl.RefreshParticipants();
    }

    private static void OnIsSpinningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (LotteryCarousel)d;
        if ((bool)e.NewValue) ctrl.StartSpin();
        else ctrl.StopSpin();
    }

    // ── 初始化 ──────────────────────────────────────────────────────

    private void RefreshParticipants()
    {
        _participantList.Clear();
        if (Participants is IEnumerable en)
            foreach (var item in en)
                if (item is Participant p) _participantList.Add(p);

        CarouselCanvas.Children.Clear();
        _asteroids.Clear();
        _nextParticipantIndex = 0;
        _colorIndex           = 0;

        if (_participantList.Count == 0) return;

        for (var i = 0; i < AsteroidCount; i++)
        {
            // 均匀分布在 z 轴深度上，保证初始时各处都有名字
            var z    = MinZ + (MaxZ - MinZ) * (i + 1.0) / AsteroidCount;
            var item = CreateAsteroid(_participantList[_nextParticipantIndex % _participantList.Count], z);
            _nextParticipantIndex++;
            _asteroids.Add(item);
            CarouselCanvas.Children.Add(item.Border);
        }

        UpdatePositions();
    }

    private AsteroidItem CreateAsteroid(Participant p, double z)
    {
        var angle  = _rng.NextDouble() * Math.PI * 2;
        var radius = Math.Sqrt(_rng.NextDouble()) * MaxSpread; // sqrt 均匀分布在圆面上
        var color  = BallColors[_colorIndex++ % BallColors.Length];

        var tb = new TextBlock
        {
            Text                = p.Name,
            FontSize            = 18,
            FontWeight          = FontWeights.Bold,
            Foreground          = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
        };
        var border = new Border
        {
            Child           = tb,
            Tag             = p,
            Padding         = new Thickness(14, 8, 14, 8),
            CornerRadius    = new CornerRadius(22),
            BorderThickness = new Thickness(1.5),
        };

        return new AsteroidItem
        {
            Participant = p,
            X3d         = Math.Cos(angle) * radius,
            Y3d         = Math.Sin(angle) * radius,
            Z           = z,
            BaseColor   = color,
            Border      = border,
            TextBlock   = tb,
        };
    }

    // ── 每帧推进 ────────────────────────────────────────────────────

    private void AdvanceTick()
    {
        foreach (var item in _asteroids)
        {
            item.Z -= _speed;
            if (item.Z >= MinZ) continue;

            // 飞出镜头 → 重置到远端，换下一个参与者
            item.Participant       = _participantList[_nextParticipantIndex % _participantList.Count];
            item.TextBlock.Text    = item.Participant.Name;
            item.Border.Tag        = item.Participant;
            _nextParticipantIndex++;

            item.Z   = MaxZ;
            var ang  = _rng.NextDouble() * Math.PI * 2;
            var r    = Math.Sqrt(_rng.NextDouble()) * MaxSpread;
            item.X3d = Math.Cos(ang) * r;
            item.Y3d = Math.Sin(ang) * r;
        }

        UpdatePositions();
    }

    // ── 渲染：透视投影 ───────────────────────────────────────────────

    private void UpdatePositions()
    {
        if (_asteroids.Count == 0 || ActualWidth == 0) return;

        var cx = ActualWidth  / 2;
        var cy = ActualHeight / 2;

        // 按 Z 降序排序（远的先画，近的覆盖在上）
        var sorted = _asteroids.OrderByDescending(a => a.Z).ToList();

        for (var i = 0; i < sorted.Count; i++)
        {
            var item  = sorted[i];
            var invZ  = FocalLength / item.Z;
            var scale = Math.Clamp(invZ, 0.05, 3.0);
            var sx    = cx + item.X3d * invZ;
            var sy    = cy + item.Y3d * invZ;

            // tNear: 0 = 远端, 1 = 近端
            var tNear   = 1.0 - Math.Clamp((item.Z - MinZ) / (MaxZ - MinZ), 0.0, 1.0);
            var opacity = 0.08 + 0.92 * tNear;
            var alpha   = (byte)(40 + (int)(tNear * 180));

            item.Border.RenderTransform       = new ScaleTransform(scale, scale);
            item.Border.RenderTransformOrigin = new Point(0.5, 0.5);
            item.Border.Opacity               = opacity;
            item.Border.Background            = new SolidColorBrush(Color.FromArgb(alpha,
                item.BaseColor.R, item.BaseColor.G, item.BaseColor.B));
            item.Border.BorderBrush           = new SolidColorBrush(Color.FromArgb(
                (byte)Math.Min(255, alpha + 55), item.BaseColor.R, item.BaseColor.G, item.BaseColor.B));

            item.Border.Effect = tNear > 0.65
                ? new DropShadowEffect { Color = item.BaseColor, BlurRadius = 22, ShadowDepth = 0, Opacity = 0.9 }
                : null;

            // 以 (sx, sy) 为视觉中心定位（border 自然尺寸约 120×36）
            Canvas.SetLeft(item.Border, sx - 60);
            Canvas.SetTop(item.Border,  sy - 18);
            Panel.SetZIndex(item.Border, i); // i=0 最远，i=N-1 最近
        }
    }

    // ── 开始 / 停止 ─────────────────────────────────────────────────

    private void StartSpin()
    {
        _timer?.Stop();
        StoppedParticipants = null;
        _speed = SpinSpeed;
        RefreshParticipants();
        if (_asteroids.Count == 0) return;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) => AdvanceTick();
        _timer.Start();
    }

    private void StopSpin()
    {
        _timer?.Stop();
        if (_asteroids.Count == 0) return;
        var decel = _speed;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) =>
        {
            decel  *= 0.94;
            _speed  = decel;
            AdvanceTick();
            if (decel >= 0.5) return;
            _timer!.Stop();
            _speed = 0;
            PickWinners();
        };
        _timer.Start();
    }

    private void PickWinners()
    {
        if (_asteroids.Count == 0) return;
        var batchCount = Math.Max(1, Math.Min(BatchSize, _participantList.Count));
        var winners = _asteroids
            .OrderBy(a => a.Z)
            .Select(a => a.Participant)
            .Distinct()
            .Take(batchCount)
            .ToList();
        if (winners.Count > 0)
            StoppedParticipants = winners;
    }
}
