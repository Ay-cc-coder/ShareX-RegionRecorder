using System.Drawing.Text;

namespace RegionRecorder;

/// <summary>统一的配色与字体，避免各处硬编码。</summary>
internal static class Theme
{
    public static readonly Color Accent = Color.FromArgb(0x3B, 0x82, 0xF6);
    public static readonly Color Recording = Color.FromArgb(0xF0, 0x3B, 0x3B);
    public static readonly Color Surface = Color.FromArgb(0x1C, 0x1C, 0x21);
    public static readonly Color SurfaceRaised = Color.FromArgb(0x2A, 0x2A, 0x31);
    public static readonly Color TextPrimary = Color.FromArgb(0xF5, 0xF5, 0xF7);
    public static readonly Color TextMuted = Color.FromArgb(0x9A, 0x9A, 0xA6);
    public static readonly Color PageBackground = Color.FromArgb(0xF6, 0xF7, 0xF9);
    public static readonly Color PageText = Color.FromArgb(0x1F, 0x23, 0x28);
    public static readonly Color PageMuted = Color.FromArgb(0x5C, 0x63, 0x6B);
    public static readonly Color Border = Color.FromArgb(0xE2, 0xE5, 0xEA);

    public static readonly string FontFamily = ResolveFontFamily();

    public static Font Font(float size, FontStyle style = FontStyle.Regular) => new(FontFamily, size, style, GraphicsUnit.Point);

    private static string ResolveFontFamily()
    {
        foreach (string candidate in new[] { "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI" })
        {
            try
            {
                using FontFamily family = new(candidate);
                if (family.IsStyleAvailable(FontStyle.Regular)) return candidate;
            }
            catch (ArgumentException)
            {
                // 字体不存在时继续尝试下一个。
            }
        }

        return SystemFonts.MessageBoxFont?.FontFamily.Name ?? "Segoe UI";
    }
}
