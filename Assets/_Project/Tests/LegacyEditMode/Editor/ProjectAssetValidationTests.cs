using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HomeProtector.Tests.LegacyEditMode
{
    public sealed class ProjectAssetValidationTests
    {
        private const string LoadingScenePath = "Assets/Scenes/StartGame_Loading.unity";
        private const string PlayableScenePath = "Assets/Scenes/isometric scene.unity";
        private const string PlayableSceneName = "isometric scene";

        private static readonly string[] PrefabSearchRoots =
        {
            "Assets/_Project",
            "Assets/Prefabs"
        };

        [Test]
        public void EnabledBuildSettingsScenesHaveNoMissingScripts()
        {
            List<string> issues = new();
            string originalScenePath = SceneManager.GetActiveScene().path;

            try
            {
                foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes.Where(scene => scene.enabled))
                {
                    Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        CollectMissingScripts(root, buildScene.path, issues);
                    }
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(originalScenePath))
                {
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                }
            }

            Assert.That(issues, Is.Empty, string.Join(Environment.NewLine, issues));
        }

        [Test]
        public void LoadingSceneStartGameTargetsPlayableScene()
        {
            string originalScenePath = SceneManager.GetActiveScene().path;

            try
            {
                EditorSceneManager.OpenScene(LoadingScenePath, OpenSceneMode.Single);

                StartGame startGame = UnityEngine.Object.FindObjectOfType<StartGame>();
                Assert.That(startGame, Is.Not.Null, "StartGame_Loading scene must contain StartGame.");
                Assert.That(startGame.TargetSceneName, Is.EqualTo(PlayableSceneName));

                SerializedObject serializedStartGame = new(startGame);
                SerializedProperty fadeManager = serializedStartGame.FindProperty("fadeManager");
                Assert.That(fadeManager, Is.Not.Null, "StartGame must serialize fadeManager.");
                Assert.That(fadeManager.objectReferenceValue, Is.Not.Null, "StartGame.fadeManager must be assigned.");

                string[] enabledScenePaths = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();
                Assert.That(enabledScenePaths, Does.Contain(LoadingScenePath));
                Assert.That(enabledScenePaths, Does.Contain(PlayableScenePath));
            }
            finally
            {
                if (!string.IsNullOrEmpty(originalScenePath))
                {
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                }
            }
        }

        [Test]
        public void MaintainedPrefabsHaveNoMissingScripts()
        {
            List<string> issues = new();
            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", PrefabSearchRoots)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .OrderBy(path => path)
                .ToArray();

            foreach (string prefabPath in prefabPaths)
            {
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    CollectMissingScripts(prefabRoot, prefabPath, issues);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            Assert.That(issues, Is.Empty, string.Join(Environment.NewLine, issues));
        }

        private static void CollectMissingScripts(GameObject root, string assetPath, List<string> issues)
        {
            int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
            if (missingCount > 0)
            {
                issues.Add($"{assetPath}: {GetHierarchyPath(root)} has {missingCount} missing script(s)");
            }

            foreach (Transform child in root.transform)
            {
                CollectMissingScripts(child.gameObject, assetPath, issues);
            }
        }

        private static string GetHierarchyPath(GameObject gameObject)
        {
            Stack<string> names = new();
            Transform current = gameObject.transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }
    }
}
