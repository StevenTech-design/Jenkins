using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Build
{
  private static string[] _scenes = {
    "Assets/Scenes/SampleScene.unity"
  };
  public static void PerformBuild(int buildType, bool isDevelopBuild) {
    Debug.Log($"[BuildScript] === Build Start ===");
    Debug.Log($"[BuildScript] buildType: {buildType}, isDevelopBuild: {isDevelopBuild}");
    BuildTarget buildTarget = GetBuildTarget(buildType);
    BuildOptions buildOptions = isDevelopBuild ? BuildOptions.Development : BuildOptions.None;
    string outputPath = GetOutputPath(buildTarget);
    Debug.Log($"[BuildScript] OutputPath: {outputPath}");
    string directory = Path.GetDirectoryName(outputPath);
    if (!Directory.Exists(directory))
    {
      Directory.CreateDirectory(directory);
    }
    BuildReport report = BuildPipeline.BuildPlayer(_scenes, outputPath, buildTarget, buildOptions);

    Debug.Log($"[BuildScript] Build result: {report.summary.result}");
    Debug.Log($"[BuildScript] === Build End ===");
  }
  private static string GetOutputPath(BuildTarget buildTarget) {
    string projectRoot = Application.dataPath.Replace("/Assets", "");
    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
    string folder = $"{projectRoot}/Builds/{timestamp}/{buildTarget}";
    string extension = "";
    switch (buildTarget)
    {
      case BuildTarget.StandaloneWindows64:
        extension = ".exe";
        break;
      case BuildTarget.Android:
        extension = ".apk";
        break;
      case BuildTarget.iOS:
        extension = ""; // Xcode 工程
        break;
      case BuildTarget.WebGL:
        extension = ""; // 文件夹
        break;
    }
    return $"{folder}/MyGame{extension}";
  }

  private static BuildTarget GetBuildTarget(int type) {
    switch (type)
    {
      case 1:
        return BuildTarget.Android;
      case 2:
        return BuildTarget.iOS;
      case 3:
        return BuildTarget.WebGL;
      case 4:
        return BuildTarget.StandaloneWindows64;
      default:
        throw new ArgumentException("Invalid build type");
    }
  }
}
