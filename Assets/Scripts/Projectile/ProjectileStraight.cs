using UnityEngine;

public class ProjectileStraight : ProjectileBase
{
    [SerializeField] private float moveSpeed = 8f;
    private Vector3 moveDirection;

    public override void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
    {
        base.Setup(target, damage);

        // 타겟 방향으로 발사
        if (target != null)
        {
            moveDirection = (target.position - transform.position).normalized;
        }
        else
        {
            moveDirection = transform.right; // 기본적으로 오른쪽 방향
        }

        // 발사체 회전
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public override void Process()
    {
        // 이동 방향으로 계속 이동
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // 히트 이펙트 생성
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
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