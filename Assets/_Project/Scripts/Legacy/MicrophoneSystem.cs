using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 마이크 입력을 통한 플레이어 활성화 및 관리를 담당하는 통합 시스템
/// </summary>
public class MicrophoneSystem : MonoBehaviour
{
    [System.Serializable]
    public class MicrophoneEvent : UnityEvent<int, int> { } // (현재 볼륨, 임계값)

    #region Microphone Settings
    [Header("Microphone Settings")]
    [SerializeField] private int baseActivationThreshold = 50; // 기본 마이크 활성화 임계값
    [SerializeField] private int minActivationThreshold = 30; // 최소 마이크 활성화 임계값
    [SerializeField] private int maxActivationThreshold = 80; // 최대 마이크 활성화 임계값
    [SerializeField] private int sampleWindow = 128; // 샘플 창 크기
    [Range(1, 100)]
    [SerializeField] private int scaledVolume; // 조정된 볼륨 값 (인스펙터에서 표시용)
    [SerializeField] private bool pauseGameOnActivation = true; // 마이크 활성화 시 게임 일시정지 여부
    #endregion

    #region Fatigue Settings
    [Header("Fatigue Scaling")]
    [SerializeField] private AnimationCurve fatigueToDifficultyMultiplier; // 피로도에 따른 난이도(볼륨) 계수
    private int currentActivationThreshold; // 현재 임계값 (피로도 적용)
    #endregion

    #region Player Settings
    [Header("Player Settings")]
    [SerializeField] private GameObject _playerObject; // 플레이어 오브젝트 (기존 프리팹 대신 씬에 있는 오브젝트)
    [SerializeField] private float placementIndicatorScale = 1f; // 배치 표시기 크기
    [SerializeField] private Color placementIndicatorColor = new Color(0, 1, 0, 0.5f); // 배치 표시기 색상
    [SerializeField] private bool playerActivationEnabled = true; // 플레이어 활성화 가능 여부
    [SerializeField] private bool oneTimeUseOnly = true; // 한 웨이브당 한 번만 사용 가능 여부
    [SerializeField] private float playerActiveTime = 50f; // 플레이어 활성화 유지 시간 (저녁 모드에서)
    [SerializeField] private bool keyboardFallbackEnabled = true;
    [SerializeField] private KeyCode keyboardActivationKey = KeyCode.F;
    private bool hasActivatedPlayer = false; // 이미 플레이어를 활성화했는지 여부
    private Vector3 defaultPlayerPosition = Vector3.zero; // 기본 플레이어 위치 (0,0,0)

    // 다른 시스템에서 플레이어 오브젝트에 접근할 수 있도록 getter 제공
    public GameObject PlayerObject => _playerObject;
    #endregion

    #region UI Elements
    [Header("UI Elements")]
    [SerializeField] private GameObject placementInstructionUI; // 플레이어 배치 안내 UI
    [SerializeField] private TMPro.TextMeshProUGUI debugText; // 디버그 텍스트 UI
    [SerializeField] private bool showDebugInfo = true; // 디버그 정보 표시 여부
    #endregion

    #region Debug Options
    [Header("Debug")]
    [SerializeField] private bool forceActive = false; // 강제 활성화 옵션
    [SerializeField] private bool verbose = true; // 상세 로그 출력 옵션
    #endregion

    #region Events
    // 볼륨 변경 이벤트
    public MicrophoneEvent onVolumeChanged = new MicrophoneEvent();
    // 플레이어 활성화 이벤트
    public UnityEvent onPlayerActivated = new UnityEvent();
    // 플레이어 비활성화 이벤트
    public UnityEvent onPlayerDeactivated = new UnityEvent();
    #endregion

    #region Private Variables
    // 마이크 관련 변수
    private AudioClip micClip; // 마이크 오디오 클립
    private string micName; // 마이크 장치 이름

    // 상태 변수
    private bool isPlacementMode = false; // 플레이어 배치 모드 여부
    private bool wasGamePaused = false; // 이전 게임 일시정지 상태
    private GameObject placementIndicator; // 배치 위치 표시기

    // 게임 일시 정지 전 기존 타임스케일 저장
    private float previousTimeScale = 1f;

