using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

// 웨이브 승리/패배 조건 및 결과 처리 시스템
public class WaveResultSystem : MonoBehaviour
{
    [Header("Victory/Defeat Settings")]
    [SerializeField] private float healthRatioDefeatThreshold = 0.3f; // 패배 조건: 체력 비율 임계값 (기본 30%)
    [SerializeField] private float victoryRewardMultiplier = 1.5f; // 승리 시 보상 배율

    [Header("UI Elements")]
    [SerializeField] private GameObject resultPanel; // 결과 패널
    [SerializeField] private TextMeshProUGUI resultTitle; // 결과 제목 (승리/패배)
    [SerializeField] private TextMeshProUGUI resultDescription; // 결과 설명
    [SerializeField] private TextMeshProUGUI rewardText; // 보상 정보 텍스트

    [Header("Sound Effects")]
    [SerializeField] private AudioClip victorySound; // 승리 효과음
    [SerializeField] private AudioClip defeatSound; // 패배 효과음

    // 결과 이벤트
    public UnityEvent onVictory = new UnityEvent();
    public UnityEvent onDefeat = new UnityEvent();

    // 시스템 참조
    private TimeSystem timeSystem;
    private WaveSystem waveSystem;
    private ResourceManager resourceManager;
    private PlayerGold playerGold;
    private PlayerExperience playerExperience;
    private AudioSource audioSource;

    // 웨이브 상태 추적
    private bool isWaveActive = false;
    private bool isWaveCompleted = false;
    private bool isHumanDestroyed = false;
    private bool isHealthBelowThreshold = false;

    // 보상 정보 저장
    private int baseGoldReward = 0;
    private int baseExpReward = 0;

