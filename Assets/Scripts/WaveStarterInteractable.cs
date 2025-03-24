using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveStarterInteractable : MonoBehaviour
{
    [Header("시스템 참조")]
    [SerializeField] private WaveSystem waveSystem;
    [SerializeField] private TimeSystem timeSystem;

    [Header("설정")]
    [SerializeField] private float interactionRadius = 2f; // 상호작용 가능 반경
    [SerializeField] private KeyCode interactionKey = KeyCode.Space; // 상호작용 키
    [SerializeField] private string playerTag = "Player"; // 플레이어 태그
    [SerializeField] private bool showDebugMessages = true; // 디버그 메시지 표시 여부

    [Header("시각적 효과")]
    [SerializeField] private GameObject interactionIndicator; // 상호작용 가능 표시기
    [SerializeField] private float pulseSpeed = 1.5f; // 표시기 맥박 속도
    [SerializeField] private float pulseScale = 0.2f; // 표시기 맥박 크기

    [Header("UI 요소")]
    [SerializeField] private GameObject interactionPrompt; // 상호작용 안내 UI
    [SerializeField] private TextMeshProUGUI promptText; // 안내 텍스트
    [SerializeField] private string defaultPromptText = "스페이스바를 눌러 밤을 시작하세요"; // 기본 안내 텍스트

    private bool playerInRange = false;
    private Transform playerTransform;
    private bool canInteract = true;

    private void Start()
    {
        // 시스템 참조 초기화
        InitializeReferences();

        // UI 초기화
        HideInteractionUI();

        // 상호작용 표시기 초기화
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(false);
        }
    }

    private void InitializeReferences()
    {
        // WaveSystem 참조 찾기
        if (waveSystem == null)
        {
            waveSystem = FindObjectOfType<WaveSystem>();
            if (waveSystem == null)
            {
                LogDebug("WaveSystem을 찾을 수 없습니다.");
            }
        }

        // TimeSystem 참조 찾기
        if (timeSystem == null)
        {
            timeSystem = FindObjectOfType<TimeSystem>();
            if (timeSystem == null)
            {
                LogDebug("TimeSystem을 찾을 수 없습니다.");
            }
        }

        // 프롬프트 텍스트 설정
        if (promptText != null)
        {
            promptText.text = defaultPromptText;
        }
    }

    private void Update()
    {
        // 플레이어가 범위 내에 있고 상호작용 가능한 상태인지 확인
        if (playerInRange && canInteract)
        {
            if (Input.GetKeyDown(interactionKey))
            {
                StartNightPhase();
            }
        }

        // 플레이어 검색 및 범위 체크
        DetectPlayerInRange();

        // 표시기 애니메이션 업데이트
        UpdateIndicatorAnimation();
    }

    // 플레이어 검색 및 범위 체크
    private void DetectPlayerInRange()
    {
        // 플레이어가 이미 범위 내에 있는 경우에는 위치 업데이트만
        if (playerInRange && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance > interactionRadius)
            {
                // 범위를 벗어남
                playerInRange = false;
                HideInteractionUI();
            }
            return;
        }

        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (distance <= interactionRadius)
            {
                // 플레이어가 범위 내에 들어옴
                playerInRange = true;
                ShowInteractionUI();
            }
            else
            {
                playerInRange = false;
                HideInteractionUI();
            }
        }
    }

    // 상호작용 UI 표시
    private void ShowInteractionUI()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
        }

        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(true);
        }
    }

    // 상호작용 UI 숨기기
    private void HideInteractionUI()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }

        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(false);
        }
    }

    // 표시기 애니메이션 업데이트
    private void UpdateIndicatorAnimation()
    {
        if (interactionIndicator != null && interactionIndicator.activeSelf)
        {
            // 맥박 효과 (크기 변경)
            float pulse = 1f + pulseScale * Mathf.Sin(Time.time * pulseSpeed);
            interactionIndicator.transform.localScale = new Vector3(pulse, pulse, pulse);
        }
    }

    // 밤 단계 시작
    private void StartNightPhase()
    {
        if (!canInteract) return;

        LogDebug("플레이어가 밤 시작 트리거 작동");

        // 상호작용 제한 (중복 방지)
        canInteract = false;

        // 웨이브 시작
        if (waveSystem != null)
        {
            waveSystem.StartWave();
            LogDebug("WaveSystem.StartWave() 호출됨");
        }

        // TimeSystem이 있는 경우 저녁으로 전환
        if (timeSystem != null)
        {
            // 저녁으로 전환 (TransitionToEvening 코루틴 사용)
            StartCoroutine(timeSystem.TransitionToEvening());
            LogDebug("TimeSystem.TransitionToEvening() 호출됨");
        }

        // 상호작용 UI 숨기기
        HideInteractionUI();

        // 상호작용 표시기 비활성화 (다음 아침까지)
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(false);
        }
    }

    // 일시 정지 해제 (다음 아침에 호출)
    public void ResetInteraction()
    {
        canInteract = true;
        LogDebug("상호작용 가능 상태 초기화됨");
    }

    // 디버그 로그 출력
    private void LogDebug(string message)
    {
        if (showDebugMessages)
        {
            Debug.Log($"[WaveStarter] {message}");
        }
    }

    // 기즈모로 상호작용 범위 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }

    // 이벤트 구독 (TimeSystem의 아침 시작 이벤트에 구독)
    private void OnEnable()
    {
        if (timeSystem != null)
        {
            timeSystem.onMorningStart.AddListener(OnMorningStart);
        }
    }

    // 이벤트 구독 해제
    private void OnDisable()
    {
        if (timeSystem != null)
        {
            timeSystem.onMorningStart.RemoveListener(OnMorningStart);
        }
    }

    // 아침 시작 이벤트 핸들러
    private void OnMorningStart()
    {
        // 아침이 되면 상호작용 가능 상태로 초기화
        ResetInteraction();
    }
}