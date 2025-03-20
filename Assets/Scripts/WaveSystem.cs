using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    private float defaultWaveDuration = 60f; // 기본 웨이브 지속 시간 (초)

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
    public int MaxWave => waves.Length;

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

    private void Start()
    {
        // 초기화
        InitializeReferences();

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
            // 웨이브 타이머 업데이트
            waveTimer += Time.deltaTime;

            // 웨이브 시간이 다 되었거나 적이 모두 처리된 경우
            if (waveTimer >= CurrentWaveDuration ||
                (enemySpawner.EnemyList.Count == 0 && enemySpawner.CurrentEnemyCount <= 0))
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
        if (!isWaveActive && currentWaveIndex < waves.Length - 1)
        {
            currentWaveIndex++;

            float waveDuration = CurrentWaveDuration;
            LogDebug($"웨이브 {CurrentWave} 시작: {CurrentWaveName}, 지속 시간: {waveDuration}초 (피로도: {playerGold?.FatigueRatio:P0})");

            // 웨이브 시작 이벤트 발생
            OnWaveStart?.Invoke(CurrentWave, CurrentWaveName);

            // 적 스폰 시작
            enemySpawner.StartWave(waves[currentWaveIndex]);

            // 적 처치 이벤트 구독
            enemySpawner.OnEnemyDestroyed += OnEnemyDestroyed;

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

    // 디버그 로그 출력 헬퍼 메소드
    private void LogDebug(string message)
    {
        if (showDebugMessages)
        {
            Debug.Log($"[WaveSystem] {message}");
        }
    }

    // OnDestroy: 이벤트 구독 해제
    private void OnDestroy()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnEnemyDestroyed -= OnEnemyDestroyed;
        }
    }
}