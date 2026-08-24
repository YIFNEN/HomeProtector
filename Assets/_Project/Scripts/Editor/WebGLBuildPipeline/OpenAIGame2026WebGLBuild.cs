using System;
using System.IO;
using System.Linq;
using HomeProtector.Editor.AssetPipeline;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HomeProtector.Editor.Build
{
    public static class OpenAIGame2026WebGLBuild
    {
        private const string LoadingScenePath = "Assets/Scenes/StartGame_Loading.unity";
        private const string PlayableScenePath = "Assets/Scenes/isometric scene.unity";
        private const string OutputFolderName = "OpenAIGame2026-WebGL";
        private const string OutputRelativePath = "Builds/" + OutputFolderName;

        [MenuItem("Home Protector/OpenAI Game Builders/Build WebGL")]
        public static void Build()
        {
            HomeProtectorAutomation.ValidateProject();

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                throw new BuildFailedException("Unity WebGL build support is not installed.");
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            ValidateScenes(scenes);

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputDirectory = GetSafeOutputDirectory(projectRoot);
            ConfigurePlayerSettings();
            RecreateOutputDirectory(outputDirectory);

            BuildPlayerOptions options = new()
            {
                scenes = scenes,
                locationPathName = outputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.StrictMode,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"WebGL build failed with {report.summary.totalErrors} error(s). " +
                    $"See the Unity build log for details.");
            }

            string indexPath = Path.Combine(outputDirectory, "index.html");
            if (!File.Exists(indexPath))
            {
                throw new BuildFailedException($"WebGL build succeeded without '{indexPath}'.");
            }

            Debug.Log(
                $"HOME_PROTECTOR_WEBGL_BUILD_OK output={outputDirectory} " +
                $"scenes={scenes.Length} bytes={report.summary.totalSize}");
        }

        private static void ValidateScenes(string[] scenes)
        {
            if (scenes.Length < 2 || scenes[0] != LoadingScenePath)
            {
                throw new BuildFailedException(
                    $"WebGL build requires '{LoadingScenePath}' as the first enabled scene.");
            }

            if (!scenes.Contains(PlayableScenePath, StringComparer.Ordinal))
            {
                throw new BuildFailedException(
                    $"WebGL build requires enabled playable scene '{PlayableScenePath}'.");
            }
        }

        private static string GetSafeOutputDirectory(string projectRoot)
        {
            string buildsRoot = Path.GetFullPath(Path.Combine(projectRoot, "Builds"));
            string outputDirectory = Path.GetFullPath(Path.Combine(projectRoot, OutputRelativePath));
            string requiredPrefix = buildsRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!outputDirectory.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    Path.GetFileName(outputDirectory),
                    OutputFolderName,
                    StringComparison.Ordinal))
            {
                throw new BuildFailedException($"Unsafe WebGL output path '{outputDirectory}'.");
            }

            return outputDirectory;
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            AssetDatabase.SaveAssets();
        }

        private static void RecreateOutputDirectory(string outputDirectory)
        {
            string buildsRoot = Directory.GetParent(outputDirectory)?.FullName
                ?? throw new BuildFailedException(
                    $"WebGL output has no parent directory: '{outputDirectory}'.");
            ThrowIfReparsePoint(buildsRoot);

            if (Directory.Exists(outputDirectory))
            {
                ThrowIfTreeContainsReparsePoint(outputDirectory);
                Directory.Delete(outputDirectory, true);
            }

            Directory.CreateDirectory(outputDirectory);
        }

        private static void ThrowIfTreeContainsReparsePoint(string directory)
        {
            ThrowIfReparsePoint(directory);

            foreach (string entry in Directory.EnumerateFileSystemEntries(
                         directory,
                         "*",
                         SearchOption.TopDirectoryOnly))
            {
                ThrowIfReparsePoint(entry);

                if (Directory.Exists(entry))
                {
                    ThrowIfTreeContainsReparsePoint(entry);
                }
            }
        }

        private static void ThrowIfReparsePoint(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return;
            }

            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new BuildFailedException(
                    $"Refusing to clean WebGL output through reparse point '{path}'.");
            }
        }
    }
}
