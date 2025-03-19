using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour
{
    [SerializeField] protected GameObject hitEffect; // 적 명중 시 효과
    [SerializeField] protected bool updateZPosition = true; // 이소메트릭 Z 위치 업데이트 여부

    protected Transform target; // 목표 대상
    protected float damage; // 데미지
    protected IsometricPositionHandler isometricPosition; // 이소메트릭 위치 핸들러

    // Awake: 초기화
    protected virtual void Awake()
    {
        // 이소메트릭 위치 핸들러 가져오거나 생성
        if (updateZPosition)
        {
            isometricPosition = GetComponent<IsometricPositionHandler>();
            if (isometricPosition == null)
            {
                isometricPosition = gameObject.AddComponent<IsometricPositionHandler>();
            }
        }
    }

    // Setup: 발사체 초기화 메소드
    public virtual void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
    {
        this.target = target;
        this.damage = damage;

        // 초기 위치 Z 값 조정
        if (updateZPosition && isometricPosition == null)
        {
            Vector3 position = transform.position;
            position.z = position.y;
            transform.position = position;
        }
    }

    // Process: 발사체의 동작을 처리하는 추상 메소드
    public abstract void Process();

    // Update: 매 프레임마다 호출
    protected virtual void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Process();

        // Z 위치 업데이트 (이소메트릭 핸들러가 없는 경우)
        if (updateZPosition && isometricPosition == null)
        {
            Vector3 position = transform.position;
            position.z = position.y;
            transform.position = position;
        }
    }

    // 히트 이펙트 생성 (오버라이드 가능)
    protected virtual void CreateHitEffect(Vector3 position)
    {
        if (hitEffect != null)
        {
            // Z 위치 조정
            position.z = position.y;

            // 히트 이펙트 생성
            GameObject effect = Instantiate(hitEffect, position, Quaternion.identity);

            // 이펙트에 이소메트릭 위치 핸들러 추가
            if (updateZPosition && effect.GetComponent<IsometricPositionHandler>() == null)
            {
                effect.AddComponent<IsometricPositionHandler>();
            }
        }
    }
}