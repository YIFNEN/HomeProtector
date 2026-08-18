using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [Header("대상 설정")]
    [SerializeField] private Transform target; // 따라다닐 대상 오브젝트의 Transform
    [SerializeField] private bool findTargetByTag = false; // 태그로 대상 찾기 활성화
    [SerializeField] private string targetTag = "Player"; // 찾을 대상의 태그

    [Header("따라가기 설정")]
    [SerializeField] private float smoothSpeed = 5.0f; // 카메라 움직임 부드러움 정도 (값이 클수록 빠르게 따라감)
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10); // 대상과의 거리 오프셋

    [Header("제한 설정")]
    [SerializeField] private bool enableBounds = false; // 카메라 이동 범위 제한 활성화
    [SerializeField] private float minX = -10f; // X축 최소값
    [SerializeField] private float maxX = 10f;  // X축 최대값
    [SerializeField] private float minY = -5f;  // Y축 최소값
    [SerializeField] private float maxY = 5f;   // Y축 최대값

    [Header("옵션")]
    [SerializeField] private bool followX = true; // X축 따라가기 활성화
    [SerializeField] private bool followY = true; // Y축 따라가기 활성화
    [SerializeField] private bool followZ = false; // Z축 따라가기 활성화 (대부분의 2D 게임에서는 false)

    [Header("전체 화면 뷰 설정")]
    [SerializeField] private Vector3 fullscreenViewPosition = new Vector3(0, 0, -10); // 전체 화면 뷰 위치
    [SerializeField] private float transitionSpeed = 2.0f; // 뷰 전환 속도
    [SerializeField] private float fullscreenOrthographicSize = 10f; // 전체 화면 뷰일 때 카메라 크기
    [SerializeField] private float normalOrthographicSize = 5f; // 일반 뷰일 때 카메라 크기
    [SerializeField] private bool useOrthographicSize = true; // 직교 크기 조정 사용 여부 (2D 게임용)

    private bool isTargetActive = true; // 대상의 활성화 상태
    private Camera mainCamera; // 카메라 컴포넌트
    private Coroutine transitionCoroutine; // 전환 코루틴 참조

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        // 태그로 대상 찾기가 활성화되어 있고 대상이 설정되지 않은 경우
        if (findTargetByTag && target == null)
        {
            GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);
            if (targetObject != null)
            {
                target = targetObject.transform;
                Debug.Log($"태그 '{targetTag}'로 대상을 찾았습니다: {target.name}");
            }
            else
            {
                Debug.LogWarning($"태그 '{targetTag}'를 가진 오브젝트를 찾을 수 없습니다.");
            }
        }

        // 초기 위치 설정 (대상이 있는 경우)
        if (target != null)
        {
            isTargetActive = target.gameObject.activeSelf;

            if (isTargetActive)
            {
                Vector3 desiredPosition = CalculateDesiredPosition();
                transform.position = desiredPosition;
                if (useOrthographicSize && mainCamera.orthographic)
                {
                    mainCamera.orthographicSize = normalOrthographicSize;
                }
            }
            else
            {
                transform.position = fullscreenViewPosition;
                if (useOrthographicSize && mainCamera.orthographic)
                {
                    mainCamera.orthographicSize = fullscreenOrthographicSize;
                }
            }
        }
        else
        {
            // 대상이 없는 경우 전체 화면 뷰로 설정
            transform.position = fullscreenViewPosition;
            if (useOrthographicSize && mainCamera.orthographic)
            {
                mainCamera.orthographicSize = fullscreenOrthographicSize;
            }
        }
    }

    private void LateUpdate()
    {
        // 대상이 없으면 실행하지 않음
        if (target == null) return;

        // 대상의 활성화 상태 확인
        bool currentTargetActive = target.gameObject.activeSelf;

        // 활성화 상태가 변경된 경우
        if (currentTargetActive != isTargetActive)
        {
            isTargetActive = currentTargetActive;

            // 이전 전환 코루틴이 실행 중이면 중지
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }

            // 새로운 전환 코루틴 시작
            if (isTargetActive)
            {
                // 대상이 활성화됨 -> 대상 따라가기로 전환
                transitionCoroutine = StartCoroutine(TransitionToTarget());
            }
            else
            {
                // 대상이 비활성화됨 -> 전체 화면 뷰로 전환
                transitionCoroutine = StartCoroutine(TransitionToFullscreenView());
            }
        }

        // 대상이 활성화된 경우에만 따라가기
        if (isTargetActive)
        {
            // 목표 위치 계산
            Vector3 desiredPosition = CalculateDesiredPosition();

            // 부드럽게 이동
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
    }

    // 목표 위치 계산
    private Vector3 CalculateDesiredPosition()
    {
        // 현재 카메라 위치
        Vector3 currentPosition = transform.position;

        // 목표 위치 (각 축별 따라가기 설정 적용)
        Vector3 targetPosition = target.position + offset;
        Vector3 desiredPosition = new Vector3(
            followX ? targetPosition.x : currentPosition.x,
            followY ? targetPosition.y : currentPosition.y,
            followZ ? targetPosition.z : currentPosition.z
        );

        // 범위 제한이 활성화된 경우 위치 제한
        if (enableBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        return desiredPosition;
    }

    // 대상 따라가기로 전환하는 코루틴
    private IEnumerator TransitionToTarget()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = CalculateDesiredPosition();
        float startSize = mainCamera.orthographic ? mainCamera.orthographicSize : 0;
        float endSize = normalOrthographicSize;
        float elapsedTime = 0;

        while (elapsedTime < 1f / transitionSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime * transitionSpeed);

            // 위치 보간
            transform.position = Vector3.Lerp(startPosition, endPosition, t);

            // 직교 카메라 크기 보간 (if applicable)
            if (useOrthographicSize && mainCamera.orthographic)
            {
                mainCamera.orthographicSize = Mathf.Lerp(startSize, endSize, t);
            }

            yield return null;
        }

        // 최종 위치와 크기로 설정
        transform.position = endPosition;
        if (useOrthographicSize && mainCamera.orthographic)
        {
            mainCamera.orthographicSize = endSize;
        }

        transitionCoroutine = null;
    }

    // 전체 화면 뷰로 전환하는 코루틴
    private IEnumerator TransitionToFullscreenView()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = fullscreenViewPosition;
        float startSize = mainCamera.orthographic ? mainCamera.orthographicSize : 0;
        float endSize = fullscreenOrthographicSize;
        float elapsedTime = 0;

        while (elapsedTime < 1f / transitionSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime * transitionSpeed);

            // 위치 보간
            transform.position = Vector3.Lerp(startPosition, endPosition, t);

            // 직교 카메라 크기 보간 (if applicable)
            if (useOrthographicSize && mainCamera.orthographic)
            {
                mainCamera.orthographicSize = Mathf.Lerp(startSize, endSize, t);
            }

            yield return null;
        }

        // 최종 위치와 크기로 설정
        transform.position = endPosition;
        if (useOrthographicSize && mainCamera.orthographic)
        {
            mainCamera.orthographicSize = endSize;
        }

        transitionCoroutine = null;
    }

    // 런타임에 대상 변경 메서드
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
        {
            isTargetActive = target.gameObject.activeSelf;

            // 이전 전환 코루틴이 실행 중이면 중지
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            // 대상이 활성화된 경우 따라가기 시작
            if (isTargetActive)
            {
                transitionCoroutine = StartCoroutine(TransitionToTarget());
            }
        }
        else
        {
            // 대상이 없는 경우 전체 화면 뷰로 전환
            transitionCoroutine = StartCoroutine(TransitionToFullscreenView());
        }

        Debug.Log($"카메라 추적 대상이 {(newTarget != null ? newTarget.name : "null")}로 변경되었습니다.");
    }

    // 런타임에 오프셋 변경 메서드
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    // 런타임에 전체 화면 뷰 위치 변경 메서드
    public void SetFullscreenViewPosition(Vector3 newPosition)
    {
        fullscreenViewPosition = newPosition;

        // 대상이 비활성화된 상태이면 즉시 새 위치로 이동
        if (target == null || !isTargetActive)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            transitionCoroutine = StartCoroutine(TransitionToFullscreenView());
        }
    }

    // 대상과의 즉시 정렬 메서드 (부드러운 이동 없이)
    public void AlignWithTarget()
    {
        if (target != null && isTargetActive)
        {
            transform.position = CalculateDesiredPosition();
            if (useOrthographicSize && mainCamera.orthographic)
            {
                mainCamera.orthographicSize = normalOrthographicSize;
            }
        }
    }

    // 전체 화면 뷰로 즉시 전환 메서드
    public void SwitchToFullscreenView()
    {
        transform.position = fullscreenViewPosition;
        if (useOrthographicSize && mainCamera.orthographic)
        {
            mainCamera.orthographicSize = fullscreenOrthographicSize;
        }
    }

    // 디버깅용 시각화
    private void OnDrawGizmosSelected()
    {
        if (enableBounds)
        {
            // 카메라 이동 제한 범위 시각화
            Gizmos.color = Color.yellow;
            Vector3 boundsCenter = new Vector3((minX + maxX) / 2, (minY + maxY) / 2, 0);
            Vector3 boundsSize = new Vector3(maxX - minX, maxY - minY, 0.1f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }

        // 전체 화면 뷰 위치 시각화
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(fullscreenViewPosition, 0.5f);
    }
}