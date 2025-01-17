using UnityEngine;

public class ProjectileQuadraticHoming : ProjectileBase
{
	private	Vector2		start, end, point;
	private	float		duration, t = 0f;
	private	Transform	target;

	public override void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
	{
		base.Setup(target, damage);

		this.target	= target;
		start		= transform.position;
		end			= this.target.position;

		// 시작 지점에서 목표까지의 거리 계산
		float distance = Vector3.Distance(start, end);
		// 재생 시간 설정 (거리 / 이동속도)
		duration = distance / movementRigidbody2D.MoveSpeed;

		// 모든 발사체의 point를 동일하게 45도 각도 위치로 설정
		//float angle = 45;

		// 순번에 따라 일정한 각도의 원형으로 위치 설정
		//float angle = 360 / maxCount * index;

		// 순번에 따라 위 or 아래 대각선(45 or 315도) 위치로 설정
		float angle = 45 + 270 * (index % 2);

		// 현재 플레이어의 회전 값 적용을 위해 angle 값에 더해준다
		angle += Utils.GetAngleFromPosition(start, end);

		// 시작지점에서 목표지점 사이의 angle 각도로 30% 떨어진 위치
		point = Utils.GetNewPoint(start, angle, distance * 0.3f);

		// point 위치 확인을 위한 디버깅 코드 [결과 확인 후 삭제]
		GameObject clone = Instantiate(gameObject, point, Quaternion.identity);
		clone.GetComponent<ProjectileQuadraticHoming>().enabled = false;
		clone.GetComponentInChildren<SpriteRenderer>().color = Color.black;
	}

public override void Process()
{
    if (target == null)
    {
        Debug.LogError("Target is null!");
        Destroy(gameObject);
        return;
    }

    end = target.position;
    t += Time.deltaTime / duration;
    t = Mathf.Clamp01(t); // t 범위 제한

    if (float.IsNaN(t) || float.IsInfinity(t))
    {
        Debug.LogError($"Invalid t value: {t}");
        return;
    }

    transform.position = Utils.QuadraticCurve(start, point, end, t);
}

}

