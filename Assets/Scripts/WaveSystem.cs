using System.Collections;
using UnityEngine;

// EnemyGroup 구조체 수정 - 직접 Transform 대신 태그 사용
[System.Serializable]
public struct EnemyGroup
{
    public GameObject enemyPrefab;
    public int count;
    public float spawnTime;
    public Transform spawnPoint;
    public string targetTag; // Transform 대신 태그 문자열 사용
}


// Enhanced Wave structure that contains multiple enemy groups
[System.Serializable]
public struct Wave
{
    public string waveName;
    public EnemyGroup[] enemyGroups;
    public float delayBeforeNextWave;
}

public class WaveSystem : MonoBehaviour
{
    [SerializeField]
    private Wave[] waves;
    [SerializeField]
    private EnemySpawner enemySpawner;
    private int currentWaveIndex = -1;
    private bool isWaveActive = false;

    public int CurrentWave => currentWaveIndex + 1;
    public int MaxWave => waves.Length;

    // Optional: Automatically start first wave
    private void Start()
    {
        // StartWave();
    }

    private void Update()
    {
        // Check if current wave is complete
        if (isWaveActive && enemySpawner.EnemyList.Count == 0 && enemySpawner.CurrentEnemyCount <= 0)
        {
            isWaveActive = false;
            float delay = currentWaveIndex < waves.Length ? waves[currentWaveIndex].delayBeforeNextWave : 2f;
            StartCoroutine(StartNextWaveAfterDelay(delay));
        }
    }

    private IEnumerator StartNextWaveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartWave();
    }

    public void StartWave()
    {
        if (!isWaveActive && currentWaveIndex < waves.Length - 1)
        {
            currentWaveIndex++;
            Debug.Log($"Starting Wave: {waves[currentWaveIndex].waveName}");
            enemySpawner.StartWave(waves[currentWaveIndex]);
            isWaveActive = true;
        }
    }
}