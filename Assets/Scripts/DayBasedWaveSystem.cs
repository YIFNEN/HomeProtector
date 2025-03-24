using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 날짜에 따라 다른 웨이브를 제공하는 시스템
public class DayBasedWaveSystem : MonoBehaviour
{
    [Header("Day-Wave Mapping")]
    [Tooltip("각 일차에 해당하는 웨이브 설정")]
    [SerializeField] private List<DayWaveMapping> dayWaveMappings = new List<DayWaveMapping>();

    [Header("Default Waves")]
    [Tooltip("일차에 맞는 웨이브가 없을 경우 사용할 기본 웨이브")]
    [SerializeField] private Wave[] defaultWaves;

    [Header("System References")]
    [SerializeField] private WaveSystem waveSystem;
    [SerializeField] private DayCounterSystem dayCounterSystem;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // 생성된 웨이브 캐싱 (메모리 최적화)
    private Dictionary<int, Wave[]> generatedWavesByDay = new Dictionary<int, Wave[]>();

    private void Awake()
    {
        // 시스템 참조 찾기
        if (waveSystem == null) waveSystem = FindObjectOfType<WaveSystem>();
        if (dayCounterSystem == null) dayCounterSystem = FindObjectOfType<DayCounterSystem>();
    }

    private void Start()
    {
        // 일차 변경 이벤트 구독
        if (dayCounterSystem != null)
        {
            dayCounterSystem.onDayChanged.AddListener(OnDayChanged);
        }

        // 초기 일차에 맞는 웨이브 설정
        if (dayCounterSystem != null && waveSystem != null)
        {
            ApplyWavesForDay(dayCounterSystem.CurrentDay);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (dayCounterSystem != null)
        {
            dayCounterSystem.onDayChanged.RemoveListener(OnDayChanged);
        }
    }

    // 일차 변경 시 호출
    private void OnDayChanged(int newDay)
    {
        // 새 일차에 맞는 웨이브 설정
        ApplyWavesForDay(newDay);
    }

    // 특정 일차에 맞는 웨이브 설정 적용
    public void ApplyWavesForDay(int day)
    {
        if (waveSystem == null) return;

        // 이전에 생성된 웨이브가 있는지 확인
        if (generatedWavesByDay.ContainsKey(day))
        {
            waveSystem.SetWaves(generatedWavesByDay[day]);

            if (debugMode)
            {
                Debug.Log($"일차 {day}에 대한 캐싱된 웨이브 설정 적용");
            }
            return;
        }

        // 해당 일차에 맞는 웨이브 매핑 찾기
        DayWaveMapping mapping = FindMappingForDay(day);

        if (mapping != null && mapping.waves.Length > 0)
        {
            // 매핑에서 웨이브 설정 (복사하여 수정)
            Wave[] waves = CloneWaveArray(mapping.waves);

            // 필요시 웨이브 속성 추가 조정 (예: 난이도, 보상 등)
            AdjustWavesForDay(waves, day);

            // WaveSystem에 웨이브 설정
            waveSystem.SetWaves(waves);

            // 생성된 웨이브 캐싱
            generatedWavesByDay[day] = waves;

            if (debugMode)
            {
                Debug.Log($"일차 {day}에 대한 새 웨이브 설정 적용: {mapping.mappingName}");
            }
        }
        else
        {
            // 매핑이 없으면 기본 웨이브 사용
            if (defaultWaves.Length > 0)
            {
                // 기본 웨이브 복사 후 조정
                Wave[] adjustedDefaultWaves = CloneWaveArray(defaultWaves);

                // 복사한 기본 웨이브 조정
                AdjustWavesForDay(adjustedDefaultWaves, day);

                // WaveSystem에 웨이브 설정
                waveSystem.SetWaves(adjustedDefaultWaves);

                // 조정된 기본 웨이브 캐싱
                generatedWavesByDay[day] = adjustedDefaultWaves;

                if (debugMode)
                {
                    Debug.Log($"일차 {day}에 대한 매핑이 없어 조정된 기본 웨이브 사용");
                }
            }
            else
            {
                Debug.LogWarning($"일차 {day}에 대한 웨이브 매핑이 없고 기본 웨이브도 설정되지 않았습니다.");
            }
        }
    }

    // 특정 일차에 맞는 웨이브 매핑 찾기
    private DayWaveMapping FindMappingForDay(int day)
    {
        foreach (DayWaveMapping mapping in dayWaveMappings)
        {
            if (mapping.day == day)
            {
                return mapping;
            }
        }

        // 매핑이 없으면 null 반환
        return null;
    }

    // 웨이브 배열 복제
    private Wave[] CloneWaveArray(Wave[] sourceWaves)
    {
        Wave[] clonedWaves = new Wave[sourceWaves.Length];

        for (int i = 0; i < sourceWaves.Length; i++)
        {
            // 웨이브 복사
            clonedWaves[i] = CloneWave(sourceWaves[i]);
        }

        return clonedWaves;
    }

    // 단일 웨이브 복제
    private Wave CloneWave(Wave sourceWave)
    {
        Wave clonedWave = new Wave
        {
            waveName = sourceWave.waveName,
            delayBeforeNextWave = sourceWave.delayBeforeNextWave,
            baseDuration = sourceWave.baseDuration,
            enemyGroups = CloneEnemyGroups(sourceWave.enemyGroups)
        };

        return clonedWave;
    }

    // 적 그룹 배열 복제
    private EnemyGroup[] CloneEnemyGroups(EnemyGroup[] sourceGroups)
    {
        EnemyGroup[] clonedGroups = new EnemyGroup[sourceGroups.Length];

        for (int i = 0; i < sourceGroups.Length; i++)
        {
            // 적 그룹 복사 - targetTag 필드 제거됨
            clonedGroups[i] = new EnemyGroup
            {
                enemyPrefab = sourceGroups[i].enemyPrefab,
                count = sourceGroups[i].count,
                spawnTime = sourceGroups[i].spawnTime,
                spawnPoint = sourceGroups[i].spawnPoint
            };
        }

        return clonedGroups;
    }

    // 웨이브 속성 추가 조정 (필요시)
    private void AdjustWavesForDay(Wave[] waves, int day)
    {
        // 일차에 따른 기본 난이도 조정 (예: 일차가 높을수록 적의 수, 체력 증가)
        float difficultyMultiplier = 1.0f + ((day - 1) * 0.05f); // 일차당 5% 증가

        for (int i = 0; i < waves.Length; i++)
        {
            // 웨이브 이름에 일차 정보 추가
            if (!waves[i].waveName.Contains($"Day {day}"))
            {
                waves[i].waveName = $"Day {day}: {waves[i].waveName}";
            }

            // 일차에 따른 웨이브 지속 시간 조정 (선택적)
            if (day > 1)
            {
                // 기존 지속 시간이 있으면 사용, 없으면 기본값
                float originalDuration = waves[i].baseDuration > 0 ? waves[i].baseDuration : 60f;
                waves[i].baseDuration = originalDuration * (1.0f + ((day - 1) * 0.02f)); // 일차당 2% 증가
            }

            // 적 그룹 조정 (선택적)
            for (int j = 0; j < waves[i].enemyGroups.Length; j++)
            {
                // 일차가 1보다 크면 적의 수 조정 (난이도 상승)
                if (day > 1)
                {
                    // 적의 수를 일차에 따라 조정 (상한선 설정)
                    int originalCount = waves[i].enemyGroups[j].count;
                    int newCount = Mathf.Min(originalCount + (day - 1), originalCount * 2);

                    // 구조체의 필드 수정을 위해 임시 변수 사용
                    EnemyGroup modifiedGroup = waves[i].enemyGroups[j];
                    modifiedGroup.count = newCount;
                    waves[i].enemyGroups[j] = modifiedGroup;
                }
            }
        }
    }

    // WaveSystem에 웨이브 설정 기능 추가 (WaveSystem 확장 필요)
    public bool SetWavesToSystem(Wave[] waves)
    {
        if (waveSystem == null || waves == null || waves.Length == 0)
            return false;

        // WaveSystem에 웨이브 설정 메소드 호출
        waveSystem.SetWaves(waves);
        return true;
    }
}

// 일차-웨이브 매핑 구조체
[System.Serializable]
public class DayWaveMapping
{
    public string mappingName; // 매핑 이름 (에디터에서 구분용)
    public int day; // 해당 일차
    public Wave[] waves; // 해당 일차에 사용할 웨이브 배열
}