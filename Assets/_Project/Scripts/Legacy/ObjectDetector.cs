using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectDetector : MonoBehaviour
{
    [SerializeField] private TowerSpawner towerSpawner;
    [SerializeField] private TowerDataViewer towerDataViewer;

    [Header("Drag Settings")]
    [SerializeField] private bool enableDrag = true;
    [SerializeField] private KeyCode rotateKey = KeyCode.R;
    [SerializeField] private bool debugMode = true;

    private Camera mainCamera;
    private GameObject selectedTower = null;
    private TowerWeapon selectedTowerWeapon = null;
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Vector3 originalPosition;
    private Vector3Int originalCell;

    // 상태 추적을 위한 변수
    private float clickStartTime;
    private bool mouseButtonDown = false;
    private bool processingClick = false;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        // 시작 시 모든 타워에 태그 및 콜라이더 설정
        EnsureAllTowersHaveTagAndCollider();
    }

    // 기존 타워에 태그 및 콜라이더 추가
    private void EnsureAllTowersHaveTagAndCollider()
    {
        TowerWeapon[] towers = FindObjectsOfType<TowerWeapon>();
        foreach (TowerWeapon tower in towers)
        {
            tower.gameObject.tag = "Tower";

            // 콜라이더 없으면 추가
            if (tower.GetComponent<Collider2D>() == null)
            {
                BoxCollider2D collider = tower.gameObject.AddComponent<BoxCollider2D>();
                SpriteRenderer renderer = tower.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    collider.size = renderer.sprite.bounds.size * 1.2f;
                }
            }
        }

        Debug.Log($"Ensured {towers.Length} towers have tags and colliders");
    }

    void Update()
    {
        // 상태 초기화 - 모든 상태 변수가 올바르게 초기화됨을 보장
        if (selectedTower == null) isDragging = false;

        // UI 클릭 확인
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 마우스 버튼 상태 추적
        if (Input.GetMouseButtonDown(0))
        {
            mouseButtonDown = true;
            clickStartTime = Time.time;
            processingClick = true;
            HandleMouseDown();
        }

        // 드래그 중 처리
        if (isDragging && selectedTower != null)
        {
            HandleDragging();
        }

        // 마우스 버튼 업 처리
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                EndDrag();
            }

            mouseButtonDown = false;
            processingClick = false;
        }
    }

    private void HandleMouseDown()
    {
        // 월드 좌표 변환
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.z = 0f;

        // 타워 감지 - 다양한 방법 시도
        TowerWeapon towerWeapon = null;

        // 1. 레이캐스트 - 정확한 지점 확인
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);
        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Tower"))
            {
                towerWeapon = hit.collider.GetComponent<TowerWeapon>();
                if (towerWeapon == null)
                {
                    towerWeapon = hit.collider.GetComponentInParent<TowerWeapon>();
                }

                if (towerWeapon != null)
                {
                    Debug.Log("Tower detected by raycast: " + towerWeapon.name);
                }
            }
        }

        // 2. 오버랩 - 여러 콜라이더 확인
        if (towerWeapon == null)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPosition, 0.5f);
            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Tower"))
                {
                    towerWeapon = collider.GetComponent<TowerWeapon>();
                    if (towerWeapon == null)
                    {
                        towerWeapon = collider.GetComponentInParent<TowerWeapon>();
                    }

                    if (towerWeapon != null)
                    {
                        Debug.Log("Tower detected by overlap: " + towerWeapon.name);
                        break;
                    }
                }
            }
        }

        // 3. 거리 기반 - 가장 가까운 타워 확인
        if (towerWeapon == null)
        {
            towerWeapon = FindClosestTower(worldPosition, 0.8f);
            if (towerWeapon != null)
            {
                Debug.Log("Tower detected by distance: " + towerWeapon.name);
            }
        }

        // 타워 감지 성공
        if (towerWeapon != null)
        {
            Debug.Log("Tower clicked: " + towerWeapon.gameObject.name);

            // 정보 패널 표시
            towerDataViewer.OnPanel(towerWeapon.transform);

            // 드래그 설정
            if (enableDrag)
            {
                selectedTower = towerWeapon.gameObject;
                selectedTowerWeapon = towerWeapon;
                StartDrag(worldPosition);
            }
            return;
        }

        // 타워가 없으면 타일에 타워 생성
        Vector3Int cellPosition = towerSpawner.GetTilemap().WorldToCell(worldPosition);

        if (!towerSpawner.IsTileOccupied(cellPosition))
        {
            Debug.Log($"Spawning tower at {cellPosition}");
            towerSpawner.SpawnTower(cellPosition);
        }
    }

    private TowerWeapon FindClosestTower(Vector3 position, float maxDistance)
    {
        TowerWeapon[] towers = FindObjectsOfType<TowerWeapon>();
        TowerWeapon closest = null;
        float closestDist = maxDistance;

        foreach (TowerWeapon tower in towers)
        {
            float dist = Vector2.Distance(position, tower.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = tower;
            }
        }

        return closest;
    }

    private void StartDrag(Vector3 clickPosition)
    {
        if (selectedTower == null) return;

        isDragging = true;
        Debug.Log("Start dragging tower: " + selectedTower.name);

        originalPosition = selectedTower.transform.position;
        dragOffset = originalPosition - clickPosition;

        // 타일맵 검사
        if (towerSpawner != null && towerSpawner.GetTilemap() != null)
        {
            originalCell = towerSpawner.GetTilemap().WorldToCell(originalPosition);

            // 타워 스포너 딕셔너리에서 임시 제거
            towerSpawner.RemoveTowerWithoutDestroy(originalCell);
            Debug.Log("Removed tower from cell: " + originalCell);
        }
        else
        {
            Debug.LogError("TowerSpawner or Tilemap not found!");
        }

        // 공격 비활성화
        if (selectedTowerWeapon != null)
        {
            selectedTowerWeapon.SetAttackEnabled(false);
        }

        // 시각적 표시
        SpriteRenderer renderer = selectedTower.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = new Color(1f, 1f, 1f, 0.7f);
        }
    }

    private void HandleDragging()
    {
        // 마우스 위치로 이동
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        Vector3 newPosition = mousePosition + dragOffset;
        selectedTower.transform.position = newPosition;

        // 이소메트릭 조정
        selectedTower.transform.position = new Vector3(
            selectedTower.transform.position.x,
            selectedTower.transform.position.y,
            selectedTower.transform.position.y
        );

        // 회전 처리
        if (Input.GetKeyDown(rotateKey) && selectedTowerWeapon != null)
        {
            selectedTowerWeapon.ToggleFlip();
            Debug.Log("Tower flipped");
        }
    }

    private void EndDrag()
    {
        if (!isDragging || selectedTower == null) return;

        isDragging = false;
        Debug.Log("End dragging tower: " + selectedTower.name);

        // 새 위치 계산
        Vector3Int newCell = towerSpawner.GetTilemap().WorldToCell(selectedTower.transform.position);

        if (newCell != originalCell)
        {
            // 셀 검사
            if (!towerSpawner.IsTileOccupied(newCell))
            {
                // 새 위치에 등록
                Vector3 centerPos = towerSpawner.GetTilemap().GetCellCenterWorld(newCell);
                centerPos.z = centerPos.y;
                selectedTower.transform.position = centerPos;

                towerSpawner.RegisterExistingTower(newCell, selectedTower);
                Debug.Log("Tower moved to new position: " + newCell);

                // 피로도 증가
                PlayerGold playerGold = FindObjectOfType<PlayerGold>();
                if (playerGold != null)
                {
                    playerGold.IncreaseFatigue();
                }
            }
            else
            {
                // 원래 위치로 복귀
                selectedTower.transform.position = originalPosition;
                towerSpawner.RegisterExistingTower(originalCell, selectedTower);
                Debug.Log("Tower returned to original position - cell occupied");
            }
        }
        else
        {
            // 원래 위치에 다시 등록
            towerSpawner.RegisterExistingTower(originalCell, selectedTower);
            Debug.Log("Tower registered at original position");
        }

        // 공격 다시 활성화
        if (selectedTowerWeapon != null)
        {
            selectedTowerWeapon.SetAttackEnabled(true);
        }

        // 시각적 표시 원복
        SpriteRenderer renderer = selectedTower.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.white;
        }

        // 선택 초기화
        selectedTower = null;
        selectedTowerWeapon = null;
    }

    // 비활성화 시 상태 초기화
    private void OnDisable()
    {
        if (isDragging && selectedTower != null)
        {
            EndDrag();
        }
    }
}