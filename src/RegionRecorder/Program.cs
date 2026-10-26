using System.Threading;

namespace RegionRecorder;

internal static class Program
{
    private const string MutexName = @"Local\ShareXRegionRecorder.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using Mutex mutex = new(true, MutexName, out bool isFirstInstance);

        if (!isFirstInstance)
        {
            // 已有实例在运行：让它直接进入框选，而不是再开一个托盘图标。
            NativeMethods.PostMessage(NativeMethods.HWND_BROADCAST, NativeMethods.WM_START_REGION_REQUEST, IntPtr.Zero, IntPtr.Zero);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.ThreadException += (_, e) => MessageBox.Show(e.Exception.Message, "ShareX 区域录屏",
            MessageBoxButtons.OK, MessageBoxIcon.Error);

        using TrayApp context = new();
        Application.Run(context);
    }
}
