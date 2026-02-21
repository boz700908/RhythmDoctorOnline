using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Patches = ModEntry.Patches;

[BepInPlugin("guid", "RDOnline", "1.0.0")]
public class BepInModEntry : BaseUnityPlugin
{
    private AssetBundle checkUpdateScene;
    private AssetBundle checkUpdateResources;
    public static string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    private Harmony Harmony;
    private float scale = 1;
    // Start is called before the first frame update
    void Start()
    {
        //TODO: 实现下载dll和ab包并加载,这里是冷加载
        try
        {
            Harmony = new Harmony("Rhythm Doctor Online");
            checkUpdateScene = AssetBundle.LoadFromFile(Path.Combine(modPath,"checkupdate.scene.assets"));
            checkUpdateResources = AssetBundle.LoadFromFile(Path.Combine(modPath,"checkupdate.resources.assets"));
            Assembly.LoadFile(Path.Combine(modPath, "CheckUpdate.dll"));
            Harmony.PatchAll();
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
        yield return new WaitForSeconds(1);
        //SceneManager.LoadScene("scnCheckUpdate");
        Logger.LogInfo($"延迟入口启动 (调用者: {memberName}, 文件: {sourceFilePath}, 行号: {sourceLineNumber})");
        yield return null;
    }

    private void OnDestroy()
    {
        Logger.LogInfo("destroy");
    }
    /*/// <summary>
    /// 移除指定 RectTransform 的所有子物体并重新添加它们。
    /// 操作前会输出每个子物体的详细状态。
    /// </summary>
    /// <param name="parent">要操作的父级 RectTransform</param>
    /// <param name="worldPositionStays">如果为 true，则子物体的世界位置、旋转和缩放保持不变；否则它们会相对于新的父级重新定位。</param>
    public void RemoveAndReaddChildren(RectTransform parent, bool worldPositionStays = true)
    {
        // 尝试修复不可见的子物体（将其设为可见）
        List<Text> texts = new List<Text>();
        int fixedCount = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                fixedCount++;
                child.localPosition = new(0, fixedCount * -15);
            }
            var txt = child.GetComponent<Text>();
            if (child.name == "RDOL") txt.text = "RDOL";
            texts.Add(txt);
        }
        typeof(scnMenu).GetField("optionsText",BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(scnMenu.instance,texts.ToArray());
        typeof(scnMenu).GetMethod("HighlightOption",BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(scnMenu.instance,new object[]{1,true,true});
        Transform val = scnMenu.instance.options.GetChild(0);
        if (val != null)
        {
            Rect rect = val.GetComponent<RectTransform>().rect;
            float height = rect.height;
            RectTransform component = scnMenu.instance.options.GetComponent<RectTransform>();
            component.position = new Vector2(component.position.x, component.position.y + height / 1.5f);
        }
    }*/

}
