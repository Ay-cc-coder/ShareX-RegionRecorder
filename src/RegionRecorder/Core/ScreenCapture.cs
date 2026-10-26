using System.Drawing.Imaging;

namespace RegionRecorder;

/// <summary>在框选开始前抓一张虚拟桌面快照，避免遮罩层把自己的画面也拍进去。</summary>
internal static class ScreenCapture
{
    public static Bitmap Capture(Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new ArgumentException("虚拟桌面尺寸无效。", nameof(bounds));

        Bitmap bitmap = new(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);
        try
        {
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }
}
