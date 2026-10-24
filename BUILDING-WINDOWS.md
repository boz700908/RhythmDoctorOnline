# 在 Windows 上出包

一键出包只需要一样东西：Unity 编辑器。BepInEx 5 加载器与游戏程序集都随仓库自带。

## 一、准备

### 1. Unity 编辑器

必须是 **2022.3.62f3c1**（`ProjectSettings/ProjectVersion.txt` 里写的就是这个版本），
并且安装时要勾上 **Windows Build Support (Mono)**。没这个模块切不到
`StandaloneWindows64`，脚本会直接报错告诉你缺什么。

如果你只有 Unity Hub 装的 2022.3 其它小版本，也能用，只是首次打开会升级工程，
升级前先备份 `ProjectSettings/`。用环境变量指定路径：

```
set RDOL_UNITY=C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe
```

### 2. BepInEx 5（Windows x64）——仓库已自带

**不需要下载。** 加载器随仓库放在 **`tools\bepinex5-win-x64\`**，版本 5.4.23.5
（Doorstop 4.5.0），出包脚本直接从这里拼整包。克隆完整后这个目录里应该有：

```
RhythmDoctorOnline\tools\bepinex5-win-x64\winhttp.dll
RhythmDoctorOnline\tools\bepinex5-win-x64\doorstop_config.ini
RhythmDoctorOnline\tools\bepinex5-win-x64\.doorstop_version
RhythmDoctorOnline\tools\bepinex5-win-x64\BepInEx\core\...
```

这个目录里的文件是仓库内容的一部分，**不要改成自己的版本**，否则打出来的包每个人
跑的加载器都不一样。升级时整目录替换，并同步改 `build-windows.bat` 顶部的注释与
`tools\bepinex5-win-x64\README.md` 里的版本号。

只支持 **BepInEx 5**。BepInEx 6 去掉了 RDOL 依赖的 `BepInEx.Harmony` 接口，装上去插件不加载。

## 二、出包

```
build-windows.bat
```

要顺手装进游戏目录：

```
build-windows.bat "C:\Program Files (x86)\Steam\steamapps\common\Rhythm Doctor"
```

脚本做的事：

1. 找 Unity（`RDOL_UNITY` → Unity Hub 默认目录）。
2. 检查 `tools\bepinex5-win-x64\winhttp.dll`。
3. 以 `-batchmode -nographics` 跑 `RDOL.Editor.ModBuilder.BuildFromCommandLine`。
4. 校验运行时真正会加载的那些文件是否都在。

产物：

| 路径 | 作用 |
| --- | --- |
| `dist\RDOnline\` | 插件本体，等于 `BepInEx\plugins\RDOnline\` |
| `dist\package\` | 插件 + BepInEx，**整个目录内容盖到游戏根目录即可** |
| `dist\build.log` | 完整 Unity 日志，出问题先看这里 |

## 三、产物里为什么是这些文件

目录布局不是随便定的，运行时代码逐个写死了路径。改结构前先读这几处：

| 运行时读取方 | 期望路径 |
| --- | --- |
| `BepInModEntry.Start()` | `<mod>/checkupdate.scene.assets`、`<mod>/checkupdate.resources.assets`、`<mod>/CheckUpdate.dll` |
| `ScnCheckUpdate.LoadAssetBundles()` | `<mod>/CacheAssets/rdol.scenes.assets`、`<mod>/CacheAssets/rdol.resources*.assets`、`<mod>/CacheAssets/RDOL.dll` |
| `ScnCheckUpdate`（版本比对与清理） | 只管 `<mod>/CacheAssets/` |
| `AssetBundleManager` | 先找已加载的 bundle，再从根目录与 `CacheAssets` 找 `rdol.resources*` |

所以 `dist\RDOnline\` 长这样：

```
BepInEx\plugins\RDOnline\
    RDOL.dll                 场景与 UI 逻辑
    RDOL.Entry.dll           BepInEx 插件入口（Harmony 补丁）
    CheckUpdate.dll          版本检查场景逻辑
    checkupdate.scene.assets  版本检查场景
    checkupdate.resources.assets
    rdol.config.json         服务器地址
    manifest-sha256.txt      每个文件的 sha256，用来核对别人传给你的包
    CacheAssets\
        rdol.scenes.assets    Login / ScnLobby / ScnRoom 等场景
        rdol.resources.assets  UI 预制体、字体、贴图、tips.json
```

### 第三方 DLL 一个都不拷

`0Harmony`、`DOTween`、`Newtonsoft.Json`、`RDTools`、`Assembly-CSharp` 这些在游戏的
`Managed` 目录和 BepInEx `core` 里本来就有。再拷一份进插件目录会让同一个类型存在两个
程序集里，Harmony 补丁和场景脚本会对着不同副本操作，症状是「补丁明明打了却不生效」，
或者 `InvalidCastException`。**这是最容易踩的坑，别往包里塞现成的 DLL。**

## 四、服务器地址

`rdol.config.json` 在仓库根目录，出包时原样拷贝。改地址只需要改这个文件后重新出包，
或者直接改游戏里 `BepInEx\plugins\RDOnline\rdol.config.json` 再进一次游戏，不必重编译。

```json
{
  "apiBase": "http://47.238.109.184:3004",
  "wsUrl": "ws://47.238.109.184:3005",
  "versionCheckUrl": "http://47.238.109.184:3004",
  "chartUploadUrl": "http://47.238.109.184:3004/charts/upload"
}
```

字段都可省，缺省时会退回代码里的默认值并在 `BepInEx\LogOutput.log` 里打一行警告。

## 五、版本号

客户端内置版本号有两处，**必须保持一致**，否则版本检查与登录会互相打架：

- `Assets/CheckUpdate/ScnCheckUpdate.cs` 的 `BuiltInVersion`
- `Assets/Scripts/Auth/LoginUI.cs` 的 `VersionCheckBuiltInVersion`

发版时把这两个一起改，再同步改服务端 `API_VERSION`（`rdol-server` 的环境变量）。

## 六、常见问题

**`error CS` 一堆，且提到 `RDString` / `Narration` / `LevelDocument`**

`Packages/Rhythm Doctor/` 里的游戏程序集没被导入。确认这个目录在（仓库已经带了），
然后在 Unity 里 `Assets > Reimport All`。

**切不到 StandaloneWindows64**

Unity 安装时没勾 Windows Build Support (Mono)。从 Unity Hub 补装模块。

**游戏里没有「多人游戏」入口**

插件没加载。看 `BepInEx\LogOutput.log`：
- 没有 `Loading [RDOnline 1.0.0]` → `winhttp.dll` 没放对，或用了 BepInEx 6。
- 有加载但后面跟异常 → 多半是插件目录里混进了重复的第三方 DLL，见第三节。

**进游戏立刻闪回主菜单 / 资源全没了**

`ScnCheckUpdate` 会按服务端清单清理 `CacheAssets` 里多余的文件。现在只有服务端
**确实下发非空清单**时才会删（`hasManifest` 判断）。如果你的服务端 `/checkupdate`
返回了错误的 `files` 列表，仍然可能删掉不该删的东西——把 `files` 去掉或修对。

**想只出插件、不做整包**

把 `build-windows.bat` 里的 `PACKAGE` 留空，或者直接删掉 `-rdolPackage` 参数；
不给 `-rdolBepInEx` 就不会拼整包，只出 `dist\RDOnline\`。

## 七、没有命令行时

Unity 编辑器菜单里也有入口：**Tools > RDOL > 出包（Windows）**，
它和批处理调用的是同一个 `ModBuilder.BuildFromCommandLine`，参数从命令行取。
