using UnityEngine;

public class ArrowManager : MonoBehaviour
{
    [SerializeField] private float speed = 5.0f; // 화살의 이동 속도
    [SerializeField] private float damage = 10f; // 화살이 적에게 주는 기본 데미지
    [SerializeField] private float timer = 1f; // 화살의 생존 시간

    private Vector3 moveDirection; // 화살이 날아가는 방향
    private float damageMultiplier = 1.0f; // 플레이어 레벨에 따른 데미지 배수

    // Setup: 화살에 방향을 설정하는 메소드
    // input: direction (Vector3) - 화살이 날아갈 방향
    // output: 없음
    // 역할: 화살을 해당 방향으로 회전시키고 이동 방향을 설정
    public void Setup(Vector3 direction)
    {
        moveDirection = direction.normalized;

        // 화살이 이동 방향을 바라보도록 회전
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // SetDamageMultiplier: 데미지 배수를 설정하는 메소드
    // input: multiplier (float) - 데미지 배수
    // output: 없음
    // 역할: 플레이어 레벨에 따른 데미지 배수 설정
    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = multiplier;
    }

    // Update: 매 프레임마다 호출되는 메소드
    // 화살을 moveDirection 방향으로 이동시키고, timer 후에 파괴
    void Update()
    {
        // Isometric 이동 (z as y에 맞게 조정)
        Vector3 movement = moveDirection * speed * Time.deltaTime;
        transform.position += movement;

        // z 위치 조정 (y와 동일하게)
        Vector3 position = transform.position;
        position.z = position.y;
        transform.position = position;
    }

    // Start: 초기화
    // 생존 시간 타이머 설정
    private void Start()
    {
        Destroy(gameObject, timer);
    }

    // OnTriggerEnter2D: 다른 콜라이더와 충돌했을 때 호출되는 메소드
    // input: collision (Collider2D) - 충돌한 콜라이더
    // output: 없음
    // 역할: "Enemy" 태그를 가진 오브젝트와 충돌 시 데미지를 주고 화살을 파괴
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // "Enemy" 태그를 가진 오브젝트인지 확인
        if (collision.CompareTag("Enemy"))
        {
            // EnemyHP 컴포넌트 가져오기
            EnemyHP enemyHP = collision.GetComponent<EnemyHP>();

            if (enemyHP != null)
            {
                // 레벨에 따른 데미지 증가 적용
                float finalDamage = damage * damageMultiplier;

                // 적에게 데미지 주기
                enemyHP.TakeDamage(finalDamage);
                Debug.Log($"적에게 데미지 {finalDamage} 적용 (기본: {damage}, 배수: {damageMultiplier})");

                // 적과 충돌 후 화살 파괴
                Destroy(gameObject);
            }
        }
    }
}