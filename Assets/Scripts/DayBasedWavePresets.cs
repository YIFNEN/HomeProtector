using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 날짜별 웨이브 프리셋 데이터
[CreateAssetMenu(fileName = "DayWavePresets", menuName = "Game/Day Wave Presets")]
public class DayBasedWavePresets : ScriptableObject
{
    [Header("Day Wave Mappings")]
    public List<DayWavePreset> dayWavePresets = new List<DayWavePreset>();

    [Header("Wave Templates")]
    public WaveTemplate[] waveTemplates;

    // 특정 일차에 맞는 웨이브 프리셋 찾기
    public DayWavePreset GetPresetForDay(int day)
    {
        // 정확히 일치하는 프리셋 찾기
        foreach (DayWavePreset preset in dayWavePresets)
        {
            if (preset.day == day)
            {
                return preset;
            }
        }

        // 정확히 일치하는 프리셋이 없으면, 일차 범위에 맞는 프리셋 찾기
        foreach (DayWavePreset preset in dayWavePresets)
        {
            if (day >= preset.dayRangeStart && day <= preset.dayRangeEnd)
            {
                return preset;
            }
        }

        // 일치하는 프리셋이 없으면 null 반환
        return null;
    }

    // 특정 일차에 맞는 웨이브 배열 생성
    public Wave[] CreateWavesForDay(int day)
    {
        // 프리셋 찾기
        DayWavePreset preset = GetPresetForDay(day);

        if (preset == null)
        {
            Debug.LogWarning($"일차 {day}에 맞는 웨이브 프리셋을 찾을 수 없습니다.");
            return new Wave[0];
        }

        // 프리셋에서 웨이브 생성
        return preset.CreateWaves(day);
    }

    // 템플릿으로부터 웨이브 생성
    public Wave CreateWaveFromTemplate(string templateName, int day)
    {
        // 템플릿 찾기
        foreach (WaveTemplate template in waveTemplates)
        {
            if (template.templateName == templateName)
            {
                return template.CreateWave(day);
            }
        }

        Debug.LogWarning($"템플릿 '{templateName}'을 찾을 수 없습니다.");
        return new Wave();
    }
}

// 일차별 웨이브 프리셋
[System.Serializable]
public class DayWavePreset
{
    public string presetName; // 에디터에서 구분용 이름

    [Header("Day Range")]
    public int day = 1; // 특정 일차
    public int dayRangeStart = 0; // 범위 시작 (특정 일차가 우선)
    public int dayRangeEnd = 0; // 범위 끝 (특정 일차가 우선)

    [Header("Preset Type")]
    public PresetType presetType = PresetType.TemplateBased;

    [Header("Template Based")]
    [Tooltip("템플릿 기반 프리셋일 때 사용할 템플릿 이름들")]
    public string[] templateNames; // 사용할 템플릿 이름들

    [Header("Direct Waves")]
    [Tooltip("직접 웨이브 정의 시 사용")]
    public Wave[] waves;

    [Header("Difficulty")]
    [Range(0.5f, 2.0f)]
    public float difficultyMultiplier = 1.0f; // 난이도 배율

    // 프리셋에 맞는 웨이브 생성
    public Wave[] CreateWaves(int currentDay)
    {
        // 직접 정의된 웨이브 사용
        if (presetType == PresetType.DirectWaves && waves != null && waves.Length > 0)
        {
            // 웨이브 복제 및 조정
            Wave[] adjustedWaves = new Wave[waves.Length];

            for (int i = 0; i < waves.Length; i++)
            {
                adjustedWaves[i] = CloneAndAdjustWave(waves[i], currentDay);
            }

            return adjustedWaves;
        }
        // 템플릿 기반 웨이브 생성
        else if (presetType == PresetType.TemplateBased && templateNames != null && templateNames.Length > 0)
        {
            // 템플릿 기반 웨이브 생성 로직 (별도 클래스 필요)
            // 여기서는 간단히 빈 웨이브 반환
            return new Wave[0];
        }

        // 기본 빈 웨이브 배열 반환
        return new Wave[0];
    }

