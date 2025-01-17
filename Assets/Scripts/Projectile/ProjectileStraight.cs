using UnityEngine;

public class ProjectileStraight : ProjectileBase
{
	public override void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
	{
		base.Setup(target, damage);

		// 발사체 이동 방향 설정
		movementRigidbody2D.MoveTo((target.position - transform.position).normalized);
	}

	public override void Process() { }
}

