using UnityEngine;

public class ProjectileCubicHoming : ProjectileBase
{
	private	Vector2		start, end, point1, point2;
	private	float		duration, t = 0f;
	private	Transform	target;

    public override void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
    {
        base.Setup(target, damage);

        if (target == null)
        {
            Debug.LogError("Target is null in ProjectileCubicHoming.Setup!");
            Destroy(gameObject); // target이 없으면 발사체 제거
            return;
        }

        this.target = target;
        start = transform.position;
        end = this.target.position;

        if (movementRigidbody2D == null)
        {
            Debug.LogError("movementRigidbody2D is null in ProjectileCubicHoming!");
            return;
        }

        // 시작 지점에서 목표까지의 거리 계산
        float distance = Vector3.Distance(start, end);
        duration = distance / movementRigidbody2D.MoveSpeed;

        float angle = 45;
        angle += Utils.GetAngleFromPosition(start, end);

        // 곡선 생성
        point1 = Utils.GetNewPoint(start, angle, distance * 0.3f);
        point2 = Utils.GetNewPoint(start, angle * -1, distance * 0.7f);
    }


    public override void Process()
	{
		end = target.position;
		t += Time.deltaTime / duration;
		transform.position = Utils.CubicCurve(start, point1, point2, end, t);
	}
}

