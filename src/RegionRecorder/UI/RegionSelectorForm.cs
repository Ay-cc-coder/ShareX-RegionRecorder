using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace RegionRecorder;

/// <summary>
/// 全屏框选层：先把桌面快照画出来，再压暗，然后让选区恢复原亮度。
/// 所有坐标都是物理像素（与 ShareX 录制引擎的约定一致）。
/// </summary>
internal sealed class RegionSelectorForm : Form
{
    private readonly Rectangle virtualScreen;
    private readonly Bitmap background;
    private readonly TaskCompletionSource<Rectangle?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private Point dragStart;
    private Point dragCurrent;
    private bool dragging;
    private Rectangle? result;

    public RegionSelectorForm(Rectangle virtualScreen, Bitmap background)
    {
        this.virtualScreen = virtualScreen;
        this.background = background;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = new Rectangle(virtualScreen.Location, virtualScreen.Size);
        ShowInTaskbar = false;
        TopMost = true;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        Cursor = Cursors.Cross;
        KeyPreview = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    public Task<Rectangle?> SelectAsync()
    {
        Show();
        Activate();
        return completion.Task;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // 再压一次，确保覆盖整个虚拟桌面而不是默认窗口尺寸。
        NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, virtualScreen.X, virtualScreen.Y,
            virtualScreen.Width, virtualScreen.Height, NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        Activate();
        Focus();
    }

    private Rectangle SelectionRectangle
    {
        get
        {
            if (!dragging) return Rectangle.Empty;
            int left = Math.Min(dragStart.X, dragCurrent.X);
            int top = Math.Min(dragStart.Y, dragCurrent.Y);
            int right = Math.Max(dragStart.X, dragCurrent.X);
            int bottom = Math.Max(dragStart.Y, dragCurrent.Y);
            return Rectangle.FromLTRB(left, top, right, bottom);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;

        Rectangle client = ClientRectangle;
        g.DrawImage(background, client);

        Rectangle selection = SelectionRectangle;

        using (SolidBrush dim = new(Color.FromArgb(126, 10, 12, 16)))
        {
            if (selection.Width < 2 || selection.Height < 2)
            {
                g.FillRectangle(dim, client);
            }
            else
            {
                using (Region outside = new(client))
                {
                    outside.Exclude(selection);
                    g.FillRegion(dim, outside);
                }

                g.SetClip(selection);
                g.DrawImage(background, client);
                g.ResetClip();

                using Pen border = new(Theme.Accent, 2f);
                g.DrawRectangle(border, selection.X, selection.Y, selection.Width - 1, selection.Height - 1);

                DrawSizeBadge(g, selection);
            }
        }

        if (dragging)
        {
            using Pen guide = new(Color.FromArgb(120, 255, 255, 255)) { DashStyle = DashStyle.Dash };
            g.DrawLine(guide, dragCurrent.X, 0, dragCurrent.X, client.Height);
            g.DrawLine(guide, 0, dragCurrent.Y, client.Width, dragCurrent.Y);
        }

        DrawHint(g, client);
    }

    private void DrawSizeBadge(Graphics g, Rectangle selection)
    {
        string text = $"{selection.Width} × {selection.Height}";
        using Font font = Theme.Font(10f, FontStyle.Bold);
        Size size = TextRenderer.MeasureText(text, font);
        const int paddingX = 12;
        const int paddingY = 6;
        int width = size.Width + paddingX * 2;
        int height = size.Height + paddingY * 2;

        int x = selection.Right - width - 10;
        int y = selection.Bottom - height - 10;
        if (x < selection.Left + 4) x = selection.Left + 4;
        if (y < selection.Top + 4) y = selection.Top + 4;

        Rectangle badge = new(x, y, width, height);
        using (GraphicsPath path = RoundedRect(badge, 6))
        using (SolidBrush fill = new(Color.FromArgb(220, 17, 19, 24)))
        {
            g.FillPath(fill, path);
        }

        TextRenderer.DrawText(g, text, font, badge, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    private void DrawHint(Graphics g, Rectangle client)
    {
        string text = dragging ? "松开鼠标开始录制 · Esc 取消" : "拖动鼠标框选录制区域 · Esc 取消";
        using Font font = Theme.Font(9.5f);
        Size size = TextRenderer.MeasureText(text, font);
        Rectangle pill = new(
            client.Width / 2 - (size.Width + 40) / 2,
            28,
            size.Width + 40,
            size.Height + 20);

        using (GraphicsPath path = RoundedRect(pill, pill.Height / 2))
        using (SolidBrush fill = new(Color.FromArgb(225, 17, 19, 24)))
        {
            g.FillPath(fill, path);
        }

        TextRenderer.DrawText(g, text, font, pill, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    internal static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        GraphicsPath path = new();
        int diameter = Math.Max(1, radius * 2);
        if (diameter > bounds.Width) diameter = bounds.Width;
        if (diameter > bounds.Height) diameter = bounds.Height;

        Rectangle arc = new(bounds.X, bounds.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.X;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Right)
        {
            result = null;
            Close();
            return;
        }

        if (e.Button != MouseButtons.Left) return;

        dragging = true;
        dragStart = e.Location;
        dragCurrent = e.Location;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!dragging) return;

        dragCurrent = e.Location;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!dragging || e.Button != MouseButtons.Left) return;

        dragging = false;
        dragCurrent = e.Location;

        Rectangle selection = SelectionRectangle;
        if (selection.Width >= 8 && selection.Height >= 8)
        {
            result = new Rectangle(
                virtualScreen.X + selection.X,
                virtualScreen.Y + selection.Y,
                selection.Width,
                selection.Height);
            Close();
        }
        else
        {
            Invalidate();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
        {
            result = null;
            Close();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        completion.TrySetResult(result);
        base.OnFormClosed(e);
    }
}
