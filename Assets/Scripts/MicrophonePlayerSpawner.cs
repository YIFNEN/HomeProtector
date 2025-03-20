using UnityEngine;
using System.Collections;

public class MicrophonePlayerSpawner : MonoBehaviour
{
    [Header("Microphone Settings")]
    [SerializeField] private int activationThreshold = 50; // 마이크 활성화 임계값
    [SerializeField] private int sampleWindow = 128; // 샘플 창 크기
    [Range(1, 100)]
    [SerializeField] private int scaledVolume; // 조정된 볼륨 값 (인스펙터에서 표시용)

    [Header("Player Spawning")]
    [SerializeField] private GameObject playerPrefab; // 생성할 플레이어 프리팹
    [SerializeField] private bool pauseGameOnActivation = true; // 마이크 활성화 시 게임 일시정지 여부
    [SerializeField] private float placementIndicatorScale = 1f; // 배치 표시기 크기
    [SerializeField] private Color placementIndicatorColor = new Color(0, 1, 0, 0.5f); // 배치 표시기 색상

    [Header("UI Elements")]
    [SerializeField] private GameObject spawnInstructionUI; // 플레이어 배치 안내 UI

    private AudioClip micClip; // 마이크 오디오 클립
    private string micName; // 마이크 장치 이름
    private bool isPlacementMode = false; // 플레이어 배치 모드 여부
    private bool wasGamePaused = false; // 이전 게임 일시정지 상태
    private GameObject placementIndicator; // 배치 위치 표시기

    // 게임 일시 정지 전 기존 타임스케일 저장
    private float previousTimeScale = 1f;

    private void Start()
    {
        // 마이크 장치 초기화
        InitializeMicrophone();

        // 배치 안내 UI 초기화
        if (spawnInstructionUI != null)
        {
            spawnInstructionUI.SetActive(false);
        }

        // 배치 표시기 생성
        CreatePlacementIndicator();
    }

    private void InitializeMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            micName = Microphone.devices[0];
            micClip = Microphone.Start(micName, true, 10, AudioSettings.outputSampleRate);
            Debug.Log("마이크 시작됨: " + micName);
        }
        else
        {
            Debug.LogWarning("마이크 장치가 없습니다.");
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
        // Unity 기본 스프라이트들 중 원형 스프라이트가 있으면 사용
        // 없으면 간단한 흰색 원형 텍스처 생성
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

    private void Update()
    {
        if (isPlacementMode)
        {
            // 배치 모드일 때 마우스 위치에 표시기 이동
            UpdatePlacementIndicator();

            // 클릭 감지하여 플레이어 생성
            if (Input.GetMouseButtonDown(0))
            {
                SpawnPlayerAtMousePosition();
            }

            // ESC 키로 배치 모드 취소
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacementMode();
            }
        }
        else
        {
            // 마이크 볼륨 확인 및 배치 모드 전환
            CheckMicrophoneVolumeForActivation();
        }
    }

    private void CheckMicrophoneVolumeForActivation()
    {
        if (Microphone.IsRecording(micName))
        {
            float volume = GetMaxVolume();
            scaledVolume = ScaleVolume(volume);

            // 볼륨이 임계값을 넘으면 배치 모드로 전환
            if (scaledVolume >= activationThreshold)
            {
                EnterPlacementMode();
            }
        }
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
        if (spawnInstructionUI != null)
        {
            spawnInstructionUI.SetActive(true);
        }

        Debug.Log("플레이어 배치 모드 시작 - 위치를 클릭하세요");
    }

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

    private void SpawnPlayerAtMousePosition()
    {
        // 마우스 위치 가져오기
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f; // 2D 환경에서 z 값 조정

        // 이소메트릭 뷰 지원
        mouseWorldPosition.z = mouseWorldPosition.y;

        // 플레이어 생성
        if (playerPrefab != null)
        {
            GameObject player = Instantiate(playerPrefab, mouseWorldPosition, Quaternion.identity);

            // IsometricPositionHandler 컴포넌트가 있는지 확인하고 추가
            if (player.GetComponent<IsometricPositionHandler>() == null)
            {
                player.AddComponent<IsometricPositionHandler>();
            }

            Debug.Log($"플레이어 생성됨: 위치 {mouseWorldPosition}");
        }
        else
        {
            Debug.LogError("플레이어 프리팹이 설정되지 않았습니다!");
        }

        // 배치 모드 종료
        ExitPlacementMode();
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
        if (spawnInstructionUI != null)
        {
            spawnInstructionUI.SetActive(false);
        }

        Debug.Log("플레이어 배치 모드 종료");
    }

    private void CancelPlacementMode()
    {
        Debug.Log("플레이어 배치 취소됨");
        ExitPlacementMode();
    }

    private float GetMaxVolume()
    {
        if (micClip == null) return 0;

        float[] samples = new float[sampleWindow];
        int micPosition = Microphone.GetPosition(micName);

        if (micPosition < samples.Length)
        {
            Debug.Log("마이크 데이터가 충분히 쌓이지 않음");
            return 0;
        }

        micClip.GetData(samples, micPosition - samples.Length);

        float maxVolume = 0f;
        foreach (float sample in samples)
        {
            maxVolume = Mathf.Max(maxVolume, Mathf.Abs(sample));
        }

        return maxVolume;
    }

    private int ScaleVolume(float volume)
    {
        float scaledVolume = Mathf.Log10(1 + volume * 9) * 100;
        return Mathf.RoundToInt(Mathf.Clamp(scaledVolume, 1, 100));
    }

    private void OnDisable()
    {
        // 마이크 정지
        if (Microphone.IsRecording(micName))
        {
            Microphone.End(micName);
        }

        // 게임이 일시정지된 상태로 종료되지 않도록 함
        if (wasGamePaused)
        {
            ResumeGame();
        }
    }
}