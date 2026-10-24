# BepInEx 5（Windows x64），随仓库自带

出包脚本 `build-windows.bat` 直接用这里的文件拼整包，不需要另外下载。

- 版本：BepInEx **5.4.23.5**（Doorstop 4.5.0）
- 来源：https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5 的 `BepInEx_win_x64_5.4.23.5.zip`
- 许可：LGPL-2.1（见 `changelog.txt` 与上游仓库）

目录里只有加载器本体（`winhttp.dll`、`doorstop_config.ini`、`BepInEx/core/*`），
不含 `BepInEx/plugins` 与 `BepInEx/cache`——那两个目录由出包脚本在拼包时生成。

本版本已包含 Unity 6 的日志写入修复与 2022.3.62f 之后的 Doorstop 兼容改动，
与游戏当前使用的 Unity 6000.3.21f1 匹配。

不要手动改这里的 DLL；升级时整目录替换，并在 `BUILDING-WINDOWS.md` 里同步版本号。
