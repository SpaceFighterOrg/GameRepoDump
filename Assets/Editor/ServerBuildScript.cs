using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ServerBuildScript
{
    private const string OutputPath = "ServerBuild/SpaceFighter";

    [MenuItem("Build/Build Linux Dedicated Server")]
    public static void BuildLinuxServer()
    {
        string pathPrefix = "Assets/Scenes/";
        var scenes = new[] { $"{pathPrefix}MainScene.unity",$"{pathPrefix}MainScene/ConfigSubscene.unity", $"{pathPrefix}MainScene/ShipSubscene.unity" };

        if (scenes.Length == 0)
        {
            Debug.LogError("[ServerBuild] No scenes enabled in Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = BuildOptions.None,
        };

        Debug.Log($"[ServerBuild] Building {scenes.Length} scene(s) → {OutputPath}");

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[ServerBuild] Success — {summary.totalSize / 1024 / 1024} MB in {summary.totalTime.TotalSeconds:F1}s");
        }
        else
        {
            Debug.LogError($"[ServerBuild] Failed: {summary.result}  errors={summary.totalErrors}");
            EditorApplication.Exit(1);
        }
    }
}
