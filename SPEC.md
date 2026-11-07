# PPTBoard · WPS 演示白板 — 需求与设计规格

- 版本：v1.0（grilling 完成，待最终确认）
- 日期：2026-11-07
- 目标机：教学一体机，Windows 10 x64，单屏 1920×1080，多点触控
- 宿主软件：WPS 演示（wpp.exe）；Microsoft Office 明确不做（Q18-C）
- 开发机：Win11 26200 / 1920×1080@125% / Office 365 / 无 WPS（需安装 WPS 后测试）

---

## 1. 产品一句话

WPS 放映时，在屏幕上浮出一套半透明 dock 与一块可翻页的白色画板：dock 接管放映控制（翻页、原生笔、画板、结束放映、画笔颜色），画板支持多指同时书写、整笔擦除、撤销、滑动清空、翻页与缩略图预览；由一个 C 监听器在检测到 wpp.exe 时拉起，注册表开机自启。

---

## 2. 交付物

一个 zip，解压后：

    PPTBoard/
      PPTBoard.exe            # WPF 浮层主程序（.NET 10 自包含 x64）
      PPTBoard.dll ...        # 自包含运行时与依赖
      PPTBoardWatcher.exe     # C 监听器（MSVC x64，静态 CRT）
      install.bat             # 安装：复制到 %LOCALAPPDATA%\PPTBoard、写 HKCU Run、启动监听器
      uninstall.bat           # 卸载：停进程、删自启项、删目录
      README.txt              # 使用说明与故障排查

安装位置：%LOCALAPPDATA%\PPTBoard\
日志位置：%LOCALAPPDATA%\PPTBoard\logs\
自启项：HKCU\Software\Microsoft\Windows\CurrentVersion\Run，值名 PPTBoardWatcher

### 2.1 发布位置与开源

- 开发期源码在工作区 elnchat\pptboard；**成品完成后整体复制到 F:\PPTBoard**。
- GitHub 公开仓库：github.com/shuixinggangzheng/pptboard，MIT License。
- 仓库包含源码、README（安装/使用/故障排查）、SPEC、LICENSE；不提交 bin/obj/logs/本地探针产物。
- 发布步骤：git init → 首次提交 → gh repo create shuixinggangzheng/pptboard --public --source . --push。

---

## 3. 架构

    +-------------------+        CreateProcess        +-------------------------+
    | PPTBoardWatcher   | ---------------------------> | PPTBoard (WPF)          |
    | (C, 常驻)          | <---- 进程退出/崩溃监测 ----- | 多窗口浮层 + 白板 + COM   |
    +---------+---------+                              +------------+------------+
              | Toolhelp32 轮询 wpp.exe                             | KWPP.Application
              v                                                     v
        WPS 演示进程                                          放映控制 / 当前页 / 指针

- 监听器与浮层是两个进程，职责边界清晰：监听器只负责“什么时候有 WPS、拉起/看护浮层”，不画任何 UI。
- 浮层只负责“WPS 在放映时显示什么、怎么画”，不做自启，不写注册表。
- 浮层通过 COM 连到正在运行的 WPS 演示实例；COM 不可用时降级（见 §15）。

---

## 4. 生命周期状态机

### 4.1 监听器

    IDLE --发现 wpp.exe--> LAUNCH --创建进程成功--> MONITOR
    MONITOR --浮层退出且 wpp.exe 仍存活--> 重启（最多 3 次，间隔 2s）--> MONITOR
    MONITOR --重启 3 次仍失败--> GIVE_UP（写日志，等 wpp.exe 退出后复位）
    MONITOR --wpp.exe 退出--> 结束浮层 --> IDLE

- 监听器启动时若 wpp.exe 已存在：直接 LAUNCH。
- 单实例：命名互斥量 PPTBoardWatcher_SingleInstance；已存在则静默退出。

### 4.2 浮层

    BOOT(loading 圆盘) --就绪--> READY(无 UI)
    READY --检测到放映--> SHOWING(dock 可见)
    SHOWING --画板按钮/无--> BOARD(画板展开)
    BOARD --灰条箭头 / dock 画板按钮 / 点留白--> SHOWING（收起动画）
    SHOWING/BOARD --放映结束--> READY（画板强制收起）
    READY --再次放映--> SHOWING（画板内容保留在内存，Q20-A）
    任意状态 --wpp.exe 退出--> 退出进程（内容丢弃）

