using UnityEngine;
using System.Collections;

// PlayerGold 클래스 확장
public class PlayerGold : MonoBehaviour
{
    [SerializeField]
    private int currentGold = 300; // 초기 골드

    [SerializeField]
    private float maxFatigue = 100f; // 최대 피로도

    [SerializeField]
    private float currentFatigue = 0f; // 현재 피로도

    [SerializeField]
    private float fatiguePerTower = 10f; // 타워 배치 시 증가하는 피로도

    [SerializeField]
    private float fatigueDecayRate = 2f; // 피로도 자연 감소 속도 (초당)

    [SerializeField]
    private float waveTimeMultiplier = 1.5f; // 피로도 100%일 때 웨이브 지속시간 배수

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
    }

    // 최대 피로도 프로퍼티
    public float MaxFatigue
    {
        get => maxFatigue;
    }

    // 피로도 비율 (0~1) 프로퍼티
    public float FatigueRatio
    {
        get => currentFatigue / maxFatigue;
    }

    // 웨이브 지속시간 배수 계산 프로퍼티
    public float WaveDurationMultiplier
    {
        get
        {
            // 피로도가 0%일 때 1.0, 100%일 때 waveTimeMultiplier (기본값 1.5)
            return 1.0f + (waveTimeMultiplier - 1.0f) * FatigueRatio;
        }
    }

    private void Awake()
    {
        playerExperience = GetComponent<PlayerExperience>();
        if (playerExperience != null)
        {
            // 경험치 시스템에서 레벨을 가져옴
            playerLevel = playerExperience.Level;
        }

        // 피로도 자연 감소 코루틴 시작
        StartCoroutine(FatigueDecayCoroutine());
    }

    // 타워 배치 시 피로도 증가 메소드
    public void IncreaseFatigue()
    {
        // 피로도 증가
        currentFatigue = Mathf.Min(maxFatigue, currentFatigue + fatiguePerTower);
        Debug.Log($"피로도 증가: {fatiguePerTower}, 현재 피로도: {currentFatigue}/{maxFatigue} ({FatigueRatio:P0})");
    }

    // 피로도 직접 설정 메소드
    public void SetFatigue(float newFatigue)
    {
        currentFatigue = Mathf.Clamp(newFatigue, 0f, maxFatigue);
    }

    // 피로도 자연 감소 코루틴
    private IEnumerator FatigueDecayCoroutine()
    {
        while (true)
        {
            // 피로도가 0보다 크면 감소
            if (currentFatigue > 0f)
            {
                currentFatigue = Mathf.Max(0f, currentFatigue - fatigueDecayRate * Time.deltaTime);
            }

            yield return null;
        }
    }
}