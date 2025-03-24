using UnityEngine;
using UnityEngine.UI;

public class PanelToggler : MonoBehaviour
{
    [Header("패널 설정")]
    [Tooltip("토글할 패널 게임 오브젝트")]
    [SerializeField] private GameObject targetPanel;

    [Tooltip("버튼 클릭 시 효과음 (선택 사항)")]
    [SerializeField] private AudioClip clickSound;

    [Header("애니메이션 설정 (선택 사항)")]
    [Tooltip("패널 애니메이션 사용 여부")]
    [SerializeField] private bool useAnimation = false;

    [Tooltip("패널이 열리거나 닫힐 때 애니메이션 시간")]
    [SerializeField] private float animationDuration = 0.3f;

    [Tooltip("애니메이션 사용 시 패널의 CanvasGroup")]
    [SerializeField] private CanvasGroup panelCanvasGroup;

    // 패널 상태 추적
    private bool isPanelActive = false;

    // 애니메이션 코루틴 참조
    private Coroutine animationCoroutine;

    // 오디오 소스 컴포넌트
    private AudioSource audioSource;

    private void Awake()
    {
        // 오디오 소스 확인
        audioSource = GetComponent<AudioSource>();

        // 대상 패널이 지정되었는지 확인
        if (targetPanel == null)
        {
            Debug.LogError("토글할 대상 패널이 지정되지 않았습니다!");
        }

        // 애니메이션 사용 시 CanvasGroup 확인
        if (useAnimation && panelCanvasGroup == null && targetPanel != null)
        {
            // CanvasGroup이 없으면 자동으로 추가
            panelCanvasGroup = targetPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = targetPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    private void Start()
    {
        // 버튼 컴포넌트 확인 및 클릭 이벤트 등록
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(TogglePanel);
        }
        else
        {
            Debug.LogWarning("이 게임 오브젝트에 Button 컴포넌트가 없습니다!");
        }

        // 초기 상태 설정
        if (targetPanel != null)
        {
            isPanelActive = targetPanel.activeSelf;

            // 애니메이션 사용 시 초기 상태에 맞게 CanvasGroup 설정
            if (useAnimation && panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = isPanelActive ? 1f : 0f;
                panelCanvasGroup.interactable = isPanelActive;
                panelCanvasGroup.blocksRaycasts = isPanelActive;
            }
        }
    }

    // 패널 토글 기능
    public void TogglePanel()
    {
        if (targetPanel == null) return;

        isPanelActive = !isPanelActive;

        // 효과음 재생
        PlayClickSound();

        if (useAnimation && panelCanvasGroup != null)
        {
            // 진행 중인 애니메이션이 있으면 중지
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            // 애니메이션 시작
            if (isPanelActive)
            {
                targetPanel.SetActive(true);
                animationCoroutine = StartCoroutine(AnimatePanel(0f, 1f, true));
            }
            else
            {
                animationCoroutine = StartCoroutine(AnimatePanel(1f, 0f, false));
            }
        }
        else
        {
            // 애니메이션 없이 즉시 전환
            targetPanel.SetActive(isPanelActive);
        }
    }

    // 패널 애니메이션 코루틴
    private System.Collections.IEnumerator AnimatePanel(float startAlpha, float endAlpha, bool enableOnComplete)
    {
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / animationDuration;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, normalizedTime);

            // CanvasGroup 업데이트
            panelCanvasGroup.alpha = currentAlpha;

            yield return null;
        }

        // 애니메이션 완료 후 최종 상태 설정
        panelCanvasGroup.alpha = endAlpha;

        // 비활성화 시 오브젝트도 비활성화
        if (!enableOnComplete)
        {
            targetPanel.SetActive(false);
        }

        // 상호작용 설정
        panelCanvasGroup.interactable = enableOnComplete;
        panelCanvasGroup.blocksRaycasts = enableOnComplete;

        animationCoroutine = null;
    }

    // 효과음 재생
    private void PlayClickSound()
    {
        if (clickSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
            else
            {
                // 게임 오브젝트에 오디오 소스가 없을 경우 일회성으로 재생
                AudioSource.PlayClipAtPoint(clickSound, Camera.main.transform.position);
            }
        }
    }

    // 패널을 강제로 열기
    public void OpenPanel()
    {
        if (!isPanelActive)
        {
            TogglePanel();
        }
    }

    // 패널을 강제로 닫기
    public void ClosePanel()
    {
        if (isPanelActive)
        {
            TogglePanel();
        }
    }

    // 스크립트가 비활성화될 때 코루틴 정리
    private void OnDisable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }
}