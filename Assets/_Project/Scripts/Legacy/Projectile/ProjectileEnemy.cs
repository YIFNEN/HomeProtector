using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileEnemy : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10f;          // 발사체 속도
    [SerializeField] private float maxLifetime = 5f;     // 최대 지속 시간
    [SerializeField] private float rotationSpeed = 10f;  // 회전 속도
    [SerializeField] private bool useHoming = true;      // 유도 기능 사용 여부
    [SerializeField] private float homingStrength = 5f;  // 유도 강도

    [Header("Effects")]
    [SerializeField] private GameObject hitEffect;       // 타격 효과
    [SerializeField] private AudioClip hitSound;         // 타격 소리
    [SerializeField] private GameObject trailEffect;     // 궤적 효과

    [Header("Isometric Settings")]
    [SerializeField] private bool useIsometricPosition = true;  // 이소메트릭 위치 사용 여부

    // 내부 변수
    private Transform target;                // 타겟
    private float damage;                    // 데미지
    private Rigidbody rb;                    // 리지드바디
    private AudioSource audioSource;         // 오디오 소스
    private Vector3 lastTargetPosition;      // 마지막 타겟 위치
    private IsometricPositionHandler isometricPosition;  // 이소메트릭 위치 핸들러
    private bool hasHit = false;             // 타격 여부

    // 초기화
    private void Awake()
    {
        // 리지드바디 확인 또는 추가
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.drag = 0.5f;
        }

        // 오디오 소스 확인 또는 추가
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && hitSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 이소메트릭 위치 핸들러 확인
        if (useIsometricPosition)
        {
            isometricPosition = GetComponent<IsometricPositionHandler>();
            if (isometricPosition == null)
            {
                isometricPosition = gameObject.AddComponent<IsometricPositionHandler>();
            }
        }
    }

    // 발사체 설정 (외부에서 호출됨)
    public void Setup(Transform targetTransform, float damageAmount)
    {
        target = targetTransform;
        damage = damageAmount;

        if (target != null)
        {
            lastTargetPosition = target.position;

            // 초기 방향을 타겟 쪽으로 설정
            Vector3 direction = (lastTargetPosition - transform.position).normalized;
            transform.forward = direction;

            // 초기 속도 적용
            rb.velocity = direction * speed;
        }

        // 궤적 효과 활성화
        if (trailEffect != null)
        {
            GameObject trail = Instantiate(trailEffect, transform.position, Quaternion.identity);
            trail.transform.SetParent(transform);
        }

        // 최대 지속 시간 후 자동 파괴
        Destroy(gameObject, maxLifetime);
    }

    // 매 프레임 실행
    private void Update()
    {
        if (hasHit) return;

        // 타겟 체크 및 추적
        UpdateTargetTracking();

        // 이소메트릭 위치 업데이트
        if (useIsometricPosition && isometricPosition == null)
        {
            // 수동으로 z 위치 조정
            Vector3 position = transform.position;
            position.z = position.y;
            transform.position = position;
        }
    }

    // 타겟 추적 업데이트
    private void UpdateTargetTracking()
    {
        // 타겟이 유효한지 확인
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            // 타겟이 없거나 비활성화된 경우 마지막 위치로 진행
            return;
        }

        // 현재 타겟 위치 업데이트
        lastTargetPosition = target.position;

        // 유도 기능이 활성화된 경우
        if (useHoming)
        {
            // 타겟 방향 계산
            Vector3 directionToTarget = (lastTargetPosition - transform.position).normalized;

            // 현재 발사체의 속도 방향
            Vector3 currentDirection = rb.velocity.normalized;

            // 두 방향을 보간하여 새 방향 계산
            Vector3 newDirection = Vector3.Slerp(currentDirection, directionToTarget, Time.deltaTime * homingStrength);

            // 속도 업데이트
            rb.velocity = newDirection * speed;

            // 발사체 방향 설정
            if (rb.velocity != Vector3.zero)
            {
                transform.forward = Vector3.Slerp(transform.forward, rb.velocity.normalized, Time.deltaTime * rotationSpeed);
            }
        }
    }

    // 충돌 감지
    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.transform);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.transform);
    }

    // 충돌 처리
    private void HandleCollision(Transform hitTransform)
    {
        // 이미 타격한 경우 무시
        if (hasHit) return;

        // 자신을 발사한 적과 충돌하지 않도록 확인 (필요시 구현)

        // 타격 처리
        hasHit = true;

        // 데미지 적용 시도
        bool damageApplied = false;

        // ResourceObject 컴포넌트 확인
        ResourceObject resource = hitTransform.GetComponent<ResourceObject>();
        if (resource != null)
        {
            resource.TakeDamage(damage);
            Debug.Log($"발사체가 {resource.ResourceName}에 {damage}의 데미지를 입힘");
            damageApplied = true;
        }

        // EnemyHP 컴포넌트 확인
        if (!damageApplied)
        {
            EnemyHP enemyHP = hitTransform.GetComponent<EnemyHP>();
            if (enemyHP != null)
            {
                enemyHP.TakeDamage(damage);
                Debug.Log($"발사체가 {hitTransform.name}에 {damage}의 데미지를 입힘 (EnemyHP)");
                damageApplied = true;
            }
        }

        // IDamageable 인터페이스 확인
        if (!damageApplied)
        {
            var damageable = hitTransform.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage((int)damage);
                Debug.Log($"발사체가 {hitTransform.name}에 {damage}의, 데미지를 입힘 (IDamageable)");
                damageApplied = true;
            }
        }

        // 타격 효과 생성
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);

            // 이소메트릭 효과 처리
            if (useIsometricPosition)
            {
                if (effect.GetComponent<IsometricPositionHandler>() == null)
                {
                    effect.AddComponent<IsometricPositionHandler>();
                }
            }
        }

        // 타격 소리 재생
        if (hitSound != null && audioSource != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }

        // 발사체 파괴
        Destroy(gameObject);
    }
}