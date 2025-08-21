using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Build
{
    /// <summary>
    /// Jenkins调用的Android构建方法
    /// </summary>
    public static void BuildAndroid()
    {
        var args = Environment.GetCommandLineArgs();
        string buildType = GetCommandLineArg(args, "-buildType", "Release");
        bool isDevelopBuild = buildType.ToLower() == "debug" || buildType.ToLower() == "development";
        
        Debug.Log($"[BuildScript] Jenkins Android构建开始，构建类型: {buildType}");
        PerformBuild(BuildTarget.Android, isDevelopBuild);
    }

    /// <summary>
    /// Jenkins调用的iOS构建方法
    /// </summary>
    public static void BuildiOS()
    {
        var args = Environment.GetCommandLineArgs();
        string buildType = GetCommandLineArg(args, "-buildType", "Release");
        bool isDevelopBuild = buildType.ToLower() == "debug" || buildType.ToLower() == "development";
        
        Debug.Log($"[BuildScript] Jenkins iOS构建开始，构建类型: {buildType}");
        PerformBuild(BuildTarget.iOS, isDevelopBuild);
    }

    /// <summary>
    /// Jenkins调用的WebGL构建方法
    /// </summary>
    public static void BuildWebGL()
    {
        var args = Environment.GetCommandLineArgs();
        string buildType = GetCommandLineArg(args, "-buildType", "Release");
        bool isDevelopBuild = buildType.ToLower() == "debug" || buildType.ToLower() == "development";
        
        Debug.Log($"[BuildScript] Jenkins WebGL构建开始，构建类型: {buildType}");
        PerformBuild(BuildTarget.WebGL, isDevelopBuild);
    }

    /// <summary>
    /// Jenkins调用的Windows构建方法
    /// </summary>
    public static void BuildWindows()
    {
        var args = Environment.GetCommandLineArgs();
        string buildType = GetCommandLineArg(args, "-buildType", "Release");
        bool isDevelopBuild = buildType.ToLower() == "debug" || buildType.ToLower() == "development";
        
        Debug.Log($"[BuildScript] Jenkins Windows构建开始，构建类型: {buildType}");
        PerformBuild(BuildTarget.StandaloneWindows64, isDevelopBuild);
    }

    /// <summary>
    /// Jenkins调用的全平台构建方法
    /// </summary>
    public static void BuildAll()
    {
        var args = Environment.GetCommandLineArgs();
        string buildType = GetCommandLineArg(args, "-buildType", "Release");
        bool isDevelopBuild = buildType.ToLower() == "debug" || buildType.ToLower() == "development";
        
        Debug.Log($"[BuildScript] Jenkins 全平台构建开始，构建类型: {buildType}");
        
        // 依次构建所有平台
        BuildTarget[] platforms = { BuildTarget.Android, BuildTarget.iOS, BuildTarget.WebGL, BuildTarget.StandaloneWindows64 };
        string[] platformNames = { "Android", "iOS", "WebGL", "Windows" };
        
        for (int i = 0; i < platforms.Length; i++)
        {
            try
            {
                Debug.Log($"[BuildScript] 开始构建 {platformNames[i]} 平台...");
                PerformBuild(platforms[i], isDevelopBuild);
                Debug.Log($"[BuildScript] {platformNames[i]} 平台构建完成");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildScript] {platformNames[i]} 平台构建失败: {e.Message}");
                // 继续构建其他平台，不中断整个流程
            }
        }
        
        Debug.Log($"[BuildScript] 全平台构建流程结束");
    }

    /// <summary>
    /// 核心构建方法
    /// </summary>
    private static void PerformBuild(BuildTarget buildTarget, bool isDevelopBuild)
    {
        try
        {
            Debug.Log($"[BuildScript] === Unity构建开始 ===");
            Debug.Log($"[BuildScript] Unity版本: {Application.unityVersion}");
            Debug.Log($"[BuildScript] 构建平台: {buildTarget}, 开发构建: {isDevelopBuild}");
            Debug.Log($"[BuildScript] 构建时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            // 获取输出路径
            string outputPath = GetOutputPath(buildTarget);
            Debug.Log($"[BuildScript] 输出路径: {outputPath}");
            
            // 创建输出目录
            string directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log($"[BuildScript] 创建输出目录: {directory}");
            }

            // 获取场景列表
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            
            // 设置构建选项
            BuildOptions buildOptions = BuildOptions.None;
            if (isDevelopBuild)
            {
                buildOptions |= BuildOptions.Development;
                buildOptions |= BuildOptions.AllowDebugging;
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
            
            // 输出构建结果
            Debug.Log($"[BuildScript] 构建结果: {report.summary.result}");
            Debug.Log($"[BuildScript] 构建耗时: {report.summary.totalTime.TotalSeconds:F2}秒");
            Debug.Log($"[BuildScript] === Unity构建结束 ===");
            
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildScript] 构建失败，错误数量: {report.summary.totalErrors}");
                EditorApplication.Exit(1);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[BuildScript] 构建失败: {e.Message}");
            Debug.LogError($"[BuildScript] 堆栈跟踪: {e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// 获取输出路径
    /// </summary>
    private static string GetOutputPath(BuildTarget buildTarget)
    {
        string projectRoot = Application.dataPath.Replace("/Assets", "");
        string platformName = buildTarget.ToString().ToLower();
        
        // 与Jenkins shell脚本保持一致的路径结构
        string folder = $"{projectRoot}/builds/{platformName}";
        
        // 根据平台类型返回不同的输出路径
        switch (buildTarget)
        {
            case BuildTarget.Android:
                return $"{folder}/MyGame.apk";
            case BuildTarget.iOS:
                return $"{folder}/iOS-Build"; // iOS输出为目录
            case BuildTarget.WebGL:
                return $"{folder}/WebGL-Build"; // WebGL输出为目录
            case BuildTarget.StandaloneWindows64:
                return $"{folder}/MyGame.exe";
            default:
                return $"{folder}/MyGame";
        }
    }

    /// <summary>
    /// 获取命令行参数
    /// </summary>
    private static T GetCommandLineArg<T>(string[] args, string name, T defaultValue)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                try
                {
                    return (T)Convert.ChangeType(args[i + 1], typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
        }
        return defaultValue;
    }
}
