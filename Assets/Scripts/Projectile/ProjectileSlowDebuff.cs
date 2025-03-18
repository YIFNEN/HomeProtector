using UnityEngine;

// 이동 속도 감소 효과를 주는 발사체
public class ProjectileSlowDebuff : ProjectileBase
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float effectRadius = 2f;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private float slowAmount = 0.5f; // 이동 속도 감소 비율 (0.5 = 50% 감소)
    [SerializeField] private float effectDuration = 3.0f; // 효과 지속 시간


    public override void Process()
    {
        // 타겟이 없으면 처리하지 않음
        if (target == null) return;

        // 발사체가 타겟에 도달했는지 확인
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance < 0.1f)
        {
            // 효과 적용
            ApplyEffectInArea(target.position);

            // 타격 효과 생성
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            // 발사체 파괴
            Destroy(gameObject);
        }
        else
        {
            // 타겟을 향해 이동
            MoveToTarget();
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
                // 기본 데미지 적용
                EnemyHP enemyHP = collider.GetComponent<EnemyHP>();
                if (enemyHP != null)
                {
                    enemyHP.TakeDamage(damage);
                }

                // 이동 속도 감소 효과 적용
                Movement2D movement = collider.GetComponent<Movement2D>();
                if (movement != null)
                {
                    movement.ApplySlow(slowAmount, effectDuration);
                }
            }
        }
    }

    private void MoveToTarget()
    {
        // 타겟 방향으로 이동
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        // 발사체 회전 (선택적)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // 에디터에서 범위 시각화
    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(target.position, effectRadius);
        }
    }
}



