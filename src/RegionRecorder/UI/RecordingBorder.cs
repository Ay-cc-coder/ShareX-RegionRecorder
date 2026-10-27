namespace RegionRecorder;

/// <summary>
/// 录制期间围绕选区的细边框：让用户始终看得见正在录哪一块。
/// 用四条 2px 的独立窗口画在选区**外侧**，并排除出录像、鼠标穿透。
/// </summary>
internal sealed class RecordingBorder : IDisposable
{
    private readonly List<BorderStrip> strips = new(4);

    public RecordingBorder(Rectangle region, Rectangle virtualScreen)
    {
        const int thickness = 2;

        // 优先画在选区外侧；贴边或全屏时退到内侧（同样会被排除出录像）。
        int top = region.Top - thickness >= virtualScreen.Top ? region.Top - thickness : region.Top;
        int bottom = region.Bottom + thickness <= virtualScreen.Bottom ? region.Bottom : region.Bottom - thickness;
        int left = region.Left - thickness >= virtualScreen.Left ? region.Left - thickness : region.Left;
        int right = region.Right + thickness <= virtualScreen.Right ? region.Right : region.Right - thickness;

        AddStrip(new Rectangle(region.Left, top, region.Width, thickness), virtualScreen);
        AddStrip(new Rectangle(region.Left, bottom, region.Width, thickness), virtualScreen);
        AddStrip(new Rectangle(left, region.Top, thickness, region.Height), virtualScreen);
        AddStrip(new Rectangle(right, region.Top, thickness, region.Height), virtualScreen);
    }

    private void AddStrip(Rectangle bounds, Rectangle virtualScreen)
    {
        Rectangle clipped = Rectangle.Intersect(bounds, virtualScreen);
        if (clipped.Width <= 0 || clipped.Height <= 0) return;

        BorderStrip strip = new(clipped);
        strips.Add(strip);
        strip.Show();
    }

    /// <summary>暂停时把边框变成琥珀色，给出明确的状态反馈。</summary>
    public void SetPaused(bool paused)
    {
        Color color = paused ? Color.FromArgb(0xFF, 0xB0, 0x20) : Theme.Accent;
        foreach (BorderStrip strip in strips) strip.BackColor = color;
    }

    public void Dispose()
    {
        foreach (BorderStrip strip in strips)
        {
            strip.Close();
            strip.Dispose();
        }

        strips.Clear();
    }

    private sealed class BorderStrip : Form
    {
        private readonly Rectangle bounds;

        public BorderStrip(Rectangle bounds)
        {
            this.bounds = bounds;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Theme.Accent;
            AutoScaleMode = AutoScaleMode.None;
            Opacity = 0.95;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT：鼠标穿透
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW：不进 Alt+Tab
                cp.ExStyle |= 0x00080000; // WS_EX_LAYERED：配合 Opacity
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE：不抢焦点
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, bounds.X, bounds.Y, bounds.Width, bounds.Height,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
            NativeMethods.ExcludeFromCapture(Handle);
        }
    }
}
