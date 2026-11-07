# PPTBoard

给 WPS 演示用的放映浮层：底部半透明 dock + 可翻页白色画板，支持多点触控同时书写、整笔橡皮、撤销、滑动清空、页面缩略图预览。由一个 C 监听器在检测到 wpp.exe 时拉起，可注册表开机自启。

## 状态

开发中。规格见 docs/SPEC.md。

## 目标环境

- Windows 10 x64（教学一体机，1920×1080，多点触控）
- WPS 演示（wpp.exe）
- 不需要安装 .NET 运行时（自包含发布）

## 目录

    src/PPTBoard.Core   布局公式、主题常量、接口契约（无 WPF）
    src/PPTBoard.Ink    笔迹模型、多指引擎、撤销（无 WPF）
    src/PPTBoard.Wps    WPS COM 与放映检测（无 WPF）
    src/PPTBoard.App    WPF 浮层窗口
    src/watcher         C 监听器
    tests/              单元测试

## 构建

    pwsh build/build.ps1

## 许可证

MIT
