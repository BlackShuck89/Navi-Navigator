using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NaviNavigator;

internal sealed record NaviSkin(
    string Id,
    string Name,
    Color Top,
    Color Bottom,
    Color Foreground,
    Color Accent,
    Color AccentLight,
    Color TabTop,
    Color TabBottom,
    Color Border
)
{
    internal static readonly NaviSkin Modern = new(
        "modern", "Navi Modern",
        Color.FromArgb(232, 240, 245), Color.FromArgb(178, 200, 214),
        Color.FromArgb(21, 48, 66), Color.FromArgb(0, 110, 119),
        Color.FromArgb(108, 204, 203),
        Color.FromArgb(247, 251, 253), Color.FromArgb(207, 225, 233),
        Color.FromArgb(104, 132, 147));

    internal static readonly NaviSkin Classic = new(
        "classic", "Navi Classic",
        Color.FromArgb(246, 247, 248), Color.FromArgb(201, 210, 217),
        Color.FromArgb(33, 54, 72), Color.FromArgb(35, 94, 123),
        Color.FromArgb(135, 196, 208),
        Color.FromArgb(248, 250, 251), Color.FromArgb(214, 223, 228),
        Color.FromArgb(126, 142, 150));

    internal static readonly NaviSkin Fusion = new(
        "fusion", "Navi Fusion",
        Color.FromArgb(61, 101, 122), Color.FromArgb(20, 41, 61),
        Color.FromArgb(246, 252, 253), Color.FromArgb(24, 165, 173),
        Color.FromArgb(111, 222, 221),
        Color.FromArgb(66, 121, 138), Color.FromArgb(30, 60, 83),
        Color.FromArgb(12, 32, 45));

    internal static readonly IReadOnlyList<NaviSkin> All = [Modern, Classic, Fusion];

    internal static NaviSkin Get(string? id) =>
        All.FirstOrDefault(s => s.Id == id) ?? Modern;
}

internal sealed class NaviColorTable(NaviSkin skin) : ProfessionalColorTable
{
    public override Color ToolStripGradientBegin => skin.Top;
    public override Color ToolStripGradientMiddle => skin.Top;
    public override Color ToolStripGradientEnd => skin.Bottom;
    public override Color MenuStripGradientBegin => skin.Top;
    public override Color MenuStripGradientEnd => skin.Bottom;
    public override Color ImageMarginGradientBegin => skin.Top;
    public override Color ImageMarginGradientMiddle => skin.Top;
    public override Color ImageMarginGradientEnd => skin.Bottom;
    public override Color ToolStripBorder => skin.Border;
    public override Color MenuBorder => skin.Border;
    public override Color MenuItemBorder => skin.Accent;
    public override Color MenuItemSelected => skin.AccentLight;
    public override Color MenuItemSelectedGradientBegin => skin.AccentLight;
    public override Color MenuItemSelectedGradientEnd => skin.AccentLight;
    public override Color ButtonSelectedGradientBegin => skin.AccentLight;
    public override Color ButtonSelectedGradientMiddle => skin.AccentLight;
    public override Color ButtonSelectedGradientEnd => skin.AccentLight;
    public override Color ButtonPressedGradientBegin => skin.AccentLight;
    public override Color ButtonPressedGradientMiddle => skin.Accent;
    public override Color ButtonPressedGradientEnd => skin.Accent;
    public override Color SeparatorDark => skin.Border;
    public override Color SeparatorLight => skin.Top;
}

internal static class NaviArt
{
    // Original vector-like emblem drawn at runtime; no Netscape/Firefox trademark assets.
    internal static Icon MakeIcon()
    {
        var bitmap = new Bitmap(64, 64);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var dark = new SolidBrush(Color.FromArgb(18, 45, 64));
            using var teal = new SolidBrush(Color.FromArgb(31, 161, 165));
            using var pearl = new SolidBrush(Color.FromArgb(228, 251, 249));
            using var violet = new SolidBrush(Color.FromArgb(106, 90, 205));
            g.FillEllipse(dark, 1, 1, 62, 62);
            g.FillEllipse(pearl, 6, 6, 52, 52);
            g.FillEllipse(teal, 9, 9, 46, 46);
            g.FillPolygon(pearl, [new Point(32, 9), new Point(38, 32), new Point(32, 55), new Point(26, 32)]);
            g.FillPolygon(violet, [new Point(9, 32), new Point(32, 26), new Point(55, 32), new Point(32, 38)]);
            g.FillEllipse(dark, 28, 28, 8, 8);
        }
        var handle = bitmap.GetHicon();
        // Icon.Clone creates an owned icon before releasing the underlying HICON.
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
            bitmap.Dispose();
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}
