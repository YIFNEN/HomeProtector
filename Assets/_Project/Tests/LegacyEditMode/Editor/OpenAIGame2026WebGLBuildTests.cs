using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using HomeProtector.Editor.Build;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace HomeProtector.Tests.LegacyEditMode
{
    public sealed class OpenAIGame2026WebGLBuildTests
    {
        private string tempRoot;
        private string junctionPath;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(
                Path.GetTempPath(),
                "HomeProtectorWebGLBuildTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(junctionPath) && Directory.Exists(junctionPath))
            {
                Directory.Delete(junctionPath);
            }

            if (!string.IsNullOrEmpty(tempRoot) && Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }

        [Test]
        public void RecreateOutputDirectoryRejectsJunctionWithoutTouchingTarget()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Assert.Ignore("Junction coverage is Windows-only.");
            }

            string buildsRoot = Path.Combine(tempRoot, "Builds");
            string externalTarget = Path.Combine(tempRoot, "OutsideTarget");
            junctionPath = Path.Combine(buildsRoot, "OpenAIGame2026-WebGL");
            string markerPath = Path.Combine(externalTarget, "marker.txt");

            Directory.CreateDirectory(buildsRoot);
            Directory.CreateDirectory(externalTarget);
            File.WriteAllText(markerPath, "preserve");
            CreateJunction(junctionPath, externalTarget);

            MethodInfo recreate = typeof(OpenAIGame2026WebGLBuild).GetMethod(
                "RecreateOutputDirectory",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(recreate, Is.Not.Null);

            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                () => recreate.Invoke(null, new object[] { junctionPath }));

            Assert.That(exception.InnerException, Is.TypeOf<BuildFailedException>());
            Assert.That(File.Exists(markerPath), Is.True);
        }

        [Test]
        public void ConfigurePlayerSettingsSetsWebCanvasDimensions()
        {
            WithConfiguredPlayerSettings(() =>
            {
                Assert.That(PlayerSettings.defaultWebScreenWidth, Is.EqualTo(1280));
                Assert.That(PlayerSettings.defaultWebScreenHeight, Is.EqualTo(720));
            });
        }

        [Test]
        public void ConfigurePlayerSettingsUsesHashedWebGLFiles()
        {
            WithConfiguredPlayerSettings(() =>
                Assert.That(PlayerSettings.WebGL.nameFilesAsHashes, Is.True));
        }

        private static void WithConfiguredPlayerSettings(Action assertions)
        {
            int standaloneWidth = PlayerSettings.defaultScreenWidth;
            int standaloneHeight = PlayerSettings.defaultScreenHeight;
            int webWidth = PlayerSettings.defaultWebScreenWidth;
            int webHeight = PlayerSettings.defaultWebScreenHeight;
            bool runInBackground = PlayerSettings.runInBackground;
            WebGLCompressionFormat compression = PlayerSettings.WebGL.compressionFormat;
            bool decompressionFallback = PlayerSettings.WebGL.decompressionFallback;
            bool dataCaching = PlayerSettings.WebGL.dataCaching;
            bool nameFilesAsHashes = PlayerSettings.WebGL.nameFilesAsHashes;

            try
            {
                MethodInfo configure = typeof(OpenAIGame2026WebGLBuild).GetMethod(
                    "ConfigurePlayerSettings",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(configure, Is.Not.Null);
                configure.Invoke(null, null);
                assertions();
            }
            finally
            {
                PlayerSettings.defaultScreenWidth = standaloneWidth;
                PlayerSettings.defaultScreenHeight = standaloneHeight;
                PlayerSettings.defaultWebScreenWidth = webWidth;
                PlayerSettings.defaultWebScreenHeight = webHeight;
                PlayerSettings.runInBackground = runInBackground;
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.decompressionFallback = decompressionFallback;
                PlayerSettings.WebGL.dataCaching = dataCaching;
                PlayerSettings.WebGL.nameFilesAsHashes = nameFilesAsHashes;
                AssetDatabase.SaveAssets();
            }
        }

        private static void CreateJunction(string junction, string target)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                Arguments = $"/d /c mklink /J \"{junction}\" \"{target}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            using Process process = Process.Start(startInfo);
            process.WaitForExit();

            Assert.That(
                process.ExitCode,
                Is.EqualTo(0),
                process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd());
        }
    }
}
