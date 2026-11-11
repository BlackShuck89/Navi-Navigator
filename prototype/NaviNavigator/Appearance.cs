using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NaviNavigator;

internal sealed class NaviColorTable(NaviSkin skin) : ProfessionalColorTable
{
    public override Color ToolStripGradientBegin => skin.TopColor;
    public override Color ToolStripGradientMiddle => skin.TopColor;
    public override Color ToolStripGradientEnd => skin.BottomColor;
    public override Color MenuStripGradientBegin => skin.TopColor;
    public override Color MenuStripGradientEnd => skin.BottomColor;
    public override Color ImageMarginGradientBegin => skin.MenuBackgroundColor;
    public override Color ImageMarginGradientMiddle => skin.MenuBackgroundColor;
    public override Color ImageMarginGradientEnd => skin.MenuBackgroundColor;
    public override Color ToolStripBorder => skin.BorderColor;
    public override Color MenuBorder => skin.BorderColor;
    public override Color MenuItemBorder => skin.AccentColor;
    public override Color MenuItemSelected => skin.AccentLightColor;
    public override Color MenuItemSelectedGradientBegin => skin.AccentLightColor;
    public override Color MenuItemSelectedGradientEnd => skin.AccentLightColor;
    public override Color ButtonSelectedGradientBegin => skin.AccentLightColor;
    public override Color ButtonSelectedGradientMiddle => skin.AccentLightColor;
    public override Color ButtonSelectedGradientEnd => skin.AccentLightColor;
    public override Color ButtonPressedGradientBegin => skin.AccentLightColor;
    public override Color ButtonPressedGradientMiddle => skin.AccentColor;
    public override Color ButtonPressedGradientEnd => skin.AccentColor;
    public override Color SeparatorDark => skin.BorderColor;
    public override Color SeparatorLight => skin.TopColor;
}

internal sealed class NaviChromeRenderer : ToolStripProfessionalRenderer
{
    private readonly NaviSkin _skin;
    private readonly Bitmap? _toolbarTexture;

    internal NaviChromeRenderer(NaviSkin skin) : base(new NaviColorTable(skin))
    {
        _skin = skin;
        RoundedEdges = false;
        string? texturePath = skin.GetTexturePath();
        if (texturePath is not null)
            _toolbarTexture = NaviArt.LoadPng(texturePath, maxPixels: 1024);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        var rect = new Rectangle(Point.Empty, e.ToolStrip.Size);
        if (rect.Width < 1 || rect.Height < 1) return;

        if (e.ToolStrip is ToolStripDropDown)
        {
            using var back = new SolidBrush(_skin.MenuBackgroundColor);
            e.Graphics.FillRectangle(back, rect);
            return;
        }

        using (var gradient = new LinearGradientBrush(rect, _skin.TopColor,
                   _skin.BottomColor, LinearGradientMode.Vertical))
            e.Graphics.FillRectangle(gradient, rect);

        if (_skin.SurfaceEffect == "gloss")
        {
            var glossRect = new Rectangle(0, 0, rect.Width, rect.Height / 2);
            if (glossRect.Height > 0)
            {
                using var gloss = new LinearGradientBrush(glossRect,
                    Color.FromArgb(100, Color.White), Color.FromArgb(10, Color.White),
                    LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(gloss, glossRect);
            }
        }
        else if (_skin.SurfaceEffect == "bevel")
        {
            using var light = new Pen(Color.FromArgb(195, Color.White));
            e.Graphics.DrawLine(light, 0, 0, rect.Width, 0);
        }

        if (_toolbarTexture is not null)
        {
            using var tile = new TextureBrush(_toolbarTexture, WrapMode.Tile);
            e.Graphics.FillRectangle(tile, rect);
        }
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(_skin.BorderColor);
        var rect = new Rectangle(Point.Empty, e.ToolStrip.Size);
        if (rect.Width < 1 || rect.Height < 1) return;
        if (e.ToolStrip is ToolStripDropDown)
            e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
        else
            e.Graphics.DrawLine(pen, 0, rect.Height - 1, rect.Width, rect.Height - 1);
    }

    protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item is not ToolStripButton button || (!button.Selected && !button.Pressed))
            return;

