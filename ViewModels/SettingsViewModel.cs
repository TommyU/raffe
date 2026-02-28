using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Raffe.Models;
using Raffe.Services;

namespace Raffe.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly DataService _dataService;
    private readonly ExcelImportService _excelService;

    [ObservableProperty] private string _companyName = "";
    [ObservableProperty] private int _year;
    [ObservableProperty] private string _defaultMusicPath  = "";
    [ObservableProperty] private string _spinningMusicPath = "";
    [ObservableProperty] private string _winnerMusicPath   = "";
    [ObservableProperty] private double _musicVolume = 0.7;

    [ObservableProperty] private int     _slideshowIntervalSeconds = 5;
    [ObservableProperty] private string? _selectedSlideshowImage;

    public ObservableCollection<string>      SlideshowImagePaths { get; } = new();
    public ObservableCollection<Participant> Participants        { get; } = new();
    public ObservableCollection<PrizeLevel>  PrizeLevels        { get; } = new();

    public SettingsViewModel(DataService dataService, ExcelImportService excelService)
    {
        _dataService = dataService;
        _excelService = excelService;
        Load();
    }

    private void Load()
    {
        CompanyName = _dataService.Data.Config.CompanyName;
        Year = _dataService.Data.Config.Year;
        DefaultMusicPath  = _dataService.Data.Config.DefaultMusicPath;
        SpinningMusicPath = _dataService.Data.Config.SpinningMusicPath;
        WinnerMusicPath   = _dataService.Data.Config.WinnerMusicPath;
        MusicVolume              = _dataService.Data.Config.MusicVolume;
        SlideshowIntervalSeconds = _dataService.Data.Config.SlideshowIntervalSeconds;

        SlideshowImagePaths.Clear();
        foreach (var p in _dataService.Data.Config.SlideshowImagePaths ?? new())
            SlideshowImagePaths.Add(p);

        Participants.Clear();
        foreach (var p in _dataService.Data.Participants)
            Participants.Add(p);

        PrizeLevels.Clear();
        foreach (var pl in _dataService.Data.PrizeLevels.OrderBy(p => p.SortOrder))
            PrizeLevels.Add(pl);
    }

    public void Save()
    {
        var cfg = _dataService.Data.Config;
        cfg.CompanyName = CompanyName;
        cfg.Year = Year;
        cfg.DefaultMusicPath  = DefaultMusicPath;
        cfg.SpinningMusicPath = SpinningMusicPath;
        cfg.WinnerMusicPath   = WinnerMusicPath;
        cfg.MusicVolume              = MusicVolume;
        cfg.SlideshowIntervalSeconds = SlideshowIntervalSeconds;
        cfg.SlideshowImagePaths      = SlideshowImagePaths.ToList();
        _dataService.Data.Participants = Participants.ToList();
        _dataService.Data.PrizeLevels  = PrizeLevels.ToList();
        _dataService.Save();
    }

    private static string? BrowseMusic() 
    {
        var dlg = new OpenFileDialog
        {
            Title  = "选择音乐文件",
            Filter = "音频文件|*.mp3;*.wav;*.wma;*.aac;*.flac|所有文件|*.*"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    [RelayCommand]
    private void AddSlideshowImage()
    {
        var dlg = new OpenFileDialog
        {
            Title      = "选择待机图片",
            Filter     = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*",
            Multiselect = true,
        };
        if (dlg.ShowDialog() != true) return;
        foreach (var file in dlg.FileNames)
            if (!SlideshowImagePaths.Contains(file))
                SlideshowImagePaths.Add(file);
    }

    [RelayCommand]
    private void RemoveSlideshowImage()
    {
        if (SelectedSlideshowImage != null)
            SlideshowImagePaths.Remove(SelectedSlideshowImage);
    }

    [RelayCommand] private void BrowseDefaultMusic()  { var f = BrowseMusic(); if (f != null) DefaultMusicPath  = f; }
    [RelayCommand] private void BrowseSpinningMusic() { var f = BrowseMusic(); if (f != null) SpinningMusicPath = f; }
    [RelayCommand] private void BrowseWinnerMusic()   { var f = BrowseMusic(); if (f != null) WinnerMusicPath   = f; }

    [RelayCommand]
    private void AddParticipant()
    {
        Participants.Add(new Participant { Name = "新成员", Department = "" });
    }

    [RelayCommand]
    private void RemoveParticipant(Participant? p)
    {
        if (p != null) Participants.Remove(p);
    }

    [RelayCommand]
    private void LoadFromExcel()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Excel文件|*.xlsx;*.xls|所有文件|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        var (imported, errors) = _excelService.Import(dlg.FileName);
        foreach (var p in imported)
            Participants.Add(p);

        if (errors.Count > 0)
            MessageBox.Show(string.Join("\n", errors), "导入提示", MessageBoxButton.OK, MessageBoxImage.Information);
        else if (imported.Count > 0)
            MessageBox.Show($"成功导入 {imported.Count} 人", "导入完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void RestartLottery()
    {
        if (MessageBox.Show("确定要清空所有抽奖结果并重新开始吗？", "重新开始", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        _dataService.ClearResults();
        MessageBox.Show("抽奖结果已清空，可重新开始抽奖", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ExportResults()
    {
        var results = _dataService.Data.Results;
        if (results.Count == 0)
        {
            MessageBox.Show("暂无抽奖结果可导出", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "Excel文件|*.xlsx|所有文件|*.*",
            FileName = $"抽奖结果_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add("中奖名单");

        ws.Cell(1, 1).Value = "部门";
        ws.Cell(1, 2).Value = "姓名";
        ws.Cell(1, 3).Value = "奖项";

        var header = ws.Range(1, 1, 1, 3);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#FF6600");
        header.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

        var ordered = results
            .Where(r => r.Winner != null && r.PrizeLevel != null)
            .OrderBy(r => r.PrizeLevel!.SortOrder)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            var r = ordered[i];
            ws.Cell(i + 2, 1).Value = r.Winner!.Department;
            ws.Cell(i + 2, 2).Value = r.Winner.Name;
            ws.Cell(i + 2, 3).Value = r.PrizeLevel!.LevelName;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(dlg.FileName);
        MessageBox.Show($"已导出 {ordered.Count} 条记录", "导出完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void SaveTemplate()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Excel文件|*.xlsx|所有文件|*.*",
            FileName = "参与者模板.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        _excelService.ExportTemplate(dlg.FileName);
        MessageBox.Show("模板已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