    // 시스템 참조
    private TimeSystem timeSystem;
    private PlayerGold playerGold;
    private PlayerExperience playerExperience;

    // 플레이어 타이머
    private Coroutine playerDeactivationCoroutine;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 필요한 컴포넌트 참조 찾기
        playerGold = FindObjectOfType<PlayerGold>();
        playerExperience = FindObjectOfType<PlayerExperience>();
        timeSystem = FindObjectOfType<TimeSystem>();

        // 플레이어 오브젝트가 없으면 찾기
        if (_playerObject == null)
        {
            _playerObject = GameObject.FindGameObjectWithTag("Player");
            if (_playerObject == null && verbose)
            {
                Debug.LogError("플레이어 오브젝트를 찾을 수 없습니다. 반드시 씬에 'Player' 태그를 가진 오브젝트가 있어야 합니다.");
            }
        }

        // 플레이어 기본 위치 저장
        if (_playerObject != null)
        {
            defaultPlayerPosition = _playerObject.transform.position;
        }

        // 애니메이션 커브가 비어있으면 기본값 설정
        InitializeFatigueCurve();

        // 배치 안내 UI 초기화
        if (placementInstructionUI != null)
        {
            placementInstructionUI.SetActive(false);
        }

        // 디버그 UI 초기화
        if (debugText != null)
        {
            debugText.gameObject.SetActive(showDebugInfo);
        }

        // 배치 표시기 생성
        CreatePlacementIndicator();

        // 초기 상태 설정
        ResetActivationState();

        if (verbose) Debug.Log("MicrophoneSystem 초기화 완료 (Awake)");
    }

    private void Start()
    {
        // 마이크 초기화 (지연 및 테스트 포함)
        StartCoroutine(InitializeAndTestMicrophone());

        // 피로도에 따른 초기 임계값 설정
        UpdateActivationThreshold();

        // TimeSystem 이벤트 구독 (존재하는 경우)
        if (timeSystem != null)
        {
            timeSystem.onMorningStart.AddListener(OnMorningStart);
            timeSystem.onEveningStart.AddListener(OnEveningStart);

            // 현재 시간에 따른 초기 플레이어 상태 설정
            SetPlayerStateBasedOnTimeOfDay();
        }
        else
        {
            if (verbose) Debug.LogWarning("TimeSystem을 찾을 수 없습니다. 기본 설정(저녁 모드)으로 설정합니다.");
            // 기본적으로 플레이어는 비활성화
            if (_playerObject != null)
            {
                _playerObject.SetActive(false);
            }

            enabled = true;
        }

        if (verbose) Debug.Log("MicrophoneSystem 초기화 완료 (Start)");
    }

    private void Update()
    {
        // 활성화 상태가 아니면 아무것도 하지 않음
        if (!enabled)
        {
            return;
        }

        // 플레이어 활성화가 비활성화되어 있는 경우
        if (!playerActivationEnabled)
        {
            if (verbose && Time.frameCount % 300 == 0) Debug.Log("플레이어 활성화가 비활성화되어 있음");
            return;
        }

        // 이미 한 번 사용했고, 한 번만 사용 가능한 경우 (저녁 모드일 때만 적용)
        if (timeSystem != null && timeSystem.CurrentTime == TimeOfDay.Evening && oneTimeUseOnly && hasActivatedPlayer)
        {
            if (verbose && Time.frameCount % 300 == 0) Debug.Log("이미 플레이어를 활성화했음");
            return;
        }

        // 임계값 업데이트
        UpdateActivationThreshold();

        if (isPlacementMode)
        {
            // 배치 모드일 때 마우스 위치에 표시기 이동
            UpdatePlacementIndicator();

            // 클릭 감지하여 플레이어 활성화
            if (Input.GetMouseButtonDown(0))
            {
                ActivatePlayerAtMousePosition();
            }

            // ESC 키로 배치 모드 취소
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacementMode();
            }
        }
        else if (timeSystem != null && timeSystem.CurrentTime == TimeOfDay.Evening)
        {
            if (keyboardFallbackEnabled && Input.GetKeyDown(keyboardActivationKey))
            {
                RequestPlayerActivation();
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            // 저녁 모드에서만 마이크 감지
            // 마이크 볼륨 확인 및 배치 모드 전환
            if (!isPlacementMode && micClip != null && micName != null && Microphone.IsRecording(micName))
            {
                float volume = GetMaxVolume();
                scaledVolume = ScaleVolume(volume);

                // 주기적으로 볼륨 정보 로깅
                if (verbose && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"마이크 볼륨: {scaledVolume}/100, 임계값: {currentActivationThreshold}/100");
                }

                // 이벤트 발생
                onVolumeChanged?.Invoke(scaledVolume, currentActivationThreshold);

                // 볼륨이 임계값을 넘으면 배치 모드 전환
                if (scaledVolume >= currentActivationThreshold)
                {
                    if (verbose) Debug.Log($"볼륨({scaledVolume})이 임계값({currentActivationThreshold})을 넘어 배치 모드 진입");
                    RequestPlayerActivation();
                }
            }
            else if (!isPlacementMode)
            {
                // 마이크가 녹음 중이 아닌 경우
                if (verbose && Time.frameCount % 300 == 0)
                {
                    Debug.LogWarning("마이크가 녹음 중이 아님, 재초기화 시도");
                    StartCoroutine(InitializeAndTestMicrophone());
                }
            }
#endif
        }

        // 디버그 정보 업데이트
        if (showDebugInfo) UpdateDebugInfo();
    }

    private void OnDisable()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        // 마이크 정지
        if (micName != null && Microphone.IsRecording(micName))
        {
            Microphone.End(micName);
            if (verbose) Debug.Log("마이크 녹음 중지됨");
        }
