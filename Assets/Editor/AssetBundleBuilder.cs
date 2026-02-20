using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

[Serializable]
public class SubTarget
{
    public string name = "新子目标";
    public string[] filePaths = Array.Empty<string>();
    public string[] folderPaths = Array.Empty<string>();
    public bool enabled = true;
}

[Serializable]
public class BuilderConfig
{
    public string outputPath = "AssetBundles";
    public bool clearOutputBeforeBuild = true;
    public bool copyToStreamingAssets = true;
    public List<PlatformBuildConfig> platforms = new List<PlatformBuildConfig>();
    public List<SubTarget> subTargets = new List<SubTarget>();
}

[Serializable]
public class PlatformBuildConfig
{
    public BuildTarget platform;
    public BuildAssetBundleOptions options = BuildAssetBundleOptions.None;
    public bool enabled = true;
    public string customOutputPath = "";
}

public class AssetBundleBuilder : EditorWindow
{
    // 基本设置
    private string outputPath = "AssetBundles";
    private bool clearOutputBeforeBuild = true;
    private bool copyToStreamingAssets = true;
    private Vector2 scrollPosition;
    
    // 平台设置
    private bool showPlatformSettings = true;
    private List<PlatformBuildConfig> platformConfigs = new List<PlatformBuildConfig>();
    private Vector2 platformsScrollPosition;
    
    // 子目标列表
    private List<SubTarget> subTargets = new List<SubTarget>();
    private Vector2 subTargetsScrollPosition;
    private bool showSubTargets = true;
    
    // 配置管理
    private string configFileName = "AssetBundleBuilderConfig.json";
    private string lastConfigPath = "";
    
    // 添加菜单项
    [MenuItem("Tools/AssetBundle 构建器")]
    public static void ShowWindow()
    {
        GetWindow<AssetBundleBuilder>("AssetBundle 构建器");
    }
    
    private void OnEnable()
    {
        InitializeDefaultPlatforms();
        LoadEditorPrefs();
    }
    
    private void OnDisable()
    {
        SaveEditorPrefs();
    }
    
