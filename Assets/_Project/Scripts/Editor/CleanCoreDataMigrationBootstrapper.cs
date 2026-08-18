using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HomeProtector.Core;
using UnityEditor;
using UnityEngine;

namespace HomeProtector.Editor
{
    public static class CleanCoreDataMigrationBootstrapper
    {
        private const string DataRoot = "Assets/_Project/Data";
        private const string TowerDataRoot = DataRoot + "/Towers";
        private const string EnemyDataRoot = DataRoot + "/Enemies";
        private const string ProjectileDataRoot = DataRoot + "/Projectiles";
        private const string WaveDataRoot = DataRoot + "/Waves";
        private const string DayWaveTablePath = DataRoot + "/DayWaveTable.asset";
        private const string LegacyPrefabRoot = "Assets/Prefabs";

        [MenuItem("Home Protector/Migrate Legacy Content Data")]
        public static void MigrateLegacyContentData()
        {
            EnsureFolders();

            Dictionary<GameObject, ProjectileDefinition> projectileDefinitions = new();
            int towerCount = MigrateTowerDefinitions(projectileDefinitions);
            int enemyCount = MigrateEnemyDefinitions();
            int waveCount = GenerateStarterWaves();
            int dayEntryCount = GenerateStarterDayWaveTable();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"Home Protector content data migration complete. Towers: {towerCount}, " +
                $"Projectiles: {projectileDefinitions.Count}, Enemies: {enemyCount}, " +
                $"Waves: {waveCount}, Day entries: {dayEntryCount}.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Project");
            EnsureFolder("Assets/_Project", "Data");
            EnsureFolder(DataRoot, "Towers");
            EnsureFolder(DataRoot, "Enemies");
            EnsureFolder(DataRoot, "Projectiles");
            EnsureFolder(DataRoot, "Waves");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static int MigrateTowerDefinitions(Dictionary<GameObject, ProjectileDefinition> projectileDefinitions)
        {
            string[] towerTemplatePaths = AssetDatabase.FindAssets("t:TowerTemplate", new[] { LegacyPrefabRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .ToArray();

            int migratedCount = 0;
            foreach (string path in towerTemplatePaths)
            {
                TowerTemplate legacyTemplate = AssetDatabase.LoadAssetAtPath<TowerTemplate>(path);
                if (legacyTemplate == null || legacyTemplate.towerPrefab == null)
                {
                    continue;
                }

                GameObject projectilePrefab = GetObjectReference<GameObject>(
                    FindComponentByTypeName(legacyTemplate.towerPrefab, "TowerWeapon"),
                    "projectilePrefab");
                ProjectileDefinition projectileDefinition = projectilePrefab != null
                    ? GetOrCreateProjectileDefinition(projectilePrefab, FirstWeaponDamage(legacyTemplate), projectileDefinitions)
                    : null;

                string assetName = legacyTemplate.name + "Definition";
                TowerDefinition definition = LoadOrCreateAsset<TowerDefinition>(TowerDataRoot + "/" + assetName + ".asset");
                WriteTowerDefinition(definition, legacyTemplate, projectileDefinition);
                migratedCount++;
            }

            return migratedCount;
        }

        private static int GenerateStarterWaves()
        {
            EnemyDefinition cockroach = LoadEnemyDefinition("EnemyCockroachDefinition");
            EnemyDefinition monkey = LoadEnemyDefinition("EnemyMonkeyDefinition");
            EnemyDefinition soldier = LoadEnemyDefinition("EnemyCommonSoldierDefinition");
            EnemyDefinition bear = LoadEnemyDefinition("EnemyBearDefinition");

            StarterWaveSpec[] starterWaves =
            {
                new("Day01_Wave01_CockroachIntro", "Day 1-1 Cockroach Intro", 20f, 4,
                    new StarterEnemyGroup(cockroach, 4, 1.2f)),
                new("Day01_Wave02_CockroachPush", "Day 1-2 Cockroach Push", 25f, 6,
                    new StarterEnemyGroup(cockroach, 6, 1.0f)),
                new("Day02_Wave01_CockroachSwarm", "Day 2-1 Cockroach Swarm", 28f, 8,
                    new StarterEnemyGroup(cockroach, 8, 0.85f)),
                new("Day02_Wave02_MonkeyIntro", "Day 2-2 Monkey Intro", 30f, 10,
                    new StarterEnemyGroup(cockroach, 6, 0.9f),
                    new StarterEnemyGroup(monkey, 2, 1.5f)),
                new("Day03_Wave01_MixedRaid", "Day 3-1 Mixed Raid", 35f, 14,
                    new StarterEnemyGroup(cockroach, 8, 0.8f),
                    new StarterEnemyGroup(monkey, 4, 1.2f),
                    new StarterEnemyGroup(soldier, 2, 1.6f)),
                new("Day03_Wave02_SoldierLine", "Day 3-2 Soldier Line", 38f, 18,
                    new StarterEnemyGroup(monkey, 3, 1.1f),
                    new StarterEnemyGroup(soldier, 5, 1.35f)),
                new("Day04_Wave01_HeavyPressure", "Day 4-1 Heavy Pressure", 42f, 22,
                    new StarterEnemyGroup(cockroach, 10, 0.7f),
                    new StarterEnemyGroup(monkey, 4, 1.0f),
                    new StarterEnemyGroup(soldier, 6, 1.2f)),
                new("Day04_Wave02_BearWarning", "Day 4-2 Bear Warning", 45f, 28,
                    new StarterEnemyGroup(soldier, 6, 1.1f),
                    new StarterEnemyGroup(bear, 1, 2.5f)),
                new("Day05_Wave01_BearPush", "Day 5-1 Bear Push", 48f, 34,
                    new StarterEnemyGroup(monkey, 6, 0.9f),
                    new StarterEnemyGroup(soldier, 8, 1.0f),
                    new StarterEnemyGroup(bear, 2, 2.2f)),
                new("Day05_Wave02_FirstWall", "Day 5-2 First Wall", 55f, 45,
                    new StarterEnemyGroup(cockroach, 12, 0.65f),
                    new StarterEnemyGroup(monkey, 8, 0.85f),
                    new StarterEnemyGroup(soldier, 10, 0.95f),
                    new StarterEnemyGroup(bear, 2, 2.0f)),
            };

            foreach (StarterWaveSpec spec in starterWaves)
            {
                WaveDefinition wave = LoadOrCreateAsset<WaveDefinition>(WaveDataRoot + "/" + spec.AssetName + ".asset");
                WriteWaveDefinition(wave, spec);
            }

            return starterWaves.Length;
        }

        private static int GenerateStarterDayWaveTable()
        {
            DayWaveTable table = LoadOrCreateAsset<DayWaveTable>(DayWaveTablePath);
            SerializedObject serializedObject = new(table);
            SerializedProperty entries = serializedObject.FindProperty("entries");
            entries.arraySize = 5;

            for (int day = 1; day <= 5; day++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(day - 1);
                entry.FindPropertyRelative("label").stringValue = $"Day {day}";
                entry.FindPropertyRelative("day").intValue = day;
                entry.FindPropertyRelative("dayRangeStart").intValue = 0;
                entry.FindPropertyRelative("dayRangeEnd").intValue = 0;

                SerializedProperty waves = entry.FindPropertyRelative("waves");
                waves.arraySize = 2;
                waves.GetArrayElementAtIndex(0).objectReferenceValue =
                    LoadWaveDefinition($"Day{day:00}_Wave01");
                waves.GetArrayElementAtIndex(1).objectReferenceValue =
                    LoadWaveDefinition($"Day{day:00}_Wave02");
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            return entries.arraySize;
        }

        private static int MigrateEnemyDefinitions()
        {
            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { LegacyPrefabRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .ToArray();

            int migratedCount = 0;
            foreach (string path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Component enemy = FindComponentByTypeName(prefab, "Enemy");
                Component enemyHp = FindComponentByTypeName(prefab, "EnemyHP");
                if (prefab == null || enemy == null || enemyHp == null)
                {
                    continue;
                }

                string assetName = prefab.name + "Definition";
                EnemyDefinition definition = LoadOrCreateAsset<EnemyDefinition>(EnemyDataRoot + "/" + assetName + ".asset");
                WriteEnemyDefinition(definition, prefab, enemy, enemyHp);
                migratedCount++;
            }

            return migratedCount;
        }

        private static ProjectileDefinition GetOrCreateProjectileDefinition(
            GameObject prefab,
            float baseDamage,
            Dictionary<GameObject, ProjectileDefinition> projectileDefinitions)
        {
            if (projectileDefinitions.TryGetValue(prefab, out ProjectileDefinition existing))
            {
                return existing;
            }

            string assetName = prefab.name + "Definition";
            ProjectileDefinition definition =
                LoadOrCreateAsset<ProjectileDefinition>(ProjectileDataRoot + "/" + assetName + ".asset");
            WriteProjectileDefinition(definition, prefab, baseDamage);
            projectileDefinitions[prefab] = definition;
            return definition;
        }

        private static void WriteTowerDefinition(
            TowerDefinition definition,
            TowerTemplate legacyTemplate,
            ProjectileDefinition projectileDefinition)
        {
            SerializedObject serializedObject = new(definition);
            SetString(serializedObject, "id", ToId(legacyTemplate.name));
            SetString(serializedObject, "displayName", legacyTemplate.name);
            SetObject(serializedObject, "towerPrefab", legacyTemplate.towerPrefab);
            SetObject(serializedObject, "previewPrefab", legacyTemplate.followTowerPrefab);

            SerializedProperty levels = serializedObject.FindProperty("levels");
            levels.arraySize = legacyTemplate.weapons != null ? legacyTemplate.weapons.Count : 0;

            for (int i = 0; i < levels.arraySize; i++)
            {
                TowerTemplate.Weapon legacyWeapon = legacyTemplate.weapons[i];
                SerializedProperty level = levels.GetArrayElementAtIndex(i);
                level.FindPropertyRelative("sprite").objectReferenceValue = legacyWeapon.sprite;
                level.FindPropertyRelative("projectile").objectReferenceValue = projectileDefinition;
                level.FindPropertyRelative("damage").floatValue = legacyWeapon.damage;
                level.FindPropertyRelative("fireRate").floatValue = legacyWeapon.rate;
                level.FindPropertyRelative("range").floatValue = legacyWeapon.range;
                level.FindPropertyRelative("cost").intValue = legacyWeapon.cost;
                level.FindPropertyRelative("sellValue").intValue = legacyWeapon.sell;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private static void WriteEnemyDefinition(
            EnemyDefinition definition,
            GameObject prefab,
            Component enemy,
            Component enemyHp)
        {
            SerializedObject serializedObject = new(definition);
            SetString(serializedObject, "id", ToId(prefab.name));
            SetString(serializedObject, "displayName", prefab.name);
            SetObject(serializedObject, "prefab", prefab);
            SetObject(serializedObject, "icon", GetPrefabSprite(prefab));
            SetFloat(serializedObject, "maxHealth", Mathf.Max(1f, GetFloat(enemyHp, "maxHP", 1f)));
            SetInt(serializedObject, "goldReward", GetInt(enemy, "gold", 1));
            SetInt(serializedObject, "experienceReward", GetInt(enemy, "expValue", 1));
            SetFloat(serializedObject, "moveSpeedMultiplier", 1f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private static void WriteProjectileDefinition(ProjectileDefinition definition, GameObject prefab, float baseDamage)
        {
            SerializedObject serializedObject = new(definition);
            SetString(serializedObject, "id", ToId(prefab.name));
            SetString(serializedObject, "displayName", prefab.name);
            SetObject(serializedObject, "prefab", prefab);
            SetObject(serializedObject, "icon", GetPrefabSprite(prefab));
            SetEnum(serializedObject, "behaviourType", GuessProjectileBehaviour(prefab));
            SetFloat(serializedObject, "baseDamage", Mathf.Max(0f, baseDamage));
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private static void WriteWaveDefinition(WaveDefinition definition, StarterWaveSpec spec)
        {
            SerializedObject serializedObject = new(definition);
            SetString(serializedObject, "id", ToId(spec.AssetName));
            SetString(serializedObject, "displayName", spec.DisplayName);
            SetFloat(serializedObject, "duration", spec.Duration);
            SetInt(serializedObject, "rewardGold", spec.RewardGold);

            SerializedProperty enemyGroups = serializedObject.FindProperty("enemyGroups");
            enemyGroups.arraySize = spec.Groups.Count;

            for (int i = 0; i < spec.Groups.Count; i++)
            {
                StarterEnemyGroup group = spec.Groups[i];
                SerializedProperty entry = enemyGroups.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("enemy").objectReferenceValue = group.Enemy;
                entry.FindPropertyRelative("count").intValue = group.Count;
                entry.FindPropertyRelative("spawnInterval").floatValue = group.SpawnInterval;
                entry.FindPropertyRelative("spawnPointOverride").objectReferenceValue = null;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Component FindComponentByTypeName(GameObject prefab, string typeName)
        {
            if (prefab == null)
            {
                return null;
            }

            return prefab.GetComponentsInChildren<Component>(true)
                .FirstOrDefault(component => component != null && component.GetType().Name == typeName);
        }

        private static T GetObjectReference<T>(UnityEngine.Object owner, string propertyName)
            where T : UnityEngine.Object
        {
            if (owner == null)
            {
                return null;
            }

            SerializedProperty property = new SerializedObject(owner).FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as T : null;
        }

        private static float FirstWeaponDamage(TowerTemplate legacyTemplate)
        {
            return legacyTemplate.weapons != null && legacyTemplate.weapons.Count > 0
                ? legacyTemplate.weapons[0].damage
                : 0f;
        }

        private static Sprite GetPrefabSprite(GameObject prefab)
        {
            SpriteRenderer renderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
            return renderer != null ? renderer.sprite : null;
        }

        private static EnemyDefinition LoadEnemyDefinition(string assetName)
        {
            EnemyDefinition definition =
                AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemyDataRoot + "/" + assetName + ".asset");
            if (definition == null)
            {
                throw new InvalidOperationException($"Missing enemy definition: {assetName}");
            }

            return definition;
        }

        private static WaveDefinition LoadWaveDefinition(string assetNamePrefix)
        {
            string[] matches = AssetDatabase.FindAssets(assetNamePrefix + " t:WaveDefinition", new[] { WaveDataRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => System.IO.Path.GetFileNameWithoutExtension(path).StartsWith(assetNamePrefix, StringComparison.Ordinal))
                .OrderBy(path => path)
                .ToArray();

            if (matches.Length == 0)
            {
                throw new InvalidOperationException($"Missing wave definition matching prefix: {assetNamePrefix}");
            }

            return AssetDatabase.LoadAssetAtPath<WaveDefinition>(matches[0]);
        }

        private static ProjectileBehaviourType GuessProjectileBehaviour(GameObject prefab)
        {
            if (HasComponentNamed(prefab, "ProjectileComboDebuff"))
            {
                return ProjectileBehaviourType.ComboDebuff;
            }

            if (HasComponentNamed(prefab, "ProjectileAttackSpeedDebuff"))
            {
                return ProjectileBehaviourType.AttackSpeedDebuff;
            }

            if (HasComponentNamed(prefab, "ProjectileSlowDebuff"))
            {
                return ProjectileBehaviourType.SlowDebuff;
            }

            if (HasComponentNamed(prefab, "ProjectileAreaDamage"))
            {
                return ProjectileBehaviourType.Area;
            }

            if (HasComponentNamed(prefab, "ProjectileHoming")
                || HasComponentNamed(prefab, "ProjectileQuadraticHoming")
                || HasComponentNamed(prefab, "ProjectileCubicHoming"))
            {
                return ProjectileBehaviourType.Homing;
            }

            return ProjectileBehaviourType.Straight;
        }

        private static bool HasComponentNamed(GameObject prefab, string typeName)
        {
            return FindComponentByTypeName(prefab, typeName) != null;
        }

        private static int GetInt(UnityEngine.Object owner, string propertyName, int fallback)
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(propertyName);
            return property != null ? property.intValue : fallback;
        }

        private static float GetFloat(UnityEngine.Object owner, string propertyName, float fallback)
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(propertyName);
            return property != null ? property.floatValue : fallback;
        }

        private static void SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            serializedObject.FindProperty(propertyName).stringValue = value;
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            serializedObject.FindProperty(propertyName).floatValue = value;
        }

        private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
        {
            serializedObject.FindProperty(propertyName).intValue = value;
        }

        private static void SetEnum<TEnum>(SerializedObject serializedObject, string propertyName, TEnum value)
            where TEnum : Enum
        {
            serializedObject.FindProperty(propertyName).enumValueIndex = Convert.ToInt32(value);
        }

        private static string ToId(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return "asset";
            }

            StringBuilder builder = new();
            foreach (char character in source)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                    continue;
                }

                if (builder.Length > 0 && builder[builder.Length - 1] != '_')
                {
                    builder.Append('_');
                }
            }

            return builder.ToString().Trim('_');
        }

        private readonly struct StarterWaveSpec
        {
            public StarterWaveSpec(
                string assetName,
                string displayName,
                float duration,
                int rewardGold,
                params StarterEnemyGroup[] groups)
            {
                AssetName = assetName;
                DisplayName = displayName;
                Duration = duration;
                RewardGold = rewardGold;
                Groups = groups.Where(group => group.Enemy != null && group.Count > 0).ToArray();
            }

            public string AssetName { get; }
            public string DisplayName { get; }
            public float Duration { get; }
            public int RewardGold { get; }
            public IReadOnlyList<StarterEnemyGroup> Groups { get; }
        }

        private readonly struct StarterEnemyGroup
        {
            public StarterEnemyGroup(EnemyDefinition enemy, int count, float spawnInterval)
            {
                Enemy = enemy;
                Count = count;
                SpawnInterval = spawnInterval;
            }

            public EnemyDefinition Enemy { get; }
            public int Count { get; }
            public float SpawnInterval { get; }
        }
    }
}
