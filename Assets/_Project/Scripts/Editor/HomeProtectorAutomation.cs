using System;
using System.Collections.Generic;
using System.Linq;
using HomeProtector.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace HomeProtector.Editor.AssetPipeline
{
    public static class HomeProtectorAutomation
    {
        private const string DataRoot = "Assets/_Project/Data";
        private const string DayWaveTablePath = DataRoot + "/DayWaveTable.asset";
        private const string PlayableScenePath = "Assets/Scenes/isometric scene.unity";
        private const string PlaceholderPostBoxPrefabPath = "Assets/Prefabs/PostBox.prefab";
        private const string ForbiddenTilemapMaterialPath = "Assets/Prefabs/Last/Materials/우체통.mat";

        public static void ValidateProject()
        {
            List<string> issues = new();

            if (EditorUtility.scriptCompilationFailed)
            {
                issues.Add("Unity reports script compilation errors.");
            }

            int sceneCount = ValidateEnabledScenes(issues);
            int dataAssetCount = ValidateFoundationData(issues);

            if (issues.Count > 0)
            {
                foreach (string issue in issues)
                {
                    Debug.LogError("HOME_PROTECTOR_VALIDATE_ERROR " + issue);
                }

                throw new BuildFailedException(
                    $"Home Protector validation failed with {issues.Count} issue(s).");
            }

            Debug.Log(
                $"HOME_PROTECTOR_VALIDATE_OK scenes={sceneCount} dataAssets={dataAssetCount}");
        }

        private static int ValidateEnabledScenes(List<string> issues)
        {
            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .ToArray();

            bool playableSceneEnabled = enabledScenes.Any(scene => scene.path == PlayableScenePath);
            if (enabledScenes.Length == 0)
            {
                issues.Add("Build Settings has no enabled scenes.");
            }

            if (!playableSceneEnabled)
            {
                issues.Add($"Build Settings must enable the playable scene: {PlayableScenePath}.");
                enabledScenes = enabledScenes
                    .Concat(new[] { new EditorBuildSettingsScene(PlayableScenePath, true) })
                    .ToArray();
            }

            int validatedSceneCount = 0;
            foreach (EditorBuildSettingsScene buildScene in enabledScenes)
            {
                string scenePath = buildScene.path;
                if (string.IsNullOrWhiteSpace(scenePath) ||
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                {
                    issues.Add($"Enabled scene does not exist: '{scenePath}'.");
                    continue;
                }

                Scene scene = SceneManager.GetSceneByPath(scenePath);
                bool openedForValidation = !scene.IsValid() || !scene.isLoaded;

                try
                {
                    if (openedForValidation)
                    {
                        scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    }

                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        ValidateHierarchy(root, scenePath, issues);
                    }

                    validatedSceneCount++;
                }
                catch (Exception exception)
                {
                    issues.Add($"Could not validate scene '{scenePath}': {exception.Message}");
                }
                finally
                {
                    if (openedForValidation && scene.IsValid() && scene.isLoaded)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            return validatedSceneCount;
        }

        private static void ValidateHierarchy(GameObject gameObject, string scenePath, List<string> issues)
        {
            int missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
            if (instanceRoot == gameObject)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                if (source != null &&
                    AssetDatabase.GetAssetPath(source) == PlaceholderPostBoxPrefabPath)
                {
                    issues.Add($"{scenePath}: {GetHierarchyPath(gameObject)} uses the temporary PostBox prefab.");
                }
            }

            TilemapRenderer tilemapRenderer = gameObject.GetComponent<TilemapRenderer>();
            if (tilemapRenderer != null &&
                AssetDatabase.GetAssetPath(tilemapRenderer.sharedMaterial) == ForbiddenTilemapMaterialPath)
            {
                issues.Add(
                    $"{scenePath}: {GetHierarchyPath(gameObject)} uses the forbidden PostBox tilemap material.");
            }

            if (missingScriptCount > 0)
            {
                issues.Add(
                    $"{scenePath}: {GetHierarchyPath(gameObject)} has " +
                    $"{missingScriptCount} missing script(s).");
            }

            foreach (Component component in gameObject.GetComponents<Component>())
            {
                if (component != null)
                {
                    try
                    {
                        CollectMissingObjectReferences(component, scenePath, gameObject, issues);
                    }
                    catch (Exception exception)
                    {
                        issues.Add(
                            $"{scenePath}: {GetHierarchyPath(gameObject)} " +
                            $"({component.GetType().Name}) reference validation threw " +
                            $"{exception.GetType().Name}: {exception.Message}");
                    }
                }
            }

            foreach (Transform child in gameObject.transform)
            {
                ValidateHierarchy(child.gameObject, scenePath, issues);
            }
        }

        private static void CollectMissingObjectReferences(
            Component component,
            string scenePath,
            GameObject owner,
            List<string> issues)
        {
            SerializedProperty property = new SerializedObject(component).GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference ||
                    property.objectReferenceValue != null ||
                    property.objectReferenceInstanceIDValue == 0)
                {
                    continue;
                }

                issues.Add(
                    $"{scenePath}: {GetHierarchyPath(owner)} " +
                    $"({component.GetType().Name}.{property.propertyPath}) has a missing reference.");
            }
        }

        private static int ValidateFoundationData(List<string> issues)
        {
            int validatedAssetCount = 0;
            DayWaveTable dayWaveTable = AssetDatabase.LoadAssetAtPath<DayWaveTable>(DayWaveTablePath);
            if (dayWaveTable == null)
            {
                issues.Add($"Missing Foundation data asset: {DayWaveTablePath}.");
            }
            else
            {
                validatedAssetCount++;
                if (!dayWaveTable.IsValid(out string message))
                {
                    issues.Add($"{DayWaveTablePath}: {message}");
                }
                IReadOnlyList<DayWaveEntry> entries = dayWaveTable.Entries;
                if (entries != null)
                {
                    for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                    {
                        DayWaveEntry entry = entries[entryIndex];
                        if (entry == null)
                        {
                            issues.Add($"{DayWaveTablePath}: entry {entryIndex} is null.");
                            continue;
                        }

                        IReadOnlyList<WaveDefinition> waves = entry.Waves;
                        if (waves == null)
                        {
                            issues.Add($"{DayWaveTablePath}: entry {entryIndex} waves collection is null.");
                            continue;
                        }

                        for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
                        {
                            if (waves[waveIndex] == null)
                            {
                                issues.Add(
                                    $"{DayWaveTablePath}: entry {entryIndex} has a missing wave " +
                                    $"at index {waveIndex}.");
                            }
                        }
                    }
                }
            }

            validatedAssetCount += ValidateDefinitions<TowerDefinition>(
                "t:TowerDefinition",
                ValidateTowerDefinition,
                issues);
            validatedAssetCount += ValidateDefinitions<EnemyDefinition>(
                "t:EnemyDefinition",
                ValidateEnemyDefinition,
                issues);
            validatedAssetCount += ValidateDefinitions<ProjectileDefinition>(
                "t:ProjectileDefinition",
                ValidateProjectileDefinition,
                issues);
            validatedAssetCount += ValidateDefinitions<WaveDefinition>(
                "t:WaveDefinition",
                ValidateWaveDefinition,
                issues);

            return validatedAssetCount;
        }

        private static int ValidateDefinitions<T>(
            string filter,
            Func<T, string> validate,
            List<string> issues)
            where T : UnityEngine.Object
        {
            string[] paths = AssetDatabase.FindAssets(filter, new[] { DataRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .ToArray();

            if (paths.Length == 0)
            {
                issues.Add($"Foundation data contains no assets matching '{filter}'.");
                return 0;
            }

            foreach (string path in paths)
            {
                T definition = AssetDatabase.LoadAssetAtPath<T>(path);
                if (definition == null)
                {
                    issues.Add($"Could not load Foundation data asset: {path}.");
                    continue;
                }

                try
                {
                    string issue = validate(definition);
                    if (!string.IsNullOrEmpty(issue))
                    {
                        issues.Add($"{path}: {issue}");
                    }
                }
                catch (Exception exception)
                {
                    issues.Add($"{path}: validation threw {exception.GetType().Name}: {exception.Message}");
                }
            }

            return paths.Length;
        }

        private static string ValidateTowerDefinition(TowerDefinition definition)
        {
            if (!definition.IsValid(out string message))
            {
                return message;
            }

            for (int levelIndex = 0; levelIndex < definition.Levels.Count; levelIndex++)
            {
                TowerLevelDefinition level = definition.Levels[levelIndex];
                if (level == null)
                {
                    return $"Tower '{definition.Id}' has a null level at index {levelIndex}.";
                }

                if (level.Projectile == null)
                {
                    return $"Tower '{definition.Id}' level {levelIndex} has no projectile.";
                }
            }

            return string.Empty;
        }

        private static string ValidateEnemyDefinition(EnemyDefinition definition)
        {
            return definition.IsValid(out string message) ? string.Empty : message;
        }

        private static string ValidateProjectileDefinition(ProjectileDefinition definition)
        {
            return definition.IsValid(out string message) ? string.Empty : message;
        }

        private static string ValidateWaveDefinition(WaveDefinition definition)
        {
            return definition.IsValid(out string message) ? string.Empty : message;
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
