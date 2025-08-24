using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Build
{
    /// <summary>
    /// Jenkins调用的构建方法 - 从命令行参数读取配置
    /// </summary>
    public static void BuildGame()
    {
        // 从命令行参数中读取配置
        string[] args = System.Environment.GetCommandLineArgs();
        string platform = "Android"; // 默认平台
        bool isDev = false; // 默认非开发版本
        
        // 解析命令行参数
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-platform" && i + 1 < args.Length)
            {
                platform = args[i + 1];
            }
            else if (args[i] == "-isDev" && i + 1 < args.Length)
            {
                bool.TryParse(args[i + 1], out isDev);
            }
        }
        
        Debug.Log($"[BuildScript] 开始构建 - 平台: {platform}, 开发版本: {isDev}");
        
        BuildTarget buildTarget = GetBuildTarget(platform);
        PerformBuild(buildTarget, isDev);
    }
    
    /// <summary>
    /// 直接调用的构建方法（保留兼容性）
    /// </summary>
    /// <param name="platform">构建平台 (android, ios, webgl, windows)</param>
    /// <param name="isDev">是否为开发版本</param>
    public static void BuildGameDirect(string platform, bool isDev = false)
    {
        Debug.Log($"[BuildScript] 直接调用构建 - 平台: {platform}, 开发版本: {isDev}");
        
        BuildTarget buildTarget = GetBuildTarget(platform);
        PerformBuild(buildTarget, isDev);
    }
    
    /// <summary>
    /// 获取构建目标平台
    /// </summary>
    private static BuildTarget GetBuildTarget(string platform)
    {
        switch (platform.ToLower())
        {
            case "android":
                return BuildTarget.Android;
            case "ios":
                return BuildTarget.iOS;
            case "webgl":
                return BuildTarget.WebGL;
            case "windows":
                return BuildTarget.StandaloneWindows64;
            default:
                Debug.LogWarning($"[BuildScript] 未知平台: {platform}, 默认使用Android");
                return BuildTarget.Android;
        }
    }

    private static void PerformBuild(BuildTarget buildTarget, bool isDev)
    {
        try
        {
            Debug.Log($"[BuildScript] 开始构建 {buildTarget}, 开发版本: {isDev}");
            
            // 获取输出路径
            string outputPath = GetOutputPath(buildTarget);
            Debug.Log($"[BuildScript] 输出路径: {outputPath}");
            
            // 创建输出目录
            string directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 获取场景列表
            string[] scenes = GetBuildScenes();
            Debug.Log($"[BuildScript] 找到 {scenes.Length} 个场景");
            
            // 设置构建选项
            BuildOptions buildOptions = BuildOptions.None;
            if (isDev)
            {
                buildOptions |= BuildOptions.Development;
                buildOptions |= BuildOptions.AllowDebugging;
                Debug.Log($"[BuildScript] 启用开发模式构建选项");
            }
            
            // 执行构建
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = buildTarget,
                options = buildOptions
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] 构建成功！");
            }
            else
            {
                Debug.LogError($"[BuildScript] 构建失败");
                EditorApplication.Exit(1);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[BuildScript] 构建异常: {e.Message}");
            EditorApplication.Exit(1);
        }
    }

    private static string[] GetBuildScenes()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
            
        if (scenes.Length == 0)
        {
            scenes = AssetDatabase.FindAssets("t:Scene")
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
        }
        
        return scenes;
    }

    private static string GetOutputPath(BuildTarget buildTarget)
    {
        string projectRoot = Application.dataPath.Replace("/Assets", "");
        string platformName = buildTarget.ToString().ToLower();
        string folder = $"{projectRoot}/builds/{platformName}";
        
        // 生成时间戳 (年月日时分秒)
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        
        switch (buildTarget)
        {
            case BuildTarget.Android:
                return $"{folder}/MyGame_{timestamp}.apk";
            case BuildTarget.iOS:
                return $"{folder}/iOS-Build_{timestamp}";
            case BuildTarget.WebGL:
                return $"{folder}/WebGL-Build_{timestamp}";
            case BuildTarget.StandaloneWindows64:
                return $"{folder}/MyGame_{timestamp}.exe";
            default:
                return $"{folder}/MyGame_{timestamp}";
        }
    }
}
