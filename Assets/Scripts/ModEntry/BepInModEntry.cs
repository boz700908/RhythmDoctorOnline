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
    AssetBundle scenesBundle;
    AssetBundle resourcesBundle;
    Assembly assembly;
    public static string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    // Start is called before the first frame update
    void Start()
    {
        //TODO: 实现下载dll和ab包并加载,这里是冷加载
        try
        {
            scenesBundle = AssetBundle.LoadFromFile(Path.Combine(modPath,"rdol.scenes.assets"));
            resourcesBundle = AssetBundle.LoadFromFile(Path.Combine(modPath,"rdol.resources.assets"));
            assembly = Assembly.LoadFrom(Path.Combine(modPath,"RDOL.dll"));
            StartCoroutine(DelayEntry());
        }
        catch (Exception e)
        {
            Logger.LogError(e.ToString());
        }
    }

    private void OnGUI()
    {
        if (GUILayout.Button("进入"))
        {
            try
            {
                SceneManager.LoadScene(scenesBundle.GetAllScenePaths().First(a => a.Contains("StartUp")));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }

    private IEnumerator DelayEntry(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        Logger.LogInfo($"延迟入口启动 (调用者: {memberName}, 文件: {sourceFilePath}, 行号: {sourceLineNumber})");
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene(scenesBundle.GetAllScenePaths().First(a => a.Contains("StartUp")));
    }

    private void OnDestroy()
    {
        Logger.LogInfo("destroy");
    }
}
