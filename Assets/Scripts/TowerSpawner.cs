using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Collections;

public class TowerSpawner : MonoBehaviour
{
    [SerializeField]
    private List<TowerTemplate> towerTemplates;// 여러 종류의 타워 템플릿

    [SerializeField]
    private EnemySpawner enemySpawner; // 현재 맵에 존재하는 적 리스트 정보

    [SerializeField]
    private Grid grid; // 타일맵이 속한 Grid 컴포넌트

    [SerializeField]
    private PlayerGold playerGold; // 플레이어 골드/피로도 참조

    [SerializeField]
    private SystemTextViewer systemTextViewer; // 시스템 메시지 뷰어

    [SerializeField]
    private Tilemap tilemap; // 타워 배치 가능한 타일맵

    [Header("Tower Placement Settings")]
    [SerializeField]
    private float fatiguePerTower = 10f; // 타워 당 증가하는 피로도
    [SerializeField]
    private KeyCode flipKey = KeyCode.Q; // 좌우반전 키
    [SerializeField]
    private float flipAnimationDuration = 0.2f; // 좌우반전 애니메이션 시간
    [SerializeField]
    private AudioClip flipSound; // 좌우반전 효과음

    // 배치된 타워들을 월드 좌표(셀 좌표) 기준으로 관리
    private Dictionary<Vector3Int, GameObject> placedTowers = new Dictionary<Vector3Int, GameObject>();

    // 현재 선택된 타워 종류 인덱스 (타워 템플릿 리스트 내의 인덱스)
    private int selectedTowerIndex = 0;

    // 타워 배치 모드 활성화 여부
    private bool isOnTowerButton = false;
    private GameObject followTowerClone = null;

    // 좌우반전 상태
    private bool isFlipped = false;
    private bool isFlipping = false;
    private AudioSource audioSource;

    // 타일맵 가져오기
    public Tilemap GetTilemap() => tilemap;

    private void Awake()
    {
        // 오디오 소스 컴포넌트 가져오기/추가
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && flipSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // PlayerGold 참조 확인
        if (playerGold == null)
        {
            playerGold = FindObjectOfType<PlayerGold>();
            if (playerGold == null)
            {
                Debug.LogError("PlayerGold 컴포넌트를 찾을 수 없습니다.");
            }
        }
    }

    // 선택한 타워 종류를 설정하고 배치 모드에 들어감
    public void SelectAndReadyTower(int index)
    {
        if (index < 0 || index >= towerTemplates.Count)
        {
            Debug.LogError("잘못된 타워 인덱스");
            return;
        }

        selectedTowerIndex = index;

        // 배치 모드 진입 전에 골드 체크
        if (towerTemplates[selectedTowerIndex].weapons[0].cost > playerGold.CurrentGold)
        {
            systemTextViewer.PrintText(SystemType.Money);
            return;
        }

        // 기존 미리보기 타워 정리
        ClearFollowTower();

        isOnTowerButton = true;
        isFlipped = false; // 좌우반전 상태 초기화

        // 선택한 타워의 followTowerPrefab을 생성하여 미리 배치 미리보기 역할
        followTowerClone = Instantiate(towerTemplates[selectedTowerIndex].followTowerPrefab);

        // 필요하다면 followTowerClone의 위치 및 기타 세팅을 여기서 진행
        StartCoroutine(OnTowerCancelSystem());
    }

    public void ReadyToSpawnTower()
    {
        if (isOnTowerButton)
        {
            return;
        }
        // 주석 처리된 코드...
    }

    void Update()
    {
        if (isOnTowerButton && followTowerClone != null)
        {
            // 마우스 위치 가져오기
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            followTowerClone.transform.position = new Vector3(mousePosition.x, mousePosition.y, 0);

            // 이소메트릭 뷰 지원
            Vector3 position = followTowerClone.transform.position;
            position.z = position.y;
            followTowerClone.transform.position = position;

            // Q키 입력 검사 (좌우반전)
            if (Input.GetKeyDown(flipKey) && !isFlipping)
            {
                StartCoroutine(FlipPreviewTower());
            }
        }
    }

