using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour
{
	[SerializeField]
	public		GameObject			hitEffect;
	protected	MovementRigidbody2D	movementRigidbody2D;

	public virtual void Setup(Transform target, float damage, int maxCount=1, int index=0)
	{
		movementRigidbody2D = GetComponent<MovementRigidbody2D>();
	}

	private void Update()
	{
		Process();
	}

	public abstract void Process();

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if ( collision.CompareTag("Enemy") )
		{
			Instantiate(hitEffect, transform.position, Quaternion.identity);
			Destroy(gameObject);

			// 적 캐릭터 피격 처리
		}
	}
}

