using UnityEngine;

public class ProjectileCubicHoming : ProjectileBase
{
    [SerializeField] private float moveSpeed = 6f;
    private Vector2 startPosition;
    private Vector2 controlPoint1;
    private Vector2 controlPoint2;
    private float journeyTime = 0f;

    public override void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
    {
        base.Setup(target, damage);

        startPosition = transform.position;

        if (target != null)
        {
            // 시작점과 목표점 사이에 두 개의 제어점 설정
            Vector2 targetPosition = target.position;
            float distance = Vector2.Distance(startPosition, targetPosition);

            // 첫 번째 제어점: 시작점에서 위쪽으로 랜덤 거리
            controlPoint1 = startPosition + new Vector2(
                Random.Range(-distance * 0.5f, distance * 0.5f),
                Random.Range(distance * 0.5f, distance)
            );

            // 두 번째 제어점: 목표점에서 위쪽으로 랜덤 거리
            controlPoint2 = targetPosition + new Vector2(
                Random.Range(-distance * 0.5f, distance * 0.5f),
                Random.Range(distance * 0.5f, distance)
            );
        }
    }

    public override void Process()
    {
        if (target == null) return;

        journeyTime += Time.deltaTime * moveSpeed / Vector2.Distance(startPosition, target.position);

        if (journeyTime >= 1f)
        {
            // 목표점에 도달
            transform.position = target.position;

            // 타겟에 데미지 적용
            EnemyHP enemyHP = target.GetComponent<EnemyHP>();
            if (enemyHP != null)
            {
                enemyHP.TakeDamage(damage);
            }

            // 히트 이펙트 생성
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
        else
        {
            // 3차 베지어 곡선을 따라 이동
            Vector2 currentPosition = Utils.CubicCurve(startPosition, controlPoint1, controlPoint2, target.position, journeyTime);
            transform.position = currentPosition;

            // 이동 방향으로 회전
            if (journeyTime > 0)
            {
                Vector2 prevPosition = Utils.CubicCurve(startPosition, controlPoint1, controlPoint2, target.position, journeyTime - 0.01f);
                Vector2 direction = (currentPosition - prevPosition).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
}