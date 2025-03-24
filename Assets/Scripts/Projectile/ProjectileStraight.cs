using UnityEngine;

public class ProjectileStraight : ProjectileBase
{
    [SerializeField] private float moveSpeed = 8f; // 발사체 이동 속도
    private Vector3 moveDirection; // 발사체 이동 방향
    private bool isFlipped = false; // 좌우반전 상태

    public override void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
    {
        base.Setup(target, damage);

        // 기본 이동 방향 설정
        moveDirection = Vector3.right; // 기본적으로 오른쪽 방향

        // 좌우반전 상태에 따라 이동 방향 결정
        if (isFlipped)
        {
            moveDirection = Vector3.left; // 반전된 경우 왼쪽 방향
        }

        // 발사체 회전
        UpdateRotation();
    }

    // 좌우반전 설정 메소드
    public void SetFlipDirection(bool flipped)
    {
        isFlipped = flipped;

        // 이동 방향 설정
        moveDirection = isFlipped ? Vector3.left : Vector3.right;

        // 회전 업데이트
        UpdateRotation();

        // 스프라이트 렌더러가 있다면 flipX 설정
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.flipX = isFlipped;
        }
        else
        {
            // 스프라이트 렌더러가 없다면 스케일 조정
            Vector3 scale = transform.localScale;
            scale.x = isFlipped ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    // 회전 각도 업데이트
    private void UpdateRotation()
    {
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public override void Process()
    {
        // 이동 방향으로 계속 이동
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // 이소메트릭 뷰 지원 (z 값 조정)
        Vector3 position = transform.position;
        position.z = position.y;
        transform.position = position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // 히트 이펙트 생성
            if (hitEffect != null)
            {
                Vector3 effectPosition = transform.position;
                effectPosition.z = effectPosition.y; // 이소메트릭 z 조정
                Instantiate(hitEffect, effectPosition, Quaternion.identity);
            }

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