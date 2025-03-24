using UnityEngine;
using System.Collections;

public class SpriteSwitcher : MonoBehaviour
{
    [Header("스프라이트 설정")]
    [SerializeField] private SpriteRenderer targetSpriteRenderer; // 변경할 스프라이트 렌더러
    [SerializeField] private Sprite defaultSprite; // 기본 스프라이트 (플레이어 비활성화 상태)
    [SerializeField] private Sprite activatedSprite; // 활성화 스프라이트 (플레이어 활성화 상태)
    [SerializeField] private bool useTimeOfDaySprites = false; // 시간에 따른 스프라이트 사용 여부

    [Header("시간에 따른 스프라이트 설정")]
    [SerializeField] private Sprite morningSprite; // 아침 스프라이트
    [SerializeField] private Sprite eveningSprite; // 저녁 스프라이트

    [Header("애니메이션 설정")]
    [SerializeField] private bool useTransitionEffect = true; // 전환 효과 사용 여부
    [SerializeField] private float transitionDuration = 0.5f; // 전환 효과 지속 시간
    [SerializeField] private bool usePulseOnActivation = true; // 활성화 시 맥박 효과 사용 여부
    [SerializeField] private float pulseScale = 1.2f; // 맥박 스케일
    [SerializeField] private float pulseDuration = 0.5f; // 맥박 지속 시간

    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = false; // 디버그 로그 표시 여부

    // 참조
    private MicrophoneSystem microphoneSystem;
    private TimeSystem timeSystem;

    // 내부 변수
    private bool isPlayerActive = false;
    private Coroutine pulseCoroutine;
    private Coroutine transitionCoroutine;
    private Vector3 originalScale;

    private void Awake()
    {
        // 타겟 스프라이트 렌더러가 없으면 이 오브젝트에서 찾기
        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        // 원래 스케일 저장
        originalScale = transform.localScale;
    }

    private void Start()
    {
        // 시스템 참조 찾기
        FindSystems();

        // 이벤트 구독
        SubscribeToEvents();

        // 초기 스프라이트 설정
        UpdateSpriteBasedOnState();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        UnsubscribeFromEvents();
    }

    private void FindSystems()
    {
        // MicrophoneSystem 참조 찾기
        microphoneSystem = FindObjectOfType<MicrophoneSystem>();
        if (microphoneSystem == null && showDebugLogs)
        {
            Debug.LogWarning("MicrophoneSystem을 찾을 수 없습니다.");
        }

        // TimeSystem 참조 찾기 (시간에 따른 스프라이트 사용 시)
        if (useTimeOfDaySprites)
        {
            timeSystem = FindObjectOfType<TimeSystem>();
            if (timeSystem == null && showDebugLogs)
            {
                Debug.LogWarning("TimeSystem을 찾을 수 없습니다.");
            }
        }
    }

