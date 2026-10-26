using System.Diagnostics;
using System.Reflection;
using ShareX.ScreenRecordingLib;

namespace RegionRecorder;

/// <summary>托盘程序：把「框选 → 倒计时 → 录制 → 保存」串起来，不显示主窗口。</summary>
internal sealed class TrayApp : ApplicationContext
{
    private const string AppTitle = "ShareX 区域录屏";

    private readonly ToolStripMenuItem recordItem;
    private readonly NotifyIcon tray;
    private readonly Icon icon;
    private readonly HotKeyWindow hotKeyWindow;

    private AppSettings settings;
    private RecordingSession? session;
    private RecordingBar? bar;
    private string? lastOutput;
    private bool busy;

    public TrayApp()
    {
        bool firstRun = !File.Exists(SettingsStore.SettingsPath);
        settings = SettingsStore.Load();
        icon = LoadIcon();

        hotKeyWindow = new HotKeyWindow();
        hotKeyWindow.StartRegionRequested += OnStartRequested;
        hotKeyWindow.SecondInstanceRequested += OnSecondInstance;

        recordItem = new ToolStripMenuItem("录制区域") { ShortcutKeyDisplayString = "Ctrl+Shift+R" };
        recordItem.Click += async (_, _) => await SafeToggleAsync();

        ContextMenuStrip menu = new() { ShowImageMargin = false, Font = Theme.Font(9.5f) };
        menu.Items.Add(recordItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateItem("设置…", (_, _) => ShowSettings()));
        menu.Items.Add(CreateItem("打开保存位置", (_, _) => OpenOutputFolder()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateItem("关于", (_, _) => ShowAbout()));
        menu.Items.Add(CreateItem("退出", (_, _) => ExitApp()));

        tray = new NotifyIcon
        {
            Icon = icon,
            Text = AppTitle,
            ContextMenuStrip = menu,
            Visible = true
        };
        tray.DoubleClick += async (_, _) => await SafeToggleAsync();
        tray.BalloonTipClicked += (_, _) => OpenLastOutput();

        if (!hotKeyWindow.TryRegister(NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, (uint)Keys.R))
        {
            tray.ShowBalloonTip(4000, AppTitle,
                "全局快捷键 Ctrl+Shift+R 被其他程序占用，请用托盘菜单开始录制。",
                ToolTipIcon.Warning);
        }

        UpdateState();

        if (firstRun)
        {
            tray.ShowBalloonTip(6000, AppTitle,
                "已启动：按 Ctrl+Shift+R 或右键托盘图标开始框选录制。",
                ToolTipIcon.Info);
        }
    }

    private static ToolStripMenuItem CreateItem(string text, EventHandler handler)
    {
        ToolStripMenuItem item = new(text);
        item.Click += handler;
        return item;
    }

    private void OnStartRequested() => _ = SafeToggleAsync();

    private void OnSecondInstance()
    {
        // 已经在录制时忽略重复启动，避免误停当前录制。
        if (session != null) return;
        _ = SafeToggleAsync();
    }

    private async Task SafeToggleAsync()
    {
        try
        {
            if (session != null)
            {
                await StopRecordingAsync();
            }
            else
            {
                await StartRegionFlowAsync();
            }
        }
        catch (Exception ex)
        {
            ShowError("录制出错", ex);
        }
    }

    private async Task StartRegionFlowAsync()
    {
        if (busy || session != null) return;

        busy = true;
        try
        {
            // 托盘菜单刚收起，稍等一拍再截图，避免把菜单拍进背景。
            await Task.Delay(120);

            Rectangle virtualScreen = NativeMethods.GetVirtualScreenBounds();
            using Bitmap screenshot = ScreenCapture.Capture(virtualScreen);

            Rectangle region;
            using (RegionSelectorForm selector = new(virtualScreen, screenshot))
            {
                Rectangle? selected = await selector.SelectAsync();
                if (selected is null) return;
                region = selected.Value;
            }

            region = Rectangle.Intersect(region, virtualScreen);
            if (region.Width < 8 || region.Height < 8) return;

            if (settings.CountdownSeconds > 0)
            {
                using CountdownForm countdown = new(region, settings.CountdownSeconds, virtualScreen, screenshot);
                if (!await countdown.RunAsync()) return;
            }

            await StartRecordingAsync(region);
        }
        catch (Exception ex)
        {
            ShowError("无法开始录制", ex);
        }
        finally
        {
            busy = false;
        }
    }

    private async Task StartRecordingAsync(Rectangle region)
    {
        // H.264 要求宽高为偶数；同时保证不超出虚拟桌面。
        Rectangle evenRegion = new(region.X, region.Y, Math.Max(2, region.Width & ~1), Math.Max(2, region.Height & ~1));

        string path = BuildOutputPath();
        long bitrate = Math.Clamp((long)evenRegion.Width * evenRegion.Height * 6, 4_000_000, 32_000_000);

        // 没有音频端点时自动关掉对应音轨，避免整段录制因音频初始化失败而报错。
        bool systemAudio = settings.CaptureSystemAudio && AudioCaptureDevices.GetDefaultSystemAudioDevice() is not null;
        bool microphone = settings.CaptureMicrophone &&
            (settings.MicrophoneDeviceId is { Length: > 0 } || AudioCaptureDevices.GetDefaultMicrophone() is not null);

        RecordingOptions options = new()
        {
            OutputPath = path,
            Region = evenRegion,
            FramesPerSecond = settings.FramesPerSecond,
            VideoBitrate = (int)bitrate,
            CaptureSystemAudio = systemAudio,
            CaptureMicrophone = microphone,
            MicrophoneDeviceId = microphone ? settings.MicrophoneDeviceId : null,
            IncludeCursor = settings.IncludeCursor,
            // 没有可用硬件编码器时回退到软件编码，而不是直接失败。
            RequireHardwareEncoder = false
        };

        RecordingSession candidate = new(options);
        try
        {
            await candidate.StartAsync();
        }
        catch (Exception ex)
        {
            candidate.Dispose();
            try { File.Delete(path); } catch (IOException) { }
            ShowError("录制启动失败", ex);
            return;
        }

        RecordingBar controlBar = new(candidate);
        controlBar.StopRequested += async (_, _) => await StopRecordingAsync();

        try
        {
            controlBar.Show();
            controlBar.PlaceNear(evenRegion);
        }
        catch (Exception ex)
        {
            // 控制条起不来就放弃本次录制，避免出现无法停止的会话。
            controlBar.Dispose();
            candidate.Abort();
            candidate.Dispose();
            ShowError("无法显示录制控制条", ex);
            return;
        }

        session = candidate;
        bar = controlBar;
        UpdateState();
    }

    private async Task StopRecordingAsync()
    {
        RecordingSession? current = session;
        if (current is null) return;

        session = null;
        busy = true;
        bar?.ShowFinalizing();

        RecordingResult? result = null;
        Exception? failure = null;

        try
        {
            result = await current.StopAsync();
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            RecordingBar? closingBar = bar;
            bar = null;
            closingBar?.Close();
            closingBar?.Dispose();
            current.Dispose();
            busy = false;
            UpdateState();
        }

        if (failure != null)
        {
            ShowError("录制未完成", failure);
            return;
        }

        if (result is null) return;

        lastOutput = result.OutputPath;
        tray.ShowBalloonTip(5000, "录制完成",
            $"{Path.GetFileName(result.OutputPath)}\n{FormatDuration(result.Duration)} · 点击打开",
            ToolTipIcon.Info);
    }

    private string BuildOutputPath()
    {
        Directory.CreateDirectory(settings.OutputFolder);

        string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string path = Path.Combine(settings.OutputFolder, $"录屏_{stamp}.mp4");

        int index = 2;
        while (File.Exists(path))
        {
            path = Path.Combine(settings.OutputFolder, $"录屏_{stamp}_{index++}.mp4");
        }

        return path;
    }

    private static string FormatDuration(TimeSpan duration) => duration.TotalHours >= 1
        ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
        : $"{duration.Minutes:00}:{duration.Seconds:00}";

    private void UpdateState()
    {
        bool recording = session != null;
        recordItem.Text = recording ? "停止录制" : "录制区域";
        recordItem.ShortcutKeyDisplayString = "Ctrl+Shift+R";
        tray.Text = recording ? $"{AppTitle} · 正在录制" : AppTitle;
    }

    private void ShowSettings()
    {
        using SettingsForm form = new(settings);
        if (form.ShowDialog() == DialogResult.OK)
        {
            UpdateState();
        }
    }

    private void ShowAbout()
    {
        string version = Application.ProductVersion.Split('+')[0];
        ShowMessage(
            $"ShareX 区域录屏 {version}\n\n" +
            "只保留 ShareX 的「区域屏幕录制」：框选一块屏幕区域，使用 Windows Graphics Capture\n" +
            "与 Media Foundation（GPU 硬编码 H.264/AAC）直接输出 MP4。\n\n" +
            "本工程衍生自 ShareX（GPLv3），并做了大幅裁剪：\n" +
            "  · 移除截图、标注、图像编辑器、OCR\n" +
            "  · 移除所有上传、分享、历史记录、热键编辑与工具集\n" +
            "  · 移除 FFmpeg 录制路径，只保留原生录制引擎\n\n" +
            "运行要求：Windows 11 22H2 或更高版本。\n\n" +
            "上游项目：https://github.com/ShareX/ShareX",
            AppTitle, MessageBoxIcon.Information);
    }

    private void OpenOutputFolder()
    {
        try
        {
            Directory.CreateDirectory(settings.OutputFolder);
            OpenFolder(settings.OutputFolder);
        }
        catch (Exception ex)
        {
            ShowError("无法打开保存位置", ex);
        }
    }

    private static void OpenFolder(string folder) => StartShell(folder);

    private void OpenLastOutput()
    {
        if (lastOutput is null) return;
        if (File.Exists(lastOutput)) StartShell(lastOutput);
        else OpenOutputFolder();
    }

    private static void StartShell(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ShowError("无法打开", ex);
        }
    }

    private void ExitApp()
    {
        try
        {
            if (session != null)
            {
                session.Abort();
                session.Dispose();
                session = null;
            }
        }
        catch (Exception)
        {
            // 退出时不再打扰用户。
        }

        bar?.Close();
        hotKeyWindow.Dispose();
        tray.Visible = false;
        ExitThread();
    }

    private static void ShowError(string title, Exception exception)
    {
        Exception root = exception;
        while (root.InnerException != null) root = root.InnerException;

        ShowMessage($"{exception.Message}\n\n{root.Message}", title, MessageBoxIcon.Error);
    }

    private static void ShowMessage(string text, string title, MessageBoxIcon icon)
    {
        using Form owner = new()
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterScreen,
            Size = new Size(1, 1),
            TopMost = true,
            Opacity = 0
        };

        owner.Show();
        try
        {
            MessageBox.Show(owner, text, title, MessageBoxButtons.OK, icon);
        }
        finally
        {
            owner.Close();
        }
    }

    private static Icon LoadIcon()
    {
        using Stream? stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("RegionRecorder.Assets.App.ico");

        if (stream is not null) return new Icon(stream);

        return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? (Icon)SystemIcons.Application.Clone();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            hotKeyWindow.Dispose();
            tray.Visible = false;
            tray.Dispose();
            icon.Dispose();
        }

        base.Dispose(disposing);
    }
}
