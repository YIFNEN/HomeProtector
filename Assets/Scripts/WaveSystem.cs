using UnityEngine;
using System.Collections;

// 기존 웨이브 구조체 확장
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
    private PlayerGold playerGold; // 플레이어 골드(피로도) 참조

    [SerializeField]
    private PlayerExperience playerExperience; // 플레이어 경험치 참조

    [SerializeField]
    private float defaultWaveDuration = 60f; // 기본 웨이브 지속 시간 (초)

    private int currentWaveIndex = -1; // 현재 웨이브 인덱스
    private bool isWaveActive = false; // 웨이브 활성화 상태
    private float waveTimer = 0f; // 웨이브 타이머
    private int enemiesKilledInWave = 0; // 웨이브 중 처치한 적 수

    // 현재 웨이브 번호 프로퍼티 (1부터 시작)
    public int CurrentWave => currentWaveIndex + 1;

    // 최대 웨이브 수 프로퍼티
    public int MaxWave => waves.Length;

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

            // 피로도에 따른 지속 시간 조정
            return baseDuration * (playerGold != null ? playerGold.WaveDurationMultiplier : 1.0f);
        }
    }

    // 남은 웨이브 시간 프로퍼티
    public float RemainingWaveTime => Mathf.Max(0, CurrentWaveDuration - waveTimer);

    // 웨이브 진행률 프로퍼티 (0~1)
    public float WaveProgress => Mathf.Clamp01(waveTimer / CurrentWaveDuration);

    private void Start()
    {
        // 초기화
        if (playerGold == null)
        {
            playerGold = FindObjectOfType<PlayerGold>();
        }

        if (playerExperience == null)
        {
            playerExperience = FindObjectOfType<PlayerExperience>();
        }

        // 자동으로 첫 웨이브 시작 (필요시 주석 해제)
        // StartWave();
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
        isWaveActive = false;

        // 경험치 정산
        if (playerExperience != null)
        {
            playerExperience.AddExperienceForWaveCompletion(enemiesKilledInWave);
        }

        Debug.Log($"웨이브 {CurrentWave} 종료! 처치한 적: {enemiesKilledInWave}마리");

        // 다음 웨이브 준비
        float delay = currentWaveIndex < waves.Length ? waves[currentWaveIndex].delayBeforeNextWave : 2f;
        StartCoroutine(StartNextWaveAfterDelay(delay));

        // 적 처치 수 초기화
        enemiesKilledInWave = 0;
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
            Debug.Log($"웨이브 {CurrentWave} 시작: {waves[currentWaveIndex].waveName}, 지속 시간: {CurrentWaveDuration}초");

            // 적 스폰 시작
            enemySpawner.StartWave(waves[currentWaveIndex]);

            // 적 처치 이벤트 구독
            enemySpawner.OnEnemyDestroyed += OnEnemyDestroyed;

            isWaveActive = true;
            waveTimer = 0f; // 타이머 초기화
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

    // OnDestroy: 이벤트 구독 해제
    private void OnDestroy()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnEnemyDestroyed -= OnEnemyDestroyed;
        }
    }
}