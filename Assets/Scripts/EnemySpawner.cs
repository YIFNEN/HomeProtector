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
    [SerializeField]
    private float targetRefreshInterval = 5f; // 타겟 주기적 갱신 간격

    private Wave currentWave;
    private int currentEnemyCount;
    private List<Enemy> enemyList;
    private Vector3 offset = new Vector3(0.5f, 0.5f, 0);
    private List<Vector3> possibleSpawnPoints = new List<Vector3>();

    // 태그별 타겟 캐시 (성능 최적화)
    private Dictionary<string, List<Transform>> taggedTargets = new Dictionary<string, List<Transform>>();

    // 타겟 추가/제거 이벤트
    public delegate void TargetEvent(string tag, Transform target);
    public event TargetEvent OnTargetAdded;
    public event TargetEvent OnTargetRemoved;

    // 적 스폰/제거 이벤트
    public delegate void EnemyEvent(Transform enemy);
    public event EnemyEvent OnEnemySpawned;
    public event EnemyEvent OnEnemyDestroyed;

    // 현재 활성화된 타겟 태그 추적
    private HashSet<string> activeTargetTags = new HashSet<string>();

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

    private void Start()
    {
        // 타겟 변경 감지를 위해 주기적으로 확인
        StartCoroutine(PeriodicTargetRefresh());
    }

    private void OnEnable()
    {
        // 씬 로드/언로드 시 타겟 변경을 감지하기 위한 이벤트 구독
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // 씬이 로드될 때 타겟 캐시 갱신
        CacheAllTaggedTargets();
    }

    // 씬의 모든 태그 타겟을 찾아서 캐싱하는 함수
    private void CacheAllTaggedTargets()
    {
        // 기존 타겟 캐시 초기화
        taggedTargets.Clear();

        // 항상 기본 타겟 태그는 캐싱
        RefreshTaggedTargets(defaultTargetTag);

        // 활성화된 모든 태그에 대해 캐싱
        foreach (string tag in activeTargetTags)
        {
            RefreshTaggedTargets(tag);
        }
    }

    // 특정 태그의 타겟을 갱신하는 함수
    private void RefreshTaggedTargets(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        // 이전 상태 저장
        List<Transform> previousTargets = new List<Transform>();
        if (taggedTargets.ContainsKey(tag))
        {
            previousTargets.AddRange(taggedTargets[tag]);
        }

        // 새로운 타겟 목록 가져오기
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
        List<Transform> newTargets = new List<Transform>();

        foreach (GameObject obj in taggedObjects)
        {
            if (obj != null)
            {
                newTargets.Add(obj.transform);
            }
        }

        // 딕셔너리 업데이트
        taggedTargets[tag] = newTargets;

        // 변경사항 확인 및 이벤트 발생
        // 1. 추가된 타겟 확인
        foreach (Transform newTarget in newTargets)
        {
            if (!previousTargets.Contains(newTarget))
            {
                // 추가된 타겟에 대한 이벤트 발생
                OnTargetAdded?.Invoke(tag, newTarget);
                Debug.Log($"Target added: {newTarget.name} with tag {tag}");
            }
        }

        // 2. 제거된 타겟 확인
        foreach (Transform oldTarget in previousTargets)
        {
            if (oldTarget != null && !newTargets.Contains(oldTarget))
            {
                // 제거된 타겟에 대한 이벤트 발생
                OnTargetRemoved?.Invoke(tag, oldTarget);
                Debug.Log($"Target removed: {oldTarget.name} with tag {tag}");
            }
        }
    }

    // 주기적으로 타겟 목록을 갱신하는 코루틴
    private IEnumerator PeriodicTargetRefresh()
    {
        while (true)
        {
            // 활성화된 모든 태그에 대해 타겟 갱신
            foreach (string tag in activeTargetTags)
            {
                RefreshTaggedTargets(tag);
            }

            // 기본 태그도 항상 확인
            RefreshTaggedTargets(defaultTargetTag);

            yield return new WaitForSeconds(targetRefreshInterval);
        }
    }

    // 특정 태그를 가진 타겟을 찾는 함수
    private Transform FindTargetByTag(string tag, Vector3 spawnPosition)
    {
        // 태그가 비어있거나 "Untagged"인 경우 기본 태그 사용
        if (string.IsNullOrEmpty(tag) || tag == "Untagged")
        {
            tag = defaultTargetTag;
        }

        // 활성 태그 목록에 추가
        activeTargetTags.Add(tag);

        // 이미 캐시된 태그 확인
        if (!taggedTargets.ContainsKey(tag))
        {
            // 캐시에 없으면 새로 찾아서 추가
            RefreshTaggedTargets(tag);
        }

        // 해당 태그를 가진 오브젝트 목록 가져오기
        List<Transform> targets = taggedTargets[tag];

        if (targets.Count == 0)
        {
            Debug.LogWarning($"태그 '{tag}'를 가진 오브젝트가 없습니다. 기본 태그를 사용합니다.");

            // 기본 태그로 다시 시도
            if (tag != defaultTargetTag)
            {
                return FindTargetByTag(defaultTargetTag, spawnPosition);
            }

            return null;
        }

        // 가장 가까운 타겟 선택
        return FindNearestTarget(targets, spawnPosition);
    }

    // 특정 적에 대해 타겟을 재할당하는 함수
    public Transform ReassignTargetForEnemy(Enemy enemy, string targetTag = null)
    {
        // 사용할 태그 결정
        string tagToUse = targetTag;
        if (string.IsNullOrEmpty(tagToUse))
        {
            // 태그가 지정되지 않은 경우 현재 웨이브의 해당 적 그룹의 태그 사용
            if (currentWave.Equals(default(Wave)) == false)
            {
                foreach (var group in currentWave.enemyGroups)
                {
                    if (group.enemyPrefab.name == enemy.gameObject.name.Replace("(Clone)", ""))
                    {
                        tagToUse = string.IsNullOrEmpty(group.targetTag) ? defaultTargetTag : group.targetTag;
                        break;
                    }
                }
            }

            // 여전히 null이면 기본 태그 사용
            if (string.IsNullOrEmpty(tagToUse))
            {
                tagToUse = defaultTargetTag;
            }
        }

        // 적의 현재 위치에서 가장 가까운 타겟 찾기
        Transform newTarget = FindTargetByTag(tagToUse, enemy.transform.position);

        // 새 타겟 설정
        if (newTarget != null)
        {
            enemy.SetTarget(newTarget);
            Debug.Log($"Reassigned target for {enemy.name}: {newTarget.name}");
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
        Debug.Log($"Reassigned targets for all {enemyList.Count} enemies");
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

    // SpawnEnemyGroups 메서드 수정
    private IEnumerator SpawnEnemyGroups()
    {
        // 웨이브의 각 적 그룹 처리
        foreach (var enemyGroup in currentWave.enemyGroups)
        {
            // 스폰 위치 결정 (스폰 포인트가 있다면 해당 위치, 없으면 기본 스포너 위치)
            Vector3 spawnPosition = enemyGroup.spawnPoint != null
                ? enemyGroup.spawnPoint.position
                : transform.position;

            // 타겟 태그로 실제 타겟 찾기 (스폰 위치 전달)
            string tagToUse = string.IsNullOrEmpty(enemyGroup.targetTag) ? defaultTargetTag : enemyGroup.targetTag;
            Transform targetToUse = FindTargetByTag(tagToUse, spawnPosition);

            // 유효한 타겟이 없는 그룹은 건너뛰기
            if (targetToUse == null)
            {
                Debug.LogWarning($"프리팹 {enemyGroup.enemyPrefab.name}을 가진 적 그룹을 건너뜁니다. 유효한 타겟이 없습니다.");
                continue;
            }

            // 이 그룹의 적 생성 시작
            yield return StartCoroutine(SpawnEnemyGroup(enemyGroup, targetToUse, tagToUse));
        }
    }

    private IEnumerator SpawnEnemyGroup(EnemyGroup enemyGroup, Transform target, string targetTag)
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

            // 타겟 태그 저장 (추후 타겟 재할당에 사용)
            enemy.SetTargetTag(targetTag);

            // 타겟과 함께 적 설정
            enemy.Setup(this, target);
            enemyList.Add(enemy);
            SpawnEnemyHPSlider(clone);

            // 이벤트 발생
            OnEnemySpawned?.Invoke(enemy.transform);

            yield return new WaitForSeconds(enemyGroup.spawnTime);
        }
    }

    // 웨이브의 타겟 태그를 다시 검증하는 함수
    private void RefreshTargetsForWave(Wave wave)
    {
        // 모든 태그 오브젝트 캐시 갱신
        CacheAllTaggedTargets();

        // 이 웨이브에서 사용하는 태그 추적
        foreach (var enemyGroup in wave.enemyGroups)
        {
            string tag = string.IsNullOrEmpty(enemyGroup.targetTag) ? defaultTargetTag : enemyGroup.targetTag;
            activeTargetTags.Add(tag);
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

        // 웨이브 시작 전 태그된 타겟 갱신
        RefreshTargetsForWave(wave);

        // 이 웨이브의 총 적 수 계산
        currentEnemyCount = 0;
        foreach (var enemyGroup in wave.enemyGroups)
        {
            // 스폰 위치 결정
            Vector3 spawnPosition = enemyGroup.spawnPoint != null
                ? enemyGroup.spawnPoint.position
                : transform.position;

            // 유효한 태그가 있는지 확인
            string tagToCheck = string.IsNullOrEmpty(enemyGroup.targetTag) ? defaultTargetTag : enemyGroup.targetTag;
            Transform target = FindTargetByTag(tagToCheck, spawnPosition);

            if (target != null)
            {
                currentEnemyCount += enemyGroup.count;
            }
        }

        StartCoroutine("SpawnEnemyGroups");
    }

    private Transform FindNearestTarget(List<Transform> targets, Vector3 spawnPosition)
    {
        if (targets == null || targets.Count == 0)
            return null;

        Transform nearestTarget = null;
        float minDistance = float.MaxValue;

        foreach (Transform target in targets)
        {
            // 이미 파괴된 오브젝트는 건너뛰기
            if (target == null)
                continue;

            float distance = Vector3.Distance(spawnPosition, target.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestTarget = target;
            }
        }

        return nearestTarget;
    }

    // 런타임에 추가된 타겟을 등록하는 함수
    public void RegisterTarget(string tag, Transform target)
    {
        if (string.IsNullOrEmpty(tag) || target == null) return;

        // 활성 태그 목록에 추가
        activeTargetTags.Add(tag);

        // 해당 태그의 타겟 목록이 없으면 새로 생성
        if (!taggedTargets.ContainsKey(tag))
        {
            taggedTargets[tag] = new List<Transform>();
        }

        // 이미 등록된 타겟이 아닌 경우에만 추가
        if (!taggedTargets[tag].Contains(target))
        {
            taggedTargets[tag].Add(target);

            // 이벤트 발생
            OnTargetAdded?.Invoke(tag, target);

            Debug.Log($"Target manually registered: {target.name} with tag {tag}");

            // 해당 태그를 사용하는 적들이 타겟을 재할당할 필요가 있는지 확인
            CheckEnemiesForTargetReassignment(tag);
        }
    }

    // 런타임에 제거된 타겟을 등록 해제하는 함수
    public void UnregisterTarget(string tag, Transform target)
    {
        if (string.IsNullOrEmpty(tag) || target == null) return;

        if (taggedTargets.ContainsKey(tag) && taggedTargets[tag].Contains(target))
        {
            taggedTargets[tag].Remove(target);

            // 이벤트 발생
            OnTargetRemoved?.Invoke(tag, target);

            Debug.Log($"Target manually unregistered: {target.name} with tag {tag}");

            // 해당 태그를 사용하는 적들이 타겟을 재할당할 필요가 있는지 확인
            CheckEnemiesForTargetReassignment(tag);
        }
    }

    // 특정 태그를 사용하는 적들의 타겟 재할당 필요성 확인
    private void CheckEnemiesForTargetReassignment(string tag)
    {
        foreach (Enemy enemy in enemyList)
        {
            if (enemy != null && enemy.TargetTag == tag)
            {
                // 현재 타겟이 없거나 파괴된 경우에만 재할당
                if (enemy.CurrentTarget == null || taggedTargets[tag].Count == 0)
                {
                    ReassignTargetForEnemy(enemy, tag);
                }
            }
        }
    }
}