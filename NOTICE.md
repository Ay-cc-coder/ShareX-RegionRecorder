# NOTICE

本仓库 **ShareX Region Recorder** 是 [ShareX](https://github.com/ShareX/ShareX) 的衍生作品。

## 上游

- 项目：ShareX — Screen capture, file sharing and productivity tool
- 仓库：https://github.com/ShareX/ShareX
- 同步基准：`f3ae963af1c03c242f58adf162d3dffc1d45c657`（2026-10-26）
- 许可：GNU General Public License v3.0（`LICENSE.txt` 为上游原文，未修改）

## 使用的上游代码

`src/ShareX.ScreenRecordingLib/` 整个目录复制自上游同名工程，仅做了下列最小改动：

1. 删除 `Encoding/GifWriter.cs`，并移除 `ScreenRecorder`、`RecordingOptions`、`Video/GraphicsCapture` 中所有 GIF 分支与 `EncodingProgressChanged` 事件（输出只保留 MP4）。
2. 移除对 `ShareX.HelpersLib`、SkiaSharp 的依赖，工程只引用 Vortice.Direct3D11 / Vortice.MediaFoundation。
3. 收紧了 `ScreenRecorder` 中原本可为空的 writer/texture 判定（GIF 移除后的必然结果）。

所有源文件保留原始 GPLv3 版权头与「ShareX Team」署名。

## 新增代码

`src/RegionRecorder/`、`tools/`、`.github/` 为本项目新增，同样以 GPLv3 发布。

## 商标

ShareX 名称与官方图标归 ShareX 团队所有。本项目为非官方衍生版本，应用图标为独立绘制，
不使用 ShareX 商标素材。

## 分发义务

以二进制形式分发本程序时，必须同时提供（或书面承诺提供）本仓库的完整对应源码，
并保留 `LICENSE.txt` 与本 NOTICE。
