using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Build
{
    // 构建配置
    private static readonly Dictionary<BuildTarget, BuildConfig> BuildConfigs = new Dictionary<BuildTarget, BuildConfig>
    {
        {
            BuildTarget.Android, new BuildConfig
            {
                Extension = ".apk",
                Scenes = GetScenes(),
                PlayerSettings = ConfigureAndroidSettings
            }
        },
        {
            BuildTarget.iOS, new BuildConfig
            {
                Extension = "",
                Scenes = GetScenes(),
                PlayerSettings = ConfigureIOSSettings
            }
        },
        {
            BuildTarget.WebGL, new BuildConfig
            {
                Extension = "",
                Scenes = GetScenes(),
                PlayerSettings = ConfigureWebGLSettings
            }
        },
        {
            BuildTarget.StandaloneWindows64, new BuildConfig
            {
                Extension = ".exe",
                Scenes = GetScenes(),
                PlayerSettings = ConfigureWindowsSettings
            }
        }
    };

    /// <summary>
    /// Jenkins调用的主要构建方法
    /// </summary>
    /// <param name="buildType">构建类型：1=Android, 2=iOS, 3=WebGL, 4=Windows</param>
    /// <param name="isDevelopBuild">是否为开发构建</param>
    public static void PerformBuild(int buildType, bool isDevelopBuild)
    {
        try
        {
            LogBuildStart(buildType, isDevelopBuild);
            
            BuildTarget buildTarget = GetBuildTarget(buildType);
            BuildOptions buildOptions = GetBuildOptions(isDevelopBuild);
            
            // 预构建配置
            PreBuildSetup(buildTarget, isDevelopBuild);
            
            // 执行构建
            string outputPath = GetOutputPath(buildTarget);
            BuildReport report = ExecuteBuild(buildTarget, outputPath, buildOptions);
            
            // 后构建处理
            PostBuildProcess(buildTarget, report, outputPath);
            
            LogBuildResult(report);
        }
        catch (Exception e)
        {
            Debug.LogError($"[BuildScript] 构建失败: {e.Message}");
            Debug.LogError($"[BuildScript] 堆栈跟踪: {e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// 命令行参数构建方法
    /// </summary>
    public static void PerformBuildFromCommandLine()
    {
        var args = Environment.GetCommandLineArgs();
        
        int buildType = GetCommandLineArg(args, "-buildType", 1);
        bool isDevelopBuild = GetCommandLineArg(args, "-isDevelopBuild", false);
        string customOutputPath = GetCommandLineArg(args, "-outputPath", "");
        
        if (!string.IsNullOrEmpty(customOutputPath))
        {
            CustomOutputPath = customOutputPath;
        }
        
        PerformBuild(buildType, isDevelopBuild);
    }

    private static string CustomOutputPath = "";

    private static void LogBuildStart(int buildType, bool isDevelopBuild)
    {
        Debug.Log($"[BuildScript] === Unity构建开始 ===");
        Debug.Log($"[BuildScript] Unity版本: {Application.unityVersion}");
        Debug.Log($"[BuildScript] 构建类型: {buildType}, 开发构建: {isDevelopBuild}");
        Debug.Log($"[BuildScript] 构建时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Debug.Log($"[BuildScript] 项目路径: {Application.dataPath}");
        
        // 使用JekinsBuild辅助类打印环境信息
        JekinsBuild.PrintBuildEnvironmentInfo();
    }

    private static void PreBuildSetup(BuildTarget buildTarget, bool isDevelopBuild)
    {
        Debug.Log($"[BuildScript] 预构建设置开始...");
        
        // 验证构建环境
        if (!JekinsBuild.ValidateBuildEnvironment())
        {
            throw new Exception("构建环境验证失败");
        }
        
        // 检查构建依赖
        if (!JekinsBuild.CheckBuildDependencies())
        {
            throw new Exception("构建依赖检查失败");
        }
        
        // 清理之前的构建缓存
        JekinsBuild.CleanBuildCache();
        
        // 应用平台特定设置
        if (BuildConfigs.ContainsKey(buildTarget))
        {
            BuildConfigs[buildTarget].PlayerSettings?.Invoke(isDevelopBuild);
        }
        
        // 设置构建符号
        SetBuildDefines(buildTarget, isDevelopBuild);
        
        Debug.Log($"[BuildScript] 预构建设置完成");
    }

    private static BuildReport ExecuteBuild(BuildTarget buildTarget, string outputPath, BuildOptions buildOptions)
    {
        Debug.Log($"[BuildScript] 开始执行构建...");
        Debug.Log($"[BuildScript] 输出路径: {outputPath}");
        
        string directory = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Debug.Log($"[BuildScript] 创建输出目录: {directory}");
        }

        string[] scenes = BuildConfigs.ContainsKey(buildTarget) 
            ? BuildConfigs[buildTarget].Scenes 
            : GetScenes();
            
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = buildTarget,
            options = buildOptions
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        return report;
    }

    private static void PostBuildProcess(BuildTarget buildTarget, BuildReport report, string outputPath)
    {
        Debug.Log($"[BuildScript] 后构建处理开始...");
        
        if (report.summary.result == BuildResult.Succeeded)
        {
            // 生成构建信息文件
            GenerateBuildInfo(buildTarget, report, outputPath);
            
            // 计算文件大小
            LogBuildSize(outputPath);
            
            // 平台特定的后处理
            PlatformSpecificPostProcess(buildTarget, outputPath);
            
            // 生成构建报告
            var buildInfo = JekinsBuild.GetCurrentBuildInfo();
            buildInfo.status = JekinsBuild.BuildStatus.Success;
            buildInfo.buildSize = report.summary.totalSize;
            buildInfo.buildDuration = (float)report.summary.totalTime.TotalSeconds;
            JekinsBuild.GenerateBuildReport(buildInfo);
        }
        else
        {
            // 构建失败时的处理
            var buildInfo = JekinsBuild.GetCurrentBuildInfo();
            buildInfo.status = JekinsBuild.BuildStatus.Failed;
            buildInfo.errorMessage = $"构建失败，错误数量: {report.summary.totalErrors}";
            JekinsBuild.GenerateBuildReport(buildInfo);
        }
        
        Debug.Log($"[BuildScript] 后构建处理完成");
    }

    private static void GenerateBuildInfo(BuildTarget buildTarget, BuildReport report, string outputPath)
    {
        var buildInfo = new
        {
            buildTarget = buildTarget.ToString(),
            buildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            unityVersion = Application.unityVersion,
            buildDuration = report.summary.totalTime.TotalSeconds,
            buildSize = report.summary.totalSize,
            outputPath = outputPath,
            gitCommit = JekinsBuild.GetGitCommitHash(),
            gitBranch = JekinsBuild.GetGitBranchName(),
            buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "local"
        };
        
        string buildInfoJson = JsonUtility.ToJson(buildInfo, true);
        string buildInfoPath = Path.Combine(Path.GetDirectoryName(outputPath), "build-info.json");
        File.WriteAllText(buildInfoPath, buildInfoJson);
        
        Debug.Log($"[BuildScript] 构建信息已保存到: {buildInfoPath}");
    }

    private static void LogBuildSize(string outputPath)
    {
        try
        {
            if (File.Exists(outputPath))
            {
                FileInfo fileInfo = new FileInfo(outputPath);
                Debug.Log($"[BuildScript] 构建文件大小: {FormatBytes(fileInfo.Length)}");
            }
            else if (Directory.Exists(outputPath))
            {
                long totalSize = GetDirectorySize(outputPath);
                Debug.Log($"[BuildScript] 构建目录大小: {FormatBytes(totalSize)}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BuildScript] 无法计算构建大小: {e.Message}");
        }
    }

    private static void PlatformSpecificPostProcess(BuildTarget buildTarget, string outputPath)
    {
        switch (buildTarget)
        {
            case BuildTarget.Android:
                PostProcessAndroid(outputPath);
                break;
            case BuildTarget.iOS:
                PostProcessIOS(outputPath);
                break;
            case BuildTarget.WebGL:
                PostProcessWebGL(outputPath);
                break;
            case BuildTarget.StandaloneWindows64:
                PostProcessWindows(outputPath);
                break;
        }
    }

    private static void PostProcessAndroid(string outputPath)
    {
        Debug.Log($"[BuildScript] Android后处理: {outputPath}");
        // 可以添加APK签名、对齐等操作
        
        // 检查APK文件是否存在
        if (File.Exists(outputPath))
        {
            FileInfo apkInfo = new FileInfo(outputPath);
            Debug.Log($"[BuildScript] APK文件大小: {FormatBytes(apkInfo.Length)}");
        }
    }

    private static void PostProcessIOS(string outputPath)
    {
        Debug.Log($"[BuildScript] iOS后处理: {outputPath}");
        // 可以添加Xcode项目配置修改
        
        // 检查Xcode项目是否生成
        string xcodeProjectPath = Path.Combine(outputPath, "Unity-iPhone.xcodeproj");
        if (Directory.Exists(xcodeProjectPath))
        {
            Debug.Log($"[BuildScript] Xcode项目已生成: {xcodeProjectPath}");
        }
    }

    private static void PostProcessWebGL(string outputPath)
    {
        Debug.Log($"[BuildScript] WebGL后处理: {outputPath}");
        // 可以添加压缩、CDN上传等操作
        
        // 检查关键WebGL文件
        string[] webglFiles = { "index.html", "Build", "TemplateData" };
        foreach (string file in webglFiles)
        {
            string filePath = Path.Combine(outputPath, file);
            if (File.Exists(filePath) || Directory.Exists(filePath))
            {
                Debug.Log($"[BuildScript] WebGL文件已生成: {file}");
            }
        }
    }

    private static void PostProcessWindows(string outputPath)
    {
        Debug.Log($"[BuildScript] Windows后处理: {outputPath}");
        // 可以添加安装包制作等操作
        
        // 检查可执行文件
        if (File.Exists(outputPath))
        {
            FileInfo exeInfo = new FileInfo(outputPath);
            Debug.Log($"[BuildScript] 可执行文件大小: {FormatBytes(exeInfo.Length)}");
        }
    }

    private static string GetOutputPath(BuildTarget buildTarget)
    {
        if (!string.IsNullOrEmpty(CustomOutputPath))
        {
            return CustomOutputPath;
        }
        
        string projectRoot = Application.dataPath.Replace("/Assets", "");
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        string buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "local";
        string folder = $"{projectRoot}/Builds/{timestamp}-{buildNumber}/{buildTarget}";
        
        string extension = BuildConfigs.ContainsKey(buildTarget) 
            ? BuildConfigs[buildTarget].Extension 
            : "";
            
        return $"{folder}/MyGame{extension}";
    }

    private static BuildTarget GetBuildTarget(int type)
    {
        switch (type)
        {
            case 1: return BuildTarget.Android;
            case 2: return BuildTarget.iOS;
            case 3: return BuildTarget.WebGL;
            case 4: return BuildTarget.StandaloneWindows64;
            default:
                throw new ArgumentException($"无效的构建类型: {type}");
        }
    }

    private static BuildOptions GetBuildOptions(bool isDevelopBuild)
    {
        BuildOptions options = BuildOptions.None;
        
        if (isDevelopBuild)
        {
            options |= BuildOptions.Development;
            options |= BuildOptions.ConnectWithProfiler;
            options |= BuildOptions.AllowDebugging;
        }
        
        // 从环境变量读取额外选项
        string extraOptions = Environment.GetEnvironmentVariable("UNITY_BUILD_OPTIONS");
        if (!string.IsNullOrEmpty(extraOptions))
        {
            Debug.Log($"[BuildScript] 额外构建选项: {extraOptions}");
            // 可以解析并添加额外选项
        }
        
        return options;
    }

    private static string[] GetScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }

    private static void SetBuildDefines(BuildTarget buildTarget, bool isDevelopBuild)
    {
        List<string> defines = new List<string>();
        
        // 添加平台特定定义
        switch (buildTarget)
        {
            case BuildTarget.Android:
                defines.Add("PLATFORM_ANDROID");
                break;
            case BuildTarget.iOS:
                defines.Add("PLATFORM_IOS");
                break;
            case BuildTarget.WebGL:
                defines.Add("PLATFORM_WEBGL");
                break;
            case BuildTarget.StandaloneWindows64:
                defines.Add("PLATFORM_WINDOWS");
                break;
        }
        
        if (isDevelopBuild)
        {
            defines.Add("DEVELOPMENT_BUILD");
        }
        
        // 添加Jenkins构建标识
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_NUMBER")))
        {
            defines.Add("JENKINS_BUILD");
        }
        
        string defineString = string.Join(";", defines);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup, 
            defineString
        );
        
        Debug.Log($"[BuildScript] 构建定义: {defineString}");
    }

    // 平台特定设置方法
    private static void ConfigureAndroidSettings(bool isDevelopBuild)
    {
        Debug.Log($"[BuildScript] 配置Android设置");
        PlayerSettings.Android.bundleVersionCode = GetVersionCode();
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel21;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        
        if (isDevelopBuild)
        {
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        }
    }

    private static void ConfigureIOSSettings(bool isDevelopBuild)
    {
        Debug.Log($"[BuildScript] 配置iOS设置");
        PlayerSettings.iOS.buildNumber = GetVersionCode().ToString();
        PlayerSettings.iOS.targetOSVersionString = "11.0";
        
        if (isDevelopBuild)
        {
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        }
    }

    private static void ConfigureWebGLSettings(bool isDevelopBuild)
    {
        Debug.Log($"[BuildScript] 配置WebGL设置");
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.memorySize = 512;
        
        if (!isDevelopBuild)
        {
            PlayerSettings.WebGL.debugSymbols = false;
        }
    }

    private static void ConfigureWindowsSettings(bool isDevelopBuild)
    {
        Debug.Log($"[BuildScript] 配置Windows设置");
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.runInBackground = true;
    }

    // 辅助方法
    private static int GetVersionCode()
    {
        string buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");
        if (int.TryParse(buildNumber, out int code))
        {
            return code;
        }
        return 1;
    }

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

    private static long GetDirectorySize(string path)
    {
        DirectoryInfo dir = new DirectoryInfo(path);
        return dir.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static void LogBuildResult(BuildReport report)
    {
        Debug.Log($"[BuildScript] 构建结果: {report.summary.result}");
        Debug.Log($"[BuildScript] 构建耗时: {report.summary.totalTime.TotalSeconds:F2}秒");
        Debug.Log($"[BuildScript] 构建大小: {FormatBytes(report.summary.totalSize)}");
        Debug.Log($"[BuildScript] === Unity构建结束 ===");
        
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[BuildScript] 构建失败，错误数量: {report.summary.totalErrors}");
            EditorApplication.Exit(1);
        }
    }

    // 构建配置类
    private class BuildConfig
    {
        public string Extension { get; set; }
        public string[] Scenes { get; set; }
        public Action<bool> PlayerSettings { get; set; }
    }
}
