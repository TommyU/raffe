using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var assetsDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "Assets");
Directory.CreateDirectory(assetsDir);

// Create 256x256 base image: golden ball on dark red (lottery theme)
using var baseImg = new Image<Rgba32>(256, 256);
var bg = Color.ParseHex("#1A0800");
var gold = Color.ParseHex("#FFD700");
var goldBorder = Color.ParseHex("#FFCC44");
baseImg.Mutate(ctx =>
{
    ctx.BackgroundColor(bg);
    ctx.Fill(bg);
    var circle = new EllipsePolygon(new PointF(128, 128), 98);
    ctx.Fill(gold, circle);
    ctx.Draw(goldBorder, 3, circle);
});

var sizes = new[] { 16, 32, 48, 256 };
var pngs = new List<(byte[] Data, int W, int H)>();

foreach (var size in sizes)
{
    using var resized = baseImg.Clone(ctx => ctx.Resize(size, size));
    using var ms = new MemoryStream();
    resized.SaveAsPng(ms);
    pngs.Add((ms.ToArray(), size, size));
}

var icoPath = Path.Combine(assetsDir, "app.ico");
await using (var fs = File.Create(icoPath))
await using (var w = new BinaryWriter(fs))
{
    w.Write((byte)0);
    w.Write((byte)0);
    w.Write((short)1);
    w.Write((short)pngs.Count);
    long offset = 6 + 16L * pngs.Count;

    foreach (var entry in pngs)
    {
        w.Write((byte)(entry.W >= 256 ? 0 : entry.W));
        w.Write((byte)(entry.H >= 256 ? 0 : entry.H));
        w.Write((byte)0);
        w.Write((byte)0);
        w.Write((short)0);
        w.Write((short)32);
        w.Write((uint)entry.Data.Length);
        w.Write((uint)offset);
        offset += entry.Data.Length;
    }
    foreach (var entry in pngs)
        w.Write(entry.Data);
}

Console.WriteLine($"Created {icoPath}");