- 放映检测优先用 COM：Application.SlideShowWindows.Count > 0（300ms 轮询）。
- COM 不可用时回退：枚举 wpp.exe 的顶层窗口，找“尺寸==屏幕且无边框”的窗口。
- 画板内容只在内存，随浮层进程退出而丢弃；收起画板、结束放映都不清空。

---

## 5. 布局与尺寸（以 1920×1080 为基准）

### 5.1 屏幕分区

    y=0     +--------------------------------------------------+
            |                画板（白色，不透明）                 |
            |                1664 × 984                         |
            |  x=128                                    x=1792  |
            |                                                   |
    y=928   | [撤销][橡皮]      ......[↑]......    [◀ 1/1 ▶]    |  灰条 56px
    y=984   +--------------------------------------------------+
            |        [ 画笔 dock 666×56，居中 ]                 |  画板下方
    y=1040  |        [ 主 dock 288×40，居中 ]                   |  贴屏幕底
    y=1080  +--------------------------------------------------+

### 5.2 尺寸公式

| 元素 | 公式 / 值 | 1920×1080 结果 |
|---|---|---|
| 左右留白 | round(SW / 15) | 128 |
| 画板宽 | SW − 2×留白 | 1664 |
| 画板底边留白 | max(round(SH / 15), 96) | 96 |
| 画板高 | SH − 底边留白 | 984 |
| 灰条 | 画板底边，高 56，通栏 | 1664×56 |
| 页面框 | 宽 round(画板宽/15)，高 56，贴灰条右端 | 111×56 |
| 画笔 dock | 宽 round(画板宽×0.40)，高 56，水平居中，画板正下方 | 666×56 |
| 主 dock | 宽 332，高 40，水平居中，贴屏幕底 | 332×40 |
| 右侧页面菜单 | 宽 320，高 = 屏幕高，贴右边缘 | 320×1080 |
| 滑动清空条 | 400×44，灰条上方 12px，水平居中 | 400×44 |
| 颜色面板 | 340×420，浮在主 dock 颜色点上方 | 340×420 |
| Loading 圆盘 | 直径 72，距右/下各 32 | 72 |

> 所有尺寸以逻辑像素（DIP）计，运行期按系统 DPI 换算；上述公式已在 1920×1080@100% 校核。若目标机是 125% 缩放，逻辑分辨率变为 1536×864，公式自动缩小，主 dock 内部固定 40px 控件会显得更宽——此时主 dock 宽度取 max(288, 内容宽)，并在验收时实测。

### 5.3 灰条内部

| 区段 | 内容 | 尺寸 |
|---|---|---|
| 左端 | 撤销、橡皮擦 | 各 40×40，左边距 16，间距 6 |
| 中央 | 向上箭头（收起画板） | 40×40，水平居中 |
| 右端 | 页面框：左箭头 / 页码 / 右箭头 | 111×56，贴右边缘 |

### 5.4 主 dock 内部

7 个 40px 控件，间距 4，左右内边距 13：40×7 + 4×6 + 13×2 + 边框 2 = 332。

    颜色点 | 上一页 | 下一页 | 笔 | 橡皮 | 画板 | 结束放映

> 2026-11-07 按用户反馈追加：主 dock 增加橡皮按钮，dock 由 288 加宽到 332。

---

## 6. 主 dock 行为

| 控件 | 点击 | 长按（500ms） | 说明 |
|---|---|---|---|
| 颜色点 | 弹出颜色面板（同画笔 dock 的颜色区 + 透明度/明度） | — | 显示当前颜色；原生笔与画板共用 |
| 上一页 | 放映上一张 | 连续翻页（每 200ms） | 第一张时置灰 |
| 下一页 | 放映下一张 | 连续翻页 | 最后一张时置灰 |
| 笔 | 切换 WPS 原生笔（当前颜色）；再点回箭头 | 弹出二级菜单：笔 / 荧光笔 / 橡皮 / 颜色 | 长按菜单选项以图标+文字列出 |
| 画板 | 拉下 / 收起画板 | — | 画板展开时按钮高亮 |
| 结束放映 | View.Exit()（丢弃墨迹） | — | 直接退出 |

- 画板展开时，上一页/下一页/笔/结束放映置灰不可点（避免在看不见的幻灯片上操作）；颜色点与画板按钮仍可用。
- dock 常驻，不做自动隐藏；半透明。
- 画板展开时主 dock 仍在屏幕底（Q7-A）。

