using UnityEngine;
using UnityEngine.Tilemaps;

public class ProjectileAreaDamage : ProjectileBase
{
    [SerializeField] private float damageRadius = 2f; // 2 tiles radius
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Grid grid; // Reference to the grid for tile-based calculations
    [SerializeField] public float damage; 


    public override void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
    {
        base.Setup(target, damage);
        grid = FindObjectOfType<Grid>();
    }

    public override void Process()
    {
        // No specific movement for area damage projectile
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // Spawn hit effect
            Instantiate(hitEffect, transform.position, Quaternion.identity);

            // Find all enemies within the radius
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, damageRadius, enemyLayer);

            foreach (Collider2D enemyCollider in hitEnemies)
            {
                EnemyHP enemyHP = enemyCollider.GetComponent<EnemyHP>();
                if (enemyHP != null)
                {
                    enemyHP.TakeDamage(damage);// 임의로 설정 드라이기 기준으로.
                }
            }

            Destroy(gameObject);
        }
    }

    // Optional: Visualize damage radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}