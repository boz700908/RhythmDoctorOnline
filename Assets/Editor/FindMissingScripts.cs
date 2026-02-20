#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class FindMissingScripts
{
    [MenuItem("Tools/Find Missing Scripts")]
    static void Find()
    {
        int count = 0;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null)
                {
                    Debug.LogWarning($"Missing Script: {GetPath(go)}", go);
                    count++;
                }
            }
        }
        Debug.Log($"扫描完成，发现 {count} 个 Missing Script");
    }

    static string GetPath(GameObject go)
    {
        return go.transform.parent == null ? go.name : GetPath(go.transform.parent.gameObject) + "/" + go.name;
    }
}
#endif