    private void Awake()
    {
        // 시스템 컴포넌트 찾기
        timeSystem = FindObjectOfType<TimeSystem>();
        waveSystem = FindObjectOfType<WaveSystem>();
        resourceManager = FindObjectOfType<ResourceManager>();
        playerGold = FindObjectOfType<PlayerGold>();
        playerExperience = FindObjectOfType<PlayerExperience>();

        // 오디오 소스 컴포넌트 가져오기/추가
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (victorySound != null || defeatSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // UI 초기화
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // 이벤트 구독
        if (timeSystem != null)
        {
            timeSystem.onEveningStart.AddListener(OnEveningStart);
            timeSystem.onMorningStart.AddListener(OnMorningStart);
        }

        if (waveSystem != null)
        {
            waveSystem.OnWaveStart += HandleWaveStart;
            waveSystem.OnWaveEnd += HandleWaveEnd;
        }

        // 재화 오브젝트 파괴 이벤트 구독
        StartCoroutine(SubscribeToResourceObjects());
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (timeSystem != null)
        {
            timeSystem.onEveningStart.RemoveListener(OnEveningStart);
            timeSystem.onMorningStart.RemoveListener(OnMorningStart);
        }

        if (waveSystem != null)
        {
            waveSystem.OnWaveStart -= HandleWaveStart;
            waveSystem.OnWaveEnd -= HandleWaveEnd;
        }
    }

    // Update: 지속적으로 패배 조건 체크
    private void Update()
    {
        if (isWaveActive && !isWaveCompleted)
        {
            // 패배 조건 1: 재화 오브젝트 체력 비율이 임계값 미만
            if (resourceManager != null && resourceManager.TotalHealthRatio < healthRatioDefeatThreshold)
            {
                isHealthBelowThreshold = true;
                HandleDefeat("재화 오브젝트 손상 심각");
            }

            // 패배 조건 2: 이미 체크됨 (Human 태그 오브젝트 파괴 이벤트에서)
        }
    }

    // 재화 오브젝트 이벤트 구독 (조금 지연하여 모든 오브젝트가 로드된 후 실행)
    private IEnumerator SubscribeToResourceObjects()
    {
        yield return new WaitForSeconds(0.5f);

        // 씬의 모든 ResourceObject 찾기
        ResourceObject[] resourceObjects = FindObjectsOfType<ResourceObject>();

        foreach (ResourceObject resource in resourceObjects)
        {
            // Human 태그를 가진 리소스 확인
            if (resource.gameObject.CompareTag("Human"))
            {
                // Human 리소스 파괴 이벤트 구독
                resource.onDestroyed.AddListener(() => OnHumanResourceDestroyed(resource));
            }
        }

        Debug.Log($"재화 오브젝트 이벤트 구독 완료: {resourceObjects.Length}개");
    }

    // Human 태그 재화 파괴 시 호출
    private void OnHumanResourceDestroyed(ResourceObject resource)
    {
        if (isWaveActive && !isWaveCompleted)
        {
            isHumanDestroyed = true;
            HandleDefeat($"중요 자원 '{resource.ResourceName}' 파괴됨");
        }
    }

    // 저녁 모드 시작 시 호출
    private void OnEveningStart()
    {
        isWaveActive = true;
        isWaveCompleted = false;
        isHumanDestroyed = false;
        isHealthBelowThreshold = false;

        // 기본 보상 금액 계산 (웨이브 시작 시점)
        CalculateBaseRewards();

        Debug.Log("전투 시작: 웨이브 결과 모니터링 시작");
    }

    // 아침 모드 시작 시 호출
    private void OnMorningStart()
    {
        isWaveActive = false;
    }

    // 웨이브 시작 시 호출
    private void HandleWaveStart(int waveNumber, string waveName)
    {
        // 웨이브 시작 시 상태 리셋
        isWaveCompleted = false;
        isHumanDestroyed = false;
        isHealthBelowThreshold = false;
    }

    // 웨이브 종료 시 호출
    private void HandleWaveEnd(int waveNumber, string waveName)
    {
        isWaveCompleted = true;

        // 승패 확인
        if (!isHumanDestroyed && !isHealthBelowThreshold)
        {
            // 모든 패배 조건을 충족하지 않았으므로 승리
            HandleVictory();
        }
        // 패배는 이미 업데이트에서 처리됨
    }

    // 승리 처리
    private void HandleVictory()
    {
        Debug.Log("웨이브 승리!");

        // 보상 지급
        int goldReward = Mathf.RoundToInt(baseGoldReward * victoryRewardMultiplier);
        int expReward = Mathf.RoundToInt(baseExpReward * victoryRewardMultiplier);

        // 골드 지급
        if (playerGold != null)
        {
            playerGold.CurrentGold += goldReward;
        }

        // 경험치 지급 (기본 경험치는 WaveSystem에서 처리)
        if (playerExperience != null)
        {
            // 추가 경험치 보너스 (기본의 0.5배)
            int bonusExp = Mathf.RoundToInt(baseExpReward * (victoryRewardMultiplier - 1.0f));
            playerExperience.AddExperience(bonusExp);
        }

        // 승리 효과음 재생
        if (audioSource != null && victorySound != null)
        {
            audioSource.PlayOneShot(victorySound);
        }

        // 승리 UI 표시
        ShowResultUI(true, "웨이브 승리!",
            $"모든 중요 자원을 지켜냈습니다.\n현재 자원 상태: {Mathf.RoundToInt(resourceManager.TotalHealthRatio * 100)}%",
            $"보상: {goldReward} 골드 (+{Mathf.RoundToInt(baseGoldReward * (victoryRewardMultiplier - 1.0f))} 보너스)\n경험치: {expReward} (+{Mathf.RoundToInt(baseExpReward * (victoryRewardMultiplier - 1.0f))} 보너스)");

        // 승리 이벤트 발생
        onVictory.Invoke();
    }

    // 패배 처리
    private void HandleDefeat(string reason)
    {
        // 이미 처리된 경우 중복 실행 방지
        if (isWaveCompleted) return;

        Debug.Log($"웨이브 패배: {reason}");
        isWaveCompleted = true;

        // 기본 보상만 지급 (배율 없음)
        int goldReward = baseGoldReward;
        int expReward = baseExpReward;

        // 골드 지급
        if (playerGold != null)
        {
            playerGold.CurrentGold += goldReward;
        }

        // 경험치는 WaveSystem에서 처리

        // 패배 효과음 재생
        if (audioSource != null && defeatSound != null)
        {
            audioSource.PlayOneShot(defeatSound);
        }

        // 패배 UI 표시
        ShowResultUI(false, "웨이브 패배!",
            $"패배 원인: {reason}\n현재 자원 상태: {Mathf.RoundToInt(resourceManager.TotalHealthRatio * 100)}%",
            $"보상: {goldReward} 골드\n경험치: {expReward}");

        // 패배 이벤트 발생
        onDefeat.Invoke();

        // 웨이브 강제 종료 (필요시)
        if (waveSystem != null && !isWaveCompleted)
        {
            // 여기에 웨이브 강제 종료 로직 추가 (WaveSystem에 메소드 필요)
        }
    }

    // 기본 보상 계산
    private void CalculateBaseRewards()
    {
        // 기본 골드 보상 = 현재 웨이브 * 10 + 50
        if (waveSystem != null)
        {
            baseGoldReward = waveSystem.CurrentWave * 10 + 50;
        }
        else
        {
            baseGoldReward = 50; // 기본값
        }

        // 기본 경험치 보상 = 현재 웨이브 * 15 + 30
        if (waveSystem != null)
        {
            baseExpReward = waveSystem.CurrentWave * 15 + 30;
        }
        else
        {
            baseExpReward = 30; // 기본값
        }
    }

    // 결과 UI 표시
    private void ShowResultUI(bool isVictory, string title, string description, string rewardInfo)
    {
        if (resultPanel == null) return;

        // 패널 활성화
        resultPanel.SetActive(true);

        // 텍스트 설정
        if (resultTitle != null)
        {
            resultTitle.text = title;
            resultTitle.color = isVictory ? Color.green : Color.red;
        }

        if (resultDescription != null)
        {
            resultDescription.text = description;
        }

        if (rewardText != null)
        {
            rewardText.text = rewardInfo;
        }

        // 시간 지연 후 패널 숨기기
        StartCoroutine(HideResultPanel(5f));
    }

    // 결과 패널 숨기기
    private IEnumerator HideResultPanel(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }
}