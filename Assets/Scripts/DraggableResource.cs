using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DraggableResource : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private bool isDraggable = true; // 드래그 가능 여부
    [SerializeField] private float dragSmoothing = 5f; // 드래그 시 이동 부드러움 정도
    [SerializeField] private bool returnToOriginalPosition = false; // 드래그 후 원래 위치로 돌아갈지 여부
    [SerializeField] private LayerMask blockedLayers; // 배치 불가능한 레이어

    [Header("Visual Effects")]
    [SerializeField] private Color dragColor = new Color(1f, 1f, 1f, 0.8f); // 드래그 중 색상
    [SerializeField] private float dragScale = 1.1f; // 드래그 중 크기 배율
    [SerializeField] private bool showShadowOnDrag = true; // 드래그 중 그림자 표시 여부

    [Header("Flip Settings")]
    [SerializeField] private KeyCode flipKey = KeyCode.Q; // 좌우반전 키
    [SerializeField] private float flipDuration = 0.2f; // 좌우반전 애니메이션 시간

    [Header("Events")]
    public UnityEvent onDragStart; // 드래그 시작 이벤트
    public UnityEvent onDragEnd; // 드래그 종료 이벤트
    public UnityEvent onFlip; // 좌우반전 이벤트

    private bool isDragging = false; // 현재 드래그 중인지 여부
    private Vector3 dragOffset; // 드래그 시 오브젝트와 마우스 간의 오프셋
    private Vector3 targetPosition; // 드래그 중 목표 위치
    private Vector3 originalPosition; // 원래 위치
    private Vector3 originalScale; // 원래 크기
    private Color originalColor; // 원래 색상
    private SpriteRenderer spriteRenderer; // 스프라이트 렌더러
    private ResourceObject resourceObject; // 재화 오브젝트 참조
    private GameObject shadowObj; // 그림자 오브젝트
    private bool isFlipped = false; // 좌우반전 상태
    private bool isFlipping = false; // 좌우반전 애니메이션 중인지 여부

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        resourceObject = GetComponent<ResourceObject>();
        originalScale = transform.localScale;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // 그림자 생성
        if (showShadowOnDrag)
        {
            CreateShadow();
        }
    }

    private void Start()
    {
        originalPosition = transform.position;

        // 그림자 초기에 비활성화
        if (shadowObj != null)
        {
            shadowObj.SetActive(false);
        }
    }

    private void Update()
    {
        // 드래그 중일 때만 처리
        if (isDragging)
        {
            // 목표 위치로 부드럽게 이동
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.unscaledDeltaTime * dragSmoothing);

            // 이소메트릭 뷰 지원 (z 값 조정)
            Vector3 position = transform.position;
            position.z = position.y;
            transform.position = position;

            // 드래그 중 Q키 입력 감지 (좌우반전)
            if (Input.GetKeyDown(flipKey) && !isFlipping)
            {
                StartCoroutine(FlipHorizontally());
            }
        }
    }

    private void OnMouseDown()
    {
        // 드래그 불가능하거나 이벤트 시스템UI가 클릭된 경우 무시
        if (!isDraggable || EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 드래그 시작
        StartDragging();
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        // 마우스 위치 계산
        Vector3 mousePosition = GetMouseWorldPosition();
        targetPosition = mousePosition + dragOffset;

        // 그림자 위치 업데이트
        UpdateShadowPosition();
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;

        // 드래그 종료
        EndDragging();
    }

    private void StartDragging()
    {
        isDragging = true;

        // 마우스 위치와 오브젝트 위치의 차이 저장
        Vector3 mousePosition = GetMouseWorldPosition();
        dragOffset = transform.position - mousePosition;

        // 드래그 시작 효과 적용
        ApplyDragVisualEffects(true);

        // 드래그 시작 이벤트 발생
        onDragStart?.Invoke();

        // 그림자 표시
        if (shadowObj != null)
        {
            shadowObj.SetActive(true);
        }
    }

    private void EndDragging()
    {
        isDragging = false;

        // 배치 위치 유효성 검사
        bool validPlacement = CheckValidPlacement();

        // 원래 위치로 돌아가기 설정이거나 배치 위치가 유효하지 않은 경우
        if (returnToOriginalPosition || !validPlacement)
        {
            // 원래 위치로 복귀
            StartCoroutine(MoveToPosition(originalPosition));
        }
        else
        {
            // 새 위치로 확정
            originalPosition = transform.position;
        }

        // 드래그 효과 제거
        ApplyDragVisualEffects(false);

        // 드래그 종료 이벤트 발생
        onDragEnd?.Invoke();

        // 그림자 숨기기
        if (shadowObj != null)
        {
            shadowObj.SetActive(false);
        }
    }

    private bool CheckValidPlacement()
    {
        // 배치 위치 유효성 검사 (충돌 등)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 0.5f, blockedLayers);

        // 블록 레이어와 충돌하는 콜라이더가 있으면 유효하지 않음
        return colliders.Length == 0;
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

    // 마우스 월드 좌표 가져오기
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
        mousePosition.z = 0;
        return mousePosition;
    }

    // 좌우반전 코루틴
    private IEnumerator FlipHorizontally()
    {
        isFlipping = true;

        // 좌우반전 이벤트 발생
        onFlip?.Invoke();

        // 스프라이트 렌더러 반전 처리
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !spriteRenderer.flipX;
        }

        // 애니메이션 진행
        float elapsedTime = 0f;
        while (elapsedTime < flipDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // 반전 상태 토글
        isFlipped = !isFlipped;
        isFlipping = false;
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

    // 그림자 생성 메소드
    private void CreateShadow()
    {
        // 이미 그림자가 있으면 생성 안함
        if (shadowObj != null) return;

        // 그림자 오브젝트 생성
        shadowObj = new GameObject("Shadow_" + gameObject.name);
        shadowObj.transform.SetParent(transform.parent);

        // 스프라이트 렌더러 복사
        SpriteRenderer shadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            shadowRenderer.sprite = spriteRenderer.sprite;
            shadowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1; // 본체보다 아래에 렌더링
            shadowRenderer.color = new Color(0, 0, 0, 0.3f); // 반투명 검정색
        }

        // 그림자 위치 및 크기 설정
        shadowObj.transform.position = new Vector3(transform.position.x, transform.position.y - 0.2f, transform.position.z);
        shadowObj.transform.localScale = transform.localScale;

        // 초기에는 비활성화
        shadowObj.SetActive(false);
    }

    // 그림자 위치 업데이트
    private void UpdateShadowPosition()
    {
        if (shadowObj == null) return;

        // 그림자 위치 업데이트 (약간 아래에 표시)
        shadowObj.transform.position = new Vector3(targetPosition.x, targetPosition.y - 0.2f, targetPosition.z);

        // 그림자도 좌우반전 상태에 맞춤
        SpriteRenderer shadowRenderer = shadowObj.GetComponent<SpriteRenderer>();
        if (shadowRenderer != null && spriteRenderer != null)
        {
            shadowRenderer.flipX = spriteRenderer.flipX;
        }
    }

    // 드래그 가능 여부 설정 메소드 (외부에서 호출 가능)
    public void SetDraggable(bool draggable)
    {
        isDraggable = draggable;
    }

    // 좌우반전 메소드 (외부에서 호출 가능)
    public void FlipObject()
    {
        if (!isFlipping)
        {
            StartCoroutine(FlipHorizontally());
        }
    }

    // 원래 위치 재설정 메소드 (외부에서 호출 가능)
    public void SetOriginalPosition(Vector3 position)
    {
        originalPosition = position;
    }

    // 현재 좌우반전 상태 반환 (외부에서 호출 가능)
    public bool IsFlipped()
    {
        return isFlipped;
    }

    // SpriteRenderer의 flipX 상태 반환 (외부에서 호출 가능)
    public bool GetSpriteFlipX()
    {
        if (spriteRenderer != null)
        {
            return spriteRenderer.flipX;
        }
        return false;
    }
}