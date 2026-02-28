using System;
using System.Text.Json.Serialization;

namespace Raffe.Models;

public class PrizeLevel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LevelName { get; set; } = string.Empty;
    public int MaxWinners { get; set; } = 1;
    public int BatchCount { get; set; } = 1;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int BatchSize { get; set; }

    public int SortOrder { get; set; }
}
