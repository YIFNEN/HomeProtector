using UnityEngine;
using System.Collections;

// PlayerGold 클래스 - 피로도 관리 포함
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

    // 경험치 시스템 참조
    private PlayerExperience playerExperience;

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

    // 정규화된 피로도 비율 반환
    public float GetNormalizedFatigueRatio()
    {
        return FatigueRatio;
    }

    // 웨이브 지속시간 계산 메서드
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

    private void Start()
    {
        // 경험치 시스템 참조 확인 및 이벤트 구독
        if (playerExperience != null)
        {
            // 레벨업 이벤트 구독
            playerExperience.onLevelUp.AddListener(OnPlayerLevelUp);
        }
        else
        {
            // 이미 Awake에서 찾았는지 확인
            if (playerExperience == null)
            {
                playerExperience = GetComponent<PlayerExperience>();
                if (playerExperience != null)
                {
                    playerExperience.onLevelUp.AddListener(OnPlayerLevelUp);
                }
                else
                {
                    // 컴포넌트를 찾을 수 없는 경우
                    playerExperience = FindObjectOfType<PlayerExperience>();
                    if (playerExperience != null)
                    {
                        playerExperience.onLevelUp.AddListener(OnPlayerLevelUp);
                    }
                    else
                    {
                        Debug.LogWarning("PlayerExperience 컴포넌트를 찾을 수 없습니다. 레벨업 시 피로도 리셋이 작동하지 않을 수 있습니다.");
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (playerExperience != null)
        {
            playerExperience.onLevelUp.RemoveListener(OnPlayerLevelUp);
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
    // 특정 양만큼 피로도 증가 메소드
    public void IncreaseFatigueByAmount(float amount)
    {
        // 피로도 증가
        currentFatigue = Mathf.Min(maxFatigue, currentFatigue + amount);
        Debug.Log($"피로도 증가: {amount}, 현재 피로도: {currentFatigue}/{maxFatigue} ({FatigueRatio:P0})");
    }

    // 역피로도 비율 (1 - 피로도 비율)
    public float GetInverseFatigueRatio()
    {
        return 1f - FatigueRatio; // 피로도가 0이면 1, 최대면 0
    }
    // 레벨업 이벤트 핸들러 - 피로도 리셋
    private void OnPlayerLevelUp(int newLevel)
    {
        // 레벨업 시 피로도 리셋
        ResetFatigue();
        // 피로도 리셋 로그
        Debug.Log($"레벨 {newLevel} 달성! 피로도가 리셋되었습니다.");
        // 추가적인 레벨업 보상 (필요시)
        // 예: 골드 보너스, 최대 피로도 증가 등
        ApplyLevelUpBonuses(newLevel);
    }

    // 레벨업 보너스 적용 (추가 기능)
    private void ApplyLevelUpBonuses(int level)
    {
        // 레벨업 보너스 골드 (레벨 * 50)
        int bonusGold = level * 50;
        CurrentGold += bonusGold;
        // 최대 피로도 증가 (레벨 * 5, 최대 150까지)
        float newMaxFatigue = 100f + (level - 1) * 5f;
        maxFatigue = Mathf.Min(newMaxFatigue, 150f);
        Debug.Log($"레벨업 보너스: {bonusGold} 골드, 최대 피로도 {maxFatigue}");
    }
}