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
}
