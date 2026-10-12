#:package SkiaSharp
#:package Svg.Skia

/*
  Draws the installers' artwork from the apps' logos in Assets, into Assets\Installer - for Pack-Installer.ps1 to hand to vpk:

    <App>_Dialog.bmp   behind the .msi's welcome and finish pages (--msiDialogBackground), 493x312 at 100%.  The page's text is
                       written over the right of it, so only the left is drawn on.
    <App>_Banner.bmp   across the top of the .msi's other pages (--msiTopBanner), 493x58 at 100%.  The page's title is written
                       over the left of it.
    <App>_Splash.png   shown by the setup program while it installs (--splashImage), with the progress bar along the bottom.

  The .msi's images have to be 24-bit BMPs.  Windows Installer stretches them to the dialog, which is bigger than 493x312 at
  a display scale over 100% - so they're drawn at twice the size, to be scaled down rather than up and blurred.  Run from the repository's root, after changing a logo or the look:

    dotnet run Scripts/InstallerArt.cs
*/

using SkiaSharp;
using Svg.Skia;

if (!Directory.Exists("Assets")) { Console.Error.WriteLine("Run from the repository's root."); return 1; }
var outDir = Path.Combine("Assets", "Installer");
Directory.CreateDirectory(outDir);

var dark = SKColor.Parse("#0e416c");
var blue = SKColor.Parse("#0063a3");
var light = SKColor.Parse("#217cbb");
var pale = SKColor.Parse("#cfe3f3");
const float msiScale = 2;

foreach (var (app, logo, subtitle) in new[] { ("GUI", "DBADash.svg", "Monitoring for SQL Server"), ("Visualizer", "PlanViewer.svg", "Visualizer") })
{
    using var svg = new SKSvg();
    svg.Load(Path.Combine("Assets", logo));

    // The .msi's welcome and finish pages: a brand panel on the left, white where the page's text goes
    Save(493, 312, msiScale, $"{app}_Dialog.bmp", c =>
    {
        const float panel = 164;
        c.Clear(SKColors.White);
        Background(c, svg, new SKRect(0, 0, panel, 312), new SKRect(-60, 170, 160, 390));
        Brand(c, svg, panel / 2, 44, 92, 22, 13, subtitle);
        using var edge = new SKPaint { Color = light };
        c.DrawRect(panel - 3, 0, 3, 312, edge);
    });

    // The .msi's other pages: white where the page's title goes, the logo on the right
    Save(493, 58, msiScale, $"{app}_Banner.bmp", c =>
    {
        c.Clear(SKColors.White);
        using (var fade = new SKPaint { Shader = SKShader.CreateLinearGradient(new(330, 0), new(493, 0), [SKColors.White, SKColor.Parse("#e3eff9")], SKShaderTileMode.Clamp) })
            c.DrawRect(330, 0, 163, 58, fade);
        DrawSvg(c, svg, new SKRect(441, 9, 481, 49));
        using var line = new SKPaint { Shader = SKShader.CreateLinearGradient(new(0, 0), new(493, 0), [dark, blue, light], SKShaderTileMode.Clamp) };
        c.DrawRect(0, 55, 493, 3, line);
    });

    // The setup program's splash - its progress bar is drawn over the bottom, so nothing goes there
    Save(480, 280, 1, $"{app}_Splash.png", c =>
    {
        Background(c, svg, new SKRect(0, 0, 480, 280), new SKRect(250, 60, 560, 370));
        Brand(c, svg, 240, 40, 104, 30, 15, subtitle);
    });
}
Console.WriteLine($"Written to {Path.GetFullPath(outDir)}");
return 0;

// The blue gradient, with a faint oversized logo in it
void Background(SKCanvas c, SKSvg svg, SKRect area, SKRect watermark)
{
    using (var p = new SKPaint { Shader = SKShader.CreateLinearGradient(new(area.Left, area.Top), new(area.Right, area.Bottom), [dark, blue], SKShaderTileMode.Clamp) })
        c.DrawRect(area, p);
    c.Save();
    c.ClipRect(area);
    using (var faint = new SKPaint { Color = SKColors.White.WithAlpha(22) })
    {
        c.SaveLayer(faint);
        DrawSvg(c, svg, watermark);
        c.Restore();
    }
    c.Restore();
}