#endif

        // 게임이 일시정지된 상태로 종료되지 않도록 함
        if (wasGamePaused)
        {
            ResumeGame();
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

        // 코루틴 정리
        if (playerDeactivationCoroutine != null)
        {
            StopCoroutine(playerDeactivationCoroutine);
        }
    }
    #endregion

    #region Initialization Methods
    private IEnumerator InitializeAndTestMicrophone()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (verbose)
        {
            Debug.Log("WebGL microphone input is unavailable. Use the keyboard fallback.");
        }

        yield break;
#else
        // 안정적인 초기화를 위해 짧게 대기
        yield return new WaitForSeconds(0.2f);

        if (Microphone.devices.Length > 0)
        {
            if (verbose)
            {
                Debug.Log($"사용 가능한 마이크 수: {Microphone.devices.Length}");
                for (int i = 0; i < Microphone.devices.Length; i++)
                {
                    Debug.Log($"마이크 {i}: {Microphone.devices[i]}");
                }
            }

            // 이미 녹음 중인 마이크가 있으면 중지
            if (micName != null && Microphone.IsRecording(micName))
            {
                Microphone.End(micName);
                yield return new WaitForSeconds(0.1f);
            }

            micName = Microphone.devices[0];
            micClip = Microphone.Start(micName, true, 10, AudioSettings.outputSampleRate);

            // 초기화 성공 확인을 위해 잠시 대기
            yield return new WaitForSeconds(0.1f);

            if (Microphone.IsRecording(micName))
            {
                if (verbose) Debug.Log($"마이크 초기화 성공: {micName}");

                // 초기 볼륨 테스트 (몇 번 샘플링)
                for (int i = 0; i < 3; i++)
                {
                    yield return new WaitForSeconds(0.1f);
                    float volume = GetMaxVolume();
                    int scaled = ScaleVolume(volume);
                    if (verbose) Debug.Log($"마이크 테스트 {i + 1}: 볼륨={scaled}/100, 원시값={volume}");
                }
            }
            else
            {
                Debug.LogError("마이크 초기화 실패: 녹음 상태가 아님");
            }
        }
        else
        {
            Debug.LogError("사용 가능한 마이크가 없습니다!");
        }
