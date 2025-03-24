using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(TowerWeapon))]
public class TowerDragRotate : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private bool isDraggable = true;
    [SerializeField] private float dragSmoothing = 5f;
    [SerializeField] private float fatiguePerTowerMovement = 2f;

    [Header("Rotation Settings")]
    [SerializeField] private KeyCode rotateKey = KeyCode.R;
    [SerializeField] private float rotationDuration = 0.2f;
    [SerializeField] private AudioClip rotateSound;

    [Header("Visual Settings")]
    [SerializeField] private Color dragColor = new Color(0.7f, 0.7f, 1f, 0.8f);
    [SerializeField] private float dragScale = 1.1f;
    [SerializeField] private bool showRangeOnDrag = true;

    // Events
    public UnityEvent onDragStart;
    public UnityEvent onDragEnd;
    public UnityEvent onRotate;

    // References
    private TowerWeapon towerWeapon;
    private TowerSpawner towerSpawner;
    private Grid grid;
    private Tilemap tilemap;
    private PlayerGold playerGold;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private GameObject rangeIndicator;

    // Drag state
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Vector3 targetPosition;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Color originalColor;
    private Vector3Int originalCell;
    private bool isRotating = false;

    private void Awake()
    {
        towerWeapon = GetComponent<TowerWeapon>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Get audio source or add one if needed
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && rotateSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // Find necessary components
        towerSpawner = FindObjectOfType<TowerSpawner>();
        grid = FindObjectOfType<Grid>();
        if (towerSpawner != null)
        {
            tilemap = towerSpawner.GetTilemap();
        }
        else
        {
            tilemap = FindObjectOfType<Tilemap>();
            Debug.LogWarning("TowerSpawner not found, using first Tilemap found in scene");
        }

        playerGold = FindObjectOfType<PlayerGold>();

        // Store the original position
        originalPosition = transform.position;
        if (grid != null)
        {
            originalCell = grid.WorldToCell(originalPosition);
        }

        // Create range indicator if needed
        if (rangeIndicator == null && towerWeapon != null && showRangeOnDrag)
        {
            CreateRangeIndicator();
        }
    }

    void Update()
    {
        if (isDragging)
        {
            // 목표 위치로 부드럽게 이동
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * dragSmoothing);

            // 이소메트릭 뷰 지원 (z 값 조정)
            Vector3 position = transform.position;
            position.z = position.y;
            transform.position = position;

            // 회전 키 체크
            if (Input.GetKeyDown(rotateKey) && !isRotating)
            {
                StartCoroutine(RotateTower());
            }
        }
    }

    private void OnMouseDown()
    {
        // Ignore if over UI or dragging is disabled
        if (!isDraggable || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        StartDragging();
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        // 마우스 위치 계산
        Vector3 mousePosition = GetMouseWorldPosition();
        targetPosition = mousePosition + dragOffset;

        // 범위 표시기 업데이트
        UpdateRangeIndicator();
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        EndDragging();
    }

    private void StartDragging()
    {
        isDragging = true;

        // 마우스 위치와 오브젝트 위치의 차이 저장
        Vector3 mousePosition = GetMouseWorldPosition();
        dragOffset = transform.position - mousePosition;

        // 시각 효과 적용
        ApplyDragVisualEffects(true);

        // 타워 공격 일시 중지
        if (towerWeapon != null)
        {
            towerWeapon.SetAttackEnabled(false);
        }

        // 타워 관리에서 제거 (파괴하지 않음)
        if (grid != null && towerSpawner != null)
        {
            Vector3Int cellPos = grid.WorldToCell(transform.position);
            towerSpawner.RemoveTowerWithoutDestroy(cellPos);
            originalCell = cellPos;
        }

        // 범위 표시기 활성화
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true);
        }

        // 드래그 시작 이벤트 발생
        onDragStart?.Invoke();
    }

    private void EndDragging()
    {
        isDragging = false;

        // 원래 위치로 돌아갈지 결정
        bool shouldReturn = false;

        // 그리드를 사용한 위치 검증 (그리드가 있는 경우)
        if (grid != null && towerSpawner != null)
        {
            Vector3Int newCell = grid.WorldToCell(transform.position);

            // 원래 위치와 다른 경우 타워 위치 업데이트
            if (newCell != originalCell)
            {
                // 새 위치 유효성 체크는 건너뛰고 항상 허용

                // 피로도 증가 (이동 거리에 따라)
                int distance = Mathf.RoundToInt(Vector3Int.Distance(originalCell, newCell));

                // 새 위치에 타워 등록
                if (towerSpawner.IsTileOccupied(newCell))
                {
                    // 위치가 이미 점유되어 있으면 원래 위치로 돌아감
                    shouldReturn = true;
                }
                else
                {
                    // 타워 위치 업데이트
                    Vector3 cellCenter = tilemap.GetCellCenterWorld(newCell);
                    cellCenter.z = cellCenter.y; // 이소메트릭 뷰 조정
                    transform.position = cellCenter;

                    // 새 위치에 타워 등록
                    towerSpawner.RegisterExistingTower(newCell, gameObject);

                    // 피로도 증가
                    if (playerGold != null)
                    {
                        playerGold.IncreaseFatigue();
                    }

                    // 원래 셀 업데이트
                    originalCell = newCell;
                }
            }
            else
            {
                // 이동하지 않은 경우, 원래 위치에 다시 등록
                towerSpawner.RegisterExistingTower(originalCell, gameObject);
            }
        }
        else
        {
            // 그리드 없이 단순 이동 (일반적인 드래그 방식)
            // 여기서는 추가 검증 없이 현재 위치를 유지
        }

        if (shouldReturn)
        {
            // 원래 위치로 돌아감
            StartCoroutine(MoveToPosition(originalPosition));

            // 원래 위치에 다시 등록
            if (grid != null && towerSpawner != null)
            {
                towerSpawner.RegisterExistingTower(originalCell, gameObject);
            }
        }

        // 시각 효과 제거
        ApplyDragVisualEffects(false);

        // 타워 공격 다시 활성화
        if (towerWeapon != null)
        {
            towerWeapon.SetAttackEnabled(true);
        }

        // 범위 표시기 비활성화
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(false);
        }

        // 드래그 종료 이벤트 발생
        onDragEnd?.Invoke();
    }

    // 마우스 월드 좌표 가져오기
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
        mousePosition.z = 0;
        return mousePosition;
    }

    // 드래그 중 시각 효과 적용/제거
    private void ApplyDragVisualEffects(bool isDragging)
    {
        if (spriteRenderer != null)
        {
            // 색상 변경
            spriteRenderer.color = isDragging ? dragColor : originalColor;
        }

        // 크기 변경 (x축 스케일의 부호는 유지하면서 크기만 조정)
        if (isDragging)
        {
            float xSign = Mathf.Sign(transform.localScale.x);
            float xMagnitude = Mathf.Abs(originalScale.x) * dragScale;
            transform.localScale = new Vector3(xSign * xMagnitude, originalScale.y * dragScale, originalScale.z * dragScale);
        }
        else
        {
            float xSign = Mathf.Sign(transform.localScale.x);
            float xMagnitude = Mathf.Abs(originalScale.x);
            transform.localScale = new Vector3(xSign * xMagnitude, originalScale.y, originalScale.z);
        }
    }

    // 회전 코루틴
    private IEnumerator RotateTower()
    {
        isRotating = true;

        // 회전 효과음 재생
        if (audioSource != null && rotateSound != null)
        {
            audioSource.PlayOneShot(rotateSound);
        }

        // TowerWeapon의 좌우반전 기능 사용
        if (towerWeapon != null)
        {
            towerWeapon.ToggleFlip();
        }
        else
        {
            // TowerWeapon이 없는 경우 직접 스프라이트 반전
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !spriteRenderer.flipX;
            }
        }

        // 회전 이벤트 발생
        onRotate?.Invoke();

        // 애니메이션 시간 대기
        float elapsedTime = 0f;
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        isRotating = false;
    }

    // 위치로 이동하는 코루틴
    private IEnumerator MoveToPosition(Vector3 position)
    {
        float duration = 0.3f;
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / duration;

            // 위치 보간
            transform.position = Vector3.Lerp(startPosition, position, progress);

            // 이소메트릭 뷰 지원 (z 값 조정)
            Vector3 currentPos = transform.position;
            currentPos.z = currentPos.y;
            transform.position = currentPos;

            yield return null;
        }

        // 최종 위치 설정
        transform.position = position;

        // 이소메트릭 뷰 지원 (z 값 조정)
        Vector3 finalPos = transform.position;
        finalPos.z = finalPos.y;
        transform.position = finalPos;
    }

    // 범위 표시기 생성
    private void CreateRangeIndicator()
    {
        // 범위 표시 오브젝트 생성
        rangeIndicator = new GameObject("RangeIndicator_" + gameObject.name);
        rangeIndicator.transform.SetParent(transform);
        rangeIndicator.transform.localPosition = Vector3.zero;

        // 스프라이트 렌더러 추가
        SpriteRenderer indicatorRenderer = rangeIndicator.AddComponent<SpriteRenderer>();

        // 간단한 원형 텍스처 생성
        float range = towerWeapon != null ? towerWeapon.Range : 3f;
        int textureSize = Mathf.CeilToInt(range * 2 * 32);

        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] colors = new Color[textureSize * textureSize];

        // 투명 색상으로 초기화
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }

        // 원 테두리 그리기
        int radius = textureSize / 2;
        int centerX = radius;
        int centerY = radius;

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));

                // 원 테두리 (약간의 두께)
                if (distance > radius - 2 && distance < radius)
                {
                    colors[y * textureSize + x] = new Color(0, 0.7f, 1f, 0.3f);
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        // 스프라이트 생성
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize),
                                    new Vector2(0.5f, 0.5f), 32);

        indicatorRenderer.sprite = sprite;
        indicatorRenderer.sortingOrder = -1; // 타워 아래에 표시

        // 크기 설정
        float scale = range * 2f / (textureSize / 32f);
        rangeIndicator.transform.localScale = new Vector3(scale, scale, 1);

        // 초기에는 비활성화
        rangeIndicator.SetActive(false);
    }

    // 범위 표시기 업데이트
    private void UpdateRangeIndicator()
    {
        if (rangeIndicator == null) return;

        // 항상 부모 타워 위치에 고정
        rangeIndicator.transform.position = new Vector3(
            transform.position.x,
            transform.position.y,
            transform.position.z - 0.01f // 살짝 앞에 표시
        );
    }

    // 드래그 가능 여부 설정 (외부에서 호출 가능)
    public void SetDraggable(bool draggable)
    {
        isDraggable = draggable;
    }

    // 회전 메서드 (외부에서 호출 가능)
    public void RotateObject()
    {
        if (!isRotating)
        {
            StartCoroutine(RotateTower());
        }
    }
}