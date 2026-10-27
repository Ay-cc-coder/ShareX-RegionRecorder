namespace RegionRecorder;

/// <summary>极简文件日志：出问题时用户可以把它发回来定位，不收集任何其他信息。</summary>
internal static class Diagnostics
{
    private static readonly object Sync = new();
    private const long MaxBytes = 1_000_000;

    public static string LogPath => Path.Combine(SettingsStore.DataFolder, "logs", "app.log");

    public static void Log(string message)
    {
        try
        {
            lock (Sync)
            {
                string path = LogPath;
                string folder = Path.GetDirectoryName(path)!;
                Directory.CreateDirectory(folder);

                FileInfo file = new(path);
                if (file.Exists && file.Length > MaxBytes)
                {
                    File.Move(path, Path.Combine(folder, "app.old.log"), true);
                }

                File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {message}{Environment.NewLine}");
            }
        }
        catch (Exception)
        {
            // 日志失败不能影响录制。
        }
    }

    public static void LogException(string context, Exception exception) =>
        Log($"{context} 失败：{exception.GetType().Name}: {exception}");
}
