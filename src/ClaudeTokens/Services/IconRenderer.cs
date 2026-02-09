using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SkiaSharp;

namespace ClaudeTokens.Services;

public static class IconRenderer
{
    /// <summary>
    /// Renders a menu bar icon with percentage text (e.g. "73%").
    /// White text on transparent background, suitable for macOS dark menu bar.
    /// </summary>
    public static WindowIcon RenderPercentageIcon(int percent)
    {
        var text = $"{percent}%";

        // Menu bar height is ~22pt; we render at 2x for Retina
        int height = 44;
        int width = text.Length <= 3 ? 80 : 100; // wider for "100%"

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = 28,
            Typeface = SKTypeface.FromFamilyName("SF Pro", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                       ?? SKTypeface.FromFamilyName("Helvetica Neue", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                       ?? SKTypeface.Default,
            TextAlign = SKTextAlign.Center,
        };

        // Center vertically
        var textBounds = new SKRect();
        paint.MeasureText(text, ref textBounds);
        float y = (height + textBounds.Height) / 2f;

        canvas.DrawText(text, width / 2f, y, paint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var stream = new MemoryStream(data.ToArray());

        return new WindowIcon(new Bitmap(stream));
    }
}
