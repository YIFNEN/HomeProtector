using UnityEngine;

public class ProjectileQuadraticHoming : ProjectileBase
{
    [SerializeField] private float moveSpeed = 6f;
    private Vector2 startPosition;
    private Vector2 controlPoint;
    private float journeyTime = 0f;
    private float journeyLength = 1f; // 곡선을 완료하는 시간

    public override void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
    {
        base.Setup(target, damage);

        startPosition = transform.position;

        if (target != null)
        {
            // 시작점과 목표점 사이에 제어점 설정
            Vector2 targetPosition = target.position;
            float angle = Random.Range(0f, 360f); // 임의의 각도로 제어점 설정
            float distance = Vector2.Distance(startPosition, targetPosition) * 0.5f;

            // 제어점 계산
            controlPoint = startPosition + new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance
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
            // 2차 베지어 곡선을 따라 이동
            Vector2 currentPosition = Utils.QuadraticCurve(startPosition, controlPoint, target.position, journeyTime);
            transform.position = currentPosition;

            // 이동 방향으로 회전
            if (journeyTime > 0)
            {
                Vector2 prevPosition = Utils.QuadraticCurve(startPosition, controlPoint, target.position, journeyTime - 0.01f);
                Vector2 direction = (currentPosition - prevPosition).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
}