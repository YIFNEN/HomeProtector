using UnityEngine;

public enum ProjectileType { Straight, Homing, QuadraticHoming, CubicHoming }

public class SkillEmission : MonoBehaviour
{
	[SerializeField]
	private	ProjectileType	projectileType = ProjectileType.Straight;
	[SerializeField]
	private	int				projectileCount = 10;
	[SerializeField]
	private	float			cooldownTime = 2f;
	[SerializeField]
	private	GameObject[]	projectiles;
	[SerializeField]
	private	Transform		skillSpawnPoint;
	[SerializeField]
	private	Transform		target;

	private	int				currentProjectileIndex = 0;
	private	float			currentAttackRate = 0;
	private	float			currentCooldownTime = 0;
	private	float			attackRate = 0.05f;		// 발사체 사이의 간격

	public	bool			IsSkillAvailable => (Time.time - currentCooldownTime > cooldownTime);

	private void Update()
	{
		OnSkill();
	}

	public void OnSkill()
	{
		// 스킬이 사용 가능한 상태인지 검사 (쿨타임)
		if ( IsSkillAvailable == false ) return;

		// attackRate 주기로 발사체 생성
		if ( Time.time - currentAttackRate > attackRate )
		{
			GameObject clone = GameObject.Instantiate(projectiles[(int)projectileType], skillSpawnPoint.position, Quaternion.identity);
			clone.GetComponent<ProjectileBase>().Setup(target, 1, projectileCount, currentProjectileIndex);

			currentProjectileIndex ++;
			currentAttackRate = Time.time;
		}

		// ProjectileCount 개수만큼 발사체를 생성한 후 쿨타임 초기화
		if ( currentProjectileIndex >= projectileCount )
		{
			currentProjectileIndex	= 0;
			currentCooldownTime		= Time.time;
		}
	}
}