---

## 7. 画板

### 7.1 外观与动效

- 纯白不透明底板，四角直角（与“黑板”一致）；左右留白 128，底边 96，顶边贴屏幕顶。
- 展开：从屏幕顶上方滑入，260ms，cubic-bezier(0.22, 0.9, 0.3, 1)；收起 220ms 反向。
- 打开时主 dock 的画板按钮高亮；灰条中央箭头向上。

### 7.2 绘制

- 输入：WM_POINTER（触摸、笔、鼠标统一），每根手指/笔一条独立笔迹，支持同时书写（Q8-B）。
- 笔迹：圆头、圆角连接、抗锯齿；当前颜色、粗细、透明度在落笔瞬间固化，之后改设置不影响已画内容。
- 粗细：3/6/10/16/24 五档 + 1–32 无级滑杆（吸附档位 ±2）；默认 6。
- 颜色：默认黑色；3 个预设（红/蓝/黑）+ 自定义。
- 性能目标：4 指同时书写时，落笔延迟 < 30ms，无明显掉帧（1080p）。

### 7.3 橡皮擦（整笔擦）

- 点灰条橡皮进入擦除态（按钮高亮），所有触点在画板上滑动即删掉碰到的整条笔迹。
- 再点一次橡皮 → 唤出“滑动清空本页”滑条（见 7.4）；擦除态保持。
- 退出擦除态：点任意颜色、粗细、或灰条向上箭头、或主 dock 画板按钮。

### 7.4 滑动清空

- 灰条上方 12px 浮出 400×44 半透明条，文案“向右滑动清空本页”，右侧圆形拖块。
- 拖块拖到最右端才执行；中途松手弹回。
- 执行后清空当前页（可撤销），滑条 600ms 后自动消失。

### 7.5 撤销

- 灰条左端“撤销”按钮；每页独立栈，最多 100 步。
- 一步 = 一条笔迹 / 一次整笔擦除 / 一次滑动清空；清空可被撤销恢复。
- 不做重做（灰条右端目前被页面框占用；后续如需再加）。
- 多指同时书写时，撤销“最近一条完成的笔迹”，正在画的笔迹不受影响。

### 7.6 页面

- 默认 1 页；新建页 = 空白页（Q5-A），插入当前位置之后并跳过去。
- 翻页：页面框左右箭头；最后一页再右翻自动新建一页（Q4-A）。
- 删除页：只从右侧菜单操作；仅剩 1 页时不可删。
- 页面切换不播放动画，直接瞬时切换。
- 每页独立笔迹、独立撤销栈。

### 7.7 内容生命周期

- 画板页与笔迹只保存在内存，属于“本次 WPS 进程会话”。
- 收起画板、结束放映都不清空；再次放映恢复。
- WPS 进程退出 → 浮层退出 → 全部丢弃（Q20-A）。

---

## 8. 画笔 dock 与颜色

### 8.1 布局（666×56，画板正下方）

    左 50%（333px）：颜色区
      [黑][白][红][蓝][黄]  [自定义彩色圆盘]
      每个 40×40，间距 8
    右 50%（333px）：粗细区
      [3][6][10][16][24] 五档 36×36 + 无级滑杆 100×8（圆头 20）

- 当前颜色/档位高亮描边。
- 点击三个预设（红/蓝/黑）→ 立即生效，透明度=100%，明度=默认。
- 点击彩色圆盘 → 原地放大为颜色面板（见 8.2）。

### 8.2 自定义颜色面板（340×420）

- 色相环（直径 240），中心显示当前色预览。
- 明度滑杆：0–100%。0 = 纯黑，50 = 色相本色，100 = 纯白（与黑/白线性混合）。
- 不透明度滑杆：10–100%。
- 底部“完成”按钮；点面板外关闭。
- 只在自定义色上生效；预设色始终不透明、不调明度（Q12-A）。

### 8.3 主 dock 颜色点

- 显示当前颜色；点击在 dock 上方弹出同一套颜色面板（340×420）。
- 面板内容与 8.2 完全一致，选择结果同步到画板画笔与 WPS 原生笔。

---

## 9. 页面框与右侧菜单

### 9.1 页面框（111×56，灰条右端）

    [◀]  1/1  [▶]