        var rect = new Rectangle(1, 2, Math.Max(1, button.Width - 3), Math.Max(1, button.Height - 5));
        using var shape = RoundedRectangle(rect, _skin.CornerRadius);
        var top = button.Pressed ? _skin.AccentColor : _skin.AccentLightColor;
        var bottom = button.Pressed ? _skin.BottomColor : _skin.TopColor;
        using var fill = new LinearGradientBrush(rect, top, bottom, LinearGradientMode.Vertical);
        using var border = new Pen(_skin.BorderColor);
        e.Graphics.FillPath(fill, shape);
        e.Graphics.DrawPath(border, shape);
        if (_skin.SurfaceEffect == "bevel")
        {
            using var highlight = new Pen(Color.FromArgb(190, Color.White));
            e.Graphics.DrawLine(highlight, rect.Left + 2, rect.Top + 1, rect.Right - 2, rect.Top + 1);
        }
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected && !e.Item.Pressed) return;
        var rect = new Rectangle(1, 1, Math.Max(1, e.Item.Width - 3), Math.Max(1, e.Item.Height - 3));
        using var back = new SolidBrush(_skin.AccentLightColor);
        using var line = new Pen(_skin.AccentColor);
        e.Graphics.FillRectangle(back, rect);
        e.Graphics.DrawRectangle(line, rect);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (e.Item is ToolStripMenuItem menuItem && menuItem.Selected)
            e.TextColor = _skin.MenuForegroundColor;
        base.OnRenderItemText(e);
    }

    internal static GraphicsPath RoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        radius = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2);
        if (radius <= 1)
        {
            path.AddRectangle(rect);
            return path;
        }
        int diameter = radius * 2;
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal static class NaviArt
{
    internal static Bitmap? LoadPng(string path, int maxPixels = 512)
    {
        try
        {
            if (new FileInfo(path).Length is < 8 or > 1024 * 1024) return null;
            using var stream = File.OpenRead(path);
            Span<byte> magic = stackalloc byte[8];
            if (stream.Read(magic) != magic.Length ||
                !magic.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
                return null;
            stream.Position = 0;
            using var loaded = Image.FromStream(stream, false, true);
            if (loaded.Width > maxPixels || loaded.Height > maxPixels || loaded.Width < 1 || loaded.Height < 1)
                return null;
            return new Bitmap(loaded);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
            or OutOfMemoryException or System.Runtime.InteropServices.ExternalException)
        {
            return null;
        }
    }

    internal static Bitmap MakeButtonIcon(string key, NaviSkin skin, int pixels)
    {
        string? localPath = skin.GetIconPath(key);
        if (localPath is not null)
        {
            using var custom = LoadPng(localPath);
            if (custom is not null)
            {
                var bitmap = new Bitmap(pixels, pixels);
                using var g = Graphics.FromImage(bitmap);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(custom, 0, 0, pixels, pixels);
                return bitmap;
            }
        }

        var output = new Bitmap(pixels, pixels);
        using var graphics = Graphics.FromImage(output);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.ScaleTransform(pixels / 32f, pixels / 32f);
        graphics.Clear(Color.Transparent);

        using var primary = new SolidBrush(skin.AccentColor);
        using var bright = new SolidBrush(skin.AccentLightColor);
        using var navy = new SolidBrush(skin.Id == "fusion" ? Color.FromArgb(12, 36, 52) : Color.FromArgb(24, 56, 78));
        using var pearl = new SolidBrush(Color.FromArgb(244, 254, 254));
        using var stroke = new Pen(skin.Id == "fusion" ? skin.AccentLightColor : skin.ForegroundColor, 2.2f);
        stroke.LineJoin = LineJoin.Round;
        stroke.StartCap = LineCap.Round;
        stroke.EndCap = LineCap.Round;

        switch (key)
        {
            case "back":
            case "forward":
            {
                graphics.FillEllipse(navy, 2, 2, 28, 28);
                graphics.FillEllipse(primary, 4, 4, 24, 24);
                graphics.DrawEllipse(new Pen(skin.AccentLightColor, 1.3f), 5, 5, 22, 22);
                PointF[] left = [new(8, 16), new(19, 7), new(19, 12), new(25, 12),
                    new(25, 20), new(19, 20), new(19, 25)];
                if (key == "forward")
                    left = left.Select(p => new PointF(32 - p.X, p.Y)).ToArray();
                graphics.FillPolygon(pearl, left);
                break;
            }
            case "reload":
            {
                graphics.FillEllipse(primary, 3, 3, 26, 26);
                graphics.DrawArc(new Pen(Color.White, 3.3f) { StartCap = LineCap.Round },
                    8, 8, 16, 16, 40, 285);
                graphics.FillPolygon(pearl,
                    new PointF[] { new(23, 7), new(25, 16), new(16, 13) });
                break;
            }
            case "stop":
            {
                graphics.FillPolygon(primary, new PointF[] {
                    new(11,3), new(21,3), new(29,11), new(29,21),
                    new(21,29), new(11,29), new(3,21), new(3,11)
                });
                using var whitePen = new Pen(Color.White, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                graphics.DrawLine(whitePen, 11, 11, 21, 21);
                graphics.DrawLine(whitePen, 21, 11, 11, 21);
                break;
            }
            case "home":
            {
                graphics.FillPolygon(primary, new PointF[] {
                    new(3,14), new(16,3), new(29,14), new(25,17), new(25,27),
                    new(7,27), new(7,17)
                });
                graphics.FillRectangle(pearl, 9, 16, 14, 10);
                graphics.FillRectangle(navy, 14, 20, 5, 6);
                graphics.DrawLines(stroke, new PointF[] { new(3, 14), new(16, 3), new(29, 14) });
                break;
            }
            case "go":
            {
                graphics.FillEllipse(primary, 2, 2, 28, 28);
                graphics.FillPolygon(pearl, new PointF[] {
                    new(7,13), new(20,13), new(17,8), new(26,16),
                    new(17,24), new(20,19), new(7,19)
                });
                break;
            }
            case "tab":
            {
                graphics.FillRectangle(navy, 5, 4, 19, 18);
                graphics.FillRectangle(bright, 7, 6, 15, 14);
                graphics.FillRectangle(primary, 11, 10, 18, 18);
                graphics.DrawRectangle(stroke, 11, 10, 18, 18);
                graphics.DrawLine(new Pen(Color.White, 2), 16, 19, 25, 19);
                graphics.DrawLine(new Pen(Color.White, 2), 20.5f, 14.5f, 20.5f, 23.5f);
                break;
            }
            case "bookmark":
            {
                PointF[] star = new PointF[10];
                for (int i = 0; i < star.Length; i++)
                {
                    double angle = Math.PI * i / 5 - Math.PI / 2;
                    float radius = (i % 2 == 0) ? 13 : 6.2f;
                    star[i] = new PointF(16 + (float)Math.Cos(angle) * radius,
                        16 + (float)Math.Sin(angle) * radius);
                }
                graphics.FillPolygon(bright, star);
                graphics.DrawPolygon(stroke, star);
                break;
            }
            case "compass":
            default:
            {
                graphics.FillEllipse(navy, 1, 1, 30, 30);
                graphics.FillEllipse(primary, 4, 4, 24, 24);
                graphics.DrawEllipse(new Pen(skin.AccentLightColor, 1.2f), 6, 6, 20, 20);
                graphics.FillPolygon(pearl, new PointF[] {
                    new(16,3), new(20,16), new(16,29), new(12,16)
                });
                graphics.FillPolygon(bright, new PointF[] {
                    new(3,16), new(16,12), new(29,16), new(16,20)
                });
                graphics.FillEllipse(navy, 13, 13, 6, 6);
                break;
            }
        }
        return output;
    }

    // Original vector-like application emblem; no historic browser trademarks.
    internal static Icon MakeIcon()
    {
        using var bitmap = MakeButtonIcon("compass", NaviSkin.Fallback, 64);
        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}
