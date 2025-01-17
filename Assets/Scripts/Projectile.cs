using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Movement2D movement2D;
    private Transform target;
    private float damage;
    private bool isInitialized = false; // 초기화 상태 확인 플래그

    public void Setup(Transform target, float damage)
    {
        movement2D = GetComponent<Movement2D>();
        this.target = target;
        this.damage = damage;
        isInitialized = true; // 초기화 완료
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
