using System.Collections.Generic;

namespace Raffe.Models;

public class LotteryData
{
    public List<Participant> Participants { get; set; } = new();
    public List<PrizeLevel> PrizeLevels { get; set; } = new();
    public List<LotteryResult> Results { get; set; } = new();
    public AppConfig Config { get; set; } = new();
}
