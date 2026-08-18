using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace HomeProtector.Tests.PlayMode
{
    public sealed class LegacyCombatPlayModeTests
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

        [UnityTest]
        public IEnumerator EnemySpawnerSpawnsEnemyAndTracksLiveCount()
        {
            Type targetManagerType = FindRequiredType("TargetManager");
            Type enemySpawnerType = FindRequiredType("EnemySpawner");
            Type enemyType = FindRequiredType("Enemy");
            Type enemyGroupType = FindRequiredType("EnemyGroup");
            Type waveType = FindRequiredType("Wave");

            Component targetManager = CreateGameObject("TargetManager").AddComponent(targetManagerType);
            GameObject target = CreateGameObject("Registered Goods Target");
            InvokePublicMethod(targetManager, "RegisterTarget", "Goods", target.transform);

            Component spawner = CreateGameObject("EnemySpawner").AddComponent(enemySpawnerType);
            GameObject enemyPrefab = CreateGameObject("Runtime Enemy Prefab");
            enemyPrefab.AddComponent<SpriteRenderer>();
            enemyPrefab.AddComponent<NavMeshAgent>();
            enemyPrefab.AddComponent(enemyType);

            object enemyGroup = Activator.CreateInstance(enemyGroupType);
            SetPublicField(enemyGroup, "enemyPrefab", enemyPrefab);
            SetPublicField(enemyGroup, "count", 1);
            SetPublicField(enemyGroup, "spawnTime", 0f);
            SetPublicField(enemyGroup, "spawnPoint", null);

            Array enemyGroups = Array.CreateInstance(enemyGroupType, 1);
            enemyGroups.SetValue(enemyGroup, 0);

            object wave = Activator.CreateInstance(waveType);
            SetPublicField(wave, "waveName", "Runtime Smoke Wave");
            SetPublicField(wave, "enemyGroups", enemyGroups);
            SetPublicField(wave, "delayBeforeNextWave", 0f);
            SetPublicField(wave, "baseDuration", 1f);

            InvokePublicMethod(spawner, "StartWave", wave);
            yield return null;

            Assert.That(GetIntProperty(spawner, "CurrentEnemyCount"), Is.EqualTo(1));

            IList enemyList = (IList)GetProperty(spawner, "EnemyList");
            Assert.That(enemyList.Count, Is.EqualTo(1));

            Component spawnedEnemy = enemyList[0] as Component;
            Assert.That(spawnedEnemy, Is.Not.Null);

            UnityEngine.Object.Destroy(spawnedEnemy.gameObject);
            yield return null;

            Assert.That(GetIntProperty(spawner, "CurrentEnemyCount"), Is.EqualTo(0));
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static Type FindRequiredType(string typeName)
        {
            Type type = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(candidate => candidate != null);

            Assert.That(type, Is.Not.Null, $"Could not find type {typeName}");
            return type;
        }

        private static void SetPublicField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, $"Missing public field: {fieldName}");
            field.SetValue(target, value);
        }

        private static object GetProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Missing public property: {propertyName}");
            return property.GetValue(target);
        }

        private static int GetIntProperty(object target, string propertyName)
        {
            return (int)GetProperty(target, propertyName);
        }

        private static object InvokePublicMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, $"Missing public method: {methodName}");
            return method.Invoke(target, args);
        }
    }
}
