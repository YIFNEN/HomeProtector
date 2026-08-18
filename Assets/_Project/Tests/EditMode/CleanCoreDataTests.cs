using System.Linq;
using HomeProtector.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HomeProtector.Tests.EditMode
{
    public sealed class CleanCoreDataTests
    {
        [Test]
        public void EmptyDayWaveTableReturnsNoWaves()
        {
            DayWaveTable table = ScriptableObject.CreateInstance<DayWaveTable>();

            Assert.That(table.GetWavesForDay(1), Is.Empty);
            UnityEngine.Object.DestroyImmediate(table);
        }

        [Test]
        public void GameSessionPublishesPhaseChanges()
        {
            GameObject gameObject = new GameObject("GameSession Test");
            GameSession session = gameObject.AddComponent<GameSession>();
            PhaseChangedEvent received = default;
            bool eventRaised = false;

            session.PhaseChanged += evt =>
            {
                received = evt;
                eventRaised = true;
            };

            session.BeginCombat();

            Assert.That(eventRaised, Is.True);
            Assert.That(received.NewPhase, Is.EqualTo(GamePhase.Combat));
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void CompleteCombatOutsideCombatIsIgnored()
        {
            GameObject gameObject = new GameObject("GameSession Test");
            GameSession session = gameObject.AddComponent<GameSession>();
            int phaseEvents = 0;
            session.PhaseChanged += _ => phaseEvents++;

            session.CompleteCombat(true);

            Assert.That(session.CurrentPhase, Is.EqualTo(GamePhase.Preparation));
            Assert.That(session.LastCombatWon, Is.False);
            Assert.That(phaseEvents, Is.Zero);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void RepeatedCombatCompletionKeepsFirstOutcomeAndPublishesOnce()
        {
            GameObject gameObject = new GameObject("GameSession Test");
            GameSession session = gameObject.AddComponent<GameSession>();
            int resultEvents = 0;
            session.PhaseChanged += evt =>
            {
                if (evt.NewPhase == GamePhase.Result)
                {
                    resultEvents++;
                }
            };

            session.BeginCombat();
            session.CompleteCombat(true);
            session.CompleteCombat(false);

            Assert.That(session.CurrentPhase, Is.EqualTo(GamePhase.Result));
            Assert.That(session.LastCombatWon, Is.True);
            Assert.That(resultEvents, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void BeginCombatOutsidePreparationIsIgnored()
        {
            GameObject gameObject = new GameObject("GameSession Test");
            GameSession session = gameObject.AddComponent<GameSession>();

            session.BeginCombat();
            session.CompleteCombat(true);
            session.BeginCombat();

            Assert.That(session.CurrentPhase, Is.EqualTo(GamePhase.Result));
            Assert.That(session.LastCombatWon, Is.True);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void AdvanceDayOutsideWinningResultIsIgnored()
        {
            GameObject gameObject = new GameObject("GameSession Test");
            GameSession session = gameObject.AddComponent<GameSession>();

            session.AdvanceDay();

            Assert.That(session.CurrentDay, Is.EqualTo(1));
            Assert.That(session.CurrentPhase, Is.EqualTo(GamePhase.Preparation));
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void AdvanceDayAfterDefeatIsIgnored()
        {
            GameObject gameObject = new GameObject("GameSession Test");
            GameSession session = gameObject.AddComponent<GameSession>();

            session.BeginCombat();
            session.CompleteCombat(false);
            session.AdvanceDay();

            Assert.That(session.CurrentDay, Is.EqualTo(1));
            Assert.That(session.CurrentPhase, Is.EqualTo(GamePhase.Result));
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void WinningResultAdvancesDayAndBeginsPreparation()
        {
            GameObject gameObject = new GameObject("GameSession Test");
            GameSession session = gameObject.AddComponent<GameSession>();

            session.BeginCombat();
            session.CompleteCombat(true);
            session.AdvanceDay();

            Assert.That(session.CurrentDay, Is.EqualTo(2));
            Assert.That(session.CurrentPhase, Is.EqualTo(GamePhase.Preparation));
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void DefeatCanReturnToPreparationWithoutAdvancingDay()
        {
            GameObject gameObject = new GameObject("GameSession Test");
            GameSession session = gameObject.AddComponent<GameSession>();

            session.BeginCombat();
            session.CompleteCombat(false);
            session.BeginPreparation();

            Assert.That(session.CurrentDay, Is.EqualTo(1));
            Assert.That(session.CurrentPhase, Is.EqualTo(GamePhase.Preparation));
            Assert.That(session.LastCombatWon, Is.False);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void GeneratedTowerDefinitionsAreValid()
        {
            string[] paths = FindAssetPaths("t:TowerDefinition", "Assets/_Project/Data/Towers");
            Assert.That(paths, Is.Not.Empty);

            foreach (string path in paths)
            {
                TowerDefinition definition = AssetDatabase.LoadAssetAtPath<TowerDefinition>(path);
                Assert.That(definition, Is.Not.Null, path);
                Assert.That(definition.IsValid(out string message), Is.True, $"{path}: {message}");
                for (int levelIndex = 0; levelIndex < definition.Levels.Count; levelIndex++)
                {
                    TowerLevelDefinition level = definition.Levels[levelIndex];
                    Assert.That(level, Is.Not.Null, $"{path}: tower level {levelIndex + 1} must not be null.");
                    Assert.That(
                        level.Projectile,
                        Is.Not.Null,
                        $"{path}: tower level {levelIndex + 1} must reference a projectile definition.");
                    Assert.That(
                        level.Damage,
                        Is.GreaterThan(0f),
                        $"{path}: tower level {levelIndex + 1} must have Damage greater than 0.");
                }
            }
        }

        [Test]
        public void GeneratedEnemyDefinitionsAreValid()
        {
            string[] paths = FindAssetPaths("t:EnemyDefinition", "Assets/_Project/Data/Enemies");
            Assert.That(paths, Is.Not.Empty);

            foreach (string path in paths)
            {
                EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                Assert.That(definition, Is.Not.Null, path);
                Assert.That(definition.IsValid(out string message), Is.True, $"{path}: {message}");
            }
        }

        [Test]
        public void GeneratedProjectileDefinitionsAreValid()
        {
            string[] paths = FindAssetPaths("t:ProjectileDefinition", "Assets/_Project/Data/Projectiles");
            Assert.That(paths, Is.Not.Empty);

            foreach (string path in paths)
            {
                ProjectileDefinition definition = AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(path);
                Assert.That(definition, Is.Not.Null, path);
                Assert.That(definition.IsValid(out string message), Is.True, $"{path}: {message}");
            }
        }

        [Test]
        public void GeneratedWaveDefinitionsAreValid()
        {
            string[] paths = FindAssetPaths("t:WaveDefinition", "Assets/_Project/Data/Waves");
            Assert.That(paths, Has.Length.EqualTo(10));

            foreach (string path in paths)
            {
                WaveDefinition definition = AssetDatabase.LoadAssetAtPath<WaveDefinition>(path);
                Assert.That(definition, Is.Not.Null, path);
                Assert.That(definition.IsValid(out string message), Is.True, $"{path}: {message}");
                Assert.That(definition.Duration, Is.GreaterThan(0f), path);
                Assert.That(definition.EnemyGroups.All(group => group.Count > 0), Is.True, path);
            }
        }

        [Test]
        public void StarterDayWaveTableCoversFirstFiveDays()
        {
            DayWaveTable table =
                AssetDatabase.LoadAssetAtPath<DayWaveTable>("Assets/_Project/Data/DayWaveTable.asset");
            Assert.That(table, Is.Not.Null);
            Assert.That(table.IsValid(out string message), Is.True, message);
            Assert.That(table.Entries, Has.Count.EqualTo(5));

            for (int day = 1; day <= 5; day++)
            {
                Assert.That(table.GetWavesForDay(day), Has.Count.EqualTo(2), $"Day {day}");
            }
        }

        private static string[] FindAssetPaths(string filter, string folder)
        {
            return AssetDatabase.FindAssets(filter, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .ToArray();
        }
    }
}
