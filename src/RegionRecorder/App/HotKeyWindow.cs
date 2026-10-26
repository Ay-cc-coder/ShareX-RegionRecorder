namespace RegionRecorder;

/// <summary>隐藏窗口：接收全局快捷键与「第二个实例」广播。</summary>
internal sealed class HotKeyWindow : NativeWindow, IDisposable
{
    private const int HotKeyId = 0x52A1;
    private bool disposed;

    public event Action? StartRegionRequested;

    public HotKeyWindow()
    {
        // 普通（不可见）顶层窗口，才能收到 HWND_BROADCAST。
        CreateHandle(new CreateParams
        {
            Caption = "ShareXRegionRecorder.MessageWindow",
            X = -32000,
            Y = -32000,
            Width = 1,
            Height = 1
        });
    }

    public bool TryRegister(uint modifiers, uint virtualKey) =>
        NativeMethods.RegisterHotKey(Handle, HotKeyId, modifiers, virtualKey);

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotKeyId)
        {
            StartRegionRequested?.Invoke();
            return;
        }

        if (m.Msg == NativeMethods.WM_START_REGION_REQUEST)
        {
            StartRegionRequested?.Invoke();
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        NativeMethods.UnregisterHotKey(Handle, HotKeyId);
        DestroyHandle();
    }
}
