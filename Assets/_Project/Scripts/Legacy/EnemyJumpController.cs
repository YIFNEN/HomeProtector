using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyJumpController : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 1.5f;        // 점프 높이
    [SerializeField] private float jumpDuration = 0.7f;      // 점프 지속 시간
    [SerializeField] private float jumpCooldown = 2.0f;      // 점프 쿨다운
    [SerializeField] private AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 점프 곡선

    [Header("Jump Triggers")]
    [SerializeField] private bool randomJumping = true;      // 랜덤 점프 활성화
    [SerializeField] private float randomJumpChance = 0.1f;  // 랜덤 점프 확률(0-1)
    [SerializeField] private bool jumpOnObstacle = true;     // 장애물에 닿으면 점프 여부

    [Header("Effects")]
    [SerializeField] private GameObject jumpEffect;          // 점프 이펙트 
    [SerializeField] private AudioClip jumpSound;            // 점프 소리

    // 참조 컴포넌트
    private Movement2D movement2D;
    private NavMeshAgent navMeshAgent;
    private Rigidbody2D rb;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private Enemy enemy;

    // 내부 변수
    private float jumpTimer = 0f;
    private float cooldownTimer = 0f;
    private bool isJumping = false;
    private Vector3 jumpStartPosition;
    private Vector3 jumpTargetPosition;
    private float originalZ;
    private Vector3 originalScale;

    private void Awake()
    {
        // 필요한 컴포넌트 참조 가져오기
        movement2D = GetComponent<Movement2D>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemy = GetComponent<Enemy>();

        if (audioSource == null && jumpSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        originalScale = transform.localScale;
    }

    private void Start()
    {
        // 쿨다운 타이머 초기화
        cooldownTimer = jumpCooldown;
        originalZ = transform.position.z;
    }

    private void Update()
    {
        // 쿨다운 타이머 업데이트
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // 점프 중이면 점프 업데이트
        if (isJumping)
        {
            UpdateJump();
            return;
        }

        // 쿨다운이 끝나면 점프 가능
        if (cooldownTimer <= 0)
        {
            // 랜덤 점프가 활성화되었으면 확률에 따라 점프
            if (randomJumping && Random.value < randomJumpChance * Time.deltaTime)
            {
                StartJump();
            }

            // 장애물 감지 및 점프 (선택적으로 구현)
            if (jumpOnObstacle && IsObstacleAhead())
            {
                StartJump();
            }
        }
    }

    // 장애물 감지 메서드
    private bool IsObstacleAhead()
    {
        if (enemy != null && enemy.CurrentTarget != null)
        {
            Vector3 direction = (enemy.CurrentTarget.position - transform.position).normalized;

            // 적의 이동 방향으로 레이캐스트
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                direction,
                1.0f,
                LayerMask.GetMask("Obstacle")
            );

            return hit.collider != null;
        }

        return false;
    }

    // 점프 시작
    public void StartJump()
    {
        if (isJumping || cooldownTimer > 0)
            return;

        isJumping = true;
        jumpTimer = 0f;
        jumpStartPosition = transform.position;

        // NavMeshAgent가 있으면 일시 정지
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.isStopped = true;
        }

        // 점프 목표 위치 설정 (현재 방향으로 약간 앞으로)
        Vector3 direction = Vector3.zero;

        if (enemy != null && enemy.CurrentTarget != null)
        {
            direction = (enemy.CurrentTarget.position - transform.position).normalized;
        }
        else
        {
            // Movement2D에서 방향을 얻을 수 없으므로 NavMeshAgent 또는 현재 이동 방향 사용
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.velocity.sqrMagnitude > 0.1f)
            {
                direction = navMeshAgent.velocity.normalized;
            }
            else
            {
                // 방향이 없으면 앞쪽으로 점프
                direction = transform.right;
            }
        }

        // 목표 위치 설정 (현재 위치에서 진행 방향으로 약간 더 앞으로)
        jumpTargetPosition = transform.position + direction * (jumpHeight * 0.7f);

        // 이펙트 및 사운드 재생
        if (jumpEffect != null)
        {
            Instantiate(jumpEffect, transform.position, Quaternion.identity);
        }

        if (jumpSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }

        // 점프 코루틴 시작
        StartCoroutine(JumpAnimation());
    }

    // 점프 업데이트
    private void UpdateJump()
    {
        jumpTimer += Time.deltaTime;

        if (jumpTimer >= jumpDuration)
        {
            // 점프 종료
            EndJump();
        }
    }

    // 점프 애니메이션 코루틴
    private IEnumerator JumpAnimation()
    {
        float elapsedTime = 0f;

        while (elapsedTime < jumpDuration)
        {
            float normalizedTime = elapsedTime / jumpDuration;
            float curveValue = jumpCurve.Evaluate(normalizedTime);

            // 위치 보간 (가로, 세로 방향은 선형 보간, 높이는 커브를 따름)
            Vector3 newPosition = Vector3.Lerp(jumpStartPosition, jumpTargetPosition, normalizedTime);

            // Z 위치 조정 (점프 높이)
            newPosition.z = originalZ + jumpHeight * curveValue;

            // 새 위치 적용
            transform.position = newPosition;

            // 점프에 따른 스케일 약간 조정 (선택적)
            transform.localScale = new Vector3(
                originalScale.x,
                originalScale.y * (1 + 0.1f * curveValue), // Y방향으로 약간 늘어남
                originalScale.z
            );

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 최종 위치 및 스케일 조정
        transform.position = new Vector3(
            jumpTargetPosition.x,
            jumpTargetPosition.y,
            originalZ  // 원래 z 위치로 복귀
        );

        transform.localScale = originalScale;

        EndJump();
    }

    // 점프 종료
    private void EndJump()
    {
        isJumping = false;
        jumpTimer = 0f;
        cooldownTimer = jumpCooldown;

        // NavMeshAgent 재개
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.isStopped = false;
        }
    }
}