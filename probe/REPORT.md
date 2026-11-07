# WPS COM 探针报告

日期：2026-11-07
环境：Windows 11 26200 / WPS Office 12.1.0.29161（中文个人版，per-user）
安装路径：C:\Users\28910\AppData\Local\Kingsoft\WPS Office\12.1.0.29161\office6

## 结论

**可行。** 外挂进程可以连到用户正在运行的 WPS 演示实例，并控制放映、读取当前页、切换原生笔与笔色。

## 关键结果

| 项 | 结果 |
|---|---|
| ProgID | KWPP.Application |
| GetActiveObject | 成功（有文档打开时）；冷启动瞬间可能 0x800401E3，需重试 |
| Presentations.Add / Slides.Add / SaveAs | 成功 |
| SlideShowSettings.Run | 成功 |
| SlideShowWindows.Count | 放映时 1，退出后 0 |
| CurrentShowPosition | 1 → Next → 2 → GotoSlide(3) → 3 |
| PointerType | 默认 4；设为 2（笔）读回 2；设为 1（箭头）读回 1 |
| PointerColor.RGB | 读 255，写 255 成功；BGR 顺序 |
| View.Exit() | 成功，Count 归 0 |
| GetObject(文件路径) | 成功，返回正在编辑的 Presentation（3 页） |
| ROT 文件 moniker | 含完整路径 C:\...\probe.pptx |

## 进程与窗口

- 放映窗口：wpp.exe，class Qt5QWindowIcon，title "WPS Presentation Slide Show - [probe.pptx]"，全屏。
- 编辑框：wps.exe，class PP12FrameClass，title "probe.pptx - WPS Office"。
- COM 服务器注册为 wps.exe /wpp /Automation。
- 监听器认 wpp.exe 有效；WPS 只在打开演示文稿时出现该进程。

## 注意

- PointerColor 的 RGB 是 BGR，不要把 WPF 的 ARGB 直接写进去。
- 连接要在 WPS 完成启动后重试；连不上时白板仍可独立工作。
- 墨迹保留提示：本次 COM Exit 未弹窗，含真实笔迹时需再验。
