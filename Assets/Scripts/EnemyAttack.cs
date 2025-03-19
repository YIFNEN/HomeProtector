using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType { None, Melee, Ranged }

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private AttackType attackType = AttackType.Melee; // 공격 유형
    [SerializeField] private float attackRange = 1.5f; // 공격 범위
    [SerializeField] private float attackRate = 1.0f; // 초당 공격 횟수
    [SerializeField] private float attackDamage = 10f; // 공격 데미지
    [SerializeField] private LayerMask targetLayers; // 대상 레이어
    [SerializeField] private string[] targetTags = { "Resource", "Tower", "Target" }; // 대상 태그

    [Header("Ranged Attack Settings")]
    [SerializeField] private GameObject projectilePrefab; // 발사체 프리팹 (원거리 공격용)
    [SerializeField] private Transform attackPoint; // 발사 위치

    [Header("Effects")]
    [SerializeField] private GameObject attackEffect; // 공격 효과
    [SerializeField] private AudioClip attackSound; // 공격 소리

    // 공격 속도 감소 효과 관련 변수
    private float originalAttackRate; // 원래 공격 속도
    private bool isAttackSlowed = false; // 공격 속도 감소 상태
    private float attackSlowTimer = 0f; // 공격 속도 감소 타이머
    private float currentAttackSlowAmount = 0f; // 현재 적용된 공격 속도 감소 비율

    private float attackTimer = 0f; // 공격 타이머
    private Enemy enemy; // Enemy 컴포넌트 참조
    private Transform currentTarget; // 현재 공격 대상
    private AudioSource audioSource; // 오디오 소스

    // Awake: 초기화
    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        audioSource = GetComponent<AudioSource>();
        originalAttackRate = attackRate; // 원래 공격 속도 저장

        // 공격 지점이 없으면 자신의 위치 사용
        if (attackPoint == null)
        {
            attackPoint = transform;
        }
    }

    // Start: 초기 타겟 찾기
    private void Start()
    {
        FindTarget();
    }

    // Update: 공격 로직 처리
    private void Update()
    {
        // 타겟 확인 및 공격 시도
        CheckTargetAndAttack();

        // 공격 속도 감소 효과 타이머 업데이트
        if (isAttackSlowed)
        {
            attackSlowTimer -= Time.deltaTime;

            // 타이머가 끝나면 공격 속도 복구
            if (attackSlowTimer <= 0)
            {
                ResetAttackRate();
            }
        }
    }

    // 타겟 확인 및 공격 시도
    private void CheckTargetAndAttack()
    {
        // 먼저 Enemy 스크립트의 현재 타겟 사용
        if (enemy != null && enemy.CurrentTarget != null)
        {
            currentTarget = enemy.CurrentTarget;
        }

        // 타겟이 없는지 확인
        if (currentTarget == null)
        {
            // 타겟이 없으면 새로 찾기
            FindTarget();

            if (currentTarget == null)
            {
                return; // 타겟이 없으면 종료
            }
        }

        // 타겟과의 거리 계산
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        // 공격 범위 내에 있으면 공격
        if (distanceToTarget <= attackRange)
        {
            // 공격 타이머 업데이트
            attackTimer += Time.deltaTime;

            // 공격 주기에 도달하면 공격
            if (attackTimer >= 1f / attackRate)
            {
                // 공격 실행
                Attack(currentTarget);
                attackTimer = 0f;
            }
        }
    }

    // 가장 가까운 타겟 찾기
    private void FindTarget()
    {
        // 모든 가능한 타겟 태그에 대해 가장 가까운 타겟 찾기
        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;

        foreach (string tag in targetTags)
        {
            GameObject[] targets = GameObject.FindGameObjectsWithTag(tag);

            foreach (GameObject potentialTarget in targets)
            {
                float distance = Vector3.Distance(transform.position, potentialTarget.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = potentialTarget.transform;
                }
            }
        }

        currentTarget = nearestTarget;

        // 기존 방식: 설정된 범위 내의 모든 콜라이더 찾기 (대체 방식)
        if (currentTarget == null && targetLayers != 0)
        {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackRange * 1.5f, targetLayers);

            foreach (Collider2D collider in hitColliders)
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    currentTarget = collider.transform;
                }
            }
        }
    }

    // 공격 실행
    private void Attack(Transform target)
    {
        switch (attackType)
        {
            case AttackType.Melee:
                MeleeAttack(target);
                break;

            case AttackType.Ranged:
                RangedAttack(target);
                break;

            default:
                break;
        }
    }

    // 근접 공격
    private void MeleeAttack(Transform target)
    {
        // 공격 효과 재생
        if (attackEffect != null)
        {
            Vector3 effectPosition = attackPoint.position;
            effectPosition.z = effectPosition.y; // 이소메트릭 z 조정
            Instantiate(attackEffect, effectPosition, Quaternion.identity);
        }

        // 공격 소리 재생
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        // IDamageable 인터페이스를 구현한 경우 (기존 코드 호환성)
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage((int)attackDamage);
            Debug.Log($"{gameObject.name}이(가) {target.name}에 {attackDamage}의 데미지를 입힘 (IDamageable)");
            return;
        }

        // 재화 오브젝트인 경우
        ResourceObject resource = target.GetComponent<ResourceObject>();
        if (resource != null)
        {
            resource.TakeDamage(attackDamage);
            Debug.Log($"{gameObject.name}이(가) {resource.ResourceName}에 {attackDamage}의 데미지를 입힘");
            return;
        }

        Debug.LogWarning($"{target.name}에는 데미지를 받을 수 있는 컴포넌트가 없습니다.");
    }

    // 원거리 공격
    private void RangedAttack(Transform target)
    {
        // 발사체 프리팹이 없으면 리턴
        if (projectilePrefab == null)
        {
            Debug.LogWarning("발사체 프리팹이 설정되지 않았습니다.");
            return;
        }

        // 공격 소리 재생
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        // 발사체 생성
        GameObject projectile = Instantiate(projectilePrefab, attackPoint.position, Quaternion.identity);

        // 발사체가 ProjectileBase를 상속받았는지 확인
        ProjectileBase projectileBase = projectile.GetComponent<ProjectileBase>();
        if (projectileBase != null)
        {
            // 발사체 초기화 (대상, 데미지)
            projectileBase.Setup(target, attackDamage);
        }
        else
        {
            // 다른 타입의 발사체 처리 (필요시 구현)
            Debug.LogWarning("발사체에 ProjectileBase 컴포넌트가 없습니다.");
            Destroy(projectile);
        }
    }

    // 공격 속도 감소 효과 적용
    public void ApplyAttackSlow(float slowAmount, float duration)
    {
        // 현재 적용된 감속보다 더 강한 감속이거나, 감속 효과가 곧 끝날 경우에만 적용
        if (slowAmount > currentAttackSlowAmount || attackSlowTimer < 0.5f)
        {
            // 감속 효과가 처음 적용되면 원래 속도 저장
            if (!isAttackSlowed)
            {
                originalAttackRate = attackRate;
            }

            // 새로운 감속 효과 적용 (공격 속도 감소 = 공격 주기 증가)
            currentAttackSlowAmount = slowAmount;
            attackRate = originalAttackRate * (1 - slowAmount);
            attackSlowTimer = duration;
            isAttackSlowed = true;

            Debug.Log($"{gameObject.name}의 공격 속도 {slowAmount * 100}% 감소 (지속시간: {duration}초)");
        }
    }

    // 공격 속도 원래대로 복구
    public void ResetAttackRate()
    {
        attackRate = originalAttackRate;
        isAttackSlowed = false;
        currentAttackSlowAmount = 0f;

        Debug.Log($"{gameObject.name}의 공격 속도 복구");
    }

    // 발사체 타워를 위한 인터페이스
    public interface IDamageable
    {
        void TakeDamage(int damage);
    }

    // 에디터에서 공격 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}