using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct EnemyGroup
{
    [Header("기본 설정")]
    [Tooltip("생성할 적 프리팹")]
    public GameObject enemyPrefab;  // 적 프리팹
    [Tooltip("생성할 적의 수")]
    public int count;               // 생성할 적의 수
    [Tooltip("적 생성 간격 (초)")]
    public float spawnTime;         // 적 생성 간격

    [Header("위치 설정")]
    [Tooltip("특정 스폰 위치 (없으면 기본 스포너 위치 사용)")]
    public Transform spawnPoint;    // 스폰 위치 (null이면 기본 위치)
}

[System.Serializable]
public struct Wave
{
    public string waveName; // 웨이브 이름
    public EnemyGroup[] enemyGroups; // 적 그룹 배열
    public float delayBeforeNextWave; // 다음 웨이브 시작 전 딜레이
    public float baseDuration; // 웨이브 기본 지속 시간 (초)
}

public class WaveSystem : MonoBehaviour
{
    [SerializeField]
    private Wave[] waves; // 웨이브 배열

    [SerializeField]
    private EnemySpawner enemySpawner; // 적 스포너 참조

    [SerializeField]
    private PlayerGold playerGold; // 플레이어 골드/피로도 참조

    [SerializeField]
    private PlayerExperience playerExperience; // 플레이어 경험치 참조

    [SerializeField]
    private float defaultWaveDuration = 30f; // 기본 웨이브 지속 시간 (초)

    [SerializeField]
    private bool cleanupEnemiesAfterAllWaves = true; // 모든 웨이브 완료 후 적 제거 여부

    [SerializeField]
    private float finalCleanupDelay = 3f; // 모든 웨이브 완료 후 적 제거까지 대기 시간 (초)

    [SerializeField]
    private bool showDebugMessages = true; // 디버그 메시지 표시 여부

    private int currentWaveIndex = -1; // 현재 웨이브 인덱스
    private bool isWaveActive = false; // 웨이브 활성화 상태
    private float waveTimer = 0f; // 웨이브 타이머
    private int enemiesKilledInWave = 0; // 웨이브 중 처치한 적 수
    private bool allWavesCompleted = false; // 모든 웨이브 완료 여부
    private bool isSubscribedToEnemySpawner = false;

    // 웨이브 이벤트 델리게이트
    public delegate void WaveEventHandler(int waveNumber, string waveName);
    public event WaveEventHandler OnWaveStart; // 웨이브 시작 이벤트
    public event WaveEventHandler OnWaveEnd; // 웨이브 종료 이벤트

    // 모든 웨이브 완료 이벤트 델리게이트
    public delegate void AllWavesCompletedHandler();
    public event AllWavesCompletedHandler OnAllWavesCompleted; // 모든 웨이브 완료 이벤트

    // 현재 웨이브 번호 프로퍼티 (1부터 시작)
    public int CurrentWave => currentWaveIndex + 1;

    // 최대 웨이브 수 프로퍼티
    public int MaxWave => waves != null ? waves.Length : 0;

    // 현재 웨이브 이름 프로퍼티
    public string CurrentWaveName => currentWaveIndex >= 0 && currentWaveIndex < waves.Length ?
                                    waves[currentWaveIndex].waveName : "None";

    // 모든 웨이브 완료 여부 프로퍼티
    public bool AllWavesCompleted => allWavesCompleted;

    // 현재 웨이브 지속 시간 프로퍼티
    public float CurrentWaveDuration
    {
        get
        {
            if (currentWaveIndex < 0 || currentWaveIndex >= waves.Length)
                return defaultWaveDuration;

            // 웨이브에 지정된 지속 시간이 있으면 사용, 없으면 기본값 사용
            float baseDuration = waves[currentWaveIndex].baseDuration > 0 ?
                                waves[currentWaveIndex].baseDuration : defaultWaveDuration;

            // 플레이어의 피로도에 따른 웨이브 지속시간 계산
            if (playerGold != null)
            {
                return playerGold.GetWaveDuration(baseDuration);
            }

            return baseDuration;
        }
    }

    // 남은 웨이브 시간 프로퍼티
    public float RemainingWaveTime => Mathf.Max(0, CurrentWaveDuration - waveTimer);

    // 웨이브 진행률 프로퍼티 (0~1)
    public float WaveProgress => Mathf.Clamp01(waveTimer / CurrentWaveDuration);

    private void OnEnable()
    {
        SubscribeEnemySpawnerEvents();
    }

    private void Start()
    {
        // 초기화
        InitializeReferences();
        SubscribeEnemySpawnerEvents();

        // 자동으로 첫 웨이브 시작 (필요시 주석 해제)
        // StartWave();
    }

