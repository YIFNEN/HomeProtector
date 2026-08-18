using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextTMPViewer : MonoBehaviour
{
    [Header("기본 UI 요소")]
    [SerializeField] private TextMeshProUGUI textPlayerHP;
    [SerializeField] private ResourceManager healthRatio;
    [SerializeField] private TextMeshProUGUI textPlayerGold;
    [SerializeField] private TextMeshProUGUI textWave;
    [SerializeField] private TextMeshProUGUI textEnemyCount;

    [Header("추가된 UI 요소")]
    [SerializeField] private TextMeshProUGUI textWaveTime; // 웨이브 남은 시간 표시용
    [SerializeField] private TextMeshProUGUI textPlayerLevel; // 플레이어 레벨 표시용
    [SerializeField] private TextMeshProUGUI textPlayerExp; // 플레이어 경험치 표시용
    [SerializeField] private TextMeshProUGUI textPlayerFatigue; // 플레이어 피로도 표시용
    [SerializeField] private Slider expSlider; // 경험치 진행도 슬라이더
    [SerializeField] private Slider fatigueSlider; // 피로도 슬라이더

    [Header("시스템 참조")]
    [SerializeField] private PlayerGold playerGold;
    [SerializeField] private WaveSystem waveSystem;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private TimeSystem timeSystem; // 시간 시스템 참조
    [SerializeField] private PlayerExperience playerExperience; // 플레이어 경험치 참조

    [Header("스크롤 뷰 설정")]
    [SerializeField] private GameObject scrollView; // 스크롤 뷰 오브젝트
    [SerializeField] private Button toggleScrollViewButton; // 스크롤 뷰 토글 버튼
    [SerializeField] private Button closeScrollViewButton; // 스크롤 뷰 닫기 버튼

    [Header("낮/밤 UI 설정")]
    [SerializeField] private GameObject morningOnlyUI; // 아침에만 표시되는 UI
    [SerializeField] private GameObject eveningOnlyUI; // 저녁에만 표시되는 UI
    [SerializeField] private Color dayTextColor = Color.black; // 낮 텍스트 색상
    [SerializeField] private Color nightTextColor = Color.white; // 밤 텍스트 색상
    [SerializeField] private Image[] backgroundElements; // 색상 변경이 필요한 배경 UI 요소
    [SerializeField] private Color dayBackgroundColor = new Color(0.9f, 0.9f, 0.9f, 0.8f); // 낮 배경 색상
    [SerializeField] private Color nightBackgroundColor = new Color(0.2f, 0.2f, 0.3f, 0.8f); // 밤 배경 색상

    [Header("카메라 설정")]
    [SerializeField] private Camera mainCamera; // 메인 카메라 참조
    [SerializeField] private Vector3 morningCameraPosition = new Vector3(0, 0, -10); // 아침 카메라 위치
    [SerializeField] private Vector3 eveningCameraPosition = new Vector3(0, 0, -10); // 저녁 카메라 위치
    [SerializeField] private Color dayCameraColor = Color.cyan; // 낮 배경색
    [SerializeField] private Color nightCameraColor = Color.black; // 밤 배경색
    [SerializeField] private float cameraMoveSpeed = 2.0f; // 카메라 이동 속도

    // 색상 전환을 위한 설정
    [SerializeField] private float colorTransitionSpeed = 1.0f; // 색상 전환 속도
    private Coroutine colorTransitionCoroutine;

    private void Awake()
    {
        // 시스템 컴포넌트 찾기
        if (timeSystem == null) timeSystem = FindObjectOfType<TimeSystem>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (waveSystem == null) waveSystem = FindObjectOfType<WaveSystem>();
        if (playerGold == null) playerGold = FindObjectOfType<PlayerGold>();
        if (playerExperience == null) playerExperience = FindObjectOfType<PlayerExperience>();
        if (enemySpawner == null) enemySpawner = FindObjectOfType<EnemySpawner>();
        if (healthRatio == null) healthRatio = FindObjectOfType<ResourceManager>();

        // 초기 UI 설정
        if (scrollView != null)
        {
            scrollView.SetActive(false);
        }

        UpdateUIBasedOnTime(timeSystem != null ? timeSystem.CurrentTime : TimeOfDay.Morning);
    }

    private void Start()
    {
        // 버튼 이벤트 설정
        if (toggleScrollViewButton != null)
        {
            toggleScrollViewButton.onClick.AddListener(ToggleScrollView);
        }

        if (closeScrollViewButton != null)
        {
            closeScrollViewButton.onClick.AddListener(CloseScrollView);
        }

        // 시간 시스템 이벤트 구독
        if (timeSystem != null)
        {
            timeSystem.onMorningStart.AddListener(OnMorningStart);
            timeSystem.onEveningStart.AddListener(OnEveningStart);
        }

        // 플레이어 경험치 이벤트 구독
        if (playerExperience != null)
        {
            playerExperience.onLevelUp.AddListener(OnLevelUp);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (timeSystem != null)
        {
            timeSystem.onMorningStart.RemoveListener(OnMorningStart);
            timeSystem.onEveningStart.RemoveListener(OnEveningStart);
        }

        if (playerExperience != null)
        {
            playerExperience.onLevelUp.RemoveListener(OnLevelUp);
        }
    }

    void Update()
    {
        // 기본 정보 업데이트
        UpdateBasicInfo();

        // 추가 정보 업데이트 (웨이브 시간, 피로도, 레벨, 경험치)
        UpdateAdvancedInfo();

        // ESC 키를 눌렀을 때 스크롤 뷰 토글
        if (Input.GetKeyDown(KeyCode.Tab) && scrollView != null)
        {
            ToggleScrollView();
        }
    }

    // 기본 정보 업데이트
    private void UpdateBasicInfo()
    {
        // 체력 비율을 퍼센트로 표시 (예: "HP: 75%")
        if (textPlayerHP != null && healthRatio != null)
        {
            textPlayerHP.text = "HP: " + (healthRatio.TotalHealthRatio * 100).ToString("0") + "%";
        }

        // 골드 업데이트
        if (textPlayerGold != null && playerGold != null)
        {
            textPlayerGold.text = "Gold: " + playerGold.CurrentGold.ToString();
        }

        // 웨이브 정보 업데이트
        if (textWave != null && waveSystem != null)
        {
            textWave.text = "Wave: " + waveSystem.CurrentWave + "/" + waveSystem.MaxWave;
        }

        // 적 카운트 업데이트
        if (textEnemyCount != null && enemySpawner != null)
        {
            textEnemyCount.text = "Enemy: " + enemySpawner.CurrentEnemyCount ;
        }// 적 카운트 업데이트
        if (textEnemyCount != null && enemySpawner != null && waveSystem != null)
        {
            int totalEnemies = 0;

            // 현재 웨이브의 총 적 수 계산
            if (waveSystem.CurrentWave > 0 && waveSystem.CurrentWave <= waveSystem.MaxWave)
            {
                Wave currentWave = waveSystem.GetCurrentWaveInfo();

                // 적 그룹별로 총 적 수 계산
                foreach (var enemyGroup in currentWave.enemyGroups)
                {
                    if (enemyGroup.enemyPrefab != null)
                    {
                        totalEnemies += enemyGroup.count;
                    }
                }
            }

            // 현재 적 수 / 총 적 수 형태로 표시
            textEnemyCount.text = $"Enemy: {enemySpawner.CurrentEnemyCount}/{totalEnemies}";
        }
    }

    // 추가 정보 업데이트 (웨이브 시간, 피로도, 레벨, 경험치)
    private void UpdateAdvancedInfo()
    {
        // 웨이브 남은 시간 업데이트
        if (textWaveTime != null && waveSystem != null)
        {
            float remainingTime = waveSystem.RemainingWaveTime;
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            textWaveTime.text = $"Time: {minutes:00}:{seconds:00}";
        }

        // 플레이어 레벨 업데이트
        if (textPlayerLevel != null && playerExperience != null)
        {
            textPlayerLevel.text = $"Level: {playerExperience.Level}";
        }

        // 플레이어 경험치 업데이트
        if (textPlayerExp != null && playerExperience != null)
        {
            int currentExp = playerExperience.CurrentExp;
            int requiredExp = playerExperience.ExpRequiredForCurrentLevel;

            if (requiredExp > 0)
            {
                textPlayerExp.text = $"EXP: {currentExp}/{requiredExp}";

                // 경험치 슬라이더 업데이트
                if (expSlider != null)
                {
                    expSlider.value = (float)currentExp / requiredExp;
                }
            }
            else
            {
                // 최대 레벨인 경우
                textPlayerExp.text = "EXP: MAX";
                if (expSlider != null)
                {
                    expSlider.value = 1.0f;
                }
            }
        }

        // 플레이어 피로도 업데이트
        if (textPlayerFatigue != null && playerGold != null)
        {
            float fatiguePercentage = playerGold.FatigueRatio * 100;
            textPlayerFatigue.text = $"Fatigue: {fatiguePercentage:0}%";

            // 피로도 색상 설정 (피로도에 따라 색상 변경)
            if (fatiguePercentage < 30)
            {
                textPlayerFatigue.color = Color.green;
            }
            else if (fatiguePercentage < 70)
            {
                textPlayerFatigue.color = Color.yellow;
            }
            else
            {
                textPlayerFatigue.color = Color.red;
            }

            // 피로도 슬라이더 업데이트
            if (fatigueSlider != null)
            {
                fatigueSlider.value = playerGold.FatigueRatio;
            }
        }
    }

    // 레벨업 이벤트 처리
    private void OnLevelUp(int newLevel)
    {
        // 레벨업 시 효과 (필요시)
        Debug.Log($"플레이어 레벨업! 레벨 {newLevel} 달성");

        // 레벨업 시 깜빡이는 효과 등 추가 가능
        if (textPlayerLevel != null)
        {
            StartCoroutine(FlashText(textPlayerLevel));
        }
    }

    // 텍스트 깜빡이는 효과
    private IEnumerator FlashText(TextMeshProUGUI text)
    {
        Color originalColor = text.color;

        for (int i = 0; i < 5; i++)
        {
            text.color = Color.yellow;
            yield return new WaitForSeconds(0.1f);
            text.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }

    #region 스크롤 뷰 관리

    // 스크롤 뷰 토글
    public void ToggleScrollView()
    {
        if (scrollView != null)
        {
            scrollView.SetActive(!scrollView.activeSelf);
        }
    }

    // 스크롤 뷰 닫기
    public void CloseScrollView()
    {
        if (scrollView != null)
        {
            scrollView.SetActive(false);
        }
    }

    #endregion

    #region 낮/밤 전환 관리

    // 아침 시작 시 호출되는 메서드
    private void OnMorningStart()
    {
        UpdateUIBasedOnTime(TimeOfDay.Morning);
        MoveCameraToPosition(morningCameraPosition);
        TransitionCameraColor(dayCameraColor);
    }

    // 저녁 시작 시 호출되는 메서드
    private void OnEveningStart()
    {
        UpdateUIBasedOnTime(TimeOfDay.Evening);
        MoveCameraToPosition(eveningCameraPosition);
        TransitionCameraColor(nightCameraColor);
    }

    // 시간에 따른 UI 업데이트
    private void UpdateUIBasedOnTime(TimeOfDay time)
    {
        // 시간에 따른 UI 표시/숨김
        if (morningOnlyUI != null)
        {
            morningOnlyUI.SetActive(time == TimeOfDay.Morning);
        }

        if (eveningOnlyUI != null)
        {
            eveningOnlyUI.SetActive(time == TimeOfDay.Evening);
        }

        // 텍스트 색상 전환
        Color targetTextColor = (time == TimeOfDay.Morning) ? dayTextColor : nightTextColor;
        Color targetBackgroundColor = (time == TimeOfDay.Morning) ? dayBackgroundColor : nightBackgroundColor;

        // UI 텍스트 색상 변경
        UpdateTextColors(targetTextColor);

        // 배경 UI 요소 색상 변경
        UpdateBackgroundColors(targetBackgroundColor);
    }

    // 모든 텍스트 색상 업데이트
    private void UpdateTextColors(Color targetColor)
    {
        // TextMeshProUGUI 컴포넌트 찾기
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI text in texts)
        {
            // 피로도 텍스트는 색상 변경에서 제외 (이미 피로도에 따라 색상이 변경됨)
            if (text == textPlayerFatigue) continue;

            // 즉시 변경 대신 부드러운 전환을 위해 코루틴 사용
            StartCoroutine(TransitionTextColor(text, targetColor));
        }
    }

    // 배경 색상 업데이트
    private void UpdateBackgroundColors(Color targetColor)
    {
        if (backgroundElements == null || backgroundElements.Length == 0) return;

        foreach (Image bg in backgroundElements)
        {
            if (bg != null)
            {
                StartCoroutine(TransitionImageColor(bg, targetColor));
            }
        }
    }

    // 텍스트 색상 전환 코루틴
    private IEnumerator TransitionTextColor(TextMeshProUGUI text, Color targetColor)
    {
        Color startColor = text.color;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * colorTransitionSpeed;
            text.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        text.color = targetColor;
    }

    // 이미지 색상 전환 코루틴
    private IEnumerator TransitionImageColor(Image image, Color targetColor)
    {
        Color startColor = image.color;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * colorTransitionSpeed;
            image.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        image.color = targetColor;
    }

    #endregion

    #region 카메라 관리

    // 카메라 위치 이동
    private void MoveCameraToPosition(Vector3 targetPosition)
    {
        if (mainCamera == null) return;

        // 코루틴을 사용하여 부드럽게 이동
        StartCoroutine(MoveCamera(targetPosition));
    }

    // 카메라 색상 전환
    private void TransitionCameraColor(Color targetColor)
    {
        if (mainCamera == null) return;

        // 이전 코루틴이 실행 중이면 중지
        if (colorTransitionCoroutine != null)
        {
            StopCoroutine(colorTransitionCoroutine);
        }

        // 새 코루틴 시작
        colorTransitionCoroutine = StartCoroutine(ChangeCameraBackgroundColor(targetColor));
    }

    // 카메라 이동 코루틴
    private IEnumerator MoveCamera(Vector3 targetPosition)
    {
        Vector3 startPosition = mainCamera.transform.position;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * cameraMoveSpeed;
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        mainCamera.transform.position = targetPosition;
    }

    // 카메라 배경색 변경 코루틴
    private IEnumerator ChangeCameraBackgroundColor(Color targetColor)
    {
        Color startColor = mainCamera.backgroundColor;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * colorTransitionSpeed;
            mainCamera.backgroundColor = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        mainCamera.backgroundColor = targetColor;
        colorTransitionCoroutine = null;
    }

    #endregion

    // 디버그용 시간 토글 메서드
    public void ToggleTimeOfDay()
    {
        if (timeSystem != null)
        {
            timeSystem.ToggleTimeOfDay();
        }
        else
        {
            Debug.LogWarning("TimeSystem이 없어 시간을 토글할 수 없습니다.");
        }
    }
}