using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class LocalMacDemo
{
    [MenuItem("Tools/Local Demo/Build macOS ARM64")]
    public static void Build()
    {
        BuildMac(BuildOptions.Development);
    }

    public static void BuildScripts()
    {
        BuildMac(BuildOptions.Development | BuildOptions.BuildScriptsOnly);
    }

    static void BuildMac(BuildOptions buildOptions)
    {
        LocalProtocolChecks.Run();
        EditorUserBuildSettings.SetPlatformSettings("Standalone", "OSXUniversal", "Architecture", "ARM64");
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            target = BuildTarget.StandaloneOSX,
            locationPathName = "Builds/L2Unity-Mac.app",
            options = buildOptions
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[LocalMacDemo] Build result: {report.summary.result}, errors: {report.summary.totalErrors}, output: {report.summary.outputPath}");
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Local macOS ARM64 demo build failed; see the Unity build log.");
    }

    [MenuItem("Tools/Local Demo/Build Windows x64")]
    public static void BuildWindows()
    {
        LocalProtocolChecks.Run();
        SetWindowsArchitecture("x64");
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = "Builds/L2Unity-Win64/L2Unity.exe",
            options = BuildOptions.Development
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[LocalMacDemo] Windows build result: {report.summary.result}, errors: {report.summary.totalErrors}, output: {report.summary.outputPath}");
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Local Windows x64 build failed; see the Unity build log.");
    }

    // On Apple Silicon Unity defaults Windows builds to ARM64; the setting lives in the Windows module, so set it via reflection.
    static void SetWindowsArchitecture(string architecture)
    {
        Type settings = Type.GetType("UnityEditor.WindowsStandalone.UserBuildSettings, UnityEditor.WindowsStandalone.Extensions");
        PropertyInfo property = settings != null ? settings.GetProperty("architecture", BindingFlags.Public | BindingFlags.Static) : null;
        if (property == null)
            throw new InvalidOperationException("Windows Build Support: architecture setting not found.");
        property.SetValue(null, Enum.Parse(property.PropertyType, architecture));
        Debug.Log($"[LocalMacDemo] Windows architecture: {property.GetValue(null)}");
    }
}