    private void InitializeReferences()
    {
        if (playerGold == null)
        {
            playerGold = FindObjectOfType<PlayerGold>();
            if (playerGold == null)
            {
                Debug.LogWarning("PlayerGold 참조를 찾을 수 없습니다!");
            }
        }

        if (playerExperience == null)
        {
            playerExperience = FindObjectOfType<PlayerExperience>();
        }

        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<EnemySpawner>();
            if (enemySpawner == null)
            {
                Debug.LogError("EnemySpawner를 찾을 수 없습니다!");
            }
        }
    }

    private void Update()
    {
        if (isWaveActive)
        {
            if (enemySpawner == null)
            {
                Debug.LogError("WaveSystem: EnemySpawner 참조가 없어 웨이브를 종료합니다.");
                EndCurrentWave();
                return;
            }

            // 웨이브 타이머 업데이트
            waveTimer += Time.deltaTime;

            // 웨이브 시간이 다 되었거나 적이 모두 처리된 경우
            if (waveTimer >= CurrentWaveDuration ||
                NoActiveEnemies())
            {
                EndCurrentWave();
            }
        }
    }

    // 현재 웨이브 종료 메소드
    private void EndCurrentWave()
    {
        if (!isWaveActive) return;

        isWaveActive = false;

        // 웨이브 종료 이벤트 발생
        OnWaveEnd?.Invoke(CurrentWave, CurrentWaveName);

        LogDebug($"웨이브 {CurrentWave} 종료! 처치한 적: {enemiesKilledInWave}마리");

        // 경험치 정산
        if (playerExperience != null)
        {
            playerExperience.AddExperienceForWaveCompletion(enemiesKilledInWave);
        }

        // 피로도 리셋 (추가된 부분)
        if (playerGold != null)
        {
            playerGold.ResetFatigue();
            LogDebug("웨이브 종료 시 피로도 리셋됨");
        }

        // 적 처치 수 초기화
        enemiesKilledInWave = 0;

        // 다음 웨이브가 있는지 확인
        if (currentWaveIndex < waves.Length - 1)
        {
            // 다음 웨이브 준비
            float delay = waves[currentWaveIndex].delayBeforeNextWave;
            StartCoroutine(StartNextWaveAfterDelay(delay));
        }
        else
        {
            // 모든 웨이브 완료
            HandleAllWavesCompleted();
        }
    }

    // 모든 웨이브 완료 처리
    private void HandleAllWavesCompleted()
    {
        allWavesCompleted = true;
        LogDebug("모든 웨이브가 완료되었습니다!");

        // 모든 웨이브 완료 이벤트 발생
        OnAllWavesCompleted?.Invoke();

        // 모든 웨이브 완료 후 적 제거
        if (cleanupEnemiesAfterAllWaves)
        {
            StartCoroutine(CleanupAllEnemiesAfterDelay());
        }

        // 게임 클리어 처리 (필요시 추가)
        // GameManager.Instance.HandleGameWin();
    }

    // 모든 웨이브 완료 후 적 제거 코루틴
    private IEnumerator CleanupAllEnemiesAfterDelay()
    {
        // 지정된 지연 시간 후 실행
        yield return new WaitForSeconds(finalCleanupDelay);

        int enemyCount = enemySpawner.EnemyList.Count;
        if (enemyCount > 0)
        {
            LogDebug($"모든 웨이브 완료 후 남은 {enemyCount}마리의 적 제거 중...");

            // 리스트를 복사하여 순회 중 변경 문제 방지
            List<Enemy> enemiesToDestroy = new List<Enemy>(enemySpawner.EnemyList);

            foreach (Enemy enemy in enemiesToDestroy)
            {
                if (enemy != null)
                {
                    // 적 제거 (Kill 타입으로 - 골드/경험치 없음)
                    enemy.gold = 0; // 골드 보상 없음
                    enemy.OnDie(EnemyDestroyType.Kill);

                    // 약간의 시간차를 두고 제거하여 시각적 효과 개선 (선택적)
                    yield return new WaitForSeconds(0.05f);
                }
            }

            LogDebug($"모든 웨이브 완료 후 적 제거 완료");
        }
    }

    // 웨이브 중 적 처치 추적 메소드
    public void OnEnemyKilled()
    {
        enemiesKilledInWave++;
    }

    // 다음 웨이브 시작 딜레이 코루틴
    private IEnumerator StartNextWaveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartWave();
    }

    // 웨이브 시작 메소드
    public void StartWave()
    {
        if (waves == null || waves.Length == 0)
        {
            LogDebug("시작할 웨이브가 없습니다.");
            return;
        }

        if (enemySpawner == null)
        {
            InitializeReferences();
        }

        if (enemySpawner == null)
        {
            Debug.LogError("WaveSystem: EnemySpawner가 없어 웨이브를 시작할 수 없습니다.");
            return;
        }

        SubscribeEnemySpawnerEvents();

        if (!isWaveActive && currentWaveIndex < waves.Length - 1)
        {
            currentWaveIndex++;

            float waveDuration = CurrentWaveDuration;
            LogDebug($"웨이브 {CurrentWave} 시작: {CurrentWaveName}, 지속 시간: {waveDuration}초 (피로도: {playerGold?.FatigueRatio:P0})");

            // 웨이브 시작 이벤트 발생
            OnWaveStart?.Invoke(CurrentWave, CurrentWaveName);

            // 적 스폰 시작
            enemySpawner.StartWave(waves[currentWaveIndex]);

            isWaveActive = true;
            waveTimer = 0f; // 타이머 초기화
        }
        else if (currentWaveIndex >= waves.Length - 1)
        {
            LogDebug("더 이상 시작할 웨이브가 없습니다!");
        }
    }

    // 적 처치 이벤트 핸들러
    private void OnEnemyDestroyed(Transform enemy)
    {
        // 적 처치 시 호출
        if (isWaveActive)
        {
            OnEnemyKilled();
        }
    }

    // 게임 재시작 또는 리셋 시 호출
    public void ResetWaveSystem()
    {
        StopAllCoroutines();
        currentWaveIndex = -1;
        isWaveActive = false;
        waveTimer = 0f;
        enemiesKilledInWave = 0;
        allWavesCompleted = false;

        // 피로도도 초기화
        if (playerGold != null)
        {
            playerGold.ResetFatigue();
        }

        LogDebug("웨이브 시스템 리셋");
    }

    private bool NoActiveEnemies()
    {
        return enemySpawner != null &&
               enemySpawner.EnemyList.Count == 0 &&
               enemySpawner.CurrentEnemyCount <= 0;
    }

    private void SubscribeEnemySpawnerEvents()
    {
        if (enemySpawner == null || isSubscribedToEnemySpawner)
        {
            return;
        }

        enemySpawner.OnEnemyDestroyed += OnEnemyDestroyed;
        isSubscribedToEnemySpawner = true;
    }

    private void UnsubscribeEnemySpawnerEvents()
    {
        if (enemySpawner == null || !isSubscribedToEnemySpawner)
        {
            return;
        }

        enemySpawner.OnEnemyDestroyed -= OnEnemyDestroyed;
        isSubscribedToEnemySpawner = false;
    }

    // 디버그 로그 출력 헬퍼 메소드
    private void LogDebug(string message)
    {
        if (showDebugMessages)
        {
            Debug.Log($"[WaveSystem] {message}");
        }
    }

    private void OnDisable()
    {
        UnsubscribeEnemySpawnerEvents();
    }

    // OnDestroy: 이벤트 구독 해제
    private void OnDestroy()
    {
        UnsubscribeEnemySpawnerEvents();
    }

    // 동적으로 웨이브를 설정하는 메소드
    public void SetWaves(Wave[] newWaves)
    {
        if (newWaves == null || newWaves.Length == 0)
        {
            Debug.LogWarning("설정하려는 웨이브가 비어있습니다.");
            return;
        }

        // 현재 진행 중인 웨이브가 있는지 확인
        if (isWaveActive)
        {
            Debug.LogWarning("웨이브가 진행 중일 때는 새 웨이브를 설정할 수 없습니다.");
            return;
        }

        // 기존 웨이브 저장
        Wave[] oldWaves = waves;

        // 새 웨이브 설정
        waves = newWaves;

        // 웨이브 관련 상태 초기화
        ResetWaveSystem();

        Debug.Log($"웨이브 설정이 변경되었습니다. 웨이브 수: {waves.Length}개");

        // 웨이브 정보 출력 (디버그용)
        for (int i = 0; i < waves.Length; i++)
        {
            Debug.Log($"웨이브 {i + 1}: {waves[i].waveName}, 적 그룹 수: {waves[i].enemyGroups.Length}개");
        }
    }

    // 웨이브 추가 메소드
    public void AddWaves(Wave[] additionalWaves)
    {
        if (additionalWaves == null || additionalWaves.Length == 0)
        {
            Debug.LogWarning("추가하려는 웨이브가 비어있습니다.");
            return;
        }

        // 기존 웨이브와 새 웨이브 병합
        Wave[] combinedWaves = new Wave[waves.Length + additionalWaves.Length];

        // 기존 웨이브 복사
        for (int i = 0; i < waves.Length; i++)
        {
            combinedWaves[i] = waves[i];
        }

        // 새 웨이브 추가
        for (int i = 0; i < additionalWaves.Length; i++)
        {
            combinedWaves[waves.Length + i] = additionalWaves[i];
        }

        // 병합된 웨이브 설정
        waves = combinedWaves;

        Debug.Log($"웨이브가 추가되었습니다. 총 웨이브 수: {waves.Length}개");
    }

    // 특정 인덱스의 웨이브 가져오기
    public Wave GetWave(int index)
    {
        if (index < 0 || index >= waves.Length)
        {
            Debug.LogWarning($"유효하지 않은 웨이브 인덱스: {index}, 웨이브 수: {waves.Length}");
            return default(Wave);
        }

        return waves[index];
    }

    // 현재 웨이브 정보 복제하여 가져오기
    public Wave GetCurrentWaveInfo()
    {
        if (currentWaveIndex < 0 || currentWaveIndex >= waves.Length)
        {
            Debug.LogWarning("현재 활성화된 웨이브가 없습니다.");
            return default(Wave);
        }

        return waves[currentWaveIndex];
    }

    // 랜덤 웨이브 생성 (선택적)
    public Wave GenerateRandomWave(int difficulty = 1)
    {
        // 빈 웨이브 생성
        Wave randomWave = new Wave();

        // 웨이브 이름 설정
        randomWave.waveName = $"Random Wave (Difficulty {difficulty})";

        // 기본 지속 시간 설정
        randomWave.baseDuration = 60f + (difficulty * 10f);

        // 적 그룹 생성
        int groupCount = Mathf.Max(1, Random.Range(1, 3 + difficulty / 2));
        randomWave.enemyGroups = new EnemyGroup[groupCount];

        // 랜덤 적 프리팹 가져오기 (Resources 폴더에서)
        GameObject[] enemyPrefabs = Resources.LoadAll<GameObject>("Prefabs/Enemies");

        // 적 프리팹이 없으면 빈 웨이브 반환
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("랜덤 웨이브 생성을 위한 적 프리팹을 찾을 수 없습니다.");
            return randomWave;
        }

        // 각 그룹 설정
        for (int i = 0; i < groupCount; i++)
        {
            EnemyGroup group = new EnemyGroup();

            // 랜덤 적 프리팹 선택
            group.enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            // 적 수량 설정 (난이도에 따라)
            group.count = Mathf.Max(3, 5 + difficulty * 2 + Random.Range(-2, 3));

            // 스폰 간격 설정
            group.spawnTime = Mathf.Max(0.5f, 2f - (difficulty * 0.1f) + Random.Range(-0.2f, 0.2f));

            // 그룹 추가
            randomWave.enemyGroups[i] = group;
        }

        // 다음 웨이브 딜레이 설정
        randomWave.delayBeforeNextWave = 5f + Random.Range(0f, 5f);

        return randomWave;
    }

    // 웨이브의 특정 속성 조정
    public void AdjustWaveDifficulty(float difficultyMultiplier)
    {
        // 웨이브 배열의 새 버전 생성 (원본 수정 방지)
        Wave[] adjustedWaves = new Wave[waves.Length];

        for (int i = 0; i < waves.Length; i++)
        {
            // 웨이브 복사
            adjustedWaves[i] = waves[i];

            // 웨이브 지속 시간 조정
            if (adjustedWaves[i].baseDuration > 0)
            {
                adjustedWaves[i].baseDuration *= Mathf.Max(0.5f, difficultyMultiplier);
            }

            // 적 그룹 복사 및 조정
            EnemyGroup[] adjustedGroups = new EnemyGroup[adjustedWaves[i].enemyGroups.Length];

            for (int j = 0; j < adjustedWaves[i].enemyGroups.Length; j++)
            {
                // 그룹 복사
                adjustedGroups[j] = adjustedWaves[i].enemyGroups[j];

                // 적 수량 조정 (구조체는 직접 수정 불가하므로 새로운 인스턴스 생성)
                int newCount = Mathf.Max(1, Mathf.RoundToInt(adjustedGroups[j].count * difficultyMultiplier));
                adjustedGroups[j].count = newCount;

                // 스폰 시간 조정 (반비례)
                float newSpawnTime = Mathf.Max(0.2f, adjustedGroups[j].spawnTime / Mathf.Max(0.5f, difficultyMultiplier));
                adjustedGroups[j].spawnTime = newSpawnTime;
            }

            // 조정된 그룹 설정
            adjustedWaves[i].enemyGroups = adjustedGroups;
        }

        // 조정된 웨이브로 업데이트
        waves = adjustedWaves;

        Debug.Log($"웨이브 난이도가 조정되었습니다. 배율: {difficultyMultiplier}");
    }
}
