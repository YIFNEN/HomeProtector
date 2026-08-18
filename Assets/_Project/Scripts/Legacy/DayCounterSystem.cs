using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

// 게임 내 일수를 관리하는 시스템
public class DayCounterSystem : MonoBehaviour
{
    [SerializeField] private int currentDay = 1; // 현재 일수 (1일차부터 시작)
    [SerializeField] private TextMeshProUGUI dayCounterText; // 일수 표시 텍스트
    [SerializeField] private GameObject dayTransitionPanel; // 일수 전환 패널 (새 날 시작 시 표시)
    [SerializeField] private TextMeshProUGUI dayTransitionText; // 일수 전환 텍스트
    [SerializeField] private float transitionDisplayTime = 3f; // 전환 패널 표시 시간

    // 일수 증가 이벤트
    [System.Serializable]
    public class DayChangeEvent : UnityEvent<int> { }

    public DayChangeEvent onDayChanged = new DayChangeEvent();

    // 시간 시스템 참조
    private TimeSystem timeSystem;

    // 현재 일수 프로퍼티
    public int CurrentDay => currentDay;

    private void Awake()
    {
        // 시간 시스템 찾기
        timeSystem = FindObjectOfType<TimeSystem>();

        // 일수 표시 UI 초기화
        UpdateDayCounterUI();
    }

    private void Start()
    {
        // 시간 시스템 이벤트 구독
        if (timeSystem != null)
        {
            timeSystem.onMorningStart.AddListener(OnMorningStart);
        }

        // 전환 패널 초기 비활성화
        if (dayTransitionPanel != null)
        {
            dayTransitionPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (timeSystem != null)
        {
            timeSystem.onMorningStart.RemoveListener(OnMorningStart);
        }
    }

    // 아침 모드 시작 시 호출 (날이 바뀌는 시점)
    private void OnMorningStart()
    {
        // 첫 번째 아침은 1일차 시작으로 간주하고 일수를 증가시키지 않음
        // 첫 번째 아침 여부는 웨이브 번호로 판단 (웨이브가 완료되었으면 일수 증가)
        WaveSystem waveSystem = FindObjectOfType<WaveSystem>();
        if (waveSystem != null && waveSystem.CurrentWave > 1)
        {
            // 일수 증가
            IncrementDay();
        }
    }

    // 일수 증가 메소드
    public void IncrementDay()
    {
        currentDay++;

        // 일수 변경 이벤트 발생
        onDayChanged.Invoke(currentDay);

        // UI 업데이트
        UpdateDayCounterUI();

        // 전환 패널 표시
        ShowDayTransitionPanel();

        Debug.Log($"새로운 하루 시작! 현재 {currentDay}일차");
    }

    // 일수 직접 설정 메소드
    public void SetDay(int day)
    {
        if (day < 1)
        {
            Debug.LogWarning("일수는 1 이상이어야 합니다.");
            return;
        }

        int previousDay = currentDay;
        currentDay = day;

        // 일수가 변경된 경우에만 이벤트 발생
        if (previousDay != currentDay)
        {
            onDayChanged.Invoke(currentDay);
            UpdateDayCounterUI();
        }
    }

    // 일수 표시 UI 업데이트
    private void UpdateDayCounterUI()
    {
        if (dayCounterText != null)
        {
            dayCounterText.text = $"Day {currentDay}";
        }
    }

    // 일수 전환 패널 표시
    private void ShowDayTransitionPanel()
    {
        if (dayTransitionPanel == null || dayTransitionText == null) return;

        // 전환 텍스트 설정
        dayTransitionText.text = $"Day {currentDay}";

        // 패널 활성화
        dayTransitionPanel.SetActive(true);

        // 일정 시간 후 패널 숨기기
        StartCoroutine(HidePanelAfterDelay(transitionDisplayTime));
    }

    // 패널을 일정 시간 후 숨기는 코루틴
    private IEnumerator HidePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (dayTransitionPanel != null)
        {
            dayTransitionPanel.SetActive(false);
        }
    }
}