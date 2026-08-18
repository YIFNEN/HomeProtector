using UnityEngine;

public class ProjectileStraight : ProjectileBase
{
    [SerializeField] private float moveSpeed = 8f; // 발사체 이동 속도
    private Vector3 moveDirection; // 발사체 이동 방향
    private bool flipDirection;

    public override void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
    {
        base.Setup(target, damage, maxCount, index);

        // 타겟 방향으로 발사
        if (target != null)
        {
            moveDirection = (target.position - transform.position).normalized;
        }
        else
        {
            moveDirection = transform.right; // 기본적으로 오른쪽 방향
        }

        if (flipDirection)
        {
            moveDirection.x *= -1f;
        }

        // 이동 방향에 따라 스프라이트 회전
        RotateToMoveDirection();
    }

    public void SetFlipDirection(bool flipped)
    {
        flipDirection = flipped;
    }

    // 이동 방향에 따라 스프라이트 회전
    private void RotateToMoveDirection()
    {
        // 이동 방향 벡터가 유효한지 확인
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;

            // 왼쪽 방향일 때 180도 추가 보정
            if (moveDirection.x < 0)
            {
                angle += 180f;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public override void Process()
    {
        // 이동 방향으로 계속 이동
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    // Update 메소드 오버라이드 - 부모 클래스의 타겟 null 체크를 우회
    protected override void Update()
    {
        // 직선 발사체는 타겟이 없어도 계속 이동해야 함
        Process();

        // Z 위치 업데이트 (이소메트릭 핸들러가 없는 경우)
        if (updateZPosition && isometricPosition == null)
        {
            Vector3 position = transform.position;
            position.z = position.y;
            transform.position = position;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // 히트 이펙트 생성
            CreateHitEffect(transform.position);

            // 데미지 적용
            EnemyHP enemyHP = collision.GetComponent<EnemyHP>();
            if (enemyHP != null)
            {
                enemyHP.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}
