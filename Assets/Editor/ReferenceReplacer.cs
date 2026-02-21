using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class GUIDReferenceReplacer : EditorWindow
{
    private UnityEngine.Object sourceObject;
    private UnityEngine.Object targetObject;
    private UnityEngine.Object[] targetFiles = new UnityEngine.Object[10];
    private bool useFileScope = false;
    private Vector2 scrollPosition;

    [MenuItem("Tools/批量替换对象引用 (GUID)")]
    public static void ShowWindow()
    {
        GetWindow<GUIDReferenceReplacer>("批量替换引用");
    }

    private void OnGUI()
    {
        GUILayout.Label("选择源对象和目标对象", EditorStyles.boldLabel);
        sourceObject = EditorGUILayout.ObjectField("源对象", sourceObject, typeof(UnityEngine.Object), false);
        targetObject = EditorGUILayout.ObjectField("目标对象", targetObject, typeof(UnityEngine.Object), false);
        
        EditorGUILayout.Space();
        GUILayout.Label("替换范围设置", EditorStyles.boldLabel);
        useFileScope = EditorGUILayout.Toggle("限制替换范围", useFileScope);
        
        if (useFileScope)
        {
            EditorGUI.indentLevel++;
            
            // 文件数量控制
            int fileCount = EditorGUILayout.IntSlider("文件数量", targetFiles.Length, 1, 20);
            if (fileCount != targetFiles.Length)
            {
                var newArray = new UnityEngine.Object[fileCount];
                System.Array.Copy(targetFiles, newArray, Mathf.Min(targetFiles.Length, fileCount));
                targetFiles = newArray;
            }
            
            EditorGUILayout.LabelField("请选择要替换的具体文件:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            
            for (int i = 0; i < targetFiles.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                targetFiles[i] = EditorGUILayout.ObjectField($"文件 {i + 1}", targetFiles[i], typeof(UnityEngine.Object), false);
                
                // 验证文件有效性
                if (targetFiles[i] != null)
                {
                    string filePath = AssetDatabase.GetAssetPath(targetFiles[i]);
                    if (Directory.Exists(filePath))
                    {
                        EditorGUILayout.HelpBox("这是文件夹，不是文件", MessageType.Warning);
                    }
                    else if (filePath.EndsWith(".cs") || filePath.EndsWith(".dll"))
                    {
                        EditorGUILayout.HelpBox("脚本文件无法处理", MessageType.Warning);
                    }
                    else if (!filePath.EndsWith(".unity") && !filePath.EndsWith(".prefab") && !IsSupportedAsset(filePath))
                    {
                        EditorGUILayout.HelpBox("不支持的文件类型", MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(Path.GetFileName(filePath), GUILayout.Width(150));
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            // 显示有效文件统计
            int validFiles = GetValidFileCount();
            EditorGUILayout.LabelField($"有效文件: {validFiles}/{targetFiles.Length}", EditorStyles.helpBox);
            
            EditorGUI.indentLevel--;
        }

        GUI.enabled = sourceObject != null && targetObject != null && (!useFileScope || (useFileScope && GetValidFileCount() > 0));
        if (GUILayout.Button("开始替换引用", GUILayout.Height(30)))
        {
            ReplaceAllReferences();
        }
        GUI.enabled = true;
    }

    private void ReplaceAllReferences()
    {
        string scopeInfo = useFileScope ? $"在指定的 {GetValidFileCount()} 个文件中" : "在整个项目中";
        if (!EditorUtility.DisplayDialog("确认替换",
            $"将{scopeInfo}所有引用「{sourceObject.name}」的地方替换为「{targetObject.name}」？\n该操作不可撤销，请确保已备份项目。", "继续", "取消"))
            return;

        float startTime = (float)EditorApplication.timeSinceStartup;

        string[] allAssets;
        if (useFileScope)
        {
            allAssets = GetSelectedFilePaths();
        }
        else
        {
            allAssets = AssetDatabase.GetAllAssetPaths();
        }
        int total = allAssets.Length;
        int processed = 0;
        int modifiedCount = 0;

        Object src = sourceObject;
        Object dst = targetObject;

        try
        {
            foreach (string path in allAssets)
            {
                // ---------- 新增：跳过所有 Packages 目录下的文件 ----------
                if (path.StartsWith("Packages/"))
                    continue;
                // ---------------------------------------------------------

                // 跳过文件夹、脚本等不需要处理的类型
                if (Directory.Exists(path) || path.EndsWith(".cs") || path.EndsWith(".dll"))
                    continue;

                processed++;
                if (EditorUtility.DisplayCancelableProgressBar("替换引用中",
                    $"正在处理: {path}\n已修改文件数: {modifiedCount}", (float)processed / total))
                {
                    EditorUtility.ClearProgressBar();
                    Debug.LogWarning("用户取消操作");
                    return;
                }

                if (path.EndsWith(".unity"))
                {
                    if (ProcessScene(path, src, dst))
                        modifiedCount++;
                }
                else if (path.EndsWith(".prefab"))
                {
                    if (ProcessPrefab(path, src, dst))
                        modifiedCount++;
                }
                else
                {
                    if (ProcessAsset(path, src, dst))
                        modifiedCount++;
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            float elapsed = (float)EditorApplication.timeSinceStartup - startTime;
            Debug.Log($"替换完成！共修改 {modifiedCount} 个文件，耗时 {elapsed:F2} 秒");
            EditorUtility.DisplayDialog("完成", $"引用替换完成！\n共修改 {modifiedCount} 个文件。", "确定");
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"替换过程中出错: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private bool ProcessAsset(string path, Object src, Object dst)
    {
        Object asset = AssetDatabase.LoadMainAssetAtPath(path);
        if (asset == null) return false;

        SerializedObject serializedObject = new SerializedObject(asset);
        bool modified = TraverseSerializedObject(serializedObject, src, dst);

        if (modified)
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        return modified;
    }

    private bool ProcessPrefab(string path, Object src, Object dst)
    {
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefabRoot == null) return false;

        GameObject instance = PrefabUtility.LoadPrefabContents(path);
        bool modified = false;

        try
        {
            foreach (var comp in instance.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;

                SerializedObject so = new SerializedObject(comp);
                if (TraverseSerializedObject(so, src, dst))
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    modified = true;
                }
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(instance);
        }

        return modified;
    }

    private bool ProcessScene(string path, Object src, Object dst)
    {
        bool modified = false;
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        if (!scene.IsValid()) return false;

        try
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                foreach (var comp in root.GetComponentsInChildren<Component>(true))
                {
                    if (comp == null) continue;

                    SerializedObject so = new SerializedObject(comp);
                    if (TraverseSerializedObject(so, src, dst))
                    {
                        so.ApplyModifiedPropertiesWithoutUndo();
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        return modified;
    }

    private bool TraverseSerializedObject(SerializedObject so, Object src, Object dst)
    {
        bool modified = false;
        SerializedProperty prop = so.GetIterator();

        while (prop.NextVisible(true))
        {
            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (prop.objectReferenceValue == src)
                {
                    prop.objectReferenceValue = dst;
                    modified = true;
                }
            }
        }

        return modified;
    }

    private string[] GetSelectedFilePaths()
    {
        var filePaths = new System.Collections.Generic.List<string>();
        
        foreach (var fileObj in targetFiles)
        {
            if (fileObj != null)
            {
                string filePath = AssetDatabase.GetAssetPath(fileObj);
                
                // 验证文件有效性
                if (!Directory.Exists(filePath) && 
                    !filePath.EndsWith(".cs") && 
                    !filePath.EndsWith(".dll") &&
                    (filePath.EndsWith(".unity") || filePath.EndsWith(".prefab") || IsSupportedAsset(filePath)))
                {
                    filePaths.Add(filePath);
                }
            }
        }
        
        return filePaths.ToArray();
    }

    private int GetValidFileCount()
    {
        int count = 0;
        foreach (var fileObj in targetFiles)
        {
            if (fileObj != null)
            {
                string filePath = AssetDatabase.GetAssetPath(fileObj);
                if (!Directory.Exists(filePath) && 
                    !filePath.EndsWith(".cs") && 
                    !filePath.EndsWith(".dll") &&
                    (filePath.EndsWith(".unity") || filePath.EndsWith(".prefab") || IsSupportedAsset(filePath)))
                {
                    count++;
                }
            }
        }
        return count;
    }

    private bool IsSupportedAsset(string path)
    {
        // 支持常见的Unity资源文件类型
        string[] supportedExtensions = { ".mat", ".anim", ".controller", ".asset", ".physicMaterial", ".renderTexture" };
        
        foreach (string ext in supportedExtensions)
        {
            if (path.EndsWith(ext))
                return true;
        }
        
        return false;
    }
}