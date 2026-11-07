PPTBoard - WPS 演示 / PowerPoint 白板浮层
================================

安装
----
1. 把 zip 解压到任意文件夹（不要直接在压缩包里运行）。
2. 双击 install.bat。
   它会把程序复制到 %LOCALAPPDATA%\PPTBoard，写入当前用户的开机自启项，
   并启动后台监听进程 PPTBoardWatcher.exe。
3. 打开一个 .pptx（WPS 演示或 Microsoft PowerPoint），按 F5 进入放映。
   屏幕底部会出现 dock；点 dock 里的画板按钮拉下白板。

使用
----
- 底部 dock：颜色点、上一页、下一页、原生笔、橡皮、画板、结束放映。
- 画板：灰条左侧是撤销 / 橡皮；中间向上箭头收起；右侧是页码和翻页。
- 画笔 dock：左半是红/蓝/黑和自定义彩色圆盘；右半是五档粗细 + 无级滑杆。颜色面板顶部也有红/蓝/黑预设。
- 主 dock 的橡皮：画板开着时切白板橡皮；画板关着时切放映原生橡皮（不会打开画板）。画板灰条上的橡皮点一次进入擦除，再点一次弹“向右滑动清空本页”。
- 页码中间的“1/1”：点开右侧页面菜单，可看缩略图、新建页、删除页。
- 点画板外的左右留白：收起画板。点颜色面板外任意位置：关闭颜色面板。
- 画板内容只在本次 WPS 进程内保留；关掉 WPS 就清空。

卸载
----
双击 uninstall.bat（或运行 %LOCALAPPDATA%\PPTBoard\uninstall.bat）。

日志
----
%LOCALAPPDATA%\PPTBoard\logs
  watcher-YYYYMMDD.log   监听器日志（保留 7 天）
  app.log                浮层日志

注意
----
- 需要 Windows 10 x64。
- 支持 WPS 演示（wpp.exe）和 Microsoft PowerPoint（POWERPNT.EXE）。
- 自包含发布，不需要单独安装 .NET 运行时。