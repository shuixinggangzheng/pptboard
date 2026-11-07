# PPTBoard 进度与验收状态

更新：2026-11-07

## 已实现

- 模块化解决方案：Core（布局/主题/接口）、Ink（多指笔迹、撤销、擦除）、Wps（COM 桥接）、App（WPF 浮层）、watcher（C）
- 主 dock：288×40，贴屏幕底居中；颜色点 / 上一页 / 下一页 / 笔 / 画板 / 结束放映
- 画板：1664×984（1920×1080 基准），滑入滑出、灰条撤销/橡皮/收起、页面框
- 画笔 dock：5 预设色 + 自定义色盘、5 档粗细 + 无级滑杆
- 颜色面板：色相环 + 明度 + 不透明度
- 滑动清空本页
- 右侧页面菜单：2 列缩略图、新建页、删除页（二次确认）、收起
- WPS 桥接：GetActiveObject + ROT 兜底，放映/翻页/跳页/结束/原生笔/笔色
- C 监听器：Toolhelp32 轮询 wpp.exe、单实例、崩溃重启 3 次、日志按天保留 7 天
- install.bat / uninstall.bat / build.ps1 / 自包含 x64 打包
- 离屏渲染工具（不弹窗、不碰鼠标，不影响桌面）

## 验证证据

| 项 | 方式 | 结果 |
|---|---|---|
| 布局公式 | xunit 4 项 | 通过 |
| 多指/撤销/擦除/翻页 | xunit 11 项 | 通过 |
| WPS COM 能力 | probe/WpsProbe.ps1 真机放映 | 全部通过（见 probe/REPORT.md） |
| WPS 桥接连接 | PPTBoard.Render --com | attached=True, slides=3 |
| 监听器进程检测 | PPTBoardWatcher.exe /verify | wpp.exe=1，退出码 0 |
| 监听器子系统 | PE 头 | GUI（2），无控制台窗口 |
| 视觉 | 离屏渲染 artifacts/render/*.png | dock / board / pen-dock / color-panel / page-menu / loading |
| 打包 | build.ps1 | PPTBoard-0.1.0-win-x64.zip（60 MB） |

## 验收清单状态

| # | 项 | 状态 |
|---|---|---|
| 1 | 安装/自启/loading | 脚本与程序已具备；安装流程未在真机跑到最后 |
| 2 | 监听器拾取 WPS | /verify 通过；拉起浮层未做端到端 |
| 3 | 放映→dock / Esc→隐藏 | 代码完成，需真机放映验证 |
| 4 | dock 6 控件/置灰/长按笔 | 6 控件与置灰已完成；长按二级菜单未做 |
| 5 | 画板滑入/留白收起 | 已在桌面上验证（现改为离屏验证） |
| 6 | 4 指同时书写 | 引擎单测通过；真机多点触控未验（开发机无触摸屏） |
| 7 | 橡皮/滑动清空/撤销 | 整笔擦与撤销已验；清空滑条仅离屏渲染 |
| 8 | 页面/缩略图/删除 | 已实现并离屏渲染；菜单交互未真机验 |
| 9 | 点击穿透 | dock 外无窗口，画板打开时留白收板已验 |
| 10 | 焦点不抢 | NOACTIVATE 已设；真机键盘未验 |
| 11 | WPS 退出→浮层退出 | 代码完成（PollProcess），未端到端验 |
| 12 | 崩溃重启 | watcher 代码完成，未模拟崩溃验 |
| 13 | 日志 | watcher 日志已验；app.log 已验 |
| 14 | 卸载 | 脚本就绪，未在真机执行 |
| 15 | DPI/置顶 | 125% 布局已验；放映置顶未验 |

## 不打扰桌面的测试方式

1) 视觉：生成离屏 PNG（不显示任何窗口）
   dotnet run --project tools/PPTBoard.Render -- artifacts/render

2) WPS 连接：只读 attach，不启动放映
   dotnet run --project tools/PPTBoard.Render -- --com

3) 监听器：只检测 wpp.exe，不拉起浮层
   artifacts\PPTBoard\PPTBoardWatcher.exe /verify

4) 单元测试
   dotnet test PPTBoard.slnx

## 待办

- 主 dock 的“长按笔”二级菜单（笔/橡皮/箭头/颜色）
- 画板打开时把 dock 的 4 个 PPT 控件置灰
- 真机端到端：安装、放映、触控、退出、崩溃重启、卸载
- 复制到 F:\PPTBoard，推 GitHub 公开仓库（MIT）
