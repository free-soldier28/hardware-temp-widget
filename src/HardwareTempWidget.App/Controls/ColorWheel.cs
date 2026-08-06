using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace HardwareTempWidget.App.Controls;

/// <summary>
/// A circular HSV color wheel. Angle selects hue, distance from the center
/// selects saturation. Value is kept from the current color.
/// </summary>
public sealed class ColorWheel : Control
{
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<ColorWheel, Color>(nameof(Color), Colors.Black);

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public event EventHandler<Color>? ColorChanged;

    private WriteableBitmap? _wheelBitmap;
    private int _bitmapSize;
    private bool _dragging;

    public ColorWheel()
    {
        ClipToBounds = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ColorProperty ||
            change.Property == BoundsProperty)
        {
            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = Math.Min(availableSize.Width, availableSize.Height);
        if (double.IsInfinity(size) || double.IsNaN(size))
        {
            size = 240;
        }
        return new Size(size, size);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _dragging = true;
        e.Pointer.Capture(this);
        UpdateColorFromPosition(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_dragging)
        {
            UpdateColorFromPosition(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _dragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    public override void Render(DrawingContext context)
    {
        var size = (int)Bounds.Width;
        if (size <= 0)
        {
            return;
        }

        if (size != _bitmapSize)
        {
            _wheelBitmap = GenerateWheel(size);
            _bitmapSize = size;
        }

        var rect = new Rect(0, 0, size, size);
        context.DrawImage(_wheelBitmap, rect, rect);

        var cx = Bounds.Width / 2;
        var cy = Bounds.Height / 2;
        var radius = (Bounds.Width / 2) - 1;

        var hsv = Color.ToHsv();
        var rad = hsv.H * Math.PI / 180.0;
        var satDist = radius * hsv.S;
        var px = cx + satDist * Math.Cos(rad);
        var py = cy + satDist * Math.Sin(rad);

        context.DrawEllipse(Brushes.White, new Pen(Brushes.Black, 2), new Rect(px - 6, py - 6, 12, 12));
        context.DrawEllipse(Brushes.Black, new Pen(Brushes.White, 1), new Rect(px - 3, py - 3, 6, 6));
    }

    private void UpdateColorFromPosition(Point p)
    {
        var cx = Bounds.Width / 2;
        var cy = Bounds.Height / 2;
        var radius = (Bounds.Width / 2) - 1;

        var dx = p.X - cx;
        var dy = p.Y - cy;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 1)
        {
            return;
        }

        var sat = Math.Clamp(dist / radius, 0.0, 1.0);

        var angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        var hue = angle < 0 ? angle + 360.0 : angle;

        var newColor = FromHsv(hue, sat, 1.0);

        Color = newColor;
        ColorChanged?.Invoke(this, newColor);
    }

    private static Color FromHsv(double h, double s, double v)
    {
        var cc = new HsvColor(1.0, h, s, v);
        return Color.FromArgb(255, cc.ToRgb().R, cc.ToRgb().G, cc.ToRgb().B);
    }

    private static WriteableBitmap GenerateWheel(int size)
    {
        var center = size / 2.0;
        var radius = size / 2.0;
        var bitmap = new WriteableBitmap(new PixelSize(size, size),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using (var fb = bitmap.Lock())
        {
            var data = new byte[fb.RowBytes * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var dist = Math.Sqrt(dx * dx + dy * dy);

                    byte b = 0, g = 0, r = 0, a = 0;
                    if (dist < radius)
                    {
                        var angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                        var hue = angle < 0 ? angle + 360.0 : angle;
                        var sat = dist / radius;
                        var c = FromHsv(hue, sat, 1.0);
                        r = c.R;
                        g = c.G;
                        b = c.B;
                        a = 255;
                    }

                    var offset = y * fb.RowBytes + x * 4;
                    data[offset] = b;
                    data[offset + 1] = g;
                    data[offset + 2] = r;
                    data[offset + 3] = a;
                }
            }

            Marshal.Copy(data, 0, fb.Address, data.Length);
        }

        return bitmap;
    }
}