using System.Drawing.Drawing2D;

namespace BrowserChooser.UI.Chooser;

internal static class Painting
{
    public const TextFormatFlags SingleLine =
        TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding |
        TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;

    /// <summary>Converts a size at 96 DPI to device pixels.</summary>
    public static int Scale(float logical, int dpi) => (int)Math.Round(logical * dpi / 96f);

    public static void PrepareForShapes(Graphics graphics)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
    }

    public static void FillRoundedRectangle(Graphics graphics, Color color, RectangleF bounds, float radius)
    {
        using GraphicsPath path = RoundedRectangle(bounds, radius);
        using var brush = new SolidBrush(color);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(Graphics graphics, Color color, float width, RectangleF bounds, float radius)
    {
        using GraphicsPath path = RoundedRectangle(bounds, radius);
        using var pen = new Pen(color, width);
        graphics.DrawPath(pen, path);
    }

    public static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        float diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
