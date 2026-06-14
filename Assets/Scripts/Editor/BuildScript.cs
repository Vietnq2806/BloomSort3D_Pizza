using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildScript
{
    [MenuItem("Build/Build Android APK")]
    public static void PerformAndroidBuild()
    {
        Debug.Log("[BuildScript] Starting Android Build...");

        // Ensure build directory exists
        string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/Android");
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }

        string apkPath = Path.Combine(buildDir, "BloomSort3D_Pizza.apk");

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        buildPlayerOptions.locationPathName = apkPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Android Build SUCCEEDED: {summary.totalSize} bytes. Path: {apkPath}");
        }
        else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError($"[BuildScript] Android Build FAILED: {summary.totalErrors} errors.");
        }
    }

    [MenuItem("Build/Build Windows EXE")]
    public static void PerformWindowsBuild()
    {
        Debug.Log("[BuildScript] Starting Windows Build...");

        // Ensure build directory exists
        string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/Windows");
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }

        string exePath = Path.Combine(buildDir, "BloomSort3D_Pizza.exe");

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        buildPlayerOptions.locationPathName = exePath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Windows Build SUCCEEDED: {summary.totalSize} bytes. Path: {exePath}");
        }
        else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError($"[BuildScript] Windows Build FAILED: {summary.totalErrors} errors.");
        }
    }
}