- 左/右箭头 32×32，中间页码 47px 宽，白底灰字，当前页码高亮。
- 点中间页码区 → 右侧页面菜单滑出。

### 9.2 右侧页面菜单（320×全高，贴右边缘）

    +--------------------------+
    | 页面           [+ 新建页] |
    |--------------------------|
    | [缩略图1]  [缩略图2]      |
    | [缩略图3]  [缩略图4]      |
    | ...（可滚动）             |
    |--------------------------|
    |          收起            |
    +--------------------------+

- 2 列缩略图，每格 132×74（16:9）+ 页码；当前页蓝色 2px 描边。
- 缩略图在页面内容变化后（切页、收菜单、新建/删除）重绘。
- 点缩略图 → 跳到该页并收起菜单。
- 缩略图右上角“×”：第一次点变红，第二次点删除（剩 1 页时禁用）。
- 底部“收起”只收菜单，不动画板。
- 菜单展开时，点菜单外的画板区域 → 先收菜单，不关画板。

### 9.3 动效

- 菜单从右滑出 220ms；收起 200ms。
- 缩略图淡入 120ms。

---

## 10. Loading 圆盘

- 触发：监听器拉起主程序时，主程序第一个窗口即圆盘（位于屏幕右下角，72px，距边 32px）。
- 过程：外圈蓝色 3px 圆条 0.9s 转满一圈，同时进度到 95%；就绪后补到 100%，盘内 0.3s 画出绿色渐变勾；停 0.2s，0.2s 淡出。
- 等待上限 3s；超时或启动失败 → 红色 ×，2s 后消失，写日志。
- 圆盘不抢焦点（NOACTIVATE），不阻塞 WPS。
- 说明：从监听到 WPF 画出圆盘有 0.3–0.6s 真空期，不额外加启动器进程。

---

## 11. 输入、焦点、点击穿透

- 所有浮层窗口：WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW，不进 Alt+Tab，点击不夺走 WPS 的键盘焦点。
- 画板展开时：
  - 画板、灰条、页面框、画笔 dock、主 dock、菜单、颜色面板可交互；
  - 四周留白（左右 128、画板外区域）点击 = 收起画板（Q28-C）；
  - 有弹层（菜单/颜色面板/清空滑条）时，点其外部先收弹层，不关画板。
  - WPS 放映交互被完全挡住，不会在看不见的幻灯片上翻页。
- 画板收起时：只有主 dock（及颜色面板）可交互，其余区域全部点击穿透给 WPS。
- 置顶：HWND_TOPMOST，放映开始、每 2s、以及 WPS 窗口激活时重新置顶。
- 键盘：浮层不处理 Esc/方向键，全部留给 WPS；Esc 结束放映后浮层自动回 READY。

---

## 12. 视觉规范

| 项 | 值 |
|---|---|
| 玻璃底色 | rgba(18, 20, 26, 0.58) |
| 描边 | 1px rgba(255, 255, 255, 0.14) |
| 圆角 | 10px |
| 投影 | 0 4px 16px rgba(0,0,0,0.28) |
| 图标 | 20px 线性，白色 92% |
| 悬停 | 底色提亮 +8% |
| 按下 | 缩放 0.96，120ms |
| 画板 | #FFFFFF |
| 灰条 | #ECECF0，图标 #3A3F4B |
| 页面框 | #FFFFFF 92%，文字 #3A3F4B |
| 强调蓝 | #2F7CF6 |
| 成功绿 | #34C759（loading 勾） |
| 字体 | Microsoft YaHei UI；页码 13px SemiBold |
| 动效曲线 | cubic-bezier(0.22, 0.9, 0.3, 1) |

不做毛玻璃/模糊（Q27-A）。

---

## 13. C 监听器（PPTBoardWatcher.exe）

| 项 | 规格 |
|---|---|
| 语言/工具链 | C（Win32 API），MSVC x64，静态 CRT |
| 检测 | Toolhelp32 轮询，每 1s，进程名 wpp.exe（大小写不敏感） |
| 单实例 | 命名互斥量 PPTBoardWatcher_SingleInstance |
| 自启 | HKCU\...\Run，值名 PPTBoardWatcher，路径带引号 |
| 拉起 | CreateProcessW(浮层路径, 工作目录=程序目录)，浮层路径相对监听器 exe |
| 崩溃重启 | 浮层退出而 wpp.exe 仍存活 → 重启，最多 3 次（间隔 2s），之后放弃并记日志 |
| WPS 退出 | 结束浮层（先 WM_CLOSE 广播注册消息，2s 后 TerminateProcess 兜底） |
| 日志 | %LOCALAPPDATA%\PPTBoard\logs\watcher.log，按天滚动，保留 7 天 |
| 自身 | 无窗口、无托盘、不进 Alt+Tab、不抢焦点 |
| 启动补漏 | 启动时 wpp.exe 已在运行则立即拉起浮层 |

