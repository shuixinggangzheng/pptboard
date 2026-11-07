# PPTBoard 进度与验收状态

更新：2026-11-07（用户确认可用）

## 已实现

- 模块化解决方案：Core（布局/主题/接口）、Ink（多指笔迹、撤销、擦除）、Wps（WPS/PowerPoint COM）、App（WPF 浮层）、watcher（C）
- 主 dock：288×40，贴屏幕底居中；颜色点 / 上一页 / 下一页 / 笔 / 画板 / 结束放映
- 画板：1664×984（1920×1080 基准），滑入滑出、灰条撤销/橡皮/收起、页面框
- 画笔 dock：5 预设色 + 自定义色盘、5 档粗细 + 无级滑杆
- 颜色面板：色相环 + 明度 + 不透明度
- 滑动清空本页
- 右侧页面菜单：2 列缩略图、新建页、删除页（二次确认）、收起
- WPS / PowerPoint 桥接：PowerPoint.Application / KWPP.Application，GetActiveObject + ROT 兜底，优先选正在放映的实例
- C 监听器：轮询 wpp.exe / POWERPNT.EXE、单实例、崩溃重启 3 次、日志按天保留 7 天
- install.bat / uninstall.bat / build.ps1 / 自包含 x64 打包
- 离屏渲染工具（不弹窗、不碰鼠标）
- 目标机自检脚本 tools/verify.ps1

## 验证证据

| 项 | 方式 | 结果 |
|---|---|---|
| 布局/颜色公式 | xunit 8 项 | 通过 |
| 多指/撤销/擦除/翻页/删页 | xunit 16 项 | 通过 |
| WPS COM 能力 | probe/WpsProbe.ps1 真机放映 | 全部通过（见 probe/REPORT.md） |
| 桥接连接 | PPTBoard.Render --com | attached=True, slides=3 |
| UI 接线 | PPTBoard.Render --selftest | 11 项 PASS |
| 安装/卸载 | 临时 LOCALAPPDATA | 文件、HKCU Run、watcher 启动、卸载清理通过 |
| 监听器重启 | 假 wpp.exe + GUI stub | 重启 1/3、2/3、3/3；WPS 退出后复位并重新拉起 |
| 视觉 | 离屏渲染 | dock / board / pen-dock / color-panel / page-menu / loading |
| 打包 | build.ps1 | PPTBoard-0.1.0-win-x64.zip（60 MB） |

## 验收清单状态

| # | 项 | 状态 |
|---|---|---|
| 1 | 安装/自启/loading | 安装/自启/卸载已验（临时目录）；loading 已验 |
| 2 | 监听器拾取 WPS/PPT | /verify 与假进程测试通过；真机端到端待验 |
| 3 | 放映→dock / Esc→隐藏 | 代码完成；隐藏桌面的 COM/ROT 隔离，无法在沙盒验 |
| 4 | dock 控件/置灰/长按笔 | 已实现；UI 自测通过 |
| 5 | 画板滑入/留白收起 | 已在桌面验证过 |
| 6 | 4 指同时书写 | 引擎单测通过；真机多点触控待验（开发机无触摸屏） |
| 7 | 橡皮/滑动清空/撤销 | 整笔擦与撤销已验；清空滑条逻辑完成 |
| 8 | 页面/缩略图/删除 | 已实现并离屏渲染；删页索引 bug 已修 |
| 9 | 点击穿透 | 画板打开时留白收板已验；dock 外无窗口 |
| 10 | 焦点不抢 | NOACTIVATE 已设；真机键盘待验 |
| 11 | WPS/PPT 退出→浮层退出 | 代码完成；待端到端 |
| 12 | 崩溃重启 | 假进程验证通过 |
| 13 | 日志 | 已验 |
| 14 | 卸载 | 已验 |
| 15 | DPI/置顶 | 125% 布局已验；放映置顶待验 |

## 沙盒测试的边界

隐藏桌面（CreateDesktop）里 WPS 能起放映，浮层也能启动、连上 COM，
但 ROT 里的演示文稿在跨桌面时不可见（bridge diag: rotDocs:0），
所以 dock 无法在沙盒里被拉起。默认桌面上 --com 能连到 3 页文档。
第 3/6/10/11/15 项需要在真实桌面或一体机上验。

## 交付

- GitHub 公开仓库：https://github.com/shuixinggangzheng/pptboard（public，MIT）
- F:\PPTBoard 已暂存：source/ + dist/PPTBoard-0.1.0-win-x64.zip + STATUS/SPEC/README
- 验收完成后更新最终版本号与发布包

## 用户反馈修复（2026-11-07）