// The logo on a white tile, "DBA Dash" and the app under it, and the logo's status dots - centred on x
void Brand(SKCanvas c, SKSvg svg, float x, float top, float tileSize, float titleSize, float subtitleSize, string subtitle)
{
    var tile = new SKRect(x - tileSize / 2, top, x + tileSize / 2, top + tileSize);
    var radius = tileSize / 5;
    using (var shadow = new SKPaint { Color = SKColors.Black.WithAlpha(60), MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 6), IsAntialias = true })
        c.DrawRoundRect(new SKRect(tile.Left, tile.Top + 4, tile.Right, tile.Bottom + 4), radius, radius, shadow);
    using (var white = new SKPaint { Color = SKColors.White, IsAntialias = true })
        c.DrawRoundRect(tile, radius, radius, white);
    var inner = tile;
    inner.Inflate(-tileSize * 0.15f, -tileSize * 0.15f);
    DrawSvg(c, svg, inner);

    using var titleFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), titleSize) { Edging = SKFontEdging.SubpixelAntialias };
    using var subtitleFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal), subtitleSize) { Edging = SKFontEdging.SubpixelAntialias };
    using var titlePaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
    using var subtitlePaint = new SKPaint { Color = pale, IsAntialias = true };
    var y = tile.Bottom + titleSize * 1.6f;
    c.DrawText("DBA Dash", x, y, SKTextAlign.Center, titleFont, titlePaint);
    y += subtitleSize * 1.55f;
    c.DrawText(subtitle, x, y, SKTextAlign.Center, subtitleFont, subtitlePaint);

    y += subtitleSize * 1.5f;
    var dots = new[] { "#006638", "#e49325", "#da212c" };
    for (var i = 0; i < dots.Length; i++)
    {
        using var dot = new SKPaint { Color = SKColor.Parse(dots[i]), IsAntialias = true };
        c.DrawCircle(x + (i - 1) * 14, y, 4, dot);
    }
}

static void DrawSvg(SKCanvas c, SKSvg svg, SKRect dest)
{
    var bounds = svg.Picture!.CullRect;
    var scale = Math.Min(dest.Width / bounds.Width, dest.Height / bounds.Height);
    c.Save();
    c.Translate(dest.MidX - bounds.MidX * scale, dest.MidY - bounds.MidY * scale);
    c.Scale(scale);
    c.DrawPicture(svg.Picture);
    c.Restore();
}

// Drawn in width x height units, at scale times that in pixels
void Save(int width, int height, float scale, string name, Action<SKCanvas> draw)
{
    using var bmp = new SKBitmap((int)(width * scale), (int)(height * scale));
    using (var canvas = new SKCanvas(bmp))
    {
        canvas.Scale(scale);
        draw(canvas);
    }
    var path = Path.Combine(outDir, name);
    if (name.EndsWith(".png"))
    {
        using var png = File.Create(path);
        bmp.Encode(png, SKEncodedImageFormat.Png, 100);
    }
    else WriteBmp(bmp, path);
}

// A 24-bit bottom-up BMP, which is what the .msi's dialogs take - Skia can't write BMPs
static void WriteBmp(SKBitmap bmp, string path)
{
    int w = bmp.Width, h = bmp.Height, stride = (w * 3 + 3) & ~3;
    using var f = new BinaryWriter(File.Create(path));
    f.Write((byte)'B'); f.Write((byte)'M'); f.Write(54 + stride * h); f.Write(0); f.Write(54);
    f.Write(40); f.Write(w); f.Write(h); f.Write((short)1); f.Write((short)24); f.Write(0); f.Write(stride * h); f.Write(2835); f.Write(2835); f.Write(0); f.Write(0);
    var row = new byte[stride];
    for (var y = h - 1; y >= 0; y--)
    {
        for (var x = 0; x < w; x++)
        {
            var p = bmp.GetPixel(x, y);
            row[x * 3] = p.Blue; row[x * 3 + 1] = p.Green; row[x * 3 + 2] = p.Red;
        }
        f.Write(row);
    }
}