    // 웨이브 복제 및 조정
    private Wave CloneAndAdjustWave(Wave sourceWave, int currentDay)
    {
        // 새 웨이브 생성
        Wave newWave = new Wave();

        // 속성 복사
        newWave.waveName = sourceWave.waveName;
        if (!string.IsNullOrEmpty(newWave.waveName) && !newWave.waveName.Contains($"Day {currentDay}"))
        {
            newWave.waveName = $"Day {currentDay}: {newWave.waveName}";
        }

        newWave.baseDuration = sourceWave.baseDuration;
        newWave.delayBeforeNextWave = sourceWave.delayBeforeNextWave;

        // 난이도에 따른 조정
        newWave.baseDuration *= difficultyMultiplier;

        // 일차가 증가함에 따른 추가 난이도 조정
        float dayProgressionMultiplier = 1.0f + ((currentDay - 1) * 0.05f); // 일차당 5% 증가

        // 적 그룹 복제 및 조정
        if (sourceWave.enemyGroups != null && sourceWave.enemyGroups.Length > 0)
        {
            newWave.enemyGroups = new EnemyGroup[sourceWave.enemyGroups.Length];

            for (int i = 0; i < sourceWave.enemyGroups.Length; i++)
            {
                EnemyGroup originalGroup = sourceWave.enemyGroups[i];
                EnemyGroup newGroup = new EnemyGroup();

                // 기본 속성 복사
                newGroup.enemyPrefab = originalGroup.enemyPrefab;
                newGroup.spawnPoint = originalGroup.spawnPoint;

                // 난이도에 따른 조정
                newGroup.count = Mathf.RoundToInt(originalGroup.count * difficultyMultiplier * dayProgressionMultiplier);
                newGroup.spawnTime = originalGroup.spawnTime / difficultyMultiplier; // 더 빠른 스폰

                // 최소값 보장
                newGroup.count = Mathf.Max(1, newGroup.count);
                newGroup.spawnTime = Mathf.Max(0.2f, newGroup.spawnTime);

                newWave.enemyGroups[i] = newGroup;
            }
        }
        else
        {
            newWave.enemyGroups = new EnemyGroup[0];
        }

        return newWave;
    }
}

// 웨이브 템플릿
[System.Serializable]
public class WaveTemplate
{
    public string templateName; // 템플릿 이름
    public string waveBaseName; // 웨이브 기본 이름
    public float baseDuration = 60f; // 기본 지속 시간
    public float delayBeforeNextWave = 5f; // 다음 웨이브 전 딜레이

    [Header("Enemy Groups")]
    public EnemyGroupTemplate[] enemyGroupTemplates;

    // 템플릿으로부터 웨이브 생성
    public Wave CreateWave(int day)
    {
        Wave wave = new Wave();

        // 웨이브 이름 설정
        wave.waveName = string.IsNullOrEmpty(waveBaseName) ? $"Day {day} Wave" : $"Day {day}: {waveBaseName}";

        // 지속 시간 설정
        wave.baseDuration = baseDuration;

        // 딜레이 설정
        wave.delayBeforeNextWave = delayBeforeNextWave;

        // 적 그룹 생성
        if (enemyGroupTemplates != null && enemyGroupTemplates.Length > 0)
        {
            wave.enemyGroups = new EnemyGroup[enemyGroupTemplates.Length];

            for (int i = 0; i < enemyGroupTemplates.Length; i++)
            {
                wave.enemyGroups[i] = enemyGroupTemplates[i].CreateEnemyGroup(day);
            }
        }
        else
        {
            wave.enemyGroups = new EnemyGroup[0];
        }

        return wave;
    }
}

// 적 그룹 템플릿
[System.Serializable]
public class EnemyGroupTemplate
{
    public GameObject enemyPrefab;
    public int baseCount = 10;
    public float baseSpawnTime = 1.0f;
    public Transform spawnPoint;

    [Header("Scaling")]
    [Range(0f, 1f)]
    public float countScalingFactor = 0.1f; // 일차에 따른 수량 증가 계수
    [Range(0f, 1f)]
    public float spawnTimeScalingFactor = 0.05f; // 일차에 따른 스폰 시간 감소 계수

    // 템플릿으로부터 적 그룹 생성
    public EnemyGroup CreateEnemyGroup(int day)
    {
        EnemyGroup group = new EnemyGroup();

        // 프리팹 설정
        group.enemyPrefab = enemyPrefab;

        // 스폰 포인트 설정
        group.spawnPoint = spawnPoint;

        // 일차에 따른 수량 조정
        float countMultiplier = 1.0f + ((day - 1) * countScalingFactor);
        group.count = Mathf.Max(1, Mathf.RoundToInt(baseCount * countMultiplier));

        // 일차에 따른 스폰 시간 조정
        float spawnTimeMultiplier = 1.0f - ((day - 1) * spawnTimeScalingFactor);
        group.spawnTime = Mathf.Max(0.2f, baseSpawnTime * spawnTimeMultiplier);

        return group;
    }
}

// 프리셋 타입 열거형
public enum PresetType
{
    DirectWaves, // 직접 웨이브 정의
    TemplateBased // 템플릿 기반 웨이브 생성
}