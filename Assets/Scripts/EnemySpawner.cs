using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemySpawner : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField]
    private Tilemap tilemap;
    [SerializeField]
    private GameObject enemyHPSliderPrefab;
    [SerializeField]
    private Transform canvasTransform;
    [SerializeField]
    private string defaultTargetTag = "Resource"; // 기본 타겟 태그

    [Header("리소스 참조")]
    [SerializeField]
    private PlayerGold playerGold;

    [Header("디버그")]
    [SerializeField]
    private bool debugMode = false;

    private Wave currentWave;
    private int currentEnemyCount;
    private List<Enemy> enemyList;
    private Vector3 offset = new Vector3(0.5f, 0.5f, 0);
    private List<Vector3> possibleSpawnPoints = new List<Vector3>();

    // 적 스폰/제거 이벤트
    public delegate void EnemyEvent(Transform enemy);
    public event EnemyEvent OnEnemySpawned;
    public event EnemyEvent OnEnemyDestroyed;

    public List<Enemy> EnemyList => enemyList;
    public int CurrentEnemyCount => currentEnemyCount;

    private void Awake()
    {
        enemyList = new List<Enemy>();

        // 타일맵이 설정되어 있으면 가능한 스폰 위치 계산
        if (tilemap != null)
        {
            CalculatePossibleSpawnPoints();
        }
    }

    private void Start()
    {
        // TargetManager 초기화 확인
        if (TargetManager.Instance == null)
        {
            Debug.LogWarning("TargetManager가 초기화되지 않았습니다. 생성합니다.");
            GameObject targetManagerObj = new GameObject("TargetManager");
            targetManagerObj.AddComponent<TargetManager>();
        }
    }

    private void CalculatePossibleSpawnPoints()
    {
        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] allTiles = tilemap.GetTilesBlock(bounds);

        // Check all tiles except the border tiles
        for (int y = 1; y < bounds.size.y - 1; ++y)
        {
            for (int x = 1; x < bounds.size.x - 1; ++x)
            {
                TileBase tile = allTiles[y * bounds.size.x + x];
                if (tile != null)
                {
                    Vector3Int localPosition = bounds.position + new Vector3Int(x, y);
                    Vector3 position = tilemap.CellToWorld(localPosition) + offset;
                    position.z = 0;
                    possibleSpawnPoints.Add(position);
                }
            }
        }

        if (debugMode)
        {
            Debug.Log($"가능한 스폰 포인트 계산 완료: {possibleSpawnPoints.Count}개");
        }
    }

    // Get spawn position based on provided spawn point or random position from tilemap
    public Vector3 GetSpawnPosition(Transform specificSpawnPoint = null)
    {
        // If a specific spawn point is provided, use it
        if (specificSpawnPoint != null)
        {
            return specificSpawnPoint.position;
        }

        // Otherwise use a random point from possible spawn points
        if (possibleSpawnPoints.Count > 0)
        {
            int index = Random.Range(0, possibleSpawnPoints.Count);
            return possibleSpawnPoints[index];
        }

        // Fallback to spawner position
        return transform.position;
    }

    // 적 그룹 생성
    private IEnumerator SpawnEnemyGroups()
    {
        // 웨이브의 각 적 그룹 처리
        foreach (var enemyGroup in currentWave.enemyGroups)
        {
            // 이 그룹의 적 생성 시작
            yield return StartCoroutine(SpawnEnemyGroup(enemyGroup));
        }
    }

    private IEnumerator SpawnEnemyGroup(EnemyGroup enemyGroup)
    {
        // 스폰 위치 결정
        Vector3 spawnPosition = enemyGroup.spawnPoint != null
            ? enemyGroup.spawnPoint.position
            : transform.position;

        for (int i = 0; i < enemyGroup.count; i++)
        {
            // 기본 위치에 적 생성
            GameObject clone = Instantiate(enemyGroup.enemyPrefab, transform.position, Quaternion.identity, transform);
            Enemy enemy = clone.GetComponent<Enemy>();

            if (enemy == null)
            {
                Debug.LogError($"프리팹 {enemyGroup.enemyPrefab.name}에 Enemy 컴포넌트가 없습니다!");
                Destroy(clone);
                continue;
            }

            // 랜덤 오프셋 설정
            Vector3 randomOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0
            );
            enemy.SetSpawnOffset(randomOffset);

            // 스폰 포인트 설정
            enemy.SetCustomSpawnPoint(enemyGroup.spawnPoint);

            // 적의 고유 설정을 사용하여 타겟 찾기
            Transform target = null;

            // TargetManager를 통해 프리팹 자신의 타겟 우선순위로 타겟 찾기
            if (TargetManager.Instance != null)
            {
                target = TargetManager.Instance.FindTargetByPriority(
                    enemy.GetTargetTagPriority(),
                    spawnPosition,
                    enemy.GetTargetSearchRadius()
                );
            }

            // 타겟을 못 찾으면 건너뛰기
            if (target == null)
            {
                Debug.LogWarning($"적 {enemy.name}을 위한 타겟을 찾을 수 없습니다. 스킵합니다.");
                Destroy(clone);
                continue;
            }

            // 적 초기화
            enemy.Setup(this, target);
            enemyList.Add(enemy);
            currentEnemyCount++;

            // HP 슬라이더 생성
            SpawnEnemyHPSlider(clone);

            // 이벤트 발생
            OnEnemySpawned?.Invoke(enemy.transform);

            yield return new WaitForSeconds(enemyGroup.spawnTime);
        }
    }

    // 특정 적에 대해 타겟을 재할당하는 함수
    public Transform ReassignTargetForEnemy(Enemy enemy, string targetTag = null)
    {
        if (enemy == null) return null;

        // 적의 자체 타겟 우선순위 사용
        Transform newTarget = TargetManager.Instance.FindTargetByPriority(
            enemy.GetTargetTagPriority(),
            enemy.transform.position,
            enemy.GetTargetSearchRadius()
        );

        // 새 타겟 설정
        if (newTarget != null)
        {
            enemy.SetTarget(newTarget);

            if (debugMode)
            {
                Debug.Log($"Reassigned target for {enemy.name}: {newTarget.name}");
            }
        }

        return newTarget;
    }

    // 모든 적에 대해 타겟을 재할당하는 함수
    public void ReassignTargetsForAllEnemies()
    {
        foreach (Enemy enemy in enemyList)
        {
            if (enemy != null)
            {
                ReassignTargetForEnemy(enemy);
            }
        }

        if (debugMode)
        {
            Debug.Log($"Reassigned targets for all {enemyList.Count} enemies");
        }
    }

    public void DestroyEnemy(EnemyDestroyType type, Enemy enemy, int gold)
    {
        if (type == EnemyDestroyType.Kill)
        {
            playerGold.CurrentGold += gold;
        }

        currentEnemyCount--;
        enemyList.Remove(enemy);

        // 이벤트 발생
        OnEnemyDestroyed?.Invoke(enemy.transform);

        Destroy(enemy.gameObject);
    }

    private void SpawnEnemyHPSlider(GameObject enemy)
    {
        GameObject sliderclone = Instantiate(enemyHPSliderPrefab);
        sliderclone.transform.SetParent(canvasTransform, false);
        sliderclone.transform.localScale = Vector3.one;
        sliderclone.GetComponent<SliderPositionAutoSetter>().Setup(enemy.transform);
        sliderclone.GetComponent<EnemyHPViewer>().Setup(enemy.GetComponent<EnemyHP>());
    }

    public void StartWave(Wave wave)
    {
        currentWave = wave;

        // 이 웨이브의 총 적 수 계산
        currentEnemyCount = 0;
        foreach (var enemyGroup in wave.enemyGroups)
        {
            // 유효성 검사
            if (enemyGroup.enemyPrefab != null)
            {
                currentEnemyCount += enemyGroup.count;
            }
        }

        StartCoroutine("SpawnEnemyGroups");
    }
}