using UnityEngine;

public class ProjectileBook : MonoBehaviour
{
    private Movement2D movement2D;
    private Transform target;
    private float damage;
    private bool isInitialized = false; // 초기화 상태 확인 플래그
    private SpriteRenderer spriteRenderer;

    public Sprite[] projectileSprites; // 3개의 발사체 이미지
    private static int spriteIndex = 0; // 전역적으로 관리되는 인덱스

    public void Setup(Transform target, float damage)
    {
        movement2D = GetComponent<Movement2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        this.target = target;
        this.damage = damage;
        isInitialized = true; // 초기화 완료

        // 스프라이트 변경 (순차적으로)
        if (projectileSprites.Length > 0)
        {
            spriteRenderer.sprite = projectileSprites[spriteIndex];
            spriteIndex = (spriteIndex + 1) % projectileSprites.Length; // 다음 이미지로 순환
        }
    }

    private void Update()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Projectile not initialized. Waiting for setup...");
            return; // 초기화 전에는 동작하지 않음
        }

        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            Debug.Log(target.position);
            movement2D.MoveTo(direction);

            if (direction == Vector3.zero)
            {
                Debug.LogWarning("Direction is zero. Not moving.");
                return;
            }
            else
            {
                Debug.Log("moveDirection set");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy")) return;
        if (collision.transform != target) return;

        collision.GetComponent<EnemyHP>().TakeDamage(damage);
        Destroy(gameObject);
    }
}
