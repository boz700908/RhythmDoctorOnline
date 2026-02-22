using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Patches = ModEntry.Patches;

[BepInPlugin("com.memsyslizi.rdol", "RDOnline", "1.0.0")]
public class BepInModEntry : BaseUnityPlugin
{
    private AssetBundle checkUpdateScene;
    private AssetBundle checkUpdateResources;
    public static string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    private Harmony Harmony;
    void Start()
    {
        try
        {
            Harmony = new Harmony("Rhythm Doctor Online");
            checkUpdateScene = AssetBundle.LoadFromFile(Path.Combine(modPath,"checkupdate.scene.assets"));
            checkUpdateResources = AssetBundle.LoadFromFile(Path.Combine(modPath,"checkupdate.resources.assets"));
            Assembly.LoadFile(Path.Combine(modPath, "CheckUpdate.dll"));
            Harmony.PatchAll();
        }
        catch (Exception e)
        {
            Logger.LogError(e.ToString());
        }
    }
    
    private void OnDestroy()
    {
        Logger.LogInfo("destroy");
    }
}
