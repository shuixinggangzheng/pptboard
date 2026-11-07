# PPTBoard

WPS 演示 / Microsoft PowerPoint 的放映浮层：底部半透明 dock + 可翻页白色画板，支持多点触控书写、整笔橡皮、撤销、滑动清空、页面缩略图。C 监听器在检测到 wpp.exe 或 POWERPNT.EXE 时拉起浮层，可注册表开机自启。

## 状态

功能开发基本完成，正在做真机验收。进度见 STATUS.md，规格见 SPEC.md，WPS COM 实测见 probe/REPORT.md。

## 环境

- Windows 10 x64（目标：1080p 教学一体机，多点触控）
- WPS 演示（wpp.exe）或 Microsoft PowerPoint（POWERPNT.EXE）
- 自包含发布，目标机不需要 .NET 运行时

## 目录

    src/PPTBoard.Core   布局公式、主题常量、接口契约（无 WPF）
    src/PPTBoard.Ink    笔迹模型、多指引擎、撤销（无 WPF）
    src/PPTBoard.Wps    WPS / PowerPoint COM 与 ROT 兜底（无 WPF）
    src/PPTBoard.App    WPF 浮层窗口
    src/watcher         C 监听器
    tools/PPTBoard.Render  离屏渲染（不弹窗）
    tools/verify.ps1       目标机只读自检
    tests/              单元测试（24 项）

## 构建与打包

    pwsh build/build.ps1

产物：artifacts/PPTBoard-0.1.0-win-x64.zip

## 安装

推荐用 setup 安装包：

    PPTBoard-0.1.0-setup.exe

双击后按向导安装到 `%LOCALAPPDATA%\PPTBoard`（无需管理员），会自动写开机自启并启动监听器；卸载在“设置 → 应用 → PPTBoard”。

也可以解压 zip 后运行 install.bat（绿色方式）。

## 构建 setup

    makensis installer/setup.nsi

需要 NSIS 3.x；输出 artifacts/PPTBoard-0.1.0-setup.exe。

## 不打扰桌面的验证

    dotnet test PPTBoard.slnx
    dotnet run --project tools/PPTBoard.Render -- artifacts/render
    dotnet run --project tools/PPTBoard.Render -- --com
    artifacts\PPTBoard\PPTBoardWatcher.exe /verify

## 仓库

- GitHub：https://github.com/shuixinggangzheng/pptboard
- 发布包：F:\PPTBoard\dist\PPTBoard-0.1.0-win-x64.zip（暂存，验收后定版）

## 许可证

MIT