using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class ModFileCopier
    {
        private static readonly string gamePluginsPath = @"E:\Rhythm Doctor\BepInEx\plugins";
        [MenuItem("Tools/复制Mod文件")]
        public static void CopyModFiles()
        {
            string modDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "RDOL");
            string assembliesDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library","ScriptAssemblies");
            string assetBundleDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ThunderKit","AssetBundleStaging","StandaloneWindows");
            if (!Directory.Exists(modDir))
            {
                Directory.CreateDirectory(modDir);
            }
            
            if (!Directory.Exists(assetBundleDir))
            {
                EditorUtility.DisplayDialog("错误", "AssetBundle目录不存在,请先构建AssetBundles", "确定");
            }
            File.Copy(Path.Combine(assembliesDir,"CheckUpdate.dll"), Path.Combine(modDir,"CheckUpdate.dll"),true);
            File.Copy(Path.Combine(assembliesDir,"RDOL.Entry.dll"), Path.Combine(modDir,"RDOL.Entry.dll"),true);
            File.Copy(Path.Combine(assetBundleDir,"checkupdate.scene.assets"), Path.Combine(modDir,"checkupdate.scene.assets"),true);
            File.Copy(Path.Combine(assetBundleDir,"checkupdate.resources.assets"), Path.Combine(modDir,"checkupdate.resources.assets"),true);
        }
        [MenuItem("Tools/启动游戏")]
        public static void StartGame()
        {
            string modDir = Path.Combine(gamePluginsPath, "RDOL");
            if (!Directory.Exists(modDir))
            {
                Directory.CreateDirectory(modDir);
            }
            Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Application.dataPath), "RDOL")).ToList().ForEach(a =>
            {
                File.Copy(a, Path.Combine(modDir, Path.GetFileName(a)), true);
            });
            Process.Start(new DirectoryInfo(gamePluginsPath).Parent.Parent.FullName + "\\Rhythm Doctor.exe");
        }
    }
}