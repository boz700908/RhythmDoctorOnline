using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

[BepInPlugin("guid", "RDOnline", "1.0.0")]
public class BepInModEntry : BaseUnityPlugin
{
    private AssetBundle checkUpdateScene;
    private AssetBundle checkUpdateResources;
    public static string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    // Start is called before the first frame update
    void Start()
    {
        //TODO: 实现下载dll和ab包并加载,这里是冷加载
        try
        {
            checkUpdateScene = AssetBundle.LoadFromFile(Path.Combine(modPath,"checkupdate.scene.assets"));
            checkUpdateResources = AssetBundle.LoadFromFile(Path.Combine(modPath,"checkupdate.resources.assets"));
            Assembly.LoadFile(Path.Combine(modPath, "CheckUpdate.dll"));
            StartCoroutine(DelayEntry());
        }
        catch (Exception e)
        {
            Logger.LogError(e.ToString());
        }
    }
    

    private IEnumerator DelayEntry(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        Logger.LogInfo($"延迟入口启动 (调用者: {memberName}, 文件: {sourceFilePath}, 行号: {sourceLineNumber})");
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene("scnCheckUpdate");
    }

    private void OnDestroy()
    {
        Logger.LogInfo("destroy");
    }
}
