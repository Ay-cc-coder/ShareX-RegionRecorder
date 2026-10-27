namespace RegionRecorder;

/// <summary>录制中悬浮在选区右上角的小控制条：计时、暂停/继续、停止。</summary>
internal sealed class RecordingBar : Form
{
    private readonly RecordingSession session;
    private readonly Label timerLabel;
    private readonly Button pauseButton;
    private readonly Button stopButton;
    private readonly System.Windows.Forms.Timer ticker = new() { Interval = 250 };
    private readonly System.Windows.Forms.Timer blinkTimer = new() { Interval = 500 };

    private bool finalizing;
    private bool blinkOn = true;

    public event EventHandler? StopRequested;

    /// <summary>暂停状态变化，用于让选区边框同步换色。</summary>
    public event Action<bool>? PauseChanged;

    public RecordingBar(RecordingSession session)
    {
        this.session = session;

        AutoScaleMode = AutoScaleMode.Font;
        AutoScaleDimensions = new SizeF(96f, 96f);
        ClientSize = new Size(372, 64);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Theme.Surface;
        Font = Theme.Font(9.5f);

        timerLabel = new Label
        {
            AutoSize = false,
            Bounds = new Rectangle(46, 15, 112, 34),
            Font = Theme.Font(15f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Text = "00:00",
            TextAlign = ContentAlignment.MiddleLeft
        };

        pauseButton = CreateButton("暂停", new Rectangle(164, 14, 92, 36), Theme.SurfaceRaised);
        pauseButton.Click += (_, _) => TogglePause();

        stopButton = CreateButton("停止", new Rectangle(266, 14, 90, 36), Theme.Recording);
        stopButton.Click += (_, _) =>
        {
            if (finalizing) return;
            StopRequested?.Invoke(this, EventArgs.Empty);
        };

        Controls.Add(timerLabel);
        Controls.Add(pauseButton);
        Controls.Add(stopButton);

        ticker.Tick += (_, _) => UpdateTimer();
        ticker.Start();
        blinkTimer.Tick += (_, _) => { blinkOn = !blinkOn; Invalidate(new Rectangle(14, 22, 20, 20)); };
        blinkTimer.Start();

        UpdateTimer();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW：不出现在 Alt+Tab
            cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE：不抢焦点
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // 控制条不能出现在自己的录像里。
        NativeMethods.ExcludeFromCapture(Handle);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        NativeMethods.ExcludeFromCapture(Handle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using Pen border = new(Color.FromArgb(48, 255, 255, 255));
        e.Graphics.DrawRectangle(border, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);

        using SolidBrush dot = new(session.IsPaused
            ? Color.FromArgb(255, 176, 32)
            : blinkOn ? Theme.Recording : Color.FromArgb(70, Theme.Recording));
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.FillEllipse(dot, 16, 27, 12, 12);
    }

    /// <summary>
    /// 优先把控制条放在选区外面（上 → 下 → 右 → 左），避免挡住正在录的内容；
    /// 只有屏幕实在放不下时才压进选区。坐标为物理像素。
    /// </summary>
    public void PlaceNear(Rectangle region, Rectangle virtualScreen)
    {
        Size size = Size;
        const int gap = 10;
        const int edge = 4;

        int maxX = virtualScreen.Right - size.Width - edge;
        int maxY = virtualScreen.Bottom - size.Height - edge;
        int minX = virtualScreen.Left + edge;
        int minY = virtualScreen.Top + edge;

        int x = Math.Clamp(region.Right - size.Width, minX, Math.Max(minX, maxX));
        int above = region.Top - gap - size.Height;
        int below = region.Bottom + gap;

        int y;
        if (above >= minY)
        {
            y = above;
        }
        else if (below + size.Height <= virtualScreen.Bottom - edge)
        {
            y = below;
        }
        else
        {
            int right = region.Right + gap;
            int left = region.Left - gap - size.Width;

            if (right + size.Width <= virtualScreen.Right - edge)
            {
                x = right;
                y = Math.Clamp(region.Top, minY, Math.Max(minY, maxY));
            }
            else if (left >= minX)
            {
                x = left;
                y = Math.Clamp(region.Top, minY, Math.Max(minY, maxY));
            }
            else
            {
                // 屏幕边缘的小选区：只能压进选区，此时靠边框识别录制范围。
                x = Math.Clamp(region.Left + gap, minX, Math.Max(minX, maxX));
                y = Math.Clamp(region.Top + gap, minY, Math.Max(minY, maxY));
            }
        }

        NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, x, y, 0, 0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    /// <summary>停止后进入「保存中」状态，避免用户重复点击。</summary>
    public void ShowFinalizing()
    {
        if (finalizing) return;
        finalizing = true;
        ticker.Stop();
        blinkTimer.Stop();
        timerLabel.Text = "保存中…";
        pauseButton.Enabled = false;
        stopButton.Enabled = false;
        Invalidate();
    }

    private void TogglePause()
    {
        if (finalizing) return;

        if (session.IsPaused)
        {
            session.Resume();
            pauseButton.Text = "暂停";
        }
        else
        {
            session.Pause();
            pauseButton.Text = "继续";
        }

        UpdateTimer();
        PauseChanged?.Invoke(session.IsPaused);
    }

    private void UpdateTimer()
    {
        if (finalizing) return;
        timerLabel.Text = Format(session.Elapsed);
        if (session.IsPaused) timerLabel.ForeColor = Color.FromArgb(255, 176, 32);
        else timerLabel.ForeColor = Color.White;
    }

    private static string Format(TimeSpan elapsed) => elapsed.TotalHours >= 1
        ? $"{(int)elapsed.TotalHours}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
        : $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";

    private static Button CreateButton(string text, Rectangle bounds, Color background) => new()
    {
        Text = text,
        Bounds = bounds,
        FlatStyle = FlatStyle.Flat,
        FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(60, 60, 70), MouseDownBackColor = Color.FromArgb(45, 45, 55) },
        BackColor = background,
        ForeColor = Color.White,
        UseVisualStyleBackColor = false,
        Font = Theme.Font(10f, FontStyle.Bold),
        TabStop = false
    };

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ticker.Dispose();
            blinkTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