---

## 14. 工程结构（模块化，强制约束）

原则：**模块之间只通过接口通信，依赖单向，UI 不得渗进模型层；尺寸只在 LayoutMetrics 里算。**

    pptboard/
      PPTBoard.sln
      src/
        PPTBoard.Core/            # 无 WPF、无 Win32；纯计算与契约
          Geometry/LayoutMetrics.cs      # 屏幕 → 各元素矩形，所有尺寸公式
          Theme/ThemeTokens.cs           # 颜色/圆角/动效常量
          Contracts/ISlideshowBridge.cs  # 放映控制接口（Wps 实现）
          Contracts/IInkSurface.cs       # 墨迹面接口（Ink 实现）
          Models/PenStyle.cs, PageRef.cs
        PPTBoard.Ink/             # 无 WPF；笔迹模型 + 多指状态机 + 撤销
          InkStroke.cs, InkPage.cs, InkDocument.cs
          InkEngine.cs                   # 每指针一条笔迹、整笔擦除、清空
          UndoStack.cs
          Rendering/StrokeGeometry.cs    # 与渲染器无关的几何数据
        PPTBoard.Wps/             # 无 WPF；COM 与放映检测
          WpsBridge.cs                   # ISlideshowBridge 实现（含降级）
          WpsComInterop.cs               # KWPP.Application 动态调用封装
          SlideshowDetector.cs           # COM 优先 + 全屏窗口回退
          NativeMethods.cs
        PPTBoard.App/             # WPF 可执行；只做窗口、输入、合成
          App.xaml(.cs)
          Boot/LoadingWindow.xaml(.cs)
          Host/SlideshowHost.cs          # 状态机、置顶、窗口生命周期
          Host/OverlayWindow.cs          # NOACTIVATE/TOOLWINDOW/穿透
          Dock/MainDockWindow.xaml(.cs)
          Dock/ColorPanelWindow.xaml(.cs)
          Board/BoardWindow.xaml(.cs)    # 不透明窗口 + 自绘墨迹
          Board/BoardInput.cs            # WM_POINTER 钩子 → InkEngine
          Board/PenDockWindow.xaml(.cs)
          Board/PageBoxWindow.xaml(.cs)
          Board/PageMenuWindow.xaml(.cs)
          Board/ClearSliderWindow.xaml(.cs)
          Theme/Styles.xaml
        watcher/                  # C，独立 exe，只认 Win32
          main.c, process.c, log.c, watcher.h
      tests/
        PPTBoard.Core.Tests/      # xunit：布局公式
        PPTBoard.Ink.Tests/       # xunit：多指、撤销、擦除、翻页
      build/build.ps1             # cl + dotnet publish + zip
      docs/SPEC.md
      README.md
      LICENSE

依赖方向（只允许单向）：

    App  ──> Ink ──> Core
     │        ^
     └──> Wps ┘     （Wps 只依赖 Core）

- Core、Ink、Wps 不得引用 WPF / System.Windows；App 不得直接 new COM 对象，只通过 ISlideshowBridge。
- Ink 的多指、擦除、撤销逻辑必须能在无 UI 环境下单元测试。
- 窗口代码不得写死像素，一律从 LayoutMetrics 取矩形。
- watcher 与 App 只约定“程序路径 + 日志目录”，不共享代码。
- 工具链：.NET 10 SDK 10.0.402（已装）、MSVC v143（D:\新建文件夹 (4)\VC\Auxiliary\Build\vcvars64.bat）。

---

## 15. WPS / PowerPoint 集成与降级（2026-11-07 实测）

实测环境：WPS Office 12.1.0.29161（个人版，per-user 安装于 %LOCALAPPDATA%\Kingsoft\WPS Office\12.1.0.29161\office6）。

补充（用户反馈后追加）：同时支持 Microsoft PowerPoint。ProgID 依次尝试 PowerPoint.Application、KWPP.Application；监听器同时检测 POWERPNT.EXE；浮层的退出轮询也认两者。

