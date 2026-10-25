#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RDOL.Editor
{
    /// <summary>
    /// 命令行出 Windows 包。用法见仓库根目录 BUILDING-WINDOWS.md。
    ///
    /// 产出的目录布局必须和运行时代码逐字对齐，改这里之前先读这几处：
    ///   BepInModEntry.Start             -&gt; &lt;modPath&gt;/checkupdate.scene.assets、checkupdate.resources.assets、CheckUpdate.dll
    ///   ScnCheckUpdate.LoadAssetBundles -&gt; &lt;modPath&gt;/CacheAssets/rdol.scenes.assets、rdol.resources*.assets、RDOL.dll
    ///   ScnCheckUpdate.StreamingAssets  -&gt; &lt;modPath&gt;/CacheAssets（版本比对、清单清理都只碰这个目录）
    ///   AssetBundleManager              -&gt; 已加载的 bundle 优先，找不到再从根目录、CacheAssets 兜底
    ///
    /// 为什么只拷三个自己的 DLL：
    /// 0Harmony / DOTween / Newtonsoft / RDTools 在游戏 Managed 目录与 BepInEx core 里
    /// 本来就有，再拷一份会让同名类型来自两个程序集，补丁与场景脚本直接撞车。
    /// 第三方 DLL 一个都不进包。
    /// </summary>
    public static class ModBuilder
    {
        const string ScenesBundle = "rdol.scenes";
        const string ResourcesBundle = "rdol.resources";
        const string CheckUpdateSceneBundle = "checkupdate.scene";
        const string CheckUpdateResourcesBundle = "checkupdate.resources";
        const string CacheDirectory = "CacheAssets";
        const string ConfigFileName = "rdol.config.json";
        const string ManifestFileName = "manifest-sha256.txt";

        const BuildAssetBundleOptions BundleOptions =
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle;

        static readonly string[] OwnAssemblies = { "RDOL", "RDOL.Entry", "CheckUpdate" };

        static string ProjectRoot
        {
            get { return Directory.GetParent(Application.dataPath).FullName; }
        }

        [MenuItem("Tools/RDOL/出包（Windows）")]
        public static void BuildFromCommandLine()
        {
            int code = 0;
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                string outDir = Value(args, "-rdolOut");
                if (string.IsNullOrEmpty(outDir))
                    outDir = Path.Combine(ProjectRoot, "dist", "RDOnline");

                Build(outDir, Value(args, "-rdolPackage"), Value(args, "-rdolBepInEx"), Value(args, "-rdolGame"));
            }
            catch (Exception e)
            {
                Debug.LogError("[RDOL] 出包失败：\n" + e);
                code = 1;
            }

            // batchmode 下必须自己退出，否则进程挂住，外面的脚本永远等不到结束
            EditorApplication.Exit(code);
        }

        /// <summary>出包主流程。outDir 是插件目录，packageDir 是可直接盖到游戏根目录的整包。</summary>
        public static void Build(string outDir, string packageDir, string bepInExDir, string gameDir)
        {
            if (string.IsNullOrEmpty(outDir)) throw new ArgumentException("缺少输出目录");

            RequireWindowsTarget();

            string staging = Path.Combine(ProjectRoot, "AssetBundles", "win");
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            Directory.CreateDirectory(staging);

            BuildScenesBundle(staging);
            BuildResourcesBundle(staging);
            BuildCheckUpdateBundles(staging);
            AssetDatabase.Refresh();

            PrepareDirectory(outDir);
            CopyBundles(staging, outDir);
            CopyAssemblies(outDir);
            CopyConfig(outDir);
            WriteManifest(outDir);
            Directory.Delete(staging, true);

            string packageRoot = BuildOverlay(outDir, packageDir, bepInExDir);
            if (!string.IsNullOrEmpty(gameDir)) InstallToGame(packageRoot, outDir, gameDir);

            Debug.Log("[RDOL] 出包完成：" + (packageRoot ?? outDir));
        }

        // ---------- 平台 ----------

        static void RequireWindowsTarget()
        {
            BuildTarget current = EditorUserBuildSettings.activeBuildTarget;
            if (current == BuildTarget.StandaloneWindows64) return;

            Debug.LogWarning("[RDOL] 当前构建目标是 " + current + "，资源包必须是 Windows64，正在切换。");
            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

            if (!switched || EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new Exception("切不到 StandaloneWindows64：请给这个 Unity 装上 Windows Build Support (Mono) 模块。");
        }

        // ---------- 资源包 ----------

        static void BuildScenesBundle(string staging)
        {
            var scenes = new List<string>();
            foreach (string file in Directory.GetFiles(
                Path.Combine(ProjectRoot, "Assets", "Scenes"), "*.unity", SearchOption.AllDirectories))
            {
                // 版本检查场景有自己单独的包，BepInModEntry 直接加载，不能混进来
                if (string.Equals(Path.GetFileNameWithoutExtension(file), "scnCheckUpdate",
                        StringComparison.OrdinalIgnoreCase)) continue;

                string assetPath = ToAssetPath(file);
                if (!string.IsNullOrEmpty(assetPath)) scenes.Add(assetPath);
            }

            if (scenes.Count == 0) throw new Exception("Assets/Scenes 里没有可打的场景。");

            Build(staging, ScenesBundle, scenes);
            Debug.Log("[RDOL] " + ScenesBundle + "：" + scenes.Count + " 个场景（" +
                      string.Join("、", scenes.Select(Path.GetFileNameWithoutExtension).ToArray()) + "）");
        }

        static void BuildResourcesBundle(string staging)
        {
            var assets = new List<string>();
            assets.AddRange(CollectAssets("Assets/Resources"));
            assets.AddRange(CollectAssets("Assets/TextMesh Pro"));
            assets = assets.Distinct().ToList();

            if (assets.Count == 0) throw new Exception("Assets/Resources 是空的，UI 与提示文案都会缺。");

            Build(staging, ResourcesBundle, assets);
            Debug.Log("[RDOL] " + ResourcesBundle + "：" + assets.Count + " 个资源");
        }

        static void BuildCheckUpdateBundles(string staging)
        {
            string scenePath = "Assets/Scenes/scnCheckUpdate.unity";
            if (!File.Exists(Path.Combine(ProjectRoot, scenePath.Replace('/', Path.DirectorySeparatorChar))))
                throw new Exception("找不到 " + scenePath + "，版本检查入口出不来。");

            var builds = new List<AssetBundleBuild>
            {
                new AssetBundleBuild { assetBundleName = CheckUpdateSceneBundle, assetNames = new[] { scenePath } }
            };

            List<string> resources = CollectAssets("Assets/CheckUpdate");
            if (resources.Count > 0)
            {
                builds.Add(new AssetBundleBuild
                {
                    assetBundleName = CheckUpdateResourcesBundle,
                    assetNames = resources.ToArray()
                });
            }

            BuildPipeline.BuildAssetBundles(staging, builds.ToArray(), BundleOptions, BuildTarget.StandaloneWindows64);
            Debug.Log("[RDOL] " + CheckUpdateSceneBundle + " + " + CheckUpdateResourcesBundle +
                      "（" + resources.Count + " 个资源）");
        }

        static void Build(string staging, string bundleName, List<string> assets)
        {
            var build = new[] { new AssetBundleBuild { assetBundleName = bundleName, assetNames = assets.ToArray() } };
            BuildPipeline.BuildAssetBundles(staging, build, BundleOptions, BuildTarget.StandaloneWindows64);
        }

        /// <summary>把一个目录下的资源收成 bundle 用的资源路径，脚本与 meta 不收。</summary>
        static List<string> CollectAssets(string folder)
        {
            var result = new List<string>();
            string full = Path.Combine(ProjectRoot, folder.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(full)) return result;

            foreach (string file in Directory.GetFiles(full, "*", SearchOption.AllDirectories))
            {
                if (IsIgnoredAsset(file)) continue;
                string assetPath = ToAssetPath(file);
                if (!string.IsNullOrEmpty(assetPath)) result.Add(assetPath);
            }
            return result;
        }

        static bool IsIgnoredAsset(string file)
        {
            string[] ignored = { ".meta", ".cs", ".asmdef", ".asmref", ".unity" };
            string name = Path.GetFileName(file);
            foreach (string suffix in ignored)
            {
                if (string.Equals(Path.GetExtension(name), suffix, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        static string ToAssetPath(string fullPath)
        {
            string root = ProjectRoot.Replace('\\', '/').TrimEnd('/') + "/";
            string path = fullPath.Replace('\\', '/');
            return path.StartsWith(root, StringComparison.Ordinal) ? path.Substring(root.Length) : null;
        }

        // ---------- 成品目录 ----------

        static void PrepareDirectory(string dir)
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
        }

        static void CopyBundles(string staging, string outDir)
        {
            string cache = Path.Combine(outDir, CacheDirectory);
            Directory.CreateDirectory(cache);

            // 版本检查场景的两个包由 BepInModEntry 从 mod 根目录直接加载
            CopyBundle(staging, CheckUpdateSceneBundle, Path.Combine(outDir, CheckUpdateSceneBundle + ".assets"));
            CopyBundle(staging, CheckUpdateResourcesBundle, Path.Combine(outDir, CheckUpdateResourcesBundle + ".assets"));

            // 主场景与资源包由 ScnCheckUpdate 从 CacheAssets 加载，只放一份，避免双份占地
            CopyBundle(staging, ScenesBundle, Path.Combine(cache, ScenesBundle + ".assets"));
            CopyBundle(staging, ResourcesBundle, Path.Combine(cache, ResourcesBundle + ".assets"));
        }

        /// <summary>Unity 输出的文件名就是 bundle 名，运行时却按 *.assets 找，所以拷的时候改名。</summary>
        static void CopyBundle(string staging, string bundleName, string target)
        {
            string source = Path.Combine(staging, bundleName);
            if (!File.Exists(source))
                throw new Exception("没有生成资源包 " + bundleName + "，看看 " + staging + " 里有什么。");

            File.Copy(source, target, true);
            Debug.Log("[RDOL] " + bundleName + " -> " + target);
        }

        static void CopyAssemblies(string outDir)
        {
            var compiled = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            if (compiled == null || compiled.Length == 0)
                compiled = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

            foreach (string name in OwnAssemblies)
            {
                var match = compiled.FirstOrDefault(a => string.Equals(a.name, name, StringComparison.Ordinal));
                if (match == null || string.IsNullOrEmpty(match.outputPath) || !File.Exists(match.outputPath))
                    throw new Exception("找不到已编译的 " + name + ".dll，先让 Unity 把项目编译一遍。");

                File.Copy(match.outputPath, Path.Combine(outDir, name + ".dll"), true);
                Debug.Log("[RDOL] " + name + ".dll <- " + match.outputPath);
            }
        }

        static void CopyConfig(string outDir)
        {
            string source = Path.Combine(ProjectRoot, ConfigFileName);
            if (!File.Exists(source))
                throw new Exception("仓库根目录没有 " + ConfigFileName + "，服务器地址会退回代码里的默认值，先把它补上。");

            File.Copy(source, Path.Combine(outDir, ConfigFileName), true);
        }

        static void WriteManifest(string outDir)
        {
            var text = new StringBuilder();
            foreach (string file in Directory.GetFiles(outDir, "*", SearchOption.AllDirectories)
                         .OrderBy(f => f, StringComparer.Ordinal))
            {
                if (string.Equals(Path.GetFileName(file), ManifestFileName, StringComparison.OrdinalIgnoreCase)) continue;
                text.AppendLine(Sha256(file) + "  " + ToRelative(outDir, file));
            }

            File.WriteAllText(Path.Combine(outDir, ManifestFileName), text.ToString(), new UTF8Encoding(false));
        }

        static string ToRelative(string root, string file)
        {
            string r = root.Replace('\\', '/').TrimEnd('/') + "/";
            return file.Replace('\\', '/').Substring(r.Length);
        }

        static string Sha256(string file)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(file))
            {
                byte[] hash = sha.ComputeHash(stream);
                var text = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) text.Append(b.ToString("x2"));
                return text.ToString();
            }
        }

        /// <summary>把插件目录和 BepInEx 5 拼成可以直接盖到游戏根目录的整包。</summary>
        static string BuildOverlay(string modDir, string packageDir, string bepInExDir)
        {
            if (string.IsNullOrEmpty(packageDir))
            {
                if (!string.IsNullOrEmpty(bepInExDir))
                    Debug.LogWarning("[RDOL] 给了 -rdolBepInEx 但没给 -rdolPackage，只出插件目录。");
                return null;
            }

            if (string.IsNullOrEmpty(bepInExDir))
                throw new Exception("出整包需要 -rdolBepInEx 指向 BepInEx 5 win_x64 解压目录（含 winhttp.dll 与 BepInEx 文件夹）。");

            if (!Directory.Exists(bepInExDir)) throw new Exception("BepInEx 目录不存在：" + bepInExDir);

            PrepareDirectory(packageDir);

            // BepInEx 自带的 plugins/cache 不要带进来，免得把别人的插件一起发出去
            CopyDirectory(bepInExDir, packageDir, new[] { "BepInEx/plugins", "BepInEx/cache" });
            CopyDirectory(modDir, Path.Combine(packageDir, "BepInEx", "plugins", "RDOnline"));

            Debug.Log("[RDOL] 整包：" + packageDir);
            return packageDir;
        }

        static void InstallToGame(string packageRoot, string modDir, string gameDir)
        {
            if (!Directory.Exists(gameDir)) throw new Exception("游戏目录不存在：" + gameDir);

            if (!File.Exists(Path.Combine(gameDir, "Rhythm Doctor.exe")))
                Debug.LogWarning("[RDOL] 游戏目录里没看到 Rhythm Doctor.exe，照常继续：" + gameDir);

            if (packageRoot != null)
                CopyDirectory(packageRoot, gameDir);
            else
                CopyDirectory(modDir, Path.Combine(gameDir, "BepInEx", "plugins", "RDOnline"));

            Debug.Log("[RDOL] 已装进游戏：" + gameDir);
        }

        /// <summary>递归复制整个目录；不必排除任何相对路径时 excludeRelative 传 null 即可。</summary>
        static void CopyDirectory(string source, string target, string[] excludeRelative = null)
        {
            string sourceRoot = source.Replace('\\', '/').TrimEnd('/') + "/";

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string normal = file.Replace('\\', '/');
                string relative = normal.StartsWith(sourceRoot, StringComparison.Ordinal)
                    ? normal.Substring(sourceRoot.Length)
                    : Path.GetFileName(file);

                if (IsExcluded(relative, excludeRelative)) continue;

                string destination = Path.Combine(target, relative.Replace('/', Path.DirectorySeparatorChar));
                string folder = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder)) Directory.CreateDirectory(folder);
                File.Copy(file, destination, true);
            }
        }

        static bool IsExcluded(string relative, string[] excludeRelative)
        {
            if (excludeRelative == null) return false;
            foreach (string rule in excludeRelative)
            {
                if (relative.Equals(rule, StringComparison.OrdinalIgnoreCase) ||
                    relative.StartsWith(rule + "/", StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        static string Value(string[] args, string flag)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return null;
        }
    }
}
#endif