    private void SubscribeToEvents()
    {
        if (microphoneSystem != null)
        {
            // 플레이어 활성화/비활성화 이벤트 구독
            microphoneSystem.onPlayerActivated.AddListener(OnPlayerActivated);
            microphoneSystem.onPlayerDeactivated.AddListener(OnPlayerDeactivated);

            // 현재 플레이어 상태 확인
            if (microphoneSystem.PlayerObject != null)
            {
                isPlayerActive = microphoneSystem.PlayerObject.activeSelf;
            }
        }

        if (useTimeOfDaySprites && timeSystem != null)
        {
            // 시간 변경 이벤트 구독
            timeSystem.onMorningStart.AddListener(OnMorningStart);
            timeSystem.onEveningStart.AddListener(OnEveningStart);
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (microphoneSystem != null)
        {
            microphoneSystem.onPlayerActivated.RemoveListener(OnPlayerActivated);
            microphoneSystem.onPlayerDeactivated.RemoveListener(OnPlayerDeactivated);
        }

        if (useTimeOfDaySprites && timeSystem != null)
        {
            timeSystem.onMorningStart.RemoveListener(OnMorningStart);
            timeSystem.onEveningStart.RemoveListener(OnEveningStart);
        }
    }

    // 플레이어 활성화 이벤트 핸들러
    private void OnPlayerActivated()
    {
        isPlayerActive = true;
        LogDebug("플레이어 활성화 감지됨");
        UpdateSpriteBasedOnState();

        // 활성화 시 맥박 효과
        if (usePulseOnActivation)
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }
            pulseCoroutine = StartCoroutine(PulseEffect());
        }
    }

    // 플레이어 비활성화 이벤트 핸들러
    private void OnPlayerDeactivated()
    {
        isPlayerActive = false;
        LogDebug("플레이어 비활성화 감지됨");
        UpdateSpriteBasedOnState();
    }

    // 아침 시작 이벤트 핸들러
    private void OnMorningStart()
    {
        LogDebug("아침 시작 감지됨");
        UpdateSpriteBasedOnState();
    }

    // 저녁 시작 이벤트 핸들러
    private void OnEveningStart()
    {
        LogDebug("저녁 시작 감지됨");
        UpdateSpriteBasedOnState();
    }

    // 상태에 따른 스프라이트 업데이트
    private void UpdateSpriteBasedOnState()
    {
        if (targetSpriteRenderer == null) return;

        Sprite targetSprite = null;

        // 시간에 따른 스프라이트 사용
        if (useTimeOfDaySprites && timeSystem != null)
        {
            // 아침/저녁에 따른 스프라이트 선택
            if (timeSystem.CurrentTime == TimeOfDay.Morning)
            {
                targetSprite = morningSprite;
            }
            else
            {
                // 저녁이면서 플레이어 활성화 상태
                if (isPlayerActive)
                {
                    targetSprite = activatedSprite;
                }
                else
                {
                    targetSprite = eveningSprite;
                }
            }
        }
        else
        {
            // 플레이어 활성화 상태에 따른 스프라이트 선택
            targetSprite = isPlayerActive ? activatedSprite : defaultSprite;
        }

        // 스프라이트 변경
        if (targetSprite != null)
        {
            if (useTransitionEffect)
            {
                // 전환 효과 사용
                TransitionToSprite(targetSprite);
            }
            else
            {
                // 즉시 변경
                targetSpriteRenderer.sprite = targetSprite;
            }
        }
    }

    // 스프라이트 전환 효과
    private void TransitionToSprite(Sprite newSprite)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(SpriteTransitionEffect(newSprite));
    }

    // 스프라이트 전환 코루틴
    private IEnumerator SpriteTransitionEffect(Sprite newSprite)
    {
        // 현재 스프라이트 저장
        Sprite oldSprite = targetSpriteRenderer.sprite;
        Color originalColor = targetSpriteRenderer.color;

        // 페이드 아웃
        for (float t = 0; t < transitionDuration / 2; t += Time.deltaTime)
        {
            float normalizedTime = t / (transitionDuration / 2);
            targetSpriteRenderer.color = new Color(
                originalColor.r,
                originalColor.g,
                originalColor.b,
                Mathf.Lerp(originalColor.a, 0, normalizedTime)
            );
            yield return null;
        }

        // 스프라이트 변경
        targetSpriteRenderer.sprite = newSprite;

        // 페이드 인
        for (float t = 0; t < transitionDuration / 2; t += Time.deltaTime)
        {
            float normalizedTime = t / (transitionDuration / 2);
            targetSpriteRenderer.color = new Color(
                originalColor.r,
                originalColor.g,
                originalColor.b,
                Mathf.Lerp(0, originalColor.a, normalizedTime)
            );
            yield return null;
        }

        // 원래 알파값으로 복원
        targetSpriteRenderer.color = originalColor;
        transitionCoroutine = null;
    }

    // 맥박 효과 코루틴
    private IEnumerator PulseEffect()
    {
        // 크기 증가
        for (float t = 0; t < pulseDuration / 2; t += Time.deltaTime)
        {
            float normalizedTime = t / (pulseDuration / 2);
            transform.localScale = Vector3.Lerp(originalScale, originalScale * pulseScale, normalizedTime);
            yield return null;
        }

        // 크기 복원
        for (float t = 0; t < pulseDuration / 2; t += Time.deltaTime)
        {
            float normalizedTime = t / (pulseDuration / 2);
            transform.localScale = Vector3.Lerp(originalScale * pulseScale, originalScale, normalizedTime);
            yield return null;
        }

        transform.localScale = originalScale;
        pulseCoroutine = null;
    }

    // 디버그 로그 출력
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SpriteSwitcher] {message}");
        }
    }

    // 수동으로 상태 업데이트 (외부에서 호출 가능)
    public void Refresh()
    {
        // 플레이어 상태 확인
        if (microphoneSystem != null && microphoneSystem.PlayerObject != null)
        {
            isPlayerActive = microphoneSystem.PlayerObject.activeSelf;
        }

        // 스프라이트 업데이트
        UpdateSpriteBasedOnState();
        LogDebug("스프라이트 상태 수동 업데이트됨");
    }

    // 수동으로 스프라이트 변경 (외부에서 호출 가능)
    public void SetSprite(Sprite sprite, bool withEffect = true)
    {
        if (targetSpriteRenderer == null || sprite == null) return;

        if (withEffect && useTransitionEffect)
        {
            TransitionToSprite(sprite);
        }
        else
        {
            targetSpriteRenderer.sprite = sprite;
        }

        LogDebug("스프라이트 수동 변경됨");
    }
}