| 能力 | COM（实测） | 降级 |
|---|---|---|
| 连接 | GetActiveObject("KWPP.Application")，WPS 有文档打开时可用；冷启动瞬间可能返回 0x800401E3，需重试 | ROT 文件 moniker 兜底：CreateFileMoniker(pptx) + IRunningObjectTable.GetObject |
| 检测放映 | Application.SlideShowWindows.Count | 找 wpp.exe 的可见全屏窗口：class=Qt5QWindowIcon，title 以 "WPS Presentation Slide Show" 开头 |
| 当前页 | SlideShowWindow.View.CurrentShowPosition | 幻灯片按钮隐藏 |
| 上一页/下一页 | View.Previous() / View.Next() | 发送 PageUp/PageDown 键盘消息 |
| 跳到某页 | View.GotoSlide(n) | 禁用 |
| 结束放映 | View.Exit() | 发送 Esc |
| 原生笔 | View.PointerType（1=箭头，2=笔）；PointerColor.RGB（BGR 顺序） | 笔按钮隐藏 |

- 实测：SlideShowSettings.Run 后 SlideShowWindows.Count=1；CurrentShowPosition、Next、GotoSlide、PointerType 读写、PointerColor 读写、Exit 全部成功。
- 已打开的 .pptx 会以文件 moniker 出现在 ROT 中，可据此拿到正在编辑的演示文稿（GetObject(file) 实测返回 3 页的 Presentation）。
- 放映窗口在 wpp.exe，类名 Qt5QWindowIcon，标题 "WPS Presentation Slide Show - [文件名]"；编辑框在 wps.exe，类名 PP12FrameClass，标题 "文件名 - WPS Office"。
- 进程模型：wpp.exe = 放映承载；wps.exe = 编辑框/共享宿主。监听器认 wpp.exe 仍然有效。
- RGB 是 BGR：设置颜色时 uint 值 = R | G<<8 | B<<16。
- 墨迹保留提示：本次 COM Exit 未弹窗；含真实笔迹时的行为待验收。

---

## 16. 验收清单

1. 安装：解压 → install.bat → 重启 → 监听器自启、无窗口；wpp.exe 启动后 1s 内浮层出现 loading 圆盘并转勾。
2. 拾取：WPS 开着时注销/重启监听器，能立即拉起浮层。
3. 放映：进入放映 → dock 出现；Esc → dock 消失；再次放映 → dock 再出现，画板内容仍在。
4. dock：6 个控件全部可用；首/末页置灰；长按“笔”出二级菜单；画板打开时 4 个 PPT 控件置灰。
5. 画板：滑入/滑出动画；四周留白点击收起；收起后笔迹保留。
6. 绘制：4 指同时书写互不干扰；颜色/粗细/透明度生效；改设置不影响已画笔迹。
7. 橡皮：整笔删除；点两次出滑动清空；中途松手不清空；清空可撤销。
8. 页面：默认 1/1；右翻末页自动新建；菜单 2 列缩略图；跳页；删除页；收起只收菜单。
9. 穿透：画板收起时点击 dock 以外区域能正常操作 WPS；画板展开时留白点击不翻页。
10. 焦点：点击任何浮层后 WPS 键盘（Esc/方向键）仍有效。
11. WPS 退出：浮层自动退出，监听器继续常驻；重启 WPS 能再次拉起。
12. 崩溃恢复：手动结束 PPTBoard.exe，监听器 2s 内重启，最多 3 次。
13. 日志：watcher.log 有启动、检测、重启、退出记录；7 天滚动。
14. 卸载：uninstall.bat 后自启项消失、目录删除、无残留进程。
15. 兼容：1080p@100% 与 125% 缩放下布局不破；WPS 全屏放映时浮层始终在最上层。

---

## 17. 风险与待验证（实现前先做探针）

