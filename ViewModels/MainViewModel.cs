using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Raffe.Models;
using Raffe.Services;

namespace Raffe.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly DataService _dataService;
    private readonly ExcelImportService _excelService;
    private readonly MusicService _musicService = new();
    private DispatcherTimer? _countdownTimer;
    // 一旦开始抽奖，不再回到待机图片状态
    private bool _lotteryStarted;

    [ObservableProperty] private string _companyName = "某某公司";
    [ObservableProperty] private int _year = DateTime.Now.Year;
    [ObservableProperty] private string _currentPrizeDisplay = "";
    [ObservableProperty] private string _currentPrizeName = "";
    [ObservableProperty] private string _lastWonPrizeName = "";
    [ObservableProperty] private bool _isSpinning;
    [ObservableProperty] private bool _isCountingDown;
    [ObservableProperty] private string _countdownText = "";
    [ObservableProperty] private bool _isIdle = true;
    [ObservableProperty] private bool _isNotIdle;
    [ObservableProperty] private int _currentBatchSize = 1;
    [ObservableProperty] private bool _showWinners;
    [ObservableProperty] private bool _showResults;

    [ObservableProperty] private IReadOnlyList<string> _slideshowImagePaths = new List<string>();
    [ObservableProperty] private int _slideshowIntervalSeconds = 5;
    [ObservableProperty] private IReadOnlyList<PrizeResultGroup> _resultGroups = new List<PrizeResultGroup>();

    partial void OnIsSpinningChanged(bool value)
    {
        UpdateIdleState();
        if (value)
            _musicService.Play(_dataService.Data.Config.SpinningMusicPath);
    }

    partial void OnIsCountingDownChanged(bool value) => UpdateIdleState();

    partial void OnIsIdleChanged(bool value)
    {
        if (value)
            _musicService.Play(_dataService.Data.Config.DefaultMusicPath);
    }

    partial void OnShowWinnersChanged(bool value)
    {
        UpdateIdleState();
        var cfg = _dataService.Data.Config;
        if (value)
            _musicService.Play(cfg.WinnerMusicPath, loop: false);
        else
            _musicService.Play(cfg.DefaultMusicPath);
    }

    private void UpdateIdleState()
    {
        IsIdle    = !_lotteryStarted && !IsSpinning && !IsCountingDown && !ShowWinners;
        IsNotIdle = !IsIdle;
    }

    public ObservableCollection<Participant> Winners { get; } = new();

    private IEnumerable<Participant>? _stoppedParticipants;
    public IEnumerable<Participant>? StoppedParticipants
    {
        get => _stoppedParticipants;
        set
        {
            if (SetProperty(ref _stoppedParticipants, value) && value != null)
                ConfirmWinners(new List<Participant>(value));
        }
    }

    [ObservableProperty] private IReadOnlyList<Participant> _displayParticipants = new List<Participant>();
    public ObservableCollection<PrizeLevel> PrizeLevels { get; } = new();
    public ObservableCollection<LotteryResult> Results { get; } = new();

    public MainViewModel()
    {
        _dataService = new DataService();
        _excelService = new ExcelImportService();
        _dataService.Load();
        LoadFromData();
    }

    private void LoadFromData()
    {
        IsSpinning = false;
        ShowWinners = false;
        ShowResults = false;
        _lotteryStarted = false;
        Winners.Clear();
        StoppedParticipants = null;

        CompanyName = _dataService.Data.Config.CompanyName;
        Year = _dataService.Data.Config.Year;

        PrizeLevels.Clear();
        foreach (var prize in _dataService.Data.PrizeLevels.OrderBy(p => p.SortOrder))
            PrizeLevels.Add(prize);

        Results.Clear();
        foreach (var r in _dataService.Data.Results)
            Results.Add(r);

        UpdateDisplayParticipants();
        UpdateCurrentPrizeDisplay();

        var cfg = _dataService.Data.Config;
        _musicService.Volume = cfg.MusicVolume;
        _musicService.Play(cfg.DefaultMusicPath);

        SlideshowImagePaths    = cfg.SlideshowImagePaths?.ToList() ?? new List<string>();
        SlideshowIntervalSeconds = cfg.SlideshowIntervalSeconds;
        UpdateIdleState();
    }

    public void Dispose() => _musicService.Dispose();

    private void UpdateCurrentPrizeDisplay()
    {
        var next = GetNextPrizeToDraw();
        CurrentPrizeName = next?.LevelName ?? "";
        CurrentPrizeDisplay = next != null ? $"当前奖项：{next.LevelName}" : "抽奖已全部完成";
    }

    private PrizeLevel? GetNextPrizeToDraw()
    {
        return PrizeLevels
            .OrderBy(p => p.SortOrder)
            .FirstOrDefault(p =>
            {
                var drawn = GetDrawnCountForPrize(p);
                return drawn < p.MaxWinners;
            });
    }

    private int GetDrawnCountForPrize(PrizeLevel prize)
    {
        return _dataService.Data.Results.Count(r =>
            r.PrizeLevel != null && (r.PrizeLevel.Id == prize.Id || r.PrizeLevel.LevelName == prize.LevelName));
    }

    private void UpdateDisplayParticipants()
    {
        var participants = _dataService.Data.Participants ?? new List<Participant>();
        var winnerIds = _dataService.Data.Results
            .Where(r => r.Winner != null)
            .Select(r => r.Winner!.Id)
            .ToHashSet();
        // Assign a NEW list so the DP binding always fires OnParticipantsChanged in the carousel
        DisplayParticipants = participants.Where(p => !winnerIds.Contains(p.Id)).ToList();
    }

    [RelayCommand]
    internal void ToggleLottery()
    {
        if (IsCountingDown) return;

        if (IsSpinning)
        {
            IsSpinning = false;
            return;
        }

        var currentPrize = GetNextPrizeToDraw();
        if (currentPrize == null)
        {
            ShowResultsScreen();
            return;
        }

        var winnerIds = _dataService.Data.Results
            .Where(r => r.Winner != null)
            .Select(r => r.Winner!.Id)
            .ToHashSet();
        var participants = _dataService.Data.Participants ?? new List<Participant>();
        var available = participants
            .Where(p => !winnerIds.Contains(p.Id))
            .ToList();

        var alreadyDrawn = GetDrawnCountForPrize(currentPrize);
        if (alreadyDrawn >= currentPrize.MaxWinners)
        {
            MessageBox.Show($"{currentPrize.LevelName} 已抽完", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (available.Count == 0)
        {
            MessageBox.Show("没有可参与抽奖的人员", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Winners.Clear();
        ShowWinners = false;
        StoppedParticipants = null;
        // Remove winners from carousel at the moment the new spin starts
        UpdateDisplayParticipants();
        var perBatch = (int)Math.Ceiling((double)currentPrize.MaxWinners / Math.Max(1, currentPrize.BatchCount));
        var remaining = currentPrize.MaxWinners - alreadyDrawn;
        CurrentBatchSize = Math.Min(perBatch, Math.Min(remaining, available.Count));
        StartCountdown();
    }

    private void StartCountdown()
    {
        _lotteryStarted = true;
        _countdownTimer?.Stop();
        var value = 3;
        CountdownText = value.ToString();
        IsCountingDown = true;

        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick += (_, _) =>
        {
            value--;
            if (value > 0)
            {
                CountdownText = value.ToString();
            }
            else
            {
                _countdownTimer!.Stop();
                IsCountingDown = false;
                IsSpinning = true;
            }
        };
        _countdownTimer.Start();
    }

    private void ConfirmWinners(List<Participant> selectedWinners)
    {
        var currentPrize = GetNextPrizeToDraw();
        if (currentPrize == null || selectedWinners.Count == 0) return;

        // capture prize name BEFORE advancing to next prize
        LastWonPrizeName = currentPrize.LevelName;

        Winners.Clear();
        foreach (var w in selectedWinners)
        {
            Winners.Add(w);
            var result = new LotteryResult { PrizeLevel = currentPrize, Winner = w };
            Results.Add(result);
            _dataService.Data.Results.Add(result);
        }
        _dataService.Save();
        ShowWinners = true;

        // Don't update carousel yet — winners stay visible until next spin starts
        UpdateCurrentPrizeDisplay();
    }

    private void ShowResultsScreen()
    {
        ShowWinners = false;
        // Build groups ordered from highest prize (descending SortOrder) to lowest
        ResultGroups = PrizeLevels
            .OrderByDescending(p => p.SortOrder)
            .Select(prize =>
            {
                var winners = Results
                    .Where(r => r.PrizeLevel != null &&
                                (r.PrizeLevel.Id == prize.Id || r.PrizeLevel.LevelName == prize.LevelName) &&
                                r.Winner != null)
                    .Select(r => r.Winner!)
                    .ToList();
                return new PrizeResultGroup(prize.LevelName, winners);
            })
            .Where(g => g.Winners.Count > 0)
            .ToList();
        ShowResults = true;
    }

    [RelayCommand]
    private void CloseResults()
    {
        ShowResults = false;
        _musicService.Play(_dataService.Data.Config.DefaultMusicPath);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var dialog = new Raffe.Views.SettingsWindow(_dataService, _excelService);
        dialog.ShowDialog();
        LoadFromData();
    }
}

public record PrizeResultGroup(string PrizeName, IReadOnlyList<Participant> Winners);
