using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// Builds the game as a Windows program that runs without Unity. Menu: Shrunk > Build Windows Game.
    /// From a terminal, with Unity closed: build.ps1. The result is Builds\Windows\Shrunk.exe.
    /// </summary>
    public static class BuildGame
    {
        public const string Folder = "Builds/Windows";

        [MenuItem("Shrunk/Build Windows Game", priority = 40)]
        public static void BuildWindows()
        {
            RunSceneBuilder.UpdateBuildScenes();
            var scenes = new System.Collections.Generic.List<string>();
            foreach (var scene in EditorBuildSettings.scenes) if (scene.enabled) scenes.Add(scene.path);
            if (scenes.Count == 0)
            {
                Debug.LogError("[Shrunk] No scenes to build. Run Shrunk > Rebuild Everything first.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            Directory.CreateDirectory(Folder);
            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = Folder + "/Shrunk.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Shrunk] Built {summary.outputPath} ({summary.totalSize / (1024 * 1024)} MB) in {summary.totalTime.TotalSeconds:F0} s.");
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(summary.outputPath);
            }
            else
            {
                Debug.LogError($"[Shrunk] Build failed: {summary.result}, {summary.totalErrors} errors.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
