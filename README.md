# ShareX 区域录屏（ShareX Region Recorder）

<img src="src/RegionRecorder/Assets/App.png" width="88" alt="应用图标">

[![build](https://github.com/Ay-cc-coder/ShareX-RegionRecorder/actions/workflows/build.yml/badge.svg)](https://github.com/Ay-cc-coder/ShareX-RegionRecorder/actions/workflows/build.yml)

从 [ShareX](https://github.com/ShareX/ShareX) 裁剪出来的**单一用途**程序：只做「框选一块屏幕区域 → 录制成 MP4」。
没有截图、没有上传、没有图像编辑、没有 OCR，也没有 FFmpeg 依赖。

> 非官方衍生项目，与 ShareX 团队无关。按 GPLv3 分发，详见 [LICENSE.txt](LICENSE.txt) 与 [NOTICE.md](NOTICE.md)。

## 它现在能做什么

- **托盘常驻**：启动后只有一个托盘图标，不弹主窗口。
- **区域框选**：按 `Ctrl+Shift+R`（或右键托盘图标）后整屏压暗，拖动框选任意矩形，松开鼠标即开始。
- **跨屏选择**：选区横跨多个显示器、包含负坐标时，自动按显示器拼接采集。
- **倒计时**：默认 3 秒（可设 0–10 秒），期间按 `Esc` 取消。
- **录制控制条**：显示计时，可暂停/继续、停止；控制条本身**不会**出现在录像里。
- **音轨**：可录制系统声音（WASAPI 环回）与麦克风，二者自动混音；没有音频设备时自动关闭对应音轨而不是报错。
- **保存**：直接输出 `H.264/AAC` 的 MP4，优先使用 GPU 硬件编码，没有可用硬件编码器时回退软件编码。
- **设置**：保存位置、帧率（24/30/60/120）、鼠标指针、系统声音、麦克风设备、倒计时。
- **单实例**：重复启动不会出现第二个托盘图标，而是直接进入框选。
- **完全离线**：不联网、不上传、不写注册表，配置只有一个 JSON 文件。

## 系统要求

| 项目 | 要求 |
| --- | --- |
| 操作系统 | Windows 11 22H2（build 22621）或更高 |
| 运行时 | 下载 Release 里的自包含包则**无需安装** .NET；自行构建则为 .NET 10 |
| 图形 | 支持 D3D11 的显卡；没有硬件 H.264 编码器时使用 Media Foundation 软件编码 |
| 音频 | 可选；系统声音依赖默认播放设备，麦克风需要授权（Windows 设置 → 隐私 → 麦克风） |

> ShareX 的录制引擎基于 `Windows.Graphics.Capture`，因此只能在 Windows 上运行；本仓库也在 macOS 上交叉编译，但产物仍然只面向 Windows。

## 使用

1. 运行 `RegionRecorder.exe`，托盘出现图标。
2. 按 `Ctrl+Shift+R`，或右键托盘图标 →「录制区域」。
3. 拖动框选要录制的区域（`Esc` 取消），松开鼠标。
4. 倒计时结束后开始录制；右上角出现控制条。
5. 点「停止」（或再按一次 `Ctrl+Shift+R`）结束，文件自动保存。
6. 托盘气泡点一下即可打开刚录好的文件。

保存位置默认为 `%USERPROFILE%\Videos\ShareX 区域录屏`，文件名形如 `录屏_2026-10-27_02-30-15.mp4`。
设置界面里可以改成任意目录。

配置与运行数据位置：

```
%APPDATA%\ShareXRegionRecorder\settings.json
```

## 和 ShareX 的差异

| 能力 | 本项目 | 原版 ShareX |
| --- | :---: | :---: |
| 区域屏幕录制（MP4 / H.264 + AAC） | ✅ | ✅ |
| 窗口录制 / 全屏录制 | ❌ | ✅ |
| 截图、滚动截图、标注、图像编辑器 | ❌ | ✅ |
| OCR、二维码、颜色选择器 | ❌ | ✅ |
| 所有上传 / 分享 / 短链 / 图床 | ❌ | ✅ |
| 历史记录、任务列表、剪贴板导入 | ❌ | ✅ |
| 自定义快捷键、Lua 脚本、命令行任务 | ❌ | ✅ |
| FFmpeg 录制、直播推流、GIF 录制 | ❌ | ✅ |
| 摄像头叠加、鼠标高亮 | ❌（界面未提供） | ✅ |
| 首次启动向导、多语言、Steam / 商店打包 | ❌ | ✅ |

裁剪方式：

- **保留** `ShareX.ScreenRecordingLib`（约 3.4k 行）：`Windows.Graphics.Capture` 采集、D3D11 处理、Media Foundation H.264/AAC 封装、WASAPI 环回与混音、硬件编码器探测。
- **删除** 截图/标注/图像编辑/上传/历史/工具/本地化/打包等全部工程。
- **重写** 界面层：原来的 Avalonia + WinForms 混合外壳换成约 1.7k 行的极简 WinForms 托盘程序，自行实现框选、倒计时、录制条和设置。
- **移除** GIF 编码路径（连带去掉 SkiaSharp 依赖）和 FFmpeg 录制路径。

## 构建

需要 .NET 10 SDK。

```powershell
# 构建
dotnet build src/RegionRecorder/RegionRecorder.csproj -c Release

# 发布自包含单文件（win-x64）
dotnet publish src/RegionRecorder/RegionRecorder.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish/win-x64
```

在 macOS / Linux 上交叉编译也可以（工程里已设置 `EnableWindowsTargeting`）：

```bash
dotnet publish src/RegionRecorder/RegionRecorder.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -o publish/win-x64
```

CI（`.github/workflows/build.yml`）会在 `windows-latest` 上为 `win-x64` / `win-arm64` 各产出一份 zip；推送 `v*` 标签时自动创建 Release 并附上这两个包。

## 目录结构

```
src/ShareX.ScreenRecordingLib/   保留的 ShareX 录制引擎（GPLv3，文件头保留原版权）
src/RegionRecorder/              本次新写的极简外壳
  App/                           托盘、单实例、全局热键
  Core/                          设置、桌面快照、录制会话封装
  UI/                            框选层、倒计时、录制条、设置窗口
tools/make-icon.py               生成应用图标
```

## 验证状态

- ✅ 已在 macOS 上用 .NET 10 SDK 交叉编译通过（`0 error / 0 warning`），并用 `dotnet publish` 产出 win-x64 自包含包。
- ✅ GitHub Actions 在 `windows-latest` 上构建、打包通过（见 Actions 页）。
- ⚠️ 尚未在真实 Windows 桌面上做端到端录屏测试（开发机为 macOS）。首次在 Windows 上运行时，若遇到采集或编码报错，请带上错误信息提 issue。

## 已知限制

- 只支持 Windows 11 22H2+；Windows 10 不可用（`Windows.Graphics.Capture` 的新接口）。
- 只有 MP4（H.264/AAC），没有 GIF、没有硬件无关的无损编码。
- 快捷键固定为 `Ctrl+Shift+R`，暂不支持自定义。
- 不监听分辨率/DPI 变化：录制过程中拖动窗口或改变显示器缩放不在支持范围内。
- 受 DRM 保护的窗口（如部分播放器、Netflix）无法被捕获，这是系统限制。

## 许可

本项目是 ShareX 的衍生作品，遵循 **GNU General Public License v3.0**（见 [LICENSE.txt](LICENSE.txt)）。
`src/ShareX.ScreenRecordingLib` 下所有文件保留原始版权头；再分发（包括二进制）时必须同时提供完整源码并保留相同许可。
ShareX 名称与图标归 ShareX 团队所有；本仓库使用的图标（`src/RegionRecorder/Assets/App.ico`）是为本项目单独绘制的，未使用 ShareX 素材。

上游：<https://github.com/ShareX/ShareX>，同步基准提交 `f3ae963af1c03c242f58adf162d3dffc1d45c657`。
