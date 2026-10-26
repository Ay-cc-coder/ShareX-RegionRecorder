using System.Text.Json.Serialization;

namespace RegionRecorder;

/// <summary>用户设置，保存在 %APPDATA%\ShareXRegionRecorder\settings.json。</summary>
internal sealed class AppSettings
{
    public string OutputFolder { get; set; } = DefaultOutputFolder;

    /// <summary>录制帧率。</summary>
    public int FramesPerSecond { get; set; } = 30;

    /// <summary>是否把鼠标指针录进画面。</summary>
    public bool IncludeCursor { get; set; } = true;

    /// <summary>是否录制系统声音（WASAPI 环回）。</summary>
    public bool CaptureSystemAudio { get; set; } = true;

    /// <summary>是否录制麦克风。</summary>
    public bool CaptureMicrophone { get; set; }

    /// <summary>麦克风设备 ID；null 表示系统默认通信设备。</summary>
    public string? MicrophoneDeviceId { get; set; }

    /// <summary>开始录制前的倒计时秒数，0 表示立即开始。</summary>
    public int CountdownSeconds { get; set; } = 3;

    [JsonIgnore]
    public static string DefaultOutputFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ShareX 区域录屏");

    /// <summary>把外部来源（配置文件、界面输入）的值收敛到合法范围。</summary>
    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(OutputFolder)) OutputFolder = DefaultOutputFolder;
        try
        {
            OutputFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(OutputFolder.Trim()));
        }
        catch (Exception)
        {
            OutputFolder = DefaultOutputFolder;
        }

        FramesPerSecond = Math.Clamp(FramesPerSecond, 5, 120);
        CountdownSeconds = Math.Clamp(CountdownSeconds, 0, 10);
        if (string.IsNullOrWhiteSpace(MicrophoneDeviceId)) MicrophoneDeviceId = null;
        if (!CaptureMicrophone) MicrophoneDeviceId ??= null;
    }

    public AppSettings Clone() => (AppSettings)MemberwiseClone();
}
