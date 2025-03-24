using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using UnityEngine.Tilemaps;

public enum TimeOfDay { Morning, Evening }

public class TimeSystem : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private TimeOfDay currentTimeOfDay = TimeOfDay.Morning;
    [SerializeField] private float morningTransitionDuration = 1.5f; // 아침 전환 효과 지속시간
    [SerializeField] private float eveningTransitionDuration = 1.5f; // 저녁 전환 효과 지속시간

    [Header("Visual Effects")]
    [SerializeField] private GameObject morningVisualEffect; // 아침 전환 시각효과
    [SerializeField] private GameObject eveningVisualEffect; // 저녁 전환 시각효과

    [Header("Color Settings")]
    [SerializeField] private bool useColorTransition = true; // 색상 전환 사용 여부
    [SerializeField] private Color morningBackgroundColor = Color.cyan; // 아침 배경색
    [SerializeField] private Color eveningBackgroundColor = Color.black; // 저녁 배경색
    [SerializeField] private Color morningTilemapColor = Color.white; // 아침 타일맵 색상
    [SerializeField] private Color eveningTilemapColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 저녁 타일맵 색상
    [SerializeField] private List<Tilemap> tilemaps = new List<Tilemap>(); // 색상을 변경할 타일맵 리스트
    [SerializeField] private float colorTransitionSpeed = 1.0f; // 색상 전환 속도

    [Header("UI References")]
    [SerializeField] private GameObject morningOnlyUI; // 아침에만 표시되는 UI
    [SerializeField] private GameObject eveningOnlyUI; // 저녁에만 표시되는 UI
    [SerializeField] private GameObject panelStage; // 웨이브 종료 후 표시될 스테이지 패널

    [Header("System References")]
    [SerializeField] private WaveSystem waveSystem; // 웨이브 시스템 참조
    [SerializeField] private PlayerGold playerGold; // 플레이어 골드 참조
    [SerializeField] private PlayerExperience playerExperience; // 플레이어 경험치 참조
    [SerializeField] private MicrophoneSystem microphoneSystem; // 통합된 마이크 시스템
    [SerializeField] private DayCounterSystem dayCounterSystem; // 일수 관리 시스템 참조
    [SerializeField] private WaveResultSystem waveResultSystem; // 웨이브 결과 시스템 참조

    [Header("Layer Settings")]
    [SerializeField] private bool useLayerBasedActivation = true; // 레이어 기반 활성화/비활성화 사용 여부
    [SerializeField] private string morningOnlyLayerName = "MorningOnly"; // 아침 전용 레이어 이름
    [SerializeField] private string eveningOnlyLayerName = "EveningOnly"; // 저녁 전용 레이어 이름
    [SerializeField] private string resourceLayerName = "Resource"; // 자원 레이어 이름
    [SerializeField] private string towerLayerName = "Tower"; // 타워 레이어 이름

    // Events
    public UnityEvent onMorningStart;
    public UnityEvent onEveningStart;

    // 현재 시간 프로퍼티
    public TimeOfDay CurrentTime => currentTimeOfDay;

    // 시간 전환 중인지 여부
    private bool isTransitioning = false;

    // 레이어 인덱스 캐싱
    private int morningLayer;
    private int eveningLayer;
    private int resourceLayer;
    private int towerLayer;

    // 색상 전환 코루틴 참조
    private Coroutine colorTransitionCoroutine;
    private Camera mainCameraCache;

    private void Awake()
    {
        // 시스템 컴포넌트 찾기
        if (waveSystem == null) waveSystem = FindObjectOfType<WaveSystem>();
        if (playerGold == null) playerGold = FindObjectOfType<PlayerGold>();
        if (playerExperience == null) playerExperience = FindObjectOfType<PlayerExperience>();
        if (microphoneSystem == null) microphoneSystem = FindObjectOfType<MicrophoneSystem>();
        if (dayCounterSystem == null) dayCounterSystem = FindObjectOfType<DayCounterSystem>();
        if (waveResultSystem == null) waveResultSystem = FindObjectOfType<WaveResultSystem>();

        // 메인 카메라 캐싱
        mainCameraCache = Camera.main;

        // 타일맵 자동 찾기 (리스트가 비어있는 경우)
        if (tilemaps.Count == 0)
        {
            Tilemap[] foundTilemaps = FindObjectsOfType<Tilemap>();
            if (foundTilemaps.Length > 0)
            {
                tilemaps.AddRange(foundTilemaps);
                Debug.Log($"{foundTilemaps.Length}개의 타일맵을 자동으로 찾았습니다.");
            }
        }

        // 레이어 인덱스 캐싱
        morningLayer = LayerMask.NameToLayer(morningOnlyLayerName);
        eveningLayer = LayerMask.NameToLayer(eveningOnlyLayerName);
        resourceLayer = LayerMask.NameToLayer(resourceLayerName);
        towerLayer = LayerMask.NameToLayer(towerLayerName);

        // 레이어가 존재하는지 확인
        if (morningLayer == -1)
            Debug.LogError($"{morningOnlyLayerName} 레이어가 존재하지 않습니다. Unity에서 레이어를 생성해주세요.");

        if (eveningLayer == -1)
            Debug.LogError($"{eveningOnlyLayerName} 레이어가 존재하지 않습니다. Unity에서 레이어를 생성해주세요.");

        if (resourceLayer == -1)
            Debug.LogError($"{resourceLayerName} 레이어가 존재하지 않습니다. Unity에서 레이어를 생성해주세요.");

        if (towerLayer == -1)
            Debug.LogError($"{towerLayerName} 레이어가 존재하지 않습니다. Unity에서 레이어를 생성해주세요.");

        // 초기 시간 설정에 따른 UI 설정
        UpdateUIBasedOnTime(currentTimeOfDay);

        // 초기 색상 설정
        if (useColorTransition)
        {
            ApplyTimeBasedColors(currentTimeOfDay);
        }
    }

    private void Start()
    {
        // 웨이브 시스템 이벤트 구독
        if (waveSystem != null)
        {
            waveSystem.OnWaveStart += HandleWaveStart;
            waveSystem.OnWaveEnd += HandleWaveEnd;
            waveSystem.OnAllWavesCompleted += HandleAllWavesCompleted;
        }

        // 초기 시간이 저녁이면 게임 시작 시 저녁 모드로 설정
        if (currentTimeOfDay == TimeOfDay.Evening)
        {
            SetEveningMode(false);
        }
        else
        {
            SetMorningMode(false);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (waveSystem != null)
        {
            waveSystem.OnWaveStart -= HandleWaveStart;
            waveSystem.OnWaveEnd -= HandleWaveEnd;
            waveSystem.OnAllWavesCompleted -= HandleAllWavesCompleted;
        }

        // 코루틴 중지
        if (colorTransitionCoroutine != null)
        {
            StopCoroutine(colorTransitionCoroutine);
        }
    }

    #region 웨이브 이벤트 핸들러

    // 웨이브 시작 처리
    private void HandleWaveStart(int waveNumber, string waveName)
    {
        // 저녁으로 전환
        StartCoroutine(TransitionToEvening());
    }

    // 웨이브 종료 처리
    private void HandleWaveEnd(int waveNumber, string waveName)
    {
        // 아침으로 전환
        StartCoroutine(TransitionToMorning());

        // 웨이브 종료 패널 표시
        if (panelStage != null)
        {
            panelStage.SetActive(true);
        }

        // 웨이브 승리/패배 확인 후 일수 증가 처리
        StartCoroutine(HandleDayCounterAfterWaveResult());
    }

    // 웨이브 결과 확인 후 일수 증가 처리 코루틴
    private IEnumerator HandleDayCounterAfterWaveResult()
    {
        // 웨이브 결과 처리 대기 (승리/패배 판정에 시간이 필요할 수 있음)
        yield return new WaitForSeconds(1f);

        // 일수 증가 처리 (첫 번째 웨이브가 아닌 경우)
        if (waveSystem != null && waveSystem.CurrentWave > 1 && dayCounterSystem != null)
        {
            // 일수 증가
            dayCounterSystem.IncrementDay();
        }
    }

    // 모든 웨이브 완료 처리
    private void HandleAllWavesCompleted()
    {
        // 모든 웨이브 완료 시 특별 처리
        // ex) 게임 클리어 화면 또는 다음 스테이지 등
        Debug.Log("모든 웨이브 완료됨! 게임 클리어 또는 다음 스테이지로 진행");
    }

    #endregion

    #region 시간 전환 관리

    // 저녁으로 전환하는 코루틴
    public IEnumerator TransitionToEvening()
    {
        if (isTransitioning || currentTimeOfDay == TimeOfDay.Evening) yield break;

        isTransitioning = true;

        // 전환 효과 표시
        if (eveningVisualEffect != null)
        {
            eveningVisualEffect.SetActive(true);
        }

        // 색상 전환 시작 (배경색과 타일맵 색상)
        if (useColorTransition)
        {
            StartColorTransition(TimeOfDay.Evening);
        }

        // 전환 지연
        yield return new WaitForSeconds(eveningTransitionDuration);

        // 전환 효과 종료
        if (eveningVisualEffect != null)
        {
            eveningVisualEffect.SetActive(false);
        }

        // 저녁 모드 설정
        SetEveningMode();

        isTransitioning = false;
    }

    // 아침으로 전환하는 코루틴
    public IEnumerator TransitionToMorning()
    {
        if (isTransitioning || currentTimeOfDay == TimeOfDay.Morning) yield break;

        isTransitioning = true;

        // 전환 효과 표시
        if (morningVisualEffect != null)
        {
            morningVisualEffect.SetActive(true);
        }

        // 색상 전환 시작 (배경색과 타일맵 색상)
        if (useColorTransition)
        {
            StartColorTransition(TimeOfDay.Morning);
        }

        // 전환 지연
        yield return new WaitForSeconds(morningTransitionDuration);

        // 전환 효과 종료
        if (morningVisualEffect != null)
        {
            morningVisualEffect.SetActive(false);
        }

        // 아침 모드 설정
        SetMorningMode();

        isTransitioning = false;
    }

    // 저녁 모드 설정
    public void SetEveningMode(bool withEvents = true)
    {
        currentTimeOfDay = TimeOfDay.Evening;

        // UI 업데이트
        UpdateUIBasedOnTime(TimeOfDay.Evening);

        // 레이어 기반 활성화/비활성화 업데이트
        if (useLayerBasedActivation)
        {
            UpdateLayersBasedOnTime(TimeOfDay.Evening);
        }

        // 게임플레이 요소 업데이트
        UpdateGameplayForEvening();

        // 애니메이션 없이 바로 색상 적용
        if (useColorTransition && !isTransitioning)
        {
            ApplyTimeBasedColors(TimeOfDay.Evening);
        }

        // 이벤트 발생
        if (withEvents)
        {
            onEveningStart?.Invoke();
        }

        Debug.Log("저녁으로 전환됨: 전투 시작");
    }

    // 아침 모드 설정
    public void SetMorningMode(bool withEvents = true)
    {
        currentTimeOfDay = TimeOfDay.Morning;

        // UI 업데이트
        UpdateUIBasedOnTime(TimeOfDay.Morning);

        // 레이어 기반 활성화/비활성화 업데이트
        if (useLayerBasedActivation)
        {
            UpdateLayersBasedOnTime(TimeOfDay.Morning);
        }

        // 게임플레이 요소 업데이트
        UpdateGameplayForMorning();

        // 애니메이션 없이 바로 색상 적용
        if (useColorTransition && !isTransitioning)
        {
            ApplyTimeBasedColors(TimeOfDay.Morning);
        }

        // 이벤트 발생
        if (withEvents)
        {
            onMorningStart?.Invoke();
        }

        Debug.Log("아침으로 전환됨: 준비 단계");
    }

    #endregion

    #region 색상 전환 관리

    // 색상 전환 시작
    private void StartColorTransition(TimeOfDay targetTime)
    {
        if (mainCameraCache == null) mainCameraCache = Camera.main;
        if (mainCameraCache == null) return;

        // 기존 코루틴 중지
        if (colorTransitionCoroutine != null)
        {
            StopCoroutine(colorTransitionCoroutine);
        }

        // 새 코루틴 시작
        colorTransitionCoroutine = StartCoroutine(
            SwapColor(
                targetTime == TimeOfDay.Morning ? eveningBackgroundColor : morningBackgroundColor,
                targetTime == TimeOfDay.Morning ? morningBackgroundColor : eveningBackgroundColor,
                targetTime == TimeOfDay.Morning ? eveningTilemapColor : morningTilemapColor,
                targetTime == TimeOfDay.Morning ? morningTilemapColor : eveningTilemapColor
            )
        );
    }

    // 색상 전환 코루틴 (배경 및 타일맵)
    private IEnumerator SwapColor(Color startBg, Color endBg, Color startTile, Color endTile)
    {
        float t = 0;
        float duration = currentTimeOfDay == TimeOfDay.Morning ? eveningTransitionDuration : morningTransitionDuration;

        while (t < 1)
        {
            t += Time.deltaTime / (duration * colorTransitionSpeed);

            // 배경색 변경
            if (mainCameraCache != null)
            {
                mainCameraCache.backgroundColor = Color.Lerp(startBg, endBg, t);
            }

            // 모든 타일맵의 색상 변경
            foreach (var tilemap in tilemaps)
            {
                if (tilemap != null)
                {
                    tilemap.color = Color.Lerp(startTile, endTile, t);
                }
            }

            yield return null;
        }

        // 최종 색상 적용
        if (mainCameraCache != null)
        {
            mainCameraCache.backgroundColor = endBg;
        }

        foreach (var tilemap in tilemaps)
        {
            if (tilemap != null)
            {
                tilemap.color = endTile;
            }
        }

        colorTransitionCoroutine = null;
    }

    // 시간에 따른 색상 즉시 적용 (애니메이션 없이)
    private void ApplyTimeBasedColors(TimeOfDay time)
    {
        if (mainCameraCache == null) mainCameraCache = Camera.main;
        if (mainCameraCache == null) return;

        // 배경색 설정
        if (mainCameraCache != null)
        {
            mainCameraCache.backgroundColor = time == TimeOfDay.Morning ? morningBackgroundColor : eveningBackgroundColor;
        }

        // 타일맵 색상 설정
        Color tileColor = time == TimeOfDay.Morning ? morningTilemapColor : eveningTilemapColor;
        foreach (var tilemap in tilemaps)
        {
            if (tilemap != null)
            {
                tilemap.color = tileColor;
            }
        }
    }

    #endregion

    #region UI 및 게임플레이 업데이트

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
    }

    // 레이어 기반 오브젝트 활성화/비활성화 업데이트
    private void UpdateLayersBasedOnTime(TimeOfDay time)
    {
        if (morningLayer < 0 || eveningLayer < 0)
        {
            Debug.LogWarning("MorningOnly 또는 EveningOnly 레이어가 존재하지 않습니다.");
            return;
        }

        if (time == TimeOfDay.Morning)
        {
            ActivateLayerObjects(morningLayer, eveningLayer);
            UpdateCameraSettings(TimeOfDay.Morning);
        }
        else
        {
            ActivateLayerObjects(eveningLayer, morningLayer);
            UpdateCameraSettings(TimeOfDay.Evening);
        }
    }

    // 아침 모드의 게임플레이 업데이트
    private void UpdateGameplayForMorning()
    {
        // 레이어 기반 자원 오브젝트 드래그 및 회전 활성화
        SetResourceObjectsByLayerDraggable(true);

        // 레이어 기반 타워 공격 비활성화
        SetTowerAttackEnabledByLayer(false);

        // 타워 구매/판매 기능 활성화
        SetTowerTradeEnabled(true);

        // 타워 업그레이드 활성화
        SetTowerUpgradeEnabledByLayer(true);

        // 플레이어 관련 설정 (MicrophoneSystem 사용)
        HandlePlayerSettings(true, false);

        // 마이크 시스템 활성화 상태 설정
        if (microphoneSystem != null)
        {
            // 마이크 시스템 자체는 이벤트를 통해 처리됨 (OnMorningStart에서)
            microphoneSystem.SetPlayerActivationEnabled(false);
        }

        // 피로도 리셋 (웨이브 종료 후)
        if (playerGold != null)
        {
            playerGold.ResetFatigue();
        }
    }

    // 저녁 모드의 게임플레이 업데이트
    private void UpdateGameplayForEvening()
    {
        // 레이어 기반 자원 오브젝트 드래그 및 회전 비활성화
        SetResourceObjectsByLayerDraggable(false);

        // 레이어 기반 타워 공격 활성화
        SetTowerAttackEnabledByLayer(true);

        // 타워 구매/판매 기능 비활성화
        SetTowerTradeEnabled(false);

        // 타워 업그레이드 활성화
        SetTowerUpgradeEnabledByLayer(true);

        // 플레이어 관련 설정 (MicrophoneSystem 사용)
        HandlePlayerSettings(false, true);

        // 마이크 시스템 활성화 상태 설정
        if (microphoneSystem != null)
        {
            // 마이크 시스템 자체는 이벤트를 통해 처리됨 (OnEveningStart에서)
            microphoneSystem.SetPlayerActivationEnabled(true);
        }
    }

    #endregion

    #region 게임 오브젝트 및 컴포넌트 관리

    // 시간에 따른 레이어 기반 오브젝트 활성화/비활성화
    private void ActivateLayerObjects(int layerToActivate, int layerToDeactivate)
    {
        if (!useLayerBasedActivation) return;

        // 찾기 전에 존재하는지 확인
        if (layerToActivate < 0 || layerToDeactivate < 0)
        {
            Debug.LogWarning("지정된 레이어 중 하나 이상이 존재하지 않습니다.");
            return;
        }

        // 활성화할 레이어의 오브젝트 찾기
        GameObject[] objectsToActivate = FindObjectsOfType<GameObject>().Where(obj => obj.layer == layerToActivate).ToArray();

        // 비활성화할 레이어의 오브젝트 찾기
        GameObject[] objectsToDeactivate = FindObjectsOfType<GameObject>().Where(obj => obj.layer == layerToDeactivate).ToArray();

        // 활성화/비활성화 처리
        foreach (GameObject obj in objectsToActivate)
        {
            obj.SetActive(true);
        }

        foreach (GameObject obj in objectsToDeactivate)
        {
            obj.SetActive(false);
        }

        Debug.Log($"레이어 {LayerMask.LayerToName(layerToActivate)} 활성화, 레이어 {LayerMask.LayerToName(layerToDeactivate)} 비활성화 완료");
    }

    // 카메라 설정 업데이트
    private void UpdateCameraSettings(TimeOfDay timeOfDay)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        // 기본 컬링 마스크 (모든 레이어)
        int defaultCullingMask = -1;

        if (morningLayer < 0 || eveningLayer < 0) return;

        // 시간에 따라 적절한 레이어 설정
        if (timeOfDay == TimeOfDay.Morning)
        {
            // 아침: MorningOnly 레이어는 표시, EveningOnly 레이어는 숨김
            mainCamera.cullingMask = defaultCullingMask;
            mainCamera.cullingMask |= (1 << morningLayer);
            mainCamera.cullingMask &= ~(1 << eveningLayer);
        }
        else
        {
            // 저녁: EveningOnly 레이어는 표시, MorningOnly 레이어는 숨김
            mainCamera.cullingMask = defaultCullingMask;
            mainCamera.cullingMask |= (1 << eveningLayer);
            mainCamera.cullingMask &= ~(1 << morningLayer);
        }
    }

    // 레이어 기반 자원 오브젝트 드래그 가능 여부 설정
    private void SetResourceObjectsByLayerDraggable(bool draggable)
    {
        if (resourceLayer < 0)
        {
            Debug.LogWarning($"{resourceLayerName} 레이어가 존재하지 않습니다.");
            return;
        }

        // Resource 레이어에 있는 오브젝트 찾기
        GameObject[] resourceObjects = FindObjectsOfType<GameObject>().Where(obj => obj.layer == resourceLayer).ToArray();
        int count = 0;

        foreach (GameObject obj in resourceObjects)
        {
            DraggableResource draggableResource = obj.GetComponent<DraggableResource>();
            if (draggableResource != null)
            {
                draggableResource.SetDraggable(draggable);
                count++;
            }
        }

        Debug.Log($"레이어 '{resourceLayerName}'에 있는 드래그 가능 오브젝트 {count}개를 {(draggable ? "활성화" : "비활성화")}했습니다.");
    }

    // 레이어 기반 타워 공격 활성화/비활성화
    private void SetTowerAttackEnabledByLayer(bool enabled)
    {
        if (towerLayer < 0)
        {
            Debug.LogWarning($"{towerLayerName} 레이어가 존재하지 않습니다.");
            return;
        }

        // Tower 레이어에 있는 오브젝트 찾기
        GameObject[] towerObjects = FindObjectsOfType<GameObject>().Where(obj => obj.layer == towerLayer).ToArray();
        int count = 0;

        if (enabled)
        {
            // 활성화는 SearchTarget 상태로 전환하는 특별 로직이 필요하므로 직접 처리
            foreach (GameObject obj in towerObjects)
            {
                TowerWeapon tower = obj.GetComponent<TowerWeapon>();
                if (tower != null)
                {
                    tower.ChangeState(WeaponState.SearchTarget);
                    count++;
                }
            }
        }
        else
        {
            // 비활성화는 코루틴 중지로 처리
            foreach (GameObject obj in towerObjects)
            {
                TowerWeapon tower = obj.GetComponent<TowerWeapon>();
                if (tower != null)
                {
                    tower.StopAllCoroutines();
                    count++;
                }
            }
        }

        Debug.Log($"레이어 '{towerLayerName}'에 있는 타워 공격 기능 {count}개를 {(enabled ? "활성화" : "비활성화")}했습니다.");
    }

    // 레이어 기반 타워 업그레이드 기능 활성화/비활성화
    private void SetTowerUpgradeEnabledByLayer(bool enabled)
    {
        if (towerLayer < 0)
        {
            Debug.LogWarning($"{towerLayerName} 레이어가 존재하지 않습니다.");
            return;
        }

        // Tower 레이어에 있는 오브젝트 찾기
        GameObject[] towerObjects = FindObjectsOfType<GameObject>().Where(obj => obj.layer == towerLayer).ToArray();
        int count = 0;

        foreach (GameObject obj in towerObjects)
        {
            TowerDataViewer viewer = obj.GetComponent<TowerDataViewer>();
            if (viewer != null)
            {
                viewer.enabled = enabled;
                count++;
            }
        }

        Debug.Log($"레이어 '{towerLayerName}'에 있는 타워 업그레이드 기능 {count}개를 {(enabled ? "활성화" : "비활성화")}했습니다.");
    }

    // 레이어 기반 컴포넌트 활성화/비활성화
    private void SetComponentsEnabledByLayer<T>(int layer, bool enabled) where T : MonoBehaviour
    {
        if (layer < 0)
        {
            Debug.LogWarning($"지정된 레이어가 존재하지 않습니다.");
            return;
        }

        // 특정 레이어에 있는 모든 게임 오브젝트 찾기
        GameObject[] layerObjects = FindObjectsOfType<GameObject>().Where(obj => obj.layer == layer).ToArray();

        int count = 0;
        foreach (GameObject obj in layerObjects)
        {
            T component = obj.GetComponent<T>();
            if (component != null)
            {
                component.enabled = enabled;
                count++;
            }
        }

        Debug.Log($"레이어 '{LayerMask.LayerToName(layer)}'에 있는 {typeof(T).Name} 컴포넌트 {count}개를 {(enabled ? "활성화" : "비활성화")}했습니다.");
    }

    // 타워 구매/판매 기능 활성화/비활성화
    private void SetTowerTradeEnabled(bool enabled)
    {
        TowerSpawner[] towerSpawners = FindObjectsOfType<TowerSpawner>();
        foreach (TowerSpawner spawner in towerSpawners)
        {
            // TowerSpawner에 SetTradeEnabled 메서드가 있는 경우
            try
            {
                // 리플렉션이나 public 메서드를 통해 호출할 수 있음
                spawner.SetTradeEnabled(enabled);
            }
            catch
            {
                // 메서드가 없다면 활성화/비활성화로 처리
                spawner.enabled = enabled;
            }
        }

        // 오브젝트 디텍터(타워 클릭 처리) 활성화/비활성화
        ObjectDetector[] detectors = FindObjectsOfType<ObjectDetector>();
        foreach (ObjectDetector detector in detectors)
        {
            detector.enabled = enabled;
        }

        Debug.Log($"타워 거래 기능이 {(enabled ? "활성화" : "비활성화")}되었습니다.");
    }

    // 플레이어 설정 처리 (MicrophoneSystem 사용)
    private void HandlePlayerSettings(bool activatePlayer, bool enableAttack)
    {
        // 마이크 시스템이 있으면 그것을 사용하여 플레이어 관리
        if (microphoneSystem != null)
        {
            if (activatePlayer)
            {
                // 아침 모드: 기본 위치 (0,0,0)에 플레이어 활성화
                microphoneSystem.ActivatePlayer(Vector3.zero);
            }
            else
            {
                // 저녁 모드: 플레이어 비활성화 (마이크 입력으로 활성화 대기)
                microphoneSystem.DeactivatePlayer();
            }

            // 플레이어 공격 기능 설정
            if (microphoneSystem.PlayerObject != null)
            {
                PlayerMovement playerMovement = microphoneSystem.PlayerObject.GetComponent<PlayerMovement>();
                if (playerMovement != null)
                {
                    playerMovement.SetAttackEnabled(enableAttack);
                }
            }

            Debug.Log($"마이크 시스템을 통해 플레이어 {(activatePlayer ? "활성화" : "비활성화")}, 공격 {(enableAttack ? "활성화" : "비활성화")}");
            return;
        }

        // 이하는 마이크 시스템이 없는 경우 기존 방식 사용 (하위 호환성 유지)

        // PlayerSingleton 클래스가 존재하는지 확인
        System.Type playerSingletonType = System.Type.GetType("PlayerSingleton");
        bool playerSingletonExists = false;
        object playerSingletonInstance = null;

        // 플레이어 싱글턴 존재 여부 확인 (리플렉션 사용)
        if (playerSingletonType != null)
        {
            // Exists 정적 속성 확인
            System.Reflection.PropertyInfo existsProperty = playerSingletonType.GetProperty("Exists",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (existsProperty != null)
            {
                playerSingletonExists = (bool)existsProperty.GetValue(null);

                // Instance 정적 속성 확인
                if (playerSingletonExists)
                {
                    System.Reflection.PropertyInfo instanceProperty = playerSingletonType.GetProperty("Instance",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                    if (instanceProperty != null)
                    {
                        playerSingletonInstance = instanceProperty.GetValue(null);
                    }
                }
            }
        }

        // 싱글턴 존재 시 처리
        if (playerSingletonExists && playerSingletonInstance != null)
        {
            // 플레이어 활성화/비활성화
            MonoBehaviour playerComponent = playerSingletonInstance as MonoBehaviour;
            if (playerComponent != null)
            {
                playerComponent.gameObject.SetActive(activatePlayer);

                // SetAttackEnabled 메서드 호출 시도
                System.Reflection.MethodInfo setAttackEnabledMethod = playerSingletonType.GetMethod("SetAttackEnabled");
                if (setAttackEnabledMethod != null)
                {
                    setAttackEnabledMethod.Invoke(playerSingletonInstance, new object[] { enableAttack });
                }

                System.Reflection.MethodInfo setPlayerActiveMethod = playerSingletonType.GetMethod("SetPlayerActive");
                if (setPlayerActiveMethod != null)
                {
                    setPlayerActiveMethod.Invoke(playerSingletonInstance, new object[] { activatePlayer });
                }
            }

            Debug.Log($"싱글턴 플레이어 {(activatePlayer ? "활성화" : "비활성화")}, 공격 {(enableAttack ? "활성화" : "비활성화")}");
            return;
        }

        // 싱글턴이 없는 경우 기존 방식으로 처리
        PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();
        foreach (PlayerMovement player in players)
        {
            // 플레이어 활성화/비활성화
            player.gameObject.SetActive(activatePlayer);

            // 플레이어 공격 활성화/비활성화
            player.SetAttackEnabled(enableAttack);
        }

        Debug.Log($"플레이어 {(activatePlayer ? "활성화" : "비활성화")}, 공격 {(enableAttack ? "활성화" : "비활성화")}");
    }
    #endregion

    #region 추가된 색상 관련 메서드

    // 카메라 배경색 직접 설정
    public void SetBackgroundColor(Color color)
    {
        if (mainCameraCache == null) mainCameraCache = Camera.main;
        if (mainCameraCache != null)
        {
            mainCameraCache.backgroundColor = color;
        }
    }

    // 타일맵 색상 직접 설정
    public void SetTilemapColor(Color color)
    {
        foreach (var tilemap in tilemaps)
        {
            if (tilemap != null)
            {
                tilemap.color = color;
            }
        }
    }

    // 아침/저녁 배경색 설정
    public void SetDayColors(Color dayBackground, Color nightBackground)
    {
        morningBackgroundColor = dayBackground;
        eveningBackgroundColor = nightBackground;

        // 현재 시간에 맞게 색상 적용
        if (currentTimeOfDay == TimeOfDay.Morning)
        {
            SetBackgroundColor(morningBackgroundColor);
        }
        else
        {
            SetBackgroundColor(eveningBackgroundColor);
        }
    }

    // 아침/저녁 타일맵 색상 설정
    public void SetTilemapColors(Color dayTileColor, Color nightTileColor)
    {
        morningTilemapColor = dayTileColor;
        eveningTilemapColor = nightTileColor;

        // 현재 시간에 맞게 색상 적용
        if (currentTimeOfDay == TimeOfDay.Morning)
        {
            SetTilemapColor(morningTilemapColor);
        }
        else
        {
            SetTilemapColor(eveningTilemapColor);
        }
    }

    // 타일맵 추가
    public void AddTilemap(Tilemap tilemap)
    {
        if (tilemap != null && !tilemaps.Contains(tilemap))
        {
            tilemaps.Add(tilemap);

            // 현재 시간에 맞는 색상 적용
            tilemap.color = currentTimeOfDay == TimeOfDay.Morning ? morningTilemapColor : eveningTilemapColor;

            Debug.Log($"타일맵 '{tilemap.name}'이(가) 색상 변경 목록에 추가되었습니다.");
        }
    }

    // 타일맵 제거
    public void RemoveTilemap(Tilemap tilemap)
    {
        if (tilemap != null && tilemaps.Contains(tilemap))
        {
            tilemaps.Remove(tilemap);

            // 원래 색상으로 복원
            tilemap.color = Color.white;

            Debug.Log($"타일맵 '{tilemap.name}'이(가) 색상 변경 목록에서 제거되었습니다.");
        }
    }

    // 색상 전환 사용 여부 설정
    public void SetColorTransitionEnabled(bool enabled)
    {
        useColorTransition = enabled;

        // 색상 전환이 비활성화되면 모든 색상을 기본값으로 리셋
        if (!enabled)
        {
            if (mainCameraCache == null) mainCameraCache = Camera.main;
            if (mainCameraCache != null)
            {
                mainCameraCache.backgroundColor = Color.black;
            }

            foreach (var tilemap in tilemaps)
            {
                if (tilemap != null)
                {
                    tilemap.color = Color.white;
                }
            }

            Debug.Log("색상 전환 기능이 비활성화되었습니다. 모든 색상이 기본값으로 리셋되었습니다.");
        }
        else
        {
            // 색상 전환이 활성화되면 현재 시간에 맞는 색상 적용
            ApplyTimeBasedColors(currentTimeOfDay);
            Debug.Log("색상 전환 기능이 활성화되었습니다. 현재 시간에 맞는 색상이 적용되었습니다.");
        }
    }

    #endregion

    // 디버그용 시간 전환 메서드
    public void ToggleTimeOfDay()
    {
        if (currentTimeOfDay == TimeOfDay.Morning)
        {
            StartCoroutine(TransitionToEvening());
        }
        else
        {
            StartCoroutine(TransitionToMorning());
        }
    }
}