1. WPS COM 能力：✅ 已实测（见 §15）。风险降为：GetActiveObject 在 WPS 冷启动瞬间可能失败，桥接层需重试并回退 ROT 文件 moniker。
2. WPS 全屏窗口识别：✅ 已确认 wpp.exe / Qt5QWindowIcon / 标题前缀；COM 仍是主路径。
3. 墨迹保留提示：View.Exit() 后 WPS 是否弹“保留墨迹”；若弹，需决定拦截还是改为模拟按键。
4. WPF WM_POINTER 多指：需验证 HwndSource 钩子能拿到全部触点且不被 WPF 触控提升吞掉；不行则退到 WM_TOUCH + GetTouchInputInfo。
5. 半透明多窗口性能：页面菜单 320×1080 为分层窗口（软件渲染），缩略图重绘时是否卡顿；必要时菜单改为“不透明深色”。
6. 一体机 DPI：目标机若是 125%，主 dock 288px 可能放不下 6 个 40px 控件；运行期按内容宽取 max。
7. WPS 更新：WPS 版本升级可能改 COM/窗口行为；验收时记录版本号。

---

## 18. 决策记录（grilling 问答）

| # | 问题 | 决定 |
|---|---|---|
| Q1 | 使用场景 | 只在全屏放映时（A） |
| Q2 | 目标屏 | 教学一体机，1080p，多点触控，单屏，Win10 |
| Q3 | 画板底下 | 纯白画板，盖住幻灯片（B） |
| Q4 | 页面来源 | 白板自己的页，默认 1 页；菜单加新建/删除（A） |
| Q5 | 新建页内容 | 空白页（A） |
| Q6 | 画笔 dock 尺寸 | 画板宽 40%（666px）×56（A） |
| Q7 | 底部 96px 排布 | 画板底边 96；画笔 dock 56 + 主 dock 40 堆叠（A） |
| Q8 | 多点触控 | 多人同时书写（B） |
| Q9 | 撤销/橡皮/清空位置 | 灰条两端（A） |
| Q10 | 橡皮 | 整笔擦；再点橡皮出清空滑条（A + 补充） |
| Q11 | 清空滑条 | 灰条上方浮出 400px，拖到头生效（A） |
| Q12 | 自定义色 | 透明度 + 明度，仅自定义色（A） |
| Q13 | 粗细 | 3/6/10/16/24，无级 1–32（A） |
| Q14 | “PPT 设置”指什么 | 放映悬浮工具栏那套（A） |
| Q15 | dock 按钮 | 6 个：颜色点、上一页、下一页、笔、画板、结束放映（黑屏删除）；后续追加橡皮，变为 7 个 |
| Q16 | “笔”行为 | 切换 WPS 原生笔，长按二级菜单（A） |
| Q17 | 生命周期 | 进程级拉起 + 放映级显示（A） |
| Q18 | Office/WPS | 原定只做 WPS（C）；后续按用户反馈追加 Microsoft PowerPoint 支持 |
| Q19 | 技术栈 | C# WPF + C 监听器（A） |
| Q20 | 内容寿命 | 仅本次 WPS 进程会话（A） |
| Q21 | 交付 | 绿色 zip + install/uninstall，x64（A） |
| Q22 | loading 时机 | 监听器拉起主程序时（A） |
| Q23 | 页面菜单 | 全高右侧抽屉 320px，2 列缩略图，收起只收菜单（A） |
| Q24 | 页面框位置 | 画板内、灰条右端，56 高（A） |
| Q25 | 收起后画笔 dock | 集成进主 dock：只多一个颜色点（B） |
| Q26 | 黑屏 | 删除（用户决定） |
| Q27 | 视觉风格 | 深色玻璃、无模糊（A） |
| Q28 | 留白点击 | 收起画板（C） |
| Q29 | 监听器参数 | 全部默认通过 |

---

## 19. 里程碑

| 阶段 | 内容 | 产出 |
|---|---|---|
| M0 | 装 WPS，写 COM 探针，验证 §17.1–17.2 | 探针报告 + 降级决策 |
| M1 | C 监听器 + install/uninstall + 自启 | 可安装、能拉起空壳 |
| M2 | WPF 壳：loading、放映检测、dock、置顶、穿透 | 能看能点 |
| M3 | 画板 + WM_POINTER 多指墨迹 | 能写 |
| M4 | 画笔 dock、颜色面板、橡皮、撤销、清空 | 能改能擦 |
| M5 | 页面系统、页面框、右侧菜单、缩略图 | 能翻页 |
| M6 | 视觉打磨、动效、验收 15 项、打包 zip | 可验收版本 |
| M7 | 复制到 F:\PPTBoard、README/LICENSE、推 GitHub 公开仓库 | 开源发布 |

---

*本规格由 /grill-me 问答逐项确定；改动任何一条请先在 §18 决策表里改，再动代码。*
