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
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, Folder + "/Shrunk.exe");

        public const string AndroidFolder = "Builds/Android";

        /// <summary>
        /// Builds an .apk to copy onto an Android phone and install by tapping it. Menu: Shrunk > Build Android Game.
        /// From a terminal, with Unity closed: build-android.ps1. The result is Builds\Android\Shrunk.apk.
        /// </summary>
        [MenuItem("Shrunk/Build Android Game", priority = 41)]
        public static void BuildAndroid()
        {
            // Who the game is, as far as the phone is concerned, and what phones it will run on.
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.shrunkgame.shrunk");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            // Held sideways, either way round.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion)) PlayerSettings.bundleVersion = "0.1";
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, AndroidFolder + "/Shrunk.apk");
        }

        /// <summary>What players see: the window title on a PC, the name under the icon on a phone.</summary>
        public const string GameTitle = "Symbiosis";

        static void Build(BuildTarget target, string path)
        {
            PlayerSettings.productName = GameTitle;
            // The splash: the game's logo on its own night blue, not the engine's.
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.02f, 0.03f, 0.055f, 1f);
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.overlayOpacity = 0f;
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/logo.png");
            if (logo != null) PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(1.6f, logo) };
            RunSceneBuilder.UpdateBuildScenes();
            var scenes = new System.Collections.Generic.List<string>();
            foreach (var scene in EditorBuildSettings.scenes) if (scene.enabled) scenes.Add(scene.path);
            if (scenes.Count == 0)
            {
                Debug.LogError("[Shrunk] No scenes to build. Run Shrunk > Rebuild Everything first.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            // Build with the target's own settings: a Windows build made while Unity is set to Android gets the phone's
            // quality level, and the PC renderer's effects are stripped out of it (a blank screen).
            if (EditorUserBuildSettings.activeBuildTarget != target)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(target), target);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = path,
                target = target,
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
