// 태그 기반 타겟팅을 지원하는 EnemySpawner 클래스 수정
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private Tilemap tilemap;
    [SerializeField]
    private GameObject enemyHPSliderPrefab;
    [SerializeField]
    private Transform canvasTransform;
    [SerializeField]
    private string defaultTargetTag = "Target"; // 기본 타겟 태그
    [SerializeField]
    private GoodsBoxHP playerHP;
    [SerializeField]
    private PlayerGold playerGold;

    private Wave currentWave;
    private int currentEnemyCount;
    private List<Enemy> enemyList;
    private Vector3 offset = new Vector3(0.5f, 0.5f, 0);
    private List<Vector3> possibleSpawnPoints = new List<Vector3>();

    // 태그별 타겟 캐시 (성능 최적화)
    private Dictionary<string, List<Transform>> taggedTargets = new Dictionary<string, List<Transform>>();

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

        // 시작 시 모든 태그 타겟을 찾아서 캐시
        CacheAllTaggedTargets();
    }
    // 씬의 모든 태그 타겟을 찾아서 캐싱하는 함수
    private void CacheAllTaggedTargets()
    {
        // 기본 타겟 태그 처리
        GameObject[] defaultTargets = GameObject.FindGameObjectsWithTag(defaultTargetTag);
        List<Transform> defaultTargetsList = new List<Transform>();

        foreach (GameObject obj in defaultTargets)
        {
            defaultTargetsList.Add(obj.transform);
        }

        taggedTargets[defaultTargetTag] = defaultTargetsList;

        // 기존 태그 캐시 초기화 (선택적)
        // 씬에 있는 모든 게임오브젝트를 검사하여 태그가 있는 오브젝트를 캐시할 수도 있습니다.
        // 하지만 성능상의 이유로 필요한 태그만 동적으로 캐시하는 방식이 더 효율적입니다.
    }

    // 특정 태그를 가진 타겟을 찾는 함수
    private Transform FindTargetByTag(string tag)
    {
        // 태그가 비어있거나 "Untagged"인 경우 기본 태그 사용
        if (string.IsNullOrEmpty(tag) || tag == "Untagged")
        {
            tag = defaultTargetTag;
        }

        // 이미 캐시된 태그 확인
        if (!taggedTargets.ContainsKey(tag))
        {
            // 캐시에 없으면 새로 찾아서 추가
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
            List<Transform> taggedTransforms = new List<Transform>();

            foreach (GameObject obj in taggedObjects)
            {
                taggedTransforms.Add(obj.transform);
            }

            taggedTargets[tag] = taggedTransforms;
        }

        // 해당 태그를 가진 오브젝트 목록에서 타겟 선택
        List<Transform> targets = taggedTargets[tag];

        if (targets.Count == 0)
        {
            Debug.LogWarning($"태그 '{tag}'를 가진 오브젝트가 없습니다. 기본 태그를 사용합니다.");

            // 기본 태그로 다시 시도
            if (tag != defaultTargetTag)
            {
                return FindTargetByTag(defaultTargetTag);
            }

            return null;
        }

        // 가장 가까운 타겟, 랜덤 타겟 등 원하는 선택 방식 구현
        // 여기서는 간단히 첫 번째 타겟을 반환
        return targets[0];
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

    private IEnumerator SpawnEnemyGroups()
    {
        // 웨이브의 각 적 그룹 처리
        foreach (var enemyGroup in currentWave.enemyGroups)
        {
            // 타겟 태그로 실제 타겟 찾기
            Transform targetToUse = FindTargetByTag(enemyGroup.targetTag);

            // 유효한 타겟이 없는 그룹은 건너뛰기
            if (targetToUse == null)
            {
                Debug.LogWarning($"프리팹 {enemyGroup.enemyPrefab.name}을 가진 적 그룹을 건너뜁니다. 유효한 타겟이 없습니다.");
                continue;
            }

            // 이 그룹의 적 생성 시작
            yield return StartCoroutine(SpawnEnemyGroup(enemyGroup, targetToUse));
        }
    }
    private IEnumerator SpawnEnemyGroup(EnemyGroup enemyGroup, Transform target)
    {
        for (int i = 0; i < enemyGroup.count; i++)
        {
            // 기본 위치에 적 생성 (Setup에서 위치 업데이트됨)
            GameObject clone = Instantiate(enemyGroup.enemyPrefab, transform.position, Quaternion.identity, transform);

            Enemy enemy = clone.GetComponent<Enemy>();

            // 다양성을 위한 랜덤 오프셋 설정
            Vector3 randomOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0
            );
            enemy.SetSpawnOffset(randomOffset);

            // 이 그룹의 스폰 포인트로 적의 스폰 위치 재정의
            enemy.SetCustomSpawnPoint(enemyGroup.spawnPoint);

            // 타겟과 함께 적 설정
            enemy.Setup(this, target);
            enemyList.Add(enemy);
            SpawnEnemyHPSlider(clone);

            yield return new WaitForSeconds(enemyGroup.spawnTime);
        }
    }
    // 웨이브의 타겟 태그를 다시 검증하는 함수
    private void RefreshTargetsForWave(Wave wave)
    {
        // 모든 태그 오브젝트 캐시 갱신 (옵션)
        // CacheAllTaggedTargets();

        // 또는 이 웨이브에서 사용하는 태그만 갱신
        foreach (var enemyGroup in wave.enemyGroups)
        {
            string tag = string.IsNullOrEmpty(enemyGroup.targetTag) ? defaultTargetTag : enemyGroup.targetTag;

            // 태그 캐시에서 제거하여 다음 검색 시 새로 찾도록 함
            taggedTargets.Remove(tag);
        }
    }

    public void DestroyEnemy(EnemyDestroyType type, Enemy enemy, int gold)
    {
        if (type == EnemyDestroyType.Arrive)
        {
            playerHP.TakeDamage(1);
        }
        else if (type == EnemyDestroyType.Kill)
        {
            playerGold.CurrentGold += gold;
        }
        currentEnemyCount--;
        enemyList.Remove(enemy);
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

        // 웨이브 시작 전 태그된 타겟 갱신
        RefreshTargetsForWave(wave);

        // 이 웨이브의 총 적 수 계산
        currentEnemyCount = 0;
        foreach (var enemyGroup in wave.enemyGroups)
        {
            // 유효한 태그가 있는지 확인
            string tagToCheck = string.IsNullOrEmpty(enemyGroup.targetTag) ? defaultTargetTag : enemyGroup.targetTag;
            Transform target = FindTargetByTag(tagToCheck);

            if (target != null)
            {
                currentEnemyCount += enemyGroup.count;
            }
        }

        StartCoroutine("SpawnEnemyGroups");
    }
}