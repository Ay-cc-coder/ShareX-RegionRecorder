using System.Drawing.Drawing2D;

namespace RegionRecorder;

/// <summary>录制前的倒计时遮罩：压暗全屏、高亮选区、显示剩余秒数，Esc 可取消。</summary>
internal sealed class CountdownForm : Form
{
    private readonly Rectangle virtualScreen;
    private readonly Rectangle region;
    private readonly Bitmap background;
    private readonly int totalSeconds;
    private int remaining;
    private volatile bool cancelled;

    public CountdownForm(Rectangle region, int seconds, Rectangle virtualScreen, Bitmap background)
    {
        this.region = region;
        this.virtualScreen = virtualScreen;
        this.background = background;
        totalSeconds = Math.Max(0, seconds);
        remaining = totalSeconds;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = new Rectangle(virtualScreen.Location, virtualScreen.Size);
        ShowInTaskbar = false;
        TopMost = true;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        KeyPreview = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    /// <summary>倒计时结束返回 true；按 Esc 或窗口被关闭返回 false。</summary>
    public async Task<bool> RunAsync()
    {
        if (totalSeconds <= 0) return true;

        Show();
        Activate();

        while (remaining > 0 && !cancelled)
        {
            Invalidate();
            await Task.Delay(1000).ConfigureAwait(true);
            if (cancelled) break;
            remaining--;
        }

        if (!IsDisposed) Close();
        return !cancelled;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, virtualScreen.X, virtualScreen.Y,
            virtualScreen.Width, virtualScreen.Height, NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        NativeMethods.ExcludeFromCapture(Handle);
        Activate();
        Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        Rectangle client = ClientRectangle;
        Rectangle local = new(region.X - virtualScreen.X, region.Y - virtualScreen.Y, region.Width, region.Height);

        g.DrawImage(background, client);

        using (SolidBrush dim = new(Color.FromArgb(118, 8, 9, 12)))
        using (Region outside = new(client))
        {
            outside.Exclude(local);
            g.FillRegion(dim, outside);
        }

        using (Pen border = new(Theme.Accent, 2f))
        {
            g.DrawRectangle(border, local.X, local.Y, local.Width - 1, local.Height - 1);
        }

        float fontSize = Math.Clamp(Math.Min(local.Width, local.Height) * 0.42f, 28f, 180f);
        string number = Math.Max(1, remaining).ToString();

        using Font numberFont = Theme.Font(fontSize, FontStyle.Bold);
        SizeF numberSize = g.MeasureString(number, numberFont);
        float centerX = local.Left + local.Width / 2f;
        float centerY = local.Top + local.Height / 2f;
        PointF origin = new(centerX - numberSize.Width / 2f, centerY - numberSize.Height / 2f);

        using (SolidBrush shadow = new(Color.FromArgb(140, 0, 0, 0)))
        {
            g.DrawString(number, numberFont, shadow, origin.X + 2, origin.Y + 3);
        }

        using (SolidBrush white = new(Color.White))
        {
            g.DrawString(number, numberFont, white, origin);
        }

        if (local.Height > 120)
        {
            using Font hintFont = Theme.Font(9.5f);
            SizeF hintSize = g.MeasureString("Esc 取消", hintFont);
            using SolidBrush hintBrush = new(Color.FromArgb(220, 255, 255, 255));
            g.DrawString("Esc 取消", hintFont, hintBrush,
                centerX - hintSize.Width / 2f, centerY + numberSize.Height / 2f + 12);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
        {
            cancelled = true;
            Close();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // 用户从外部关闭时同样视为取消。
        if (remaining > 0) cancelled = true;
        base.OnFormClosing(e);
    }
}
