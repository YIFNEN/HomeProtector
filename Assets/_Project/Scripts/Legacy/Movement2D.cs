using UnityEngine;
using UnityEngine.AI;

public class Movement2D : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 1.0f;
    [SerializeField]
    private Vector3 moveDirection = Vector3.zero;

    private float originalMoveSpeed;
    private bool isSlowed = false;
    private float slowTimer = 0f;
    private float currentSlowAmount = 0f;

    // NavMeshAgent 참조 추가
    private NavMeshAgent navMeshAgent;
    private bool useNavMesh = false;

    public float MoveSpeed => moveSpeed;

    private void Awake()
    {
        // 초기 이동 속도 저장
        originalMoveSpeed = moveSpeed;

        // NavMeshAgent 확인
        navMeshAgent = GetComponent<NavMeshAgent>();
        useNavMesh = navMeshAgent != null;

        // NavMeshAgent가 있으면 초기 속도 동기화
        if (useNavMesh)
        {
            originalMoveSpeed = navMeshAgent.speed;
            moveSpeed = originalMoveSpeed;
        }
    }

    void Update()
    {
        // NavMeshAgent가 없을 경우에만 직접 이동
        if (!useNavMesh)
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }

        // 감속 효과가 적용 중이라면 타이머 업데이트
        if (isSlowed)
        {
            slowTimer -= Time.deltaTime;
            // 타이머가 끝나면 이동 속도 복구
            if (slowTimer <= 0)
            {
                ResetMoveSpeed();
            }
        }
    }

    public void MoveTo(Vector3 direction)
    {
        moveDirection = direction;
    }

    // 이동 속도 감소 효과 적용
    public void ApplySlow(float slowAmount, float duration)
    {
        // 현재 적용된 감속보다 더 강한 감속이거나, 감속 효과가 곧 끝날 경우에만 적용
        if (slowAmount > currentSlowAmount || slowTimer < 0.5f)
        {
            // 감속 효과가 처음 적용되면 원래 속도 저장
            if (!isSlowed)
            {
                originalMoveSpeed = useNavMesh ? navMeshAgent.speed : moveSpeed;
            }

            // 새로운 감속 효과 적용
            currentSlowAmount = slowAmount;
            moveSpeed = originalMoveSpeed * (1 - slowAmount);

            // NavMeshAgent가 있으면 속도 적용
            if (useNavMesh && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.speed = moveSpeed;
            }

            slowTimer = duration;
            isSlowed = true;

            Debug.Log($"{gameObject.name}의 이동 속도 {slowAmount * 100}% 감소 (지속시간: {duration}초)");
        }
    }

    // 이동 속도 원래대로 복구
    public void ResetMoveSpeed()
    {
        moveSpeed = originalMoveSpeed;

        // NavMeshAgent가 있으면 속도 복구
        if (useNavMesh && navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.speed = originalMoveSpeed;
        }

        isSlowed = false;
        currentSlowAmount = 0f;

        Debug.Log($"{gameObject.name}의 이동 속도 복구");
    }
}