using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RealmCommander.Editor
{
    public static class PortfolioBuildUtility
    {
        private const string BuildPath = "Builds/Windows/RealmCommander.exe";

        [MenuItem("Tools/Realm Commander/Build Windows Portfolio Player")]
        public static void BuildWindowsPortfolioPlayer()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));
            string[] scenes =
            {
                "Assets/Scenes/MainMenuScene.unity",
                "Assets/Scenes/LobbyScene.unity",
                "Assets/Scenes/MainScene.unity"
            };

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Windows build failed: {report.summary.result}");

            Debug.Log($"[PortfolioBuild] PASS path={Path.GetFullPath(BuildPath)} size={report.summary.totalSize}");
        }
    }
}
