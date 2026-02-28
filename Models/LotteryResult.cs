using System;

namespace Raffe.Models;

public class LotteryResult
{
    public PrizeLevel PrizeLevel { get; set; } = null!;
    public Participant Winner { get; set; } = null!;
    public DateTime DrawTime { get; set; } = DateTime.Now;
}
