using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#pragma warning disable 0455
namespace ModEntry
{
#if RHYTHMDOCTOR
    public class Patches
    {
        private const string modName = "RDOL";
        [HarmonyPatch(typeof(scnMenu), "Awake")]
        public class Patch_scnMenu_Start
        {
            public static void Prefix(scnMenu __instance)
            {
                RectTransform optionsContainer = __instance.optionsContainer;
                if (optionsContainer != null)
                {
                    try
                    {
                        Transform val = optionsContainer.Find("customLevels");
                        GameObject val2 = Object.Instantiate(val.gameObject);
                        val2.transform.SetParent(optionsContainer.transform, false);
                        val2.transform.SetSiblingIndex(val.GetSiblingIndex() + 1);
                        val2.name = modName;
                        val2.gameObject.SetActive(true);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                    }
                }
            }

            public static void Postfix(scnMenu __instance)
            {
                RectTransform options = __instance.options;
                if (options != null)
                {
                    Transform val = __instance.optionsContainer.Find(modName);
                    if (val != null)
                    {
                        Rect rect = val.GetComponent<RectTransform>().rect;
                        float height = rect.height;
                        RectTransform component = options.GetComponent<RectTransform>();
                        component.position = new Vector2(component.position.x, component.position.y + height / 1.5f);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RDString), "GetWithCheck")]
        public static class RDString_GetWithCheck
        {
            private static bool Prefix(string key, out bool exists, ref string __result)
            {
                if (key == "mainMenu." + modName)
                {
                    __result = "多人游戏";
                    exists = true;
                    return false;
                }
                exists = false;
                return true;
            }
        }

        [HarmonyPatch(typeof(scnMenu), "SelectOption")]
        public static class scnMenu_SelectOption
        {
            private static void Prefix(scnMenu __instance)
            {
                int num = (int)__instance.GetType()
                    .GetField("currentOption", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(__instance);
                if ((__instance.GetType()
                        .GetField("optionsText", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(__instance) as Text[])[num].gameObject.name == modName)
                {
                    __instance.conductor.StopSong();
                    typeof(scnMenu).GetMethod("TransitionToScene", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[1] { "scnCheckUpdate" });
                }
            }
        }

        [HarmonyPatch(typeof(scnBase), "GoToScene")]
        static class scnBase_GoToScene_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                ILGenerator generator)
            {
                /*
                 * // [291 5 - 291 37]
    IL_0000: call         class DebugSettings RDBase::get_debugSettings()
    IL_0005: callvirt     instance bool DebugSettings::get_Debug()
    IL_000a: brtrue.s     IL_0017

    // [292 7 - 292 40]
    IL_000c: call         class DebugSettings RDBase::get_debugSettings()
    IL_0011: ldc.i4.0
    IL_0012: callvirt     instance void DebugSettings::set_Auto(bool)

    // [293 5 - 293 63]
    IL_0017: call         valuetype [UnityEngine.CoreModule]UnityEngine.SceneManagement.Scene [UnityEngine.CoreModule]UnityEngine.SceneManagement.SceneManager::GetActiveScene()
    IL_001c: stloc.0      // V_0
    IL_001d: ldloca.s     V_0
    IL_001f: call         instance string [UnityEngine.CoreModule]UnityEngine.SceneManagement.Scene::get_name()
    IL_0024: stsfld       string scnBase::lastSceneName

    // [294 5 - 294 62]
    IL_0029: call         !0/*class AudioManager* / class Singleton`1<class AudioManager>::get_Instance()
    IL_002e: callvirt     instance void AudioManager::DestroyAllAudioSources()

    // [295 5 - 295 33]
    IL_0033: ldarg.0      // name
    IL_0034: call         void [UnityEngine.CoreModule]UnityEngine.SceneManagement.SceneManager::LoadScene(string)
    IL_0039: ret
                 */
                //移除DestroyAllAudioSources的调用
                var codes = new List<CodeInstruction>(instructions);
                
                // 查找 DestroyAllAudioSources 调用的相关指令
                for (int i = 0; i < codes.Count - 1; i++)
                {
                    // 查找 callvirt instance void AudioManager::DestroyAllAudioSources() 指令
                    if (codes[i].opcode == OpCodes.Callvirt && 
                        codes[i].operand.ToString().Contains("DestroyAllAudioSources"))
                    {
                        // 移除前一条指令 (get_Instance() 调用)
                        if (i > 0 && codes[i - 1].opcode == OpCodes.Call)
                        {
                            codes.RemoveAt(i - 1);
                            codes.RemoveAt(i - 1); // 因为删除了一个元素，索引需要调整
                            break;
                        }
                    }
                }
                
                return codes.AsEnumerable();
            }
        }
    }
#endif
}