1. 结束放映后再次放映 dock 不出现 → ShowDock 时清除残留的 Opacity 动画再淡入；隐藏完成后显式清零。离屏桌面验证：隐藏→再次显示后 visible=True、opacity=1。
2. 主 dock 没有橡皮 → 新增橡皮按钮，dock 288→332；点橡皮打开画板并进入擦除态，再点回画笔；选颜色/粗细自动回画笔。
3. 颜色面板点框外不能关闭 → 新增全屏 PopoverCatcher，点面板外任意位置关闭；Win32 z-order 验证 hitPanel=面板、hitOutside=catcher。
4. 预设颜色改为红/蓝/黑三个。

## 用户反馈修复（第二轮）

5. 主 dock 点橡皮不再打开白板：画板开着时切白板橡皮，关着时切放映原生橡皮（3/1），互不干扰。
6. 颜色预设：画笔 dock 和颜色面板顶部都提供红/蓝/黑三个预设；颜色面板打开时按当前色正确显示明度（黑=0%、白=100%）。
7. 不透明度滑条被裁掉：颜色面板 420→500，滑条区域加大，轨道提亮。
8. 按钮点击面积：dock 保持 40px 视觉，按最近按钮映射到整条 dock；间隙/内边距也能点中。

## 用户反馈修复（第三轮）

9. 未打开白板时 dock 橡皮无法擦 PPT 笔迹、点击反而翻页：WPS 的 COM 没有橡皮指针类型（官方 PointerType 只有 Arrow/Pen/AutoArrow/None/AlwaysHidden），设 3 会退回自动箭头，点击就翻页。改为发送 WPS 官方放映快捷键 Ctrl+E（PowerPoint 同理）；再点一次用 PointerType=1 回箭头。已确认 WPS 学堂文档：Ctrl+P 绘图笔、Ctrl+E 擦除屏幕绘图。

## 用户反馈修复（第四轮）

10. 颜色面板：再点主 dock 颜色点即关闭（面板可见时 ColorRequested 直接 Hide；全屏点击层也仍可用）。
11. 画板与放映界面互斥：画板打开时先把原生笔/橡皮复位为箭头，dock 的笔/橡皮只控制白板工具（笔按钮不再置灰），长按笔的原生菜单禁用；上一页/下一页/结束放映仍置灰。关闭画板后恢复按放映能力启用。

## 用户反馈修复（第五轮）

12. 退出放映时清空白板笔迹：PollShow 检测到放映从运行变停止时，CloseBoard + InkDocument.ResetAll（只留一页空白）；再次放映从空白页开始。新增 ResetAll 单测（17 项 Ink 测试通过）。
13. 再点 dock 颜色点关闭面板：给全屏点击层用 SetWindowRgn 挖掉主 dock 矩形，颜色点恢复可点；ColorRequested 在面板可见时直接 Hide。隐藏桌面验证：hitDock=主 dock（不是点击层）、面板打开后 Toggle → visible=False。

## 用户反馈修复（第六轮）

14. PowerPoint 里等一会 dock 消失：COM 在 Office 忙时可能短暂掉线，单次 PollShow=false 就把 dock 淡出。修复：
    - ShowWindowProbe 改为按进程名 wpp/POWERPNT 查找全屏窗口（类名/标题变化也能认），COM 掉线时窗口兜底保底；
    - PollShow 连续 3 次（约 0.9s）检测不到才确认结束并隐藏，单次抖动不再闪掉 dock。
    - 用 FakeSlideshowBridge 在隐藏桌面跑了 12s 生命周期：0.6s 的假掉线没有隐藏，9s 后连续 3 次才 HideDock 并清空白板。

## 验收确认

用户于 2026-11-07 实测后确认“ok 没问题了”，核心链路（放映→dock、画板、笔/橡皮、颜色、页面、退出清空、长等待 dock 稳定）通过。

## 安装包

- `PPTBoard-0.1.0-setup.exe`（NSIS，43.2 MB，无管理员，写入 HKCU 自启 + 应用卸载项）
- 已在本机静默安装/卸载各验证一次；当前本机为 setup 安装态。
- 开发机上的 WPS Office 已按要求卸载（安装目录、自启项、卸载项、COM 类、快捷方式、设置项），`%LOCALAPPDATA%\Kingsoft\WPS Cloud Files` 用户云缓存保留。

## 待办

- 真实桌面/一体机端到端验收（放映、触控、焦点、退出、置顶）
- 复制到 F:\PPTBoard，推 GitHub 公开仓库（MIT）
- 长按笔菜单在触摸屏上的手感（当前 500ms）