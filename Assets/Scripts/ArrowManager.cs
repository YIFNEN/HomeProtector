using UnityEngine;

public class ArrowManager : MonoBehaviour
{
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float damage = 10f; // Damage amount
    [SerializeField] private float timer = 1f; // Lifetime of the arrow

    private Transform target;
    private Vector3 moveDirection;

    public void Setup(Vector3 direction)
    {
        moveDirection = direction.normalized;
        // Rotate the arrow to face the direction
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Update()
    {
        // Move in the set direction
        transform.position += moveDirection * speed * Time.deltaTime;

        // Destroy after timer expires
        Destroy(gameObject, timer);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the collided object has the Enemy tag
        if (collision.CompareTag("Enemy"))
        {
            // Try to get the EnemyHP component
            EnemyHP enemyHP = collision.GetComponent<EnemyHP>();

            if (enemyHP != null)
            {
                // Deal damage to the enemy
                enemyHP.TakeDamage(damage);

                // Destroy the arrow after hitting an enemy
                Destroy(gameObject);
            }
        }
    }
}