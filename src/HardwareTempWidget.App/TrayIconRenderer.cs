using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace HardwareTempWidget.App;

internal static class TrayIconRenderer
{
    private const int Size = 32;

    public static WindowIcon Render(float? celsius)
    {
        var pixelSize = new PixelSize(Size, Size);
        using var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96, 96));

        using (var context = bitmap.CreateDrawingContext())
        {
            context.FillRectangle(new SolidColorBrush(Color.Parse("#1E1E28")), new Rect(0, 0, Size, Size));

            var text = celsius is { } value ? $"{value:F0}" : "--";
            var typeface = new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold);
            var formatted = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 15, new SolidColorBrush(ColorFor(celsius)))
            {
                TextAlignment = TextAlignment.Center,
            };

            var origin = new Point((Size - formatted.Width) / 2, (Size - formatted.Height) / 2);
            context.DrawText(formatted, origin);
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream);
        stream.Position = 0;
        return new WindowIcon(stream);
    }

    private static Color ColorFor(float? celsius) => celsius switch
    {
        null => Colors.Gray,
        <= 60 => Colors.LimeGreen,
        <= 80 => Colors.Orange,
        _ => Colors.OrangeRed,
    };
}