#endif
    }

    private void InitializeFatigueCurve()
    {
        if (fatigueToDifficultyMultiplier == null || fatigueToDifficultyMultiplier.keys.Length == 0)
        {
            // 피로도가 높을수록 크게 소리내야 함
            fatigueToDifficultyMultiplier = new AnimationCurve(
                new Keyframe(0f, 0.5f), // 피로도 0%일 때 난이도 50%
                new Keyframe(0.5f, 1.0f), // 피로도 50%일 때 난이도 100%
                new Keyframe(1f, 1.5f)  // 피로도 100%일 때 난이도 150%
            );
        }
    }

    private void CreatePlacementIndicator()
    {
        // 배치 표시기 생성 (간단한 원형 스프라이트)
        placementIndicator = new GameObject("PlacementIndicator");
        SpriteRenderer renderer = placementIndicator.AddComponent<SpriteRenderer>();

        // 원형 스프라이트 사용 또는 기본 스프라이트
        renderer.sprite = GetCircleSprite();
        renderer.color = placementIndicatorColor;

        // 크기 설정
        placementIndicator.transform.localScale = new Vector3(placementIndicatorScale, placementIndicatorScale, 1f);

        // 초기에는 비활성화
        placementIndicator.SetActive(false);
    }

    private Sprite GetCircleSprite()
    {
        // 기본 원형 스프라이트 반환
        Sprite circleSprite = Resources.Load<Sprite>("UI/Circle");

        if (circleSprite == null)
        {
            // 간단한 흰색 원형 텍스처 생성
            Texture2D texture = new Texture2D(128, 128);
            Color[] colors = new Color[128 * 128];

            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(64, 64));
                    colors[y * 128 + x] = distance < 64 ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(colors);
            texture.Apply();

            circleSprite = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        }

        return circleSprite;
    }
    #endregion

    #region Microphone Volume Processing
    private void CheckMicrophoneVolumeForActivation()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return;
#else
        if (micName != null && Microphone.IsRecording(micName))
        {
            float volume = GetMaxVolume();
            scaledVolume = ScaleVolume(volume);

            // 이벤트 발생
            onVolumeChanged?.Invoke(scaledVolume, currentActivationThreshold);

            // 볼륨이 임계값을 넘으면 배치 모드 전환
            if (scaledVolume >= currentActivationThreshold)
            {
                RequestPlayerActivation();
            }
        }
        else if (verbose)
        {
            Debug.LogWarning("마이크가 녹음 중이 아니어서 볼륨 확인 불가");
        }
#endif
    }

    private float GetMaxVolume()
    {
        if (micClip == null) return 0;
#if UNITY_WEBGL && !UNITY_EDITOR
        return 0f;
#else

        float[] samples = new float[sampleWindow];
        int micPosition = Microphone.GetPosition(micName);

        // 안전한 위치 확인
        if (micPosition < samples.Length)
        {
            if (verbose) Debug.Log($"마이크 데이터가 충분히 쌓이지 않음: {micPosition}/{samples.Length}");
            return 0;
        }

        // 샘플 가져오기
        try
        {
            micClip.GetData(samples, micPosition - samples.Length);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"마이크 데이터 가져오기 실패: {e.Message}");
            return 0;
        }

        // 최대 볼륨 계산
        float maxVolume = 0f;
        foreach (float sample in samples)
        {
            maxVolume = Mathf.Max(maxVolume, Mathf.Abs(sample));
        }

        return maxVolume;
