using UnityEngine;
public class ProjectileAreaDamage : ProjectileBase
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float damageRadius = 2f;
    [SerializeField] private string enemyTag = "Enemy"; // 레이어 대신 태그를 사용
    private bool hasExploded = false;

    public override void Process()
    {
        if (hasExploded || target == null) return;
        // 타겟을 향해 이동
        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
        // 이동 방향으로 회전
        Vector3 direction = (target.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        // 타겟에 충분히 가까워지면 폭발
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget < 0.2f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        // 히트 이펙트 생성
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // 범위 내 모든 콜라이더를 감지 (레이어 필터링 없이)
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(transform.position, damageRadius);

        // 태그로 필터링하여 데미지 적용
        foreach (Collider2D collider in allColliders)
        {
            if (collider.CompareTag(enemyTag))
            {
                EnemyHP enemyHP = collider.GetComponent<EnemyHP>();
                if (enemyHP != null)
                {
                    enemyHP.TakeDamage(damage);
                }
            }
        }

        // 폭발 후 약간의 지연 후 파괴
        Destroy(gameObject, 0.1f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasExploded && collision.CompareTag(enemyTag))
        {
            Explode();
        }
    }

    // 에디터에서 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}