using System;
using System.Collections;
using System.Collections.Generic;
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

    private List<Participant> _participantList = new();
    private List<(TextBlock tb, Border container, Color baseColor)> _items = new();
    private double _currentAngle;
    private const int ItemCount = 20;

    private static readonly Color[] BallColors =
    {
        Color.FromRgb(255, 80, 80),
        Color.FromRgb(255, 160, 40),
        Color.FromRgb(255, 220, 0),
        Color.FromRgb(80, 220, 100),
        Color.FromRgb(40, 180, 255),
        Color.FromRgb(180, 80, 255),
        Color.FromRgb(255, 100, 180),
    };
    private DispatcherTimer? _timer;

    public LotteryCarousel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private static void OnParticipantsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (LotteryCarousel)d;
        if (!ctrl.IsSpinning)
            ctrl.RefreshParticipants();
    }

    private static void OnIsSpinningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (LotteryCarousel)d;
        if ((bool)e.NewValue)
            ctrl.StartSpin();
        else
            ctrl.StopSpin();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => RefreshParticipants();
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdatePositions();

    private void RefreshParticipants()
    {
        _participantList.Clear();
        if (Participants is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is Participant p)
                    _participantList.Add(p);
            }
        }

        CarouselCanvas.Children.Clear();
        _items.Clear();

        if (_participantList.Count == 0) return;

        // Each person appears exactly once; cap at ItemCount for very large lists
        var displayList = _participantList.Count <= ItemCount
            ? new List<Participant>(_participantList)
            : _participantList.GetRange(0, ItemCount);

        _ballColorIndex = 0;
        foreach (var p in displayList)
        {
            var (tb, border, color) = CreateBallItem(p);
            _items.Add((tb, border, color));
            CarouselCanvas.Children.Add(border);
        }

        UpdatePositions();
    }

    private int _ballColorIndex;

    private (TextBlock, Border, Color) CreateBallItem(Participant p)
    {
        var baseColor = BallColors[_ballColorIndex % BallColors.Length];
        _ballColorIndex++;

        var tb = new TextBlock
        {
            Text = p.Name,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.White),
            Tag = p,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var border = new Border
        {
            Child = tb,
            Tag = p,
            Padding = new Thickness(14, 8, 14, 8),
            CornerRadius = new CornerRadius(22),
            Background = new SolidColorBrush(Color.FromArgb(120, baseColor.R, baseColor.G, baseColor.B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(160, baseColor.R, baseColor.G, baseColor.B)),
            BorderThickness = new Thickness(1.5)
        };
        return (tb, border, baseColor);
    }

    private void UpdatePositions()
    {
        if (_items.Count == 0) return;

        var centerX = ActualWidth / 2;
        var centerY = ActualHeight / 2;

        var radiusX = Math.Max(100, ActualWidth * 0.40);
        var radiusY = Math.Max(60,  ActualHeight * 0.32);

        for (var i = 0; i < _items.Count; i++)
        {
            var angle = _currentAngle + 2 * Math.PI * i / _items.Count;
            var depthScale = 0.5 + 0.5 * Math.Sin(angle * 2);
            var r = radiusX * (0.6 + 0.4 * depthScale);
            var x = centerX + r * Math.Cos(angle);
            var y = centerY + radiusY * Math.Sin(angle);

            var frontFactor = (Math.Cos(angle) + 1) / 2;
            var scale = 0.6 + 0.4 * frontFactor;
            var opacity = 0.5 + 0.5 * frontFactor;

            var (tb, border, baseColor) = _items[i];
            border.RenderTransform = new ScaleTransform(scale, scale);
            border.RenderTransformOrigin = new Point(0.5, 0.5);
            border.Opacity = opacity;

            var alpha = (byte)(80 + (int)(frontFactor * 120));
            border.Background = new SolidColorBrush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));
            border.BorderBrush = new SolidColorBrush(Color.FromArgb((byte)(alpha + 40), baseColor.R, baseColor.G, baseColor.B));

            if (frontFactor > 0.75)
            {
                border.Effect = new DropShadowEffect
                {
                    Color = baseColor,
                    BlurRadius = 22,
                    ShadowDepth = 0,
                    Opacity = 0.9
                };
            }
            else
            {
                border.Effect = null;
            }

            Canvas.SetLeft(border, x - 60);
            Canvas.SetTop(border, y - 18);
        }
    }

    private void StartSpin()
    {
        _timer?.Stop();
        StoppedParticipants = null;
        RefreshParticipants();
        if (_items.Count == 0) return;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (s, e) =>
        {
            _currentAngle += 0.08;
            UpdatePositions();
        };
        _timer.Start();
    }

    private void StopSpin()
    {
        _timer?.Stop();
        if (_items.Count == 0) return;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        var decel = 0.08;
        _timer.Tick += (s, e) =>
        {
            decel *= 0.96;
            _currentAngle += decel;
            UpdatePositions();
            if (decel < 0.0015)
            {
                _timer.Stop();
                SnapToCenter();
            }
        };
        _timer.Start();
    }

    private void SnapToCenter()
    {
        if (_items.Count == 0) return;
        var step = 2 * Math.PI / _items.Count;
        var normalized = ((_currentAngle % step) + step) % step;
        if (normalized > step / 2) normalized -= step;
        _currentAngle -= normalized;
        UpdatePositions();

        var centerIdx = (int)Math.Round(-_currentAngle / step) % _items.Count;
        if (centerIdx < 0) centerIdx += _items.Count;

        var batchCount = Math.Max(1, Math.Min(BatchSize, _items.Count));
        var indices = new List<int> { centerIdx };
        for (var d = 1; d <= _items.Count / 2; d++)
        {
            indices.Add((centerIdx + d) % _items.Count);
            if (d != 0) indices.Add((centerIdx - d + _items.Count) % _items.Count);
        }

        var winners = new List<Participant>();
        var seen = new HashSet<Participant>();
        foreach (var idx in indices)
        {
            if (_items[idx].tb.Tag is Participant p && seen.Add(p))
            {
                winners.Add(p);
                if (winners.Count >= batchCount) break;
            }
        }

        if (winners.Count > 0)
            StoppedParticipants = winners;
    }
}