#endif
    }

    private int ScaleVolume(float volume)
    {
        float scaledVolume = Mathf.Log10(1 + volume * 9) * 100;
        return Mathf.RoundToInt(Mathf.Clamp(scaledVolume, 1, 100));
    }
    #endregion

    #region Fatigue System
    private void UpdateActivationThreshold()
    {
        if (playerGold == null) return;

        // 피로도 비율 가져오기 (0~1)
        float fatigueRatio = playerGold.GetNormalizedFatigueRatio();

        // 애니메이션 커브에서 난이도 계수 가져오기
        float difficultyMultiplier = fatigueToDifficultyMultiplier.Evaluate(fatigueRatio);

        // 기본 임계값에 난이도 계수 적용
        float scaledThreshold = baseActivationThreshold * difficultyMultiplier;

        // 최소/최대 범위 내로 제한
        currentActivationThreshold = Mathf.Clamp(Mathf.RoundToInt(scaledThreshold), minActivationThreshold, maxActivationThreshold);
    }
    #endregion

    #region Player Placement Mode
    public void RequestPlayerActivation()
    {
        if (!playerActivationEnabled || isPlacementMode)
        {
            return;
        }

        if (timeSystem != null && timeSystem.CurrentTime != TimeOfDay.Evening)
        {
            return;
        }

        if (oneTimeUseOnly && hasActivatedPlayer)
        {
            return;
        }

        EnterPlacementMode();
    }

    private void EnterPlacementMode()
    {
        isPlacementMode = true;

        // 게임 일시정지
        if (pauseGameOnActivation)
        {
            PauseGame();
        }

        // 배치 표시기 활성화
        if (placementIndicator != null)
        {
            placementIndicator.SetActive(true);
        }

        // 배치 안내 UI 표시
        if (placementInstructionUI != null)
        {
            placementInstructionUI.SetActive(true);
        }

        Debug.Log("플레이어 배치 모드 시작 - 위치를 클릭하세요");
    }

    private void UpdatePlacementIndicator()
    {
        if (placementIndicator != null)
        {
            // 마우스 위치로 표시기 이동
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPosition.z = 0f; // 2D 환경에서 z 값 조정

            // 이소메트릭 뷰 지원
            mouseWorldPosition.z = mouseWorldPosition.y;

            placementIndicator.transform.position = mouseWorldPosition;
        }
    }

    private void ActivatePlayerAtMousePosition()
    {
        if (_playerObject == null)
        {
            Debug.LogError("플레이어 오브젝트가 설정되지 않았습니다!");
            ExitPlacementMode();
            return;
        }

        // 마우스 위치 가져오기
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f; // 2D 환경에서 z 값 조정

        // 이소메트릭 뷰 지원
        mouseWorldPosition.z = mouseWorldPosition.y;

        // 플레이어 활성화 (위치 지정)
        ActivatePlayer(mouseWorldPosition);

        // 배치 모드 종료
        ExitPlacementMode();
    }

    private IEnumerator DeactivatePlayerAfterTime(float deactivationTime)
    {
        yield return new WaitForSeconds(deactivationTime);
        if (_playerObject != null)
        {
            _playerObject.SetActive(false);
            Debug.Log($"플레이어 자동 비활성화됨 ({deactivationTime}초 경과)");
            onPlayerDeactivated.Invoke();
        }

        playerDeactivationCoroutine = null;
    }

    private void ExitPlacementMode()
    {
        isPlacementMode = false;

        // 게임 재개
        if (wasGamePaused)
        {
            ResumeGame();
        }

        // 배치 표시기 비활성화
        if (placementIndicator != null)
        {
            placementIndicator.SetActive(false);
        }

        // 배치 안내 UI 숨기기
        if (placementInstructionUI != null)
        {
            placementInstructionUI.SetActive(false);
        }

        Debug.Log("플레이어 배치 모드 종료");
    }

    private void CancelPlacementMode()
    {
        Debug.Log("플레이어 배치 취소됨");
        ExitPlacementMode();
    }
    #endregion

    #region Game Pause/Resume
    private void PauseGame()
    {
        // 현재 타임스케일 저장 및 게임 일시정지
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        wasGamePaused = true;
    }

    private void ResumeGame()
    {
        // 이전 타임스케일로 복원
        Time.timeScale = previousTimeScale;
        wasGamePaused = false;
    }
    #endregion

    #region Time System Events
    private void OnMorningStart()
    {
        // 아침에는 마이크 시스템 비활성화 (강제 활성화 옵션 확인)
        if (!forceActive)
        {
            enabled = false;
            if (verbose) Debug.Log("아침 모드: 마이크 시스템 비활성화");
        }
        else
        {
            if (verbose) Debug.Log("아침 모드지만 forceActive 옵션으로 활성화 유지");
        }

        // 배치 모드 종료
        if (isPlacementMode)
        {
            CancelPlacementMode();
        }

        // 아침에는 플레이어 활성화 상태로 유지, 기본 위치(0,0,0)로 설정
        if (_playerObject != null)
        {
            _playerObject.transform.position = defaultPlayerPosition;
            _playerObject.SetActive(true);

            // 공격 비활성화 (아침 모드)
            PlayerMovement playerMovement = _playerObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.SetAttackEnabled(false);
            }

            if (verbose) Debug.Log("아침 모드: 플레이어 기본 위치에 활성화");
        }

        // 자동 비활성화 타이머 취소 (아침에는 계속 활성화)
        if (playerDeactivationCoroutine != null)
        {
            StopCoroutine(playerDeactivationCoroutine);
            playerDeactivationCoroutine = null;
        }
    }

    private void OnEveningStart()
    {
        // 저녁에는 마이크 시스템 활성화
        enabled = true;
        if (verbose) Debug.Log("저녁 모드: 마이크 시스템 활성화");

        // 저녁에는 플레이어 비활성화 (마이크로 활성화할 때까지)
        if (_playerObject != null)
        {
            _playerObject.SetActive(false);
            if (verbose) Debug.Log("저녁 모드: 플레이어 비활성화");
        }

        // 새 웨이브에 대한 상태 초기화
        ResetActivationState();
    }

    private void SetPlayerStateBasedOnTimeOfDay()
    {
        if (timeSystem == null || _playerObject == null) return;

        if (timeSystem.CurrentTime == TimeOfDay.Morning)
        {
            // 아침에는 플레이어 활성화 상태로 유지, 기본 위치(0,0,0)로 설정
            _playerObject.transform.position = defaultPlayerPosition;
            _playerObject.SetActive(true);

            // 마이크 시스템 비활성화 (강제 활성화 옵션 확인)
            if (!forceActive)
            {
                enabled = false;
                if (verbose) Debug.Log("아침 모드: 마이크 시스템 비활성화");
            }

            // 공격 비활성화 (아침 모드)
            PlayerMovement playerMovement = _playerObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.SetAttackEnabled(false);
            }

            if (verbose) Debug.Log("아침 모드: 플레이어 기본 위치에 활성화");
        }
        else // Evening
        {
            // 저녁에는 플레이어 비활성화 (마이크로 활성화할 때까지)
            _playerObject.SetActive(false);

            // 마이크 시스템 활성화
            enabled = true;

            if (verbose) Debug.Log("저녁 모드: 플레이어 비활성화, 마이크 시스템 활성화");
        }
    }
    #endregion

    #region Public Methods
    // 활성화 상태 초기화 메서드
    public void ResetActivationState()
    {
        hasActivatedPlayer = false;
        if (verbose) Debug.Log("플레이어 활성화 상태 초기화됨");
    }

    // 활성화 임계값 설정
    public void SetBaseActivationThreshold(int threshold)
    {
        baseActivationThreshold = threshold;
        UpdateActivationThreshold();
        if (verbose) Debug.Log($"기본 활성화 임계값 변경: {threshold}");
    }

    // 플레이어 활성화 기능 활성화/비활성화
    public void SetPlayerActivationEnabled(bool enabled)
    {
        playerActivationEnabled = enabled;
        if (verbose) Debug.Log($"플레이어 활성화 기능 {(enabled ? "활성화" : "비활성화")}됨");

        // 비활성화 시 배치 모드 취소
        if (!enabled && isPlacementMode)
        {
            CancelPlacementMode();
        }
    }

    // 이미 활성화한 상태인지 확인
    public bool HasActivatedPlayer()
    {
        return hasActivatedPlayer;
    }

    // 현재 활성화 임계값 반환
    public int GetCurrentActivationThreshold()
    {
        return currentActivationThreshold;
    }

    // 현재 볼륨 반환
    public int GetCurrentVolume()
    {
        return scaledVolume;
    }

    // 볼륨이 임계값을 넘는지 확인
    public bool IsVolumeAboveThreshold()
    {
        return scaledVolume >= currentActivationThreshold;
    }

    // 플레이어 즉시 활성화 (외부에서 호출 가능)
    public void ActivatePlayer(Vector3 position)
    {
        if (_playerObject == null) return;

        _playerObject.transform.position = position;
        _playerObject.SetActive(true);

        // IsometricPositionHandler 컴포넌트 추가 확인
        if (_playerObject.GetComponent<IsometricPositionHandler>() == null)
        {
            _playerObject.AddComponent<IsometricPositionHandler>();
        }

        // 저녁 모드에 맞게 공격 활성화
        PlayerMovement playerMovement = _playerObject.GetComponent<PlayerMovement>();
        if (playerMovement != null && timeSystem != null && timeSystem.CurrentTime == TimeOfDay.Evening)
        {
            playerMovement.SetAttackEnabled(true);
            // 저녁 모드에서 Bow 애니메이션 트리거 활성화
            Animator animator = _playerObject.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Bow");

                // 지속적인 Bow 애니메이션 실행을 위한 코루틴 시작
                StartCoroutine(ContinuousBowAnimation(animator));
            }
        }

        // 한 번만 사용 가능한 경우 사용 완료 처리
        if (oneTimeUseOnly && timeSystem != null && timeSystem.CurrentTime == TimeOfDay.Evening)
        {
            hasActivatedPlayer = true;
        }

        // 저녁 모드에서 일정 시간 후 비활성화
        if (timeSystem != null && timeSystem.CurrentTime == TimeOfDay.Evening && playerActiveTime > 0)
        {
            if (playerDeactivationCoroutine != null)
            {
                StopCoroutine(playerDeactivationCoroutine);
            }

            playerDeactivationCoroutine = StartCoroutine(DeactivatePlayerAfterTime(playerActiveTime));
        }

        onPlayerActivated.Invoke();

        if (verbose) Debug.Log($"플레이어 활성화됨: 위치 {position}");
    }
    // Bow 애니메이션을 지속적으로 실행하는 코루틴
    private IEnumerator ContinuousBowAnimation(Animator animator)
    {
        // 플레이어가 활성화되어 있는 동안 반복
        while (_playerObject != null && _playerObject.activeSelf)
        {
            // Bow 애니메이션 트리거
            animator.SetTrigger("Bow");

            // 애니메이션 길이에 맞춰 대기 (약 1초)
            yield return new WaitForSeconds(1.0f);
        }
    }
    // 플레이어 즉시 비활성화 (외부에서 호출 가능)
    public void DeactivatePlayer()
    {
        if (_playerObject == null) return;

        _playerObject.SetActive(false);

        if (playerDeactivationCoroutine != null)
        {
            StopCoroutine(playerDeactivationCoroutine);
            playerDeactivationCoroutine = null;
        }

        onPlayerDeactivated.Invoke();

        if (verbose) Debug.Log("플레이어 비활성화됨");
    }

    // 디버그 정보 갱신 메서드 (외부에서 호출 가능)
    public void UpdateVolumeVisualizers()
    {
        UpdateDebugInfo();
    }
    #endregion

    #region Debug Information
    private void UpdateDebugInfo()
    {
        if (!showDebugInfo || debugText == null) return;

        // 피로도 정보
        string fatigueInfo = playerGold != null ?
            $"피로도: {Mathf.RoundToInt(playerGold.CurrentFatigue)}/{Mathf.RoundToInt(playerGold.MaxFatigue)} ({playerGold.GetNormalizedFatigueRatio():P0})" :
            "피로도 정보 없음";

        // 마이크 정보
        string micInfo = $"볼륨: {scaledVolume}/100, 필요 볼륨: {currentActivationThreshold}/100";

        // 상태 정보
        string statusInfo = scaledVolume >= currentActivationThreshold ?
            "<color=green>활성화 가능</color>" :
            $"<color=red>볼륨 부족 ({scaledVolume - currentActivationThreshold})</color>";

        // 시스템 상태 정보
        string systemInfo = $"활성화: {(enabled ? "O" : "X")}, 플레이어 활성화 가능: {(playerActivationEnabled ? "O" : "X")}, 사용됨: {(hasActivatedPlayer ? "O" : "X")}";

        // 플레이어 상태 정보
        string playerInfo = _playerObject != null ?
            $"플레이어 상태: {(_playerObject.activeSelf ? "활성화" : "비활성화")}" :
            "플레이어 오브젝트 없음";

        // 시간 정보
        string timeInfo = timeSystem != null ?
            $"현재 시간: {timeSystem.CurrentTime}" :
            "시간 시스템 없음";

        // 통합 정보
        debugText.text = $"{timeInfo}\n{fatigueInfo}\n{micInfo}\n{statusInfo}\n{systemInfo}\n{playerInfo}";
    }
    #endregion
}