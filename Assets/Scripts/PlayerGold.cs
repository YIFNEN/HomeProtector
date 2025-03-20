using UnityEngine;
using System.Collections;

// PlayerGold 클래스 수정 (피로도 관리 포함)
public class PlayerGold : MonoBehaviour
{
    [SerializeField]
    private int currentGold = 300; // 초기 골드

    [Header("Fatigue Settings")]
    [SerializeField]
    private float maxFatigue = 100f; // 최대 피로도
    [SerializeField]
    private float currentFatigue = 0f; // 현재 피로도
    [SerializeField]
    private float fatiguePerTower = 10f; // 타워 배치 시 증가하는 피로도

    [Header("Wave Duration Settings")]
    [SerializeField]
    private float minWaveDuration = 60f; // 최소 웨이브 지속 시간 (1분)
    [SerializeField]
    private float maxWaveDuration = 300f; // 최대 웨이브 지속 시간 (5분)

    public int playerLevel = 1; // 플레이어 레벨

    private PlayerExperience playerExperience; // 플레이어 경험치 시스템 참조

    // 현재 골드 프로퍼티
    public int CurrentGold
    {
        set => currentGold = Mathf.Max(0, value);
        get => currentGold;
    }

    // 현재 피로도 프로퍼티
    public float CurrentFatigue
    {
        get => currentFatigue;
        set => currentFatigue = Mathf.Clamp(value, 0f, maxFatigue);
    }

    // 최대 피로도 프로퍼티
    public float MaxFatigue => maxFatigue;

    // 피로도 비율 (0~1) 프로퍼티
    public float FatigueRatio => currentFatigue / maxFatigue;

    // 웨이브 지속시간 계산 프로퍼티
    public float GetWaveDuration(float baseDuration)
    {
        // 피로도에 비례하여 웨이브 지속시간 결정 (최소~최대 범위 내)
        float durationRange = maxWaveDuration - minWaveDuration;
        float duration = minWaveDuration + (durationRange * FatigueRatio);

        // 기본 지속시간이 있으면 그것과 계산된 지속시간 중 큰 값 사용
        return Mathf.Max(baseDuration, duration);
    }

    private void Awake()
    {
        playerExperience = GetComponent<PlayerExperience>();
        if (playerExperience != null)
        {
            // 경험치 시스템에서 레벨을 가져옴
            playerLevel = playerExperience.Level;
        }
    }

    // 타워 배치 시 피로도 증가 메소드
    public void IncreaseFatigue()
    {
        // 피로도 증가
        currentFatigue = Mathf.Min(maxFatigue, currentFatigue + fatiguePerTower);
        Debug.Log($"피로도 증가: {fatiguePerTower}, 현재 피로도: {currentFatigue}/{maxFatigue} ({FatigueRatio:P0})");
    }

    // 피로도 리셋 메소드 (웨이브 종료 시 호출)
    public void ResetFatigue()
    {
        currentFatigue = 0f;
        Debug.Log("피로도 리셋됨!");
    }

    // 피로도 직접 설정 메소드
    public void SetFatigue(float newFatigue)
    {
        currentFatigue = Mathf.Clamp(newFatigue, 0f, maxFatigue);
    }
}