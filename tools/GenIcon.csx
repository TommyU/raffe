#!/usr/bin/env dotnet script
#r "nuget: System.Drawing.Common, 8.0.0"
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "Assets");
Directory.CreateDirectory(dir);
var icoPath = Path.Combine(dir, "app.ico");

using var bmp = new Bitmap(256, 256);
using (var g = Graphics.FromImage(bmp))
{
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.Clear(Color.FromArgb(0x1A, 0x08, 0x00));
    using var brush = new LinearGradientBrush(
        new Rectangle(0, 0, 256, 256),
        Color.FromArgb(255, 255, 200, 0),
        Color.FromArgb(255, 255, 140, 0),
        135f);
    g.FillEllipse(brush, 24, 24, 208, 208);
    using var pen = new Pen(Color.FromArgb(180, 255, 200, 50), 4);
    g.DrawEllipse(pen, 26, 26, 204, 204);
}
using (var ico = Icon.FromHandle(bmp.GetHicon()))
{
    using var fs = File.Create(icoPath);
    ico.Save(fs);
}
// Icon.FromHandle doesn't support multiple sizes; use simple save
using (var ms = new MemoryStream())
{
    bmp.Save(ms, ImageFormat.Png);
    File.WriteAllBytes(Path.Combine(dir, "app.png"), ms.ToArray());
}
Console.WriteLine($"Created {icoPath}");
