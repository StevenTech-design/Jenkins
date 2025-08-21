// 简单的语法检查脚本
using System;
using UnityEngine;

public class TestBuildSyntax
{
    public static void TestMethod()
    {
        // 测试我们添加的方法是否能正确编译
        var args = Environment.GetCommandLineArgs();
        string buildType = "Debug";
        bool isDevelopBuild = buildType.ToLower() == "debug" || buildType.ToLower() == "development";
        
        Debug.Log($"测试构建类型: {buildType}, 开发构建: {isDevelopBuild}");
        
        // 测试平台数组
        int[] platforms = { 1, 2, 3, 4 };
        string[] platformNames = { "Android", "iOS", "WebGL", "Windows" };
        
        for (int i = 0; i < platforms.Length; i++)
        {
            Debug.Log($"平台 {i}: {platformNames[i]}");
        }
        
        Debug.Log("语法检查通过");
    }
}