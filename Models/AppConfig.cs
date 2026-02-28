using System;

namespace Raffe.Models;

public class AppConfig
{
    public string CompanyName { get; set; } = "某某公司";
    public int Year { get; set; } = DateTime.Now.Year;

    public string DefaultMusicPath  { get; set; } = "";
    public string SpinningMusicPath { get; set; } = "";
    public string WinnerMusicPath   { get; set; } = "";
    public double MusicVolume       { get; set; } = 0.7;
}
