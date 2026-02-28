using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Raffe.Models;

namespace Raffe.Services;

public class DataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _dataPath;
    private LotteryData _data = new();

    public LotteryData Data => _data;

    public DataService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Raffe");
        Directory.CreateDirectory(appData);
        _dataPath = Path.Combine(appData, "lottery.json");
    }

    public void Load()
    {
        if (!File.Exists(_dataPath))
        {
            _data = CreateDefaultData();
            Save();
            return;
        }

        try
        {
            var json = File.ReadAllText(_dataPath);
            _data = JsonSerializer.Deserialize<LotteryData>(json, JsonOptions) ?? new LotteryData();
            _data.Participants ??= new List<Participant>();
            _data.PrizeLevels ??= new List<PrizeLevel>();
            _data.Results ??= new List<LotteryResult>();
            MigratePrizeLevels();
        }
        catch
        {
            _data = CreateDefaultData();
        }
    }

    public void ClearResults()
    {
        _data.Results.Clear();
        Save();
    }

    private void MigratePrizeLevels()
    {
        foreach (var pl in _data.PrizeLevels)
        {
            if (pl.BatchCount <= 0 && pl.BatchSize > 0)
                pl.BatchCount = Math.Max(1, pl.MaxWinners / pl.BatchSize);
            if (pl.BatchCount <= 0) pl.BatchCount = 1;
        }
    }

    public void Save()
    {
        File.WriteAllText(_dataPath, JsonSerializer.Serialize(_data, JsonOptions));
    }

    private static LotteryData CreateDefaultData()
    {
        return new LotteryData
        {
            PrizeLevels = new List<PrizeLevel>
            {
                new() { LevelName = "五等奖", MaxWinners = 10, BatchCount = 2, SortOrder = 1 },
                new() { LevelName = "四等奖", MaxWinners = 5, BatchCount = 2, SortOrder = 2 },
                new() { LevelName = "三等奖", MaxWinners = 3, BatchCount = 2, SortOrder = 3 },
                new() { LevelName = "二等奖", MaxWinners = 2, BatchCount = 2, SortOrder = 4 },
                new() { LevelName = "特等奖", MaxWinners = 1, BatchCount = 1, SortOrder = 5 }
            }
        };
    }
}
