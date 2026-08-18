using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HomeProtector.Tests.PlayMode
{
    public sealed class SceneSmokeTests
    {
        [UnityTest]
        public IEnumerator StartGameLoadingSceneTransitionsToPlayableScene()
        {
            yield return LoadScene("StartGame_Loading");

            MonoBehaviour startGame = Object.FindObjectsOfType<MonoBehaviour>()
                .FirstOrDefault(component => component.GetType().Name == "StartGame");
            Assert.That(startGame, Is.Not.Null);

            MethodInfo startMethod = startGame.GetType().GetMethod(
                "StartGameFlow",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(startMethod, Is.Not.Null);

            startMethod.Invoke(startGame, null);

            float timeoutAt = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != "isometric scene" && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("isometric scene"));
        }

        [UnityTest]
        public IEnumerator PlayableSceneLoadsCombatEngine()
        {
            yield return LoadScene("isometric scene");

            Scene activeScene = SceneManager.GetActiveScene();
            Assert.That(activeScene.name, Is.EqualTo("isometric scene"));

            string[] requiredComponentNames =
            {
                "WaveSystem",
                "EnemySpawner",
                "TowerSpawner",
                "TargetManager",
                "ResourceManager",
            };
            MonoBehaviour[] runtimeComponents = Object.FindObjectsOfType<MonoBehaviour>(true);

            foreach (string componentName in requiredComponentNames)
            {
                Assert.That(
                    runtimeComponents.Any(component => component.GetType().Name == componentName),
                    Is.True,
                    $"Playable scene is missing required combat component {componentName}.");
            }
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

    }
}
