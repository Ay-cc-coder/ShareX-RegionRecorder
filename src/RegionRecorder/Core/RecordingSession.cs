using ShareX.ScreenRecordingLib;

namespace RegionRecorder;

/// <summary>对 ShareX 原生录制器的一层薄封装，界面只与它交互。</summary>
internal sealed class RecordingSession : IDisposable
{
    private readonly ScreenRecorder recorder;

    public RecordingSession(RecordingOptions options)
    {
        recorder = new ScreenRecorder(options);
        OutputPath = options.OutputPath;
    }

    public string OutputPath { get; }

    public TimeSpan Elapsed => recorder.Elapsed;

    public bool IsPaused => recorder.IsPaused;

    public VideoEncoderInfo? Encoder => recorder.Encoder;

    public Task StartAsync(CancellationToken cancellationToken = default) =>
        recorder.StartAsync(cancellationToken);

    public void Pause() => recorder.Pause();

    public void Resume() => recorder.Resume();

    public Task<RecordingResult> StopAsync() => recorder.StopAsync();

    /// <summary>放弃本次录制：停止采集并删除半成品文件。</summary>
    public void Abort() => recorder.RequestAbort();

    public void Dispose() => recorder.Dispose();
}
