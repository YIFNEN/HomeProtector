using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// 시간에 따른 UI 변경을 관리하는 클래스
public class TimeBasedUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject morningUI; // 아침 전용 UI
    [SerializeField] private GameObject eveningUI; // 저녁 전용 UI
    [SerializeField] private GameObject panelStage; // 웨이브 종료 후 표시될 스테이지 패널

    [Header("Morning UI Elements")]
    [SerializeField] private GameObject fatiguePanel; // 피로도 표시 패널
    [SerializeField] private TextMeshProUGUI fatigueText; // 피로도 표시 텍스트
    [SerializeField] private Slider fatigueSlider; // 피로도 표시 슬라이더

    [Header("Evening UI Elements")]
    [SerializeField] private GameObject waveTimerPanel; // 웨이브 타이머 패널
    [SerializeField] private TextMeshProUGUI waveTimerText; // 웨이브 남은 시간 표시
    [SerializeField] private Slider waveProgressSlider; // 웨이브 진행도 슬라이더
    [SerializeField] private GameObject resourceHealthPanel; // 재화 오브젝트 체력 패널
    [SerializeField] private TextMeshProUGUI resourceHealthText; // 재화 오브젝트 체력 텍스트
    [SerializeField] private Slider resourceHealthSlider; // 재화 오브젝트 체력 슬라이더

    [Header("Common UI Elements")]
    [SerializeField] private GameObject playerStatsPanel; // 플레이어 스탯 패널
    [SerializeField] private TextMeshProUGUI playerLevelText; // 플레이어 레벨 표시
    [SerializeField] private TextMeshProUGUI playerExpText; // 플레이어 경험치 표시
    [SerializeField] private Slider expSlider; // 경험치 진행도 슬라이더
    [SerializeField] private TextMeshProUGUI goldText; // 골드 표시

    [Header("Day Counter UI")]
    [SerializeField] private TextMeshProUGUI dayCounterText; // 일수 표시 텍스트
    [SerializeField] private GameObject dayTransitionPanel; // 일수 전환 패널
    [SerializeField] private TextMeshProUGUI dayTransitionText; // 일수 전환 텍스트

    [Header("Level Up UI")]
    [SerializeField] private GameObject levelUpPanel; // 레벨업 알림 패널
    [SerializeField] private TextMeshProUGUI levelUpText; // 레벨업 알림 텍스트
    [SerializeField] private float levelUpNotificationTime = 3f; // 레벨업 알림 표시 시간

    [Header("Result UI")]
    [SerializeField] private GameObject waveResultPanel; // 웨이브 결과 패널

    [Header("System References")]
    [SerializeField] private TimeSystem timeSystem; // 시간 시스템 참조
    [SerializeField] private WaveSystem waveSystem; // 웨이브 시스템 참조
    [SerializeField] private PlayerGold playerGold; // 플레이어 골드 참조
    [SerializeField] private PlayerExperience playerExperience; // 플레이어 경험치 참조
    [SerializeField] private ResourceManager resourceManager; // 재화 관리자 참조
    [SerializeField] private DayCounterSystem dayCounterSystem; // 일수 관리 시스템 참조
    [SerializeField] private WaveResultSystem waveResultSystem; // 웨이브 결과 시스템 참조

    // Awake: 초기화
    private void Awake()
    {
        // 필요한 시스템 컴포넌트 찾기
        if (timeSystem == null) timeSystem = FindObjectOfType<TimeSystem>();
        if (waveSystem == null) waveSystem = FindObjectOfType<WaveSystem>();
        if (playerGold == null) playerGold = FindObjectOfType<PlayerGold>();
        if (playerExperience == null) playerExperience = FindObjectOfType<PlayerExperience>();
        if (resourceManager == null) resourceManager = FindObjectOfType<ResourceManager>();
        if (dayCounterSystem == null) dayCounterSystem = FindObjectOfType<DayCounterSystem>();
        if (waveResultSystem == null) waveResultSystem = FindObjectOfType<WaveResultSystem>();

        // UI 초기 설정
        if (panelStage != null)
        {
            panelStage.SetActive(false);
        }

        if (dayTransitionPanel != null)
        {
            dayTransitionPanel.SetActive(false);
        }

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }

        if (waveResultPanel != null)
        {
            waveResultPanel.SetActive(false);
        }
    }

    // Start: 이벤트 구독
    private void Start()
    {
        // 시간 시스템 이벤트 구독
        if (timeSystem != null)
        {
            timeSystem.onMorningStart.AddListener(OnMorningStart);
            timeSystem.onEveningStart.AddListener(OnEveningStart);
        }

        // 웨이브 시스템 이벤트 구독
        if (waveSystem != null)
        {
            waveSystem.OnWaveEnd += HandleWaveEnd;
        }

        // 일수 카운터 시스템 이벤트 구독
        if (dayCounterSystem != null)
        {
            dayCounterSystem.onDayChanged.AddListener(OnDayChanged);
        }

        // 플레이어 경험치 시스템 이벤트 구독
        if (playerExperience != null)
        {
            playerExperience.onLevelUp.AddListener(OnLevelUp);
        }

        // 웨이브 결과 시스템 이벤트 구독
        if (waveResultSystem != null)
        {
            waveResultSystem.onVictory.AddListener(OnWaveVictory);
            waveResultSystem.onDefeat.AddListener(OnWaveDefeat);
        }

        // 초기 UI 상태 설정
        UpdateUIForCurrentTime();
        UpdateDayCounter();
    }

    // OnDestroy: 이벤트 구독 해제
    private void OnDestroy()
    {
        // 시간 시스템 이벤트 구독 해제
        if (timeSystem != null)
        {
            timeSystem.onMorningStart.RemoveListener(OnMorningStart);
            timeSystem.onEveningStart.RemoveListener(OnEveningStart);
        }

        // 웨이브 시스템 이벤트 구독 해제
        if (waveSystem != null)
        {
            waveSystem.OnWaveEnd -= HandleWaveEnd;
        }

        // 일수 카운터 시스템 이벤트 구독 해제
        if (dayCounterSystem != null)
        {
            dayCounterSystem.onDayChanged.RemoveListener(OnDayChanged);
        }

        // 플레이어 경험치 시스템 이벤트 구독 해제
        if (playerExperience != null)
        {
            playerExperience.onLevelUp.RemoveListener(OnLevelUp);
        }

        // 웨이브 결과 시스템 이벤트 구독 해제
        if (waveResultSystem != null)
        {
            waveResultSystem.onVictory.RemoveListener(OnWaveVictory);
            waveResultSystem.onDefeat.RemoveListener(OnWaveDefeat);
        }
    }

    // Update: UI 업데이트
    private void Update()
    {
        // 공통 UI 업데이트
        UpdateCommonUI();

        // 시간에 따른 특정 UI 업데이트
        if (timeSystem != null)
        {
            if (timeSystem.CurrentTime == TimeOfDay.Morning)
            {
                UpdateMorningUI();
            }
            else
            {
                UpdateEveningUI();
            }
        }

        // 일수 표시 업데이트
        UpdateDayCounter();
    }

    #region 이벤트 핸들러

    // 아침 시작 시 UI 변경
    private void OnMorningStart()
    {
        // 아침 UI 활성화, 저녁 UI 비활성화
        if (morningUI != null) morningUI.SetActive(true);
        if (eveningUI != null) eveningUI.SetActive(false);

        // 스테이지 패널 표시
        if (panelStage != null)
        {
            panelStage.SetActive(true);

            // 일정 시간 후 자동으로 숨기기
            StartCoroutine(HidePanelAfterDelay(panelStage, 3f));
        }
    }

    // 저녁 시작 시 UI 변경
    private void OnEveningStart()
    {
        // 저녁 UI 활성화, 아침 UI 비활성화
        if (morningUI != null) morningUI.SetActive(false);
        if (eveningUI != null) eveningUI.SetActive(true);

        // 스테이지 패널 숨기기
        if (panelStage != null)
        {
            panelStage.SetActive(false);
        }
    }

    // 웨이브 종료 처리
    private void HandleWaveEnd(int waveNumber, string waveName)
    {
        // 스테이지 패널 표시
        if (panelStage != null)
        {
            panelStage.SetActive(true);

            // UI 내용 업데이트 (웨이브 정보, 경험치 획득 등)
            UpdateStagePanel(waveNumber, waveName);
        }
    }

    // 일수 변경 이벤트 핸들러
    private void OnDayChanged(int newDay)
    {
        UpdateDayCounter();
        ShowDayTransition(newDay);
    }

    // 레벨업 이벤트 핸들러
    private void OnLevelUp(int newLevel)
    {
        ShowLevelUpNotification(newLevel);
    }

    // 웨이브 승리 이벤트 핸들러
    private void OnWaveVictory()
    {
        // 웨이브 승리 시 추가 UI 표시 (필요시)
        Debug.Log("웨이브 승리!");
    }

    // 웨이브 패배 이벤트 핸들러
    private void OnWaveDefeat()
    {
        // 웨이브 패배 시 추가 UI 표시 (필요시)
        Debug.Log("웨이브 패배!");
    }

    #endregion

    #region UI 업데이트 메서드

    // 스테이지 패널 내용 업데이트
    private void UpdateStagePanel(int waveNumber, string waveName)
    {
        // 패널 내 텍스트 컴포넌트 찾기
        TextMeshProUGUI[] texts = panelStage.GetComponentsInChildren<TextMeshProUGUI>();

        foreach (TextMeshProUGUI text in texts)
        {
            // 웨이브 정보 텍스트 업데이트
            if (text.name.Contains("WaveInfo"))
            {
                text.text = $"웨이브 {waveNumber}: {waveName} 완료!";
            }
            // 경험치 획득 텍스트 업데이트
            else if (text.name.Contains("ExpGain") && playerExperience != null)
            {
                text.text = $"경험치: {playerExperience.CurrentExp} / {playerExperience.ExpRequiredForCurrentLevel}";
            }
        }
    }

    // 현재 시간에 맞게 UI 업데이트
    private void UpdateUIForCurrentTime()
    {
        if (timeSystem != null)
        {
            if (timeSystem.CurrentTime == TimeOfDay.Morning)
            {
                OnMorningStart();
            }
            else
            {
                OnEveningStart();
            }
        }
    }

    // 공통 UI 업데이트
    private void UpdateCommonUI()
    {
        // 플레이어 레벨 업데이트
        if (playerLevelText != null && playerExperience != null)
        {
            playerLevelText.text = $"LV. {playerExperience.Level}";
        }

        // 플레이어 경험치 업데이트
        if (playerExpText != null && playerExperience != null)
        {
            int currentExp = playerExperience.CurrentExp;
            int requiredExp = playerExperience.ExpRequiredForCurrentLevel;

            if (requiredExp > 0)
            {
                playerExpText.text = $"EXP: {currentExp} / {requiredExp}";

                // 경험치 슬라이더 업데이트
                if (expSlider != null)
                {
                    expSlider.value = (float)currentExp / requiredExp;
                }
            }
            else
            {
                // 최대 레벨인 경우
                playerExpText.text = $"EXP: MAX";
                if (expSlider != null)
                {
                    expSlider.value = 1f;
                }
            }
        }

        // 골드 업데이트
        if (goldText != null && playerGold != null)
        {
            goldText.text = $"Gold: {playerGold.CurrentGold}";
        }
    }

    // 아침 전용 UI 업데이트
    private void UpdateMorningUI()
    {
        // 피로도 UI 업데이트
        if (fatigueText != null && playerGold != null)
        {
            fatigueText.text = $"피로도: {(int)(playerGold.FatigueRatio * 100)}%";
        }

        // 피로도 슬라이더 업데이트
        if (fatigueSlider != null && playerGold != null)
        {
            fatigueSlider.value = playerGold.FatigueRatio;
        }
    }

    // 저녁 전용 UI 업데이트
    private void UpdateEveningUI()
    {
        // 웨이브 타이머 업데이트
        if (waveTimerText != null && waveSystem != null)
        {
            float remainingTime = waveSystem.RemainingWaveTime;
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);

            waveTimerText.text = $"남은 시간: {minutes:00}:{seconds:00}";
        }

        // 웨이브 진행도 슬라이더 업데이트
        if (waveProgressSlider != null && waveSystem != null)
        {
            waveProgressSlider.value = waveSystem.WaveProgress;
        }

        // 재화 오브젝트 체력 업데이트
        if (resourceHealthText != null && resourceManager != null)
        {
            float healthRatio = resourceManager.TotalHealthRatio;
            resourceHealthText.text = $"자원 상태: {(int)(healthRatio * 100)}%";

            // 위험 수준에 따라 색상 변경
            if (healthRatio < 0.3f)
            {
                resourceHealthText.color = Color.red;
            }
            else if (healthRatio < 0.6f)
            {
                resourceHealthText.color = Color.yellow;
            }
            else
            {
                resourceHealthText.color = Color.green;
            }
        }

        // 재화 오브젝트 체력 슬라이더 업데이트
        if (resourceHealthSlider != null && resourceManager != null)
        {
            resourceHealthSlider.value = resourceManager.TotalHealthRatio;
        }
    }

    // 일수 표시 업데이트
    private void UpdateDayCounter()
    {
        if (dayCounterText != null && dayCounterSystem != null)
        {
            dayCounterText.text = $"Day {dayCounterSystem.CurrentDay}";
        }
    }

    #endregion

    #region 알림 및 UI 효과

    // 일수 전환 표시
    private void ShowDayTransition(int day)
    {
        if (dayTransitionPanel == null || dayTransitionText == null) return;

        // 전환 텍스트 설정
        dayTransitionText.text = $"Day {day}";

        // 패널 활성화
        dayTransitionPanel.SetActive(true);

        // 일정 시간 후 패널 숨기기
        StartCoroutine(HidePanelAfterDelay(dayTransitionPanel, 3f));
    }

    // 레벨업 알림 표시
    public void ShowLevelUpNotification(int level)
    {
        if (levelUpPanel == null || levelUpText == null) return;

        // 레벨업 텍스트 설정
        levelUpText.text = $"Level Up!\nLevel {level} 달성!";

        // 패널 활성화
        levelUpPanel.SetActive(true);

        // 일정 시간 후 패널 숨기기
        StartCoroutine(HidePanelAfterDelay(levelUpPanel, levelUpNotificationTime));
    }

    // 패널을 일정 시간 후 숨기는 코루틴
    private IEnumerator HidePanelAfterDelay(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    #endregion
}