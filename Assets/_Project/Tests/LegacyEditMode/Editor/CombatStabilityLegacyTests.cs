using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HomeProtector.Tests.LegacyEditMode
{
    public sealed class CombatStabilityLegacyTests
    {
        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void EnemySpawnerPrunesDestroyedEnemiesFromLiveCount()
        {
            EnemySpawner spawner = CreateGameObject("EnemySpawner").AddComponent<EnemySpawner>();
            Enemy enemy = CreateGameObject("Enemy").AddComponent<Enemy>();
            List<Enemy> enemies = new() { enemy };

            SetPrivateField(spawner, "enemyList", enemies);
            SetPrivateField(spawner, "currentEnemyCount", 1);

            UnityEngine.Object.DestroyImmediate(enemy.gameObject);

            Assert.That(spawner.EnemyList, Is.Empty);
            Assert.That(spawner.CurrentEnemyCount, Is.EqualTo(0));
        }

        [Test]
        public void WaveSystemSubscribesToEnemyDestroyedOnlyOnce()
        {
            EnemySpawner spawner = CreateGameObject("EnemySpawner").AddComponent<EnemySpawner>();
            WaveSystem waveSystem = CreateGameObject("WaveSystem").AddComponent<WaveSystem>();

            SetPrivateField(waveSystem, "enemySpawner", spawner);

            InvokePrivateMethod(waveSystem, "SubscribeEnemySpawnerEvents");
            InvokePrivateMethod(waveSystem, "SubscribeEnemySpawnerEvents");

            Assert.That(GetEventHandlerCount(spawner, "OnEnemyDestroyed"), Is.EqualTo(1));

            InvokePrivateMethod(waveSystem, "UnsubscribeEnemySpawnerEvents");

            Assert.That(GetEventHandlerCount(spawner, "OnEnemyDestroyed"), Is.EqualTo(0));
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field: {fieldName}");
            field.SetValue(target, value);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private method: {methodName}");
            method.Invoke(target, null);
        }

        private static int GetEventHandlerCount(object target, string eventFieldName)
        {
            FieldInfo eventField = target.GetType().GetField(eventFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(eventField, Is.Not.Null, $"Missing event backing field: {eventFieldName}");

            MulticastDelegate handlers = eventField.GetValue(target) as MulticastDelegate;
            return handlers?.GetInvocationList().Length ?? 0;
        }
    }
}
