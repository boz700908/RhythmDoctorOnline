using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#pragma warning disable 0455
namespace ModEntry
{
#if RHYTHMDOCTOR
    public class Patches
    {
        private const string modName = "RDOL";

        /// <summary>
        /// 主菜单入口的显示文字。不做新的本地化资源，跟随游戏当前语言：
        /// 中文用中文，其他语言用英文，避免出现中文夹杂在拉丁字体里。
        /// 显示与朗读取同一段文字，所以读出来的就是看到的。
        /// </summary>
        private static string ModLabel => RDString.isChinese ? "多人游戏" : "Multiplayer";

        [HarmonyPatch(typeof(scnMenu), "Awake")]
        public class Patch_scnMenu_Start
        {
            public static void Prefix(scnMenu __instance)
            {
                scnBase.currentLevelSelect = "scnMenu";
                RectTransform optionsContainer = __instance.optionsContainer;
                if (optionsContainer == null) return;

                try
                {
                    // 已经加过就不再克隆，避免 Awake 被再次调用时重复添加
                    if (optionsContainer.Find(modName) != null) return;

                    Transform source = optionsContainer.Find("customLevels");
                    if (source == null) return;

                    GameObject clone = Object.Instantiate(source.gameObject);
                    clone.transform.SetParent(optionsContainer.transform, false);
                    clone.transform.SetSiblingIndex(source.GetSiblingIndex() + 1);
                    clone.name = modName;
                    clone.SetActive(true);
                }
                catch (Exception ex)
                {
                    Debug.Log("[RDOL] 主菜单入口创建失败: " + ex);
                }
            }

            public static void Postfix(scnMenu __instance)
            {
                RectTransform options = __instance.options;
                if (options == null) return;

                Transform entry = __instance.optionsContainer != null
                    ? __instance.optionsContainer.Find(modName)
                    : null;
                if (entry == null) return;

                // 多出来的这一项要把选项整体上移，否则会顶出可视区
                Rect rect = entry.GetComponent<RectTransform>().rect;
                RectTransform component = options.GetComponent<RectTransform>();
                component.position = new Vector2(component.position.x, component.position.y + rect.height / 1.5f);
            }
        }

        /// <summary>
        /// 主菜单的选项文字由 Localize() 通过 RDString.Get("mainMenu." + name) 取得，
        /// 我们的克隆项名字是 RDOL，所以在这里给出文字。
        /// </summary>
        [HarmonyPatch(typeof(RDString), "GetWithCheck")]
        public static class RDString_GetWithCheck
        {
            private static bool Prefix(string key, out bool exists, ref string __result)
            {
                if (key == "mainMenu." + modName)
                {
                    __result = ModLabel;
                    exists = true;
                    return false;
                }
                exists = false;
                return true;
            }
        }

        /// <summary>
        /// 原版 SelectOption 的 switch 里没有 RDOL 分支，命中时原方法会继续往下走。
        /// 这里改成返回 false，由补丁自己完成跳转。
        /// </summary>
        [HarmonyPatch(typeof(scnMenu), "SelectOption")]
        public static class scnMenu_SelectOption
        {
            private static readonly FieldInfo CurrentOptionField =
                typeof(scnMenu).GetField("currentOption", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly FieldInfo OptionsTextField =
                typeof(scnMenu).GetField("optionsText", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly MethodInfo TransitionMethod =
                typeof(scnMenu).GetMethod("TransitionToScene", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly MethodInfo ConfirmSoundMethod =
                typeof(scnMenu).GetMethod("PlayConfirmSound", BindingFlags.Instance | BindingFlags.NonPublic);

            private static bool Prefix(scnMenu __instance)
            {
                object rawIndex = CurrentOptionField != null ? CurrentOptionField.GetValue(__instance) : null;
                if (!(rawIndex is int)) return true;
                int index = (int)rawIndex;

                Text[] texts = OptionsTextField != null ? OptionsTextField.GetValue(__instance) as Text[] : null;
                if (texts == null || index < 0 || index >= texts.Length) return true;

                Text current = texts[index];
                if (current == null || current.gameObject.name != modName) return true;

                try
                {
                    ConfirmSoundMethod?.Invoke(__instance, null);
                }
                catch (Exception) { }

                __instance.conductor.StopSong();
                TransitionMethod?.Invoke(__instance, new object[] { "scnCheckUpdate" });
                return false;
            }
        }

        
        static class scnBase_GoToScene_Patch
        {
            static MethodInfo targetMethod = typeof(AudioManager).GetMethod("DestroyAllAudioSources");
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
                    if (codes[i].opcode == OpCodes.Callvirt)
                    {
                        // 更安全的方法：检查操作数是否为 MethodInfo 并匹配方法名
                        if (codes[i].operand is MethodInfo methodInfo)
                        {
                            if (methodInfo == targetMethod)
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
                    }
                }
                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(AudioManager), "DestroyAllAudioSources")]
        static class AudioManager_DestroyAllAudioSources_Patch
        {
             static bool Prefix(scnMenu __instance)
             {
                 bool isGoToScene = scnBase.currentLevelSelect == "ScnRoom";
                 Debug.Log(isGoToScene);
                 return !isGoToScene;
             }
        }
    }
#endif
}