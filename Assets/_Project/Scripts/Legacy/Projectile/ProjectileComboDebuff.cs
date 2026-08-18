using UnityEngine;
public class ProjectileComboDebuff : ProjectileBase
{
    [SerializeField] private float effectRadius = 2f;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private float moveSpeed = 5f;
    [Header("이동 속도 감소 효과")]
    [SerializeField] private float moveSlowAmount = 0.3f; // 이동 속도 감소 비율 (0.3 = 30% 감소)
    [SerializeField] private float moveSlowDuration = 4.0f; // 이동 속도 감소 지속 시간
    [Header("공격 속도 감소 효과")]
    [SerializeField] private float attackSlowAmount = 0.25f; // 공격 속도 감소 비율 (0.25 = 25% 감소)
    [SerializeField] private float attackSlowDuration = 3.0f; // 공격 속도 감소 지속 시간
    [Header("효과 시각화")]
    [SerializeField] private GameObject debuffEffectPrefab; // 디버프 효과 시각화 프리팹 (선택적)
    [Header("충돌 감지")]
    [SerializeField] private bool useCollisionDetection = true; // 충돌 감지 사용 여부
    [SerializeField] private LayerMask enemyLayer; // 적 레이어 (충돌 감지용)

    // 이동 방향 변수 추가
    private Vector3 moveDirection;
    // 발사체가 이미 충돌했는지 확인하는 플래그
    private bool hasHit = false;

    public override void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
    {
        base.Setup(target, damage, maxCount, index);

        // 타겟 방향으로 초기 이동 방향 설정
        if (target != null)
        {
            moveDirection = (target.position - transform.position).normalized;
            // 초기 방향에 따른 회전 설정
            RotateToMoveDirection(moveDirection);
        }

        // 충돌 감지를 위해 콜라이더가 없는 경우 추가
        if (useCollisionDetection && GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.3f; // 적절한 크기로 조정
        }
    }

    public override void Process()
    {
        // 타겟이 없거나 이미 충돌했으면 처리하지 않음
        if (target == null || hasHit) return;

        // 발사체가 타겟에 도달했는지 확인
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance < 0.1f)
        {
            // 효과 적용
            ApplyEffectInArea(transform.position);
            // 발사체 파괴
            DestroyProjectile();
        }
        else
        {
            // 충돌 감지를 사용하는 경우 이동 중 적과의 충돌 검사
            if (useCollisionDetection)
            {
                CheckCollisionDuringMovement();
            }

            // 타겟을 향해 이동
            MoveToTarget();
        }
    }

    // 이동 중 충돌 체크 (레이캐스트 사용)
    private void CheckCollisionDuringMovement()
    {
        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position,
            0.3f, // 충돌 체크 반경
            moveDirection,
            moveSpeed * Time.deltaTime,
            enemyLayer
        );

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag(enemyTag))
            {
                // 충돌 지점에 효과 적용
                ApplyEffectInArea(hit.point);
                // 발사체 파괴
                DestroyProjectile();
            }
        }
    }

    // 트리거 충돌 이벤트
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasHit && other.CompareTag(enemyTag))
        {
            // 충돌 지점에 효과 적용
            ApplyEffectInArea(transform.position);
            // 발사체 파괴
            DestroyProjectile();
        }
    }

    private void ApplyEffectInArea(Vector3 centerPosition)
    {
        // 효과 범위 내의 모든 콜라이더 감지
        Collider2D[] colliders = Physics2D.OverlapCircleAll(centerPosition, effectRadius);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(enemyTag))
            {
                GameObject enemy = collider.gameObject;
                // 기본 데미지 적용
                EnemyHP enemyHP = enemy.GetComponent<EnemyHP>();
                if (enemyHP != null)
                {
                    enemyHP.TakeDamage(damage);
                }
                // 이동 속도 감소 효과 적용
                Movement2D movement = enemy.GetComponent<Movement2D>();
                if (movement != null)
                {
                    movement.ApplySlow(moveSlowAmount, moveSlowDuration);
                }
                // 공격 속도 감소 효과 적용
                EnemyAttack enemyAttack = enemy.GetComponent<EnemyAttack>();
                if (enemyAttack != null)
                {
                    enemyAttack.ApplyAttackSlow(attackSlowAmount, attackSlowDuration);
                }
                // 디버프 효과 시각화 (선택적)
                if (debuffEffectPrefab != null)
                {
                    GameObject effectObj = Instantiate(debuffEffectPrefab, enemy.transform.position, Quaternion.identity);
                    effectObj.transform.SetParent(enemy.transform);
                    Destroy(effectObj, Mathf.Max(moveSlowDuration, attackSlowDuration));
                }
            }
        }
    }

    private void MoveToTarget()
    {
        // 새로운 타겟 방향 계산
        Vector3 direction = (target.position - transform.position).normalized;

        // 방향이 변경되었다면 회전 업데이트
        if (Vector3.Dot(direction, moveDirection) < 0.99f)
        {
            moveDirection = direction;
            RotateToMoveDirection(moveDirection);
        }

        // 타겟 방향으로 이동
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    // 이동 방향에 따라 스프라이트 회전
    private void RotateToMoveDirection(Vector3 direction)
    {
        // 이동 방향 벡터가 유효한지 확인
        if (direction.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 왼쪽 방향일 때 180도 추가 보정
            if (direction.x < 0)
            {
                angle += 180f;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // 발사체 파괴 공통 메서드
    private void DestroyProjectile()
    {
        // 중복 파괴 방지를 위한 플래그 설정
        hasHit = true;

        // 타격 효과 생성
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // 발사체 파괴
        Destroy(gameObject);
    }

    // 에디터에서 범위 시각화
    private void OnDrawGizmosSelected()
    {
        // 효과 범위 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, effectRadius);

        // 충돌 감지 범위 시각화
        if (useCollisionDetection)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }

    // Update 메소드 오버라이드 - 부모 클래스의 타겟 null 체크를 우회
    protected override void Update()
    {
        // 발사체가 이미 적중했으면 더 이상 처리하지 않음
        if (hasHit) return;

        // 복합 디버프 발사체는 타겟이 있을 때만 처리
        if (target != null)
        {
            Process();
        }

        // Z 위치 업데이트 (이소메트릭 핸들러가 없는 경우)
        if (updateZPosition && isometricPosition == null)
        {
            Vector3 position = transform.position;
            position.z = position.y;
            transform.position = position;
        }
    }
}