    // 좌우반전 코루틴
    private IEnumerator FlipPreviewTower()
    {
        if (followTowerClone == null) yield break;

        isFlipping = true;

        // 효과음 재생
        if (flipSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(flipSound);
        }

        // 현재 x 스케일 값
        float startScaleX = followTowerClone.transform.localScale.x;
        float targetScaleX = -startScaleX; // 부호 반전
        float elapsedTime = 0f;

        // 반전 애니메이션
        while (elapsedTime < flipAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / flipAnimationDuration;

            // 스케일 보간
            float currentScaleX = Mathf.Lerp(startScaleX, targetScaleX, progress);
            followTowerClone.transform.localScale = new Vector3(currentScaleX, followTowerClone.transform.localScale.y, followTowerClone.transform.localScale.z);

            yield return null;
        }

        // 최종 스케일 설정
        followTowerClone.transform.localScale = new Vector3(targetScaleX, followTowerClone.transform.localScale.y, followTowerClone.transform.localScale.z);

        // 반전 상태 토글
        isFlipped = !isFlipped;
        isFlipping = false;
    }

    public void SpawnTower(Vector3Int cellPosition)
    {
        if (isOnTowerButton == false)
        {
            return;
        }
        if (IsTileOccupied(cellPosition)) return; // 이미 타워가 존재하면 실행 X

        TowerTemplate selectedTower = towerTemplates[selectedTowerIndex];

        if (selectedTower.weapons[0].cost > playerGold.CurrentGold)
        {
            systemTextViewer.PrintText(SystemType.Money);
            return;
        }

        Vector3 towerPosition = tilemap.GetCellCenterWorld(cellPosition); // 타일 중심의 월드 좌표

        // Isometric 뷰를 위한 z 위치 조정 (y와 동일하게)
        towerPosition.z = towerPosition.y;

        GameObject newTower = Instantiate(selectedTower.towerPrefab, towerPosition, Quaternion.identity); // 타워 생성

        // 좌우반전 상태 적용
        if (isFlipped)
        {
            // 스프라이트 렌더러 가져와서 좌우반전 적용
            SpriteRenderer[] renderers = newTower.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.flipX = true;
            }

            // 또는 스케일을 사용하여 반전
            Vector3 scale = newTower.transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            newTower.transform.localScale = scale;
        }

        TowerWeapon towerWeapon = newTower.GetComponent<TowerWeapon>();

        if (towerWeapon != null)
        {
            towerWeapon.Setup(selectedTower, enemySpawner, playerGold, towerPosition); // Setup 호출

            // 좌우반전 상태 전달 (TowerWeapon에 관련 기능이 있는 경우)
            if (isFlipped && towerWeapon.GetType().GetMethod("SetFlipped") != null)
            {
                towerWeapon.GetType().GetMethod("SetFlipped").Invoke(towerWeapon, new object[] { true });
            }
        }

        isOnTowerButton = false;
        placedTowers[cellPosition] = newTower; // 셀 좌표와 타워 연결
        playerGold.CurrentGold -= selectedTower.weapons[0].cost; // 골드 감소

        // 타워 배치에 따른 피로도 증가
        playerGold.IncreaseFatigue(); // 피로도 증가 메서드 호출

        // 배치 모드 종료 및 리소스 정리
        EndPlacementMode();

        Debug.Log($"Tower placed at {cellPosition}");
    }

    // 타워 배치 모드 종료
    private void EndPlacementMode()
    {
        StopCoroutine("OnTowerCancelSystem");
        ClearFollowTower();
    }

    // 미리보기 타워 정리
    private void ClearFollowTower()
    {
        if (followTowerClone != null)
        {
            Destroy(followTowerClone);
            followTowerClone = null;
        }
    }

    public void RemoveTower(Vector3Int cellPosition)
    {
        if (placedTowers.TryGetValue(cellPosition, out GameObject tower)) // 타워 찾기
        {
            Destroy(tower); // 타워 오브젝트 제거
            placedTowers.Remove(cellPosition); // 관리 목록에서 제거

            Debug.Log($"Tower removed from {cellPosition}");
        }
        else
        {
            Debug.Log("No tower found at the specified position.");
        }
    }

    private IEnumerator OnTowerCancelSystem()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                isOnTowerButton = false;
                ClearFollowTower();
                break;
            }
            yield return null;
        }
    }

    public bool IsTileOccupied(Vector3Int cellPosition)
    {
        return placedTowers.ContainsKey(cellPosition);
    }
}