    private void InitializeDefaultPlatforms()
    {
        if (platformConfigs.Count == 0)
        {
            platformConfigs = new List<PlatformBuildConfig>
            {
                new PlatformBuildConfig { platform = BuildTarget.StandaloneWindows, enabled = true },
                new PlatformBuildConfig { platform = BuildTarget.Android, enabled = false },
                new PlatformBuildConfig { platform = BuildTarget.iOS, enabled = false },
                new PlatformBuildConfig { platform = BuildTarget.WebGL, enabled = false },
                new PlatformBuildConfig { platform = BuildTarget.StandaloneOSX, enabled = false }
            };
        }
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("AssetBundle 构建器", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 配置管理按钮
        DisplayConfigManagement();
        
        EditorGUILayout.Space();
        
        // 输出路径
        EditorGUILayout.BeginHorizontal();
        outputPath = EditorGUILayout.TextField("输出路径", outputPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.SaveFolderPanel("选择输出文件夹", outputPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                outputPath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // 清理选项
        clearOutputBeforeBuild = EditorGUILayout.Toggle("构建前清理", clearOutputBeforeBuild);
        copyToStreamingAssets = EditorGUILayout.Toggle("复制到StreamingAssets", copyToStreamingAssets);
        
        EditorGUILayout.Space();
        
        // 平台设置
        DisplayPlatformSettingsSection();
        
        EditorGUILayout.Space();
        
        // 构建按钮
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("构建所有平台", GUILayout.Width(120)))
        {
            BuildAllPlatforms();
        }
        if (GUILayout.Button("构建当前平台", GUILayout.Width(120)))
        {
            BuildCurrentPlatform();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 子目标管理部分
        DisplaySubTargetsSection();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DisplayConfigManagement()
    {
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.LabelField("配置管理", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("导出配置", GUILayout.Width(100)))
        {
            ExportConfiguration();
        }
        
        if (GUILayout.Button("导入配置", GUILayout.Width(100)))
        {
            ImportConfiguration();
        }
        
        if (GUILayout.Button("重置配置", GUILayout.Width(100)))
        {
            if (EditorUtility.DisplayDialog("重置配置", "确定要重置所有配置吗？", "是", "否"))
            {
                ResetConfiguration();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 显示最后加载的配置文件路径
        if (!string.IsNullOrEmpty(lastConfigPath))
        {
            EditorGUILayout.HelpBox($"最后加载的配置: {Path.GetFileName(lastConfigPath)}", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void ExportConfiguration()
    {
        try
        {
            // 创建配置对象
            var config = new BuilderConfig
            {
                outputPath = outputPath,
                clearOutputBeforeBuild = clearOutputBeforeBuild,
                copyToStreamingAssets = copyToStreamingAssets,
                platforms = platformConfigs,
                subTargets = subTargets
            };
            
            // 默认保存到项目根目录
            string defaultPath = Path.Combine(Application.dataPath, "..", configFileName);
            string savePath = EditorUtility.SaveFilePanel("导出配置", 
                Path.GetDirectoryName(defaultPath), 
                Path.GetFileName(defaultPath), 
                "json");
            
            if (string.IsNullOrEmpty(savePath))
                return;
            
            // 序列化配置
            string json = JsonUtility.ToJson(config, true);
            
            // 写入文件
            File.WriteAllText(savePath, json);
            
            // 记录最后保存的路径
            lastConfigPath = savePath;
            
            Debug.Log($"配置已导出到: {savePath}");
            EditorUtility.DisplayDialog("导出成功", $"配置已成功导出到:\n{savePath}", "确定");
        }
        catch (Exception e)
        {
            Debug.LogError($"导出配置失败: {e.Message}");
            EditorUtility.DisplayDialog("导出失败", $"导出配置时发生错误:\n{e.Message}", "确定");
        }
    }
    
    private void ImportConfiguration()
    {
        try
        {
            // 选择配置文件
            string loadPath = EditorUtility.OpenFilePanel("导入配置", 
                Path.GetDirectoryName(lastConfigPath), 
                "json");
            
            if (string.IsNullOrEmpty(loadPath))
                return;
            
            if (!File.Exists(loadPath))
            {
                EditorUtility.DisplayDialog("导入失败", "配置文件不存在", "确定");
                return;
            }
            
            // 询问是否确认导入
            if (!EditorUtility.DisplayDialog("导入配置", 
                "导入配置将覆盖当前所有设置，是否继续？", 
                "导入", "取消"))
            {
                return;
            }
            
            // 读取并解析配置文件
            string json = File.ReadAllText(loadPath);
            var config = JsonUtility.FromJson<BuilderConfig>(json);
            
            if (config == null)
            {
                EditorUtility.DisplayDialog("导入失败", "配置文件格式错误", "确定");
                return;
            }
            
            // 应用配置
            outputPath = config.outputPath;
            clearOutputBeforeBuild = config.clearOutputBeforeBuild;
            copyToStreamingAssets = config.copyToStreamingAssets;
            platformConfigs = config.platforms;
            subTargets = config.subTargets;
            
            // 记录最后加载的路径
            lastConfigPath = loadPath;
            
            // 保存到EditorPrefs
            SaveEditorPrefs();
            
            Debug.Log($"配置已从 {loadPath} 导入");
            EditorUtility.DisplayDialog("导入成功", "配置已成功导入", "确定");
        }
        catch (Exception e)
        {
            Debug.LogError($"导入配置失败: {e.Message}");
            EditorUtility.DisplayDialog("导入失败", $"导入配置时发生错误:\n{e.Message}", "确定");
        }
    }
    
    private void ResetConfiguration()
    {
        // 重置基本设置
        outputPath = "AssetBundles";
        clearOutputBeforeBuild = true;
        copyToStreamingAssets = true;
        
        // 重置平台配置
        platformConfigs.Clear();
        InitializeDefaultPlatforms();
        
        // 清空子目标
        subTargets.Clear();
        
        // 清空最后配置路径
        lastConfigPath = "";
        
        Debug.Log("配置已重置为默认值");
    }
    
    private void DisplayPlatformSettingsSection()
    {
        showPlatformSettings = EditorGUILayout.Foldout(showPlatformSettings, "平台设置", true);
        
        if (showPlatformSettings)
        {
            EditorGUILayout.BeginVertical("box");
            
            platformsScrollPosition = EditorGUILayout.BeginScrollView(platformsScrollPosition, GUILayout.Height(200));
            
            for (int i = 0; i < platformConfigs.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                var config = platformConfigs[i];
                
                EditorGUILayout.BeginHorizontal();
                config.enabled = EditorGUILayout.Toggle(config.enabled, GUILayout.Width(20));
                EditorGUILayout.LabelField(config.platform.ToString(), EditorStyles.boldLabel, GUILayout.Width(150));
                
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    platformConfigs.RemoveAt(i);
                    return;
                }
                EditorGUILayout.EndHorizontal();
                
                config.options = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField("构建选项", config.options);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("自定义输出路径:", GUILayout.Width(120));
                config.customOutputPath = EditorGUILayout.TextField(config.customOutputPath);
                if (GUILayout.Button("清除", GUILayout.Width(60)))
                {
                    config.customOutputPath = "";
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndScrollView();
            
            // 添加新平台按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加平台", GUILayout.Width(100)))
            {
                AddPlatformPopup();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("重置默认平台", GUILayout.Width(120)))
            {
                InitializeDefaultPlatforms();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
    }
    
    private void AddPlatformPopup()
    {
        var menu = new GenericMenu();
        
        // 获取所有BuildTarget枚举值
        var allPlatforms = Enum.GetValues(typeof(BuildTarget)).Cast<BuildTarget>();
        
        foreach (var platform in allPlatforms)
        {
            // 跳过已存在的平台
            if (platformConfigs.Any(c => c.platform == platform))
                continue;
                
            menu.AddItem(new GUIContent(platform.ToString()), false, () =>
            {
                platformConfigs.Add(new PlatformBuildConfig { platform = platform });
            });
        }
        
        menu.ShowAsContext();
    }
    
    private void DisplaySubTargetsSection()
    {
        EditorGUILayout.Space();
        showSubTargets = EditorGUILayout.Foldout(showSubTargets, "子目标管理", true);
        
        if (showSubTargets)
        {
            EditorGUILayout.BeginVertical("box");
            
            // 添加新子目标按钮
            if (GUILayout.Button("添加子目标", GUILayout.Width(100)))
            {
                AddNewSubTarget();
            }
            
            EditorGUILayout.Space();
            
            // 显示子目标列表
            subTargetsScrollPosition = EditorGUILayout.BeginScrollView(subTargetsScrollPosition, GUILayout.Height(200));
            
            if (subTargets.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无子目标，请点击\"添加子目标\"按钮添加", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < subTargets.Count; i++)
                {
                    DisplaySubTargetItem(i);
                }
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
    
    private void DisplaySubTargetItem(int index)
    {
        var subTarget = subTargets[index];
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        subTarget.enabled = EditorGUILayout.Toggle(subTarget.enabled, GUILayout.Width(20));
        subTarget.name = EditorGUILayout.TextField("名称:", subTarget.name);
        EditorGUILayout.EndHorizontal();
        
        // 文件路径
        DisplayPathArray("文件路径:", ref subTarget.filePaths, typeof(Object), false);
        
        EditorGUILayout.Space();
        
        // 文件夹路径
        DisplayPathArray("文件夹路径:", ref subTarget.folderPaths, typeof(DefaultAsset), false);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("上移", GUILayout.Width(60)))
        {
            MoveSubTargetUp(index);
        }
        if (GUILayout.Button("下移", GUILayout.Width(60)))
        {
            MoveSubTargetDown(index);
        }
        if (GUILayout.Button("构建", GUILayout.Width(60)))
        {
            BuildSubTargetForAllPlatforms(index);
        }
        if (GUILayout.Button("删除", GUILayout.Width(60)))
        {
            DeleteSubTarget(index);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
    
    private void DisplayPathArray(string label, ref string[] paths, Type objectType, bool allowSceneObjects)
    {
        EditorGUILayout.LabelField(label);
        
        if (paths == null) paths = new string[0];
        
        for (int i = 0; i < paths.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            Object obj = null;
            if (!string.IsNullOrEmpty(paths[i]))
            {
                obj = AssetDatabase.LoadAssetAtPath<Object>(paths[i]);
            }
            
            Object newObj = EditorGUILayout.ObjectField($"路径 {i+1}:", obj, objectType, false);
            
            if (newObj != obj)
            {
                paths[i] = newObj != null ? AssetDatabase.GetAssetPath(newObj) : "";
            }
            
            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                var tempList = paths.ToList();
                tempList.RemoveAt(i);
                paths = tempList.ToArray();
                return;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        if (GUILayout.Button($"添加{label.TrimEnd(':')}", GUILayout.Width(120)))
        {
            var tempList = paths.ToList();
            tempList.Add("");
            paths = tempList.ToArray();
        }
    }
    
    private void AddNewSubTarget()
    {
        subTargets.Add(new SubTarget());
    }
    
    private void DeleteSubTarget(int index)
    {
        if (index >= 0 && index < subTargets.Count)
        {
            subTargets.RemoveAt(index);
        }
    }
    
    private void MoveSubTargetUp(int index)
    {
        if (index > 0 && index < subTargets.Count)
        {
            var temp = subTargets[index];
            subTargets[index] = subTargets[index - 1];
            subTargets[index - 1] = temp;
        }
    }
    
    private void MoveSubTargetDown(int index)
    {
        if (index >= 0 && index < subTargets.Count - 1)
        {
            var temp = subTargets[index];
            subTargets[index] = subTargets[index + 1];
            subTargets[index + 1] = temp;
        }
    }
    
    private void BuildAllPlatforms()
    {
        if (!ValidateBeforeBuild()) return;
        
        var enabledPlatforms = platformConfigs.Where(c => c.enabled).ToList();
        if (enabledPlatforms.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "请至少启用一个平台", "确定");
            return;
        }
        
        var enabledSubTargets = subTargets.Where(s => s.enabled).ToList();
        if (enabledSubTargets.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "请至少启用一个子目标", "确定");
            return;
        }
        
        if (EditorUtility.DisplayDialog("确认构建", 
            $"确定要构建 {enabledSubTargets.Count} 个子目标到 {enabledPlatforms.Count} 个平台吗？", 
            "构建", "取消"))
        {
            EditorApplication.delayCall += () => 
            {
                BuildForPlatformsAndSubTargets(enabledPlatforms, enabledSubTargets);
            };
        }
    }
    
    private void BuildCurrentPlatform()
    {
        if (!ValidateBeforeBuild()) return;
        
        var enabledSubTargets = subTargets.Where(s => s.enabled).ToList();
        if (enabledSubTargets.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "请至少启用一个子目标", "确定");
            return;
        }
        
        // 构建第一个启用的平台
        var platformConfig = platformConfigs.FirstOrDefault(c => c.enabled);
        if (platformConfig == null)
        {
            EditorUtility.DisplayDialog("警告", "请至少启用一个平台", "确定");
            return;
        }
        
        BuildForPlatformsAndSubTargets(new List<PlatformBuildConfig> { platformConfig }, enabledSubTargets);
    }
    
    private void BuildSubTargetForAllPlatforms(int index)
    {
        if (index < 0 || index >= subTargets.Count)
        {
            Debug.LogError("无效的子目标索引");
            return;
        }
        
        var subTarget = subTargets[index];
        if (!subTarget.enabled)
        {
            Debug.LogWarning($"子目标 '{subTarget.name}' 已禁用，无法构建");
            return;
        }
        
        var enabledPlatforms = platformConfigs.Where(c => c.enabled).ToList();
        if (enabledPlatforms.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "请至少启用一个平台", "确定");
            return;
        }
        
        if (EditorUtility.DisplayDialog("确认构建", 
            $"确定要构建子目标 '{subTarget.name}' 到 {enabledPlatforms.Count} 个平台吗？", 
            "构建", "取消"))
        {
            BuildForPlatformsAndSubTargets(enabledPlatforms, new List<SubTarget> { subTarget });
        }
    }
    
    private void BuildForPlatformsAndSubTargets(List<PlatformBuildConfig> platforms, List<SubTarget> targets)
    {
        try
        {
            int totalTasks = platforms.Count * targets.Count;
            int completedTasks = 0;
            
            foreach (var platformConfig in platforms)
            {
                foreach (var subTarget in targets)
                {
                    BuildSubTarget(subTarget, platformConfig);
                    completedTasks++;
                    
                    // 更新进度条
                    float progress = (float)completedTasks / totalTasks;
                    Debug.Log($"构建进度: {completedTasks}/{totalTasks}");
                    EditorUtility.DisplayProgressBar("AssetBundle构建进度", 
                        $"正在构建: {subTarget.name} ({platformConfig.platform})", 
                        progress);
                }
            }
            
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("构建完成", $"成功构建 {completedTasks} 个AssetBundle", "确定");
            
            // 打开输出文件夹
            EditorUtility.RevealInFinder(outputPath);
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("构建失败", $"构建过程中发生错误: {e.Message}", "确定");
            Debug.LogError($"构建失败: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void BuildSubTarget(SubTarget subTarget, PlatformBuildConfig platformConfig)
    {
        try
        {
            string bundleName = "";
            Debug.Log($">>> 开始构建子目标: {subTarget.name} (平台: {platformConfig.platform})");
            
            // 收集资源
            var assetPaths = CollectAssetPaths(subTarget);
            if (assetPaths.Count == 0)
            {
                Debug.LogWarning($"!!! 子目标 '{subTarget.name}' 没有找到有效的资源文件，跳过构建");
                return;
            }
            
            Debug.Log($"收集到 {assetPaths.Count} 个资源用于构建 '{subTarget.name}'");
            
            // 确定输出路径
            string platformOutputPath = GetPlatformOutputPath(platformConfig);
            Debug.Log($"平台输出路径: {platformOutputPath}");
            
            // 确保目录存在
            if (!Directory.Exists(platformOutputPath))
            {
                Directory.CreateDirectory(platformOutputPath);
                Debug.Log($"创建目录: {platformOutputPath}");
            }
            
            // 清理该子目标的旧bundle文件（如果启用清理选项）
            if (clearOutputBeforeBuild)
            {
                bundleName = subTarget.name.ToLower().Replace(" ", "_");
                string bundlePath = Path.Combine(platformOutputPath, bundleName);
                string manifestPath = bundlePath + ".manifest";
                
                if (File.Exists(bundlePath))
                {
                    File.Delete(bundlePath);
                    Debug.Log($"已删除旧bundle文件: {bundlePath}");
                }
                if (File.Exists(manifestPath))
                {
                    File.Delete(manifestPath);
                    Debug.Log($"已删除旧manifest文件: {manifestPath}");
                }
            }
            
            // 构建AssetBundle
            bundleName = subTarget.name.ToLower().Replace(" ", "_");
            Debug.Log($"构建bundle名称: {bundleName}");
            
            var build = new AssetBundleBuild
            {
                assetBundleName = bundleName,
                assetNames = assetPaths.ToArray()
            };
            
            BuildPipeline.BuildAssetBundles(platformOutputPath, new[] { build }, 
                platformConfig.options, platformConfig.platform);
            
            Debug.Log($"AssetBundle构建完成: {bundleName}");
            
            // 复制到StreamingAssets
            if (copyToStreamingAssets)
            {
                CopyToStreamingAssets(platformConfig.platform, platformOutputPath, bundleName);
            }
            
            Debug.Log($"<<< 成功构建: {subTarget.name} -> {platformConfig.platform} -> {platformOutputPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"!!! 构建子目标 '{subTarget.name}' 失败: {e.Message}\n{e.StackTrace}");
            throw;
        }
    }
    
    private List<string> CollectAssetPaths(SubTarget subTarget)
    {
        List<string> assetPaths = new List<string>();
        
        Debug.Log($"开始收集子目标 '{subTarget.name}' 的资源...");
        
        // 收集文件
        if (subTarget.filePaths != null)
        {
            Debug.Log($"文件路径数量: {subTarget.filePaths.Length}");
            foreach (string filePath in subTarget.filePaths)
            {
                Debug.Log($"处理文件路径: '{filePath}'");
                if (!string.IsNullOrEmpty(filePath))
                {
                    if (File.Exists(filePath))
                    {
                        string assetPath = ConvertToAssetPath(filePath);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            assetPaths.Add(assetPath);
                            Debug.Log($"添加文件: {assetPath}");
                        }
                        else
                        {
                            Debug.LogWarning($"无法转换为有效资产路径: {filePath}");
                        }
                    }
                    else if (Directory.Exists(filePath))
                    {
                        // 如果是目录，收集目录中的所有资源
                        CollectAssetsFromFolder(filePath, assetPaths);
                    }
                    else
                    {
                        Debug.LogWarning($"文件或目录不存在: {filePath}");
                    }
                }
            }
        }
        
        // 收集文件夹中的资源
        if (subTarget.folderPaths != null)
        {
            Debug.Log($"文件夹路径数量: {subTarget.folderPaths.Length}");
            foreach (string folderPath in subTarget.folderPaths)
            {
                Debug.Log($"处理文件夹路径: '{folderPath}'");
                if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                {
                    CollectAssetsFromFolder(folderPath, assetPaths);
                }
                else
                {
                    Debug.LogWarning($"文件夹不存在: {folderPath}");
                }
            }
        }
        
        Debug.Log($"子目标 '{subTarget.name}' 总共收集到 {assetPaths.Count} 个资源");
        return assetPaths.Distinct().ToList(); // 去重
    }
    
    private void CollectAssetsFromFolder(string folderPath, List<string> assetPaths)
    {
        string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        
        foreach (string file in files)
        {
            if (IsAssetFile(file))
            {
                string assetPath = ConvertToAssetPath(file);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    assetPaths.Add(assetPath);
                }
            }
        }
    }
    
    private string ConvertToAssetPath(string fullPath)
    {
        fullPath = fullPath.Replace("\\", "/");
        
        // 如果已经是Assets开头的路径
        if (fullPath.StartsWith("Assets/"))
        {
            return AssetDatabase.AssetPathToGUID(fullPath) != "" ? fullPath : null;
        }
        
        // 转换绝对路径为相对路径
        if (fullPath.StartsWith(Application.dataPath))
        {
            string relativePath = "Assets" + fullPath.Substring(Application.dataPath.Length);
            return AssetDatabase.AssetPathToGUID(relativePath) != "" ? relativePath : null;
        }
        
        return null;
    }
    
    private string GetPlatformOutputPath(PlatformBuildConfig config)
    {
        string basePath = outputPath;
        
        if (!string.IsNullOrEmpty(config.customOutputPath))
        {
            return config.customOutputPath;
        }
        
        // 按平台创建子文件夹
        return Path.Combine(basePath, config.platform.ToString());
    }
    
    private void CopyToStreamingAssets(BuildTarget platform, string sourcePath, string bundleName)
    {
        try
        {
            string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, "AssetBundles", platform.ToString());
            
            if (!Directory.Exists(streamingAssetsPath))
            {
                Directory.CreateDirectory(streamingAssetsPath);
            }
            
            string sourceFile = Path.Combine(sourcePath, bundleName);
            string destFile = Path.Combine(streamingAssetsPath, bundleName);
            
            if (File.Exists(sourceFile))
            {
                File.Copy(sourceFile, destFile, true);
                Debug.Log($"已复制到StreamingAssets: {destFile}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"复制到StreamingAssets失败: {e.Message}");
        }
    }
    
    private bool IsAssetFile(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        string[] excludeExtensions = { ".meta", ".cs", ".js", ".boo", ".dll" };
        return !excludeExtensions.Contains(extension);
    }
    
    private bool ValidateBeforeBuild()
    {
        if (subTargets.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "请先添加子目标", "确定");
            return false;
        }
        
        if (platformConfigs.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "请先添加平台配置", "确定");
            return false;
        }
        
        return true;
    }
    
    private void SaveEditorPrefs()
    {
        try
        {
            // 保存基本设置
            EditorPrefs.SetString("AB_OutputPath", outputPath);
            EditorPrefs.SetBool("AB_ClearBeforeBuild", clearOutputBeforeBuild);
            EditorPrefs.SetBool("AB_CopyToStreamingAssets", copyToStreamingAssets);
            EditorPrefs.SetString("AB_LastConfigPath", lastConfigPath);
            
            // 保存平台配置
            EditorPrefs.SetInt("AB_PlatformsCount", platformConfigs.Count);
            for (int i = 0; i < platformConfigs.Count; i++)
            {
                var config = platformConfigs[i];
                string prefix = $"AB_Platform_{i}_";
                EditorPrefs.SetInt(prefix + "platform", (int)config.platform);
                EditorPrefs.SetInt(prefix + "options", (int)config.options);
                EditorPrefs.SetBool(prefix + "enabled", config.enabled);
                EditorPrefs.SetString(prefix + "customPath", config.customOutputPath);
            }
            
            // 保存子目标
            EditorPrefs.SetInt("AB_SubTargetsCount", subTargets.Count);
            for (int i = 0; i < subTargets.Count; i++)
            {
                var target = subTargets[i];
                string prefix = $"AB_SubTarget_{i}_";
                EditorPrefs.SetString(prefix + "name", target.name);
                EditorPrefs.SetBool(prefix + "enabled", target.enabled);
                
                // 保存文件路径
                if (target.filePaths != null)
                {
                    EditorPrefs.SetInt(prefix + "filesCount", target.filePaths.Length);
                    for (int j = 0; j < target.filePaths.Length; j++)
                    {
                        EditorPrefs.SetString(prefix + $"file_{j}", target.filePaths[j]);
                    }
                }
                
                // 保存文件夹路径
                if (target.folderPaths != null)
                {
                    EditorPrefs.SetInt(prefix + "foldersCount", target.folderPaths.Length);
                    for (int j = 0; j < target.folderPaths.Length; j++)
                    {
                        EditorPrefs.SetString(prefix + $"folder_{j}", target.folderPaths[j]);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"保存配置失败: {e.Message}");
        }
    }
    
    private void LoadEditorPrefs()
    {
        try
        {
            // 加载基本设置
            outputPath = EditorPrefs.GetString("AB_OutputPath", "AssetBundles");
            clearOutputBeforeBuild = EditorPrefs.GetBool("AB_ClearBeforeBuild", true);
            copyToStreamingAssets = EditorPrefs.GetBool("AB_CopyToStreamingAssets", true);
            lastConfigPath = EditorPrefs.GetString("AB_LastConfigPath", "");
            
            // 加载平台配置
            int platformsCount = EditorPrefs.GetInt("AB_PlatformsCount", 0);
            platformConfigs.Clear();
            
            for (int i = 0; i < platformsCount; i++)
            {
                string prefix = $"AB_Platform_{i}_";
                var config = new PlatformBuildConfig
                {
                    platform = (BuildTarget)EditorPrefs.GetInt(prefix + "platform", 0),
                    options = (BuildAssetBundleOptions)EditorPrefs.GetInt(prefix + "options", 0),
                    enabled = EditorPrefs.GetBool(prefix + "enabled", true),
                    customOutputPath = EditorPrefs.GetString(prefix + "customPath", "")
                };
                platformConfigs.Add(config);
            }
            
            if (platformConfigs.Count == 0)
            {
                InitializeDefaultPlatforms();
            }
            
            // 加载子目标
            int subTargetsCount = EditorPrefs.GetInt("AB_SubTargetsCount", 0);
            subTargets.Clear();
            
            for (int i = 0; i < subTargetsCount; i++)
            {
                string prefix = $"AB_SubTarget_{i}_";
                var target = new SubTarget
                {
                    name = EditorPrefs.GetString(prefix + "name", "新子目标"),
                    enabled = EditorPrefs.GetBool(prefix + "enabled", true)
                };
                
                // 加载文件路径
                int filesCount = EditorPrefs.GetInt(prefix + "filesCount", 0);
                target.filePaths = new string[filesCount];
                for (int j = 0; j < filesCount; j++)
                {
                    target.filePaths[j] = EditorPrefs.GetString(prefix + $"file_{j}", "");
                }
                
                // 加载文件夹路径
                int foldersCount = EditorPrefs.GetInt(prefix + "foldersCount", 0);
                target.folderPaths = new string[foldersCount];
                for (int j = 0; j < foldersCount; j++)
                {
                    target.folderPaths[j] = EditorPrefs.GetString(prefix + $"folder_{j}", "");
                }
                
                subTargets.Add(target);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"加载配置失败: {e.Message}");
            InitializeDefaultPlatforms();
        }
    }
}