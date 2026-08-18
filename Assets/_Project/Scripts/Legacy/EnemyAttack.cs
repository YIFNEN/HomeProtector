using System;
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

    [Header("Ranged Attack Settings")]
    [SerializeField] private GameObject projectilePrefab; // 발사체 프리팹 (원거리 공격용)
    [SerializeField] private Transform attackPoint; // 발사 위치

    [Header("Effects")]
    [SerializeField] private GameObject attackEffect; // 공격 효과
    [SerializeField] private AudioClip attackSound; // 공격 소리

    [Header("Isometric Settings")]
    [SerializeField] private bool useIsometricPosition = true; // 이소메트릭 위치 사용 여부

    // 공격 속도 감소 효과 관련 변수
    private float originalAttackRate; // 원래 공격 속도
    private bool isAttackSlowed = false; // 공격 속도 감소 상태
    private float attackSlowTimer = 0f; // 공격 속도 감소 타이머
    private float currentAttackSlowAmount = 0f; // 현재 적용된 공격 속도 감소 비율

    private float attackTimer = 0f; // 공격 타이머
    private Enemy enemy; // Enemy 컴포넌트 참조
    private Transform currentTarget; // 현재 공격 대상
    private AudioSource audioSource; // 오디오 소스
    private IsometricPositionHandler isometricPosition; // 이소메트릭 위치 핸들러
    private bool isInitialized = false; // 초기화 여부 확인용

    // Awake: 초기화
    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        // 오디오 소스 가져오기 또는 필요시 생성
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && attackSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        originalAttackRate = attackRate; // 원래 공격 속도 저장

        // 공격 지점이 없으면 자신의 위치 사용
        if (attackPoint == null)
        {
            attackPoint = transform;
        }

        // 이소메트릭 위치 핸들러 확인
        if (useIsometricPosition)
        {
            isometricPosition = GetComponent<IsometricPositionHandler>();
        }
    }

    // Start: 초기화 및 타겟 설정
    private void Start()
    {
        isInitialized = true;

        // Enemy 컴포넌트에서 현재 타겟을 가져오기 시도
        if (enemy != null && enemy.CurrentTarget != null)
        {
            currentTarget = enemy.CurrentTarget;
        }
    }

    // Update: 공격 로직 처리
    private void Update()
    {
        if (!isInitialized) return;

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
        // Enemy 스크립트의 현재 타겟 사용 (항상 Enemy에서 타겟을 관리)
        if (enemy != null && enemy.CurrentTarget != null)
        {
            currentTarget = enemy.CurrentTarget;
        }

        // 타겟이 없거나 유효하지 않은지 확인
        if (currentTarget == null || (TargetManager.Instance != null && !TargetManager.Instance.IsTargetValid(currentTarget)))
        {
            return; // 유효한 타겟이 없으면 종료
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

    // 공격 실행
    private void Attack(Transform target)
    {
        if (target == null) return;

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
        if (target == null) return;

        // 공격 효과 재생
        if (attackEffect != null)
        {
            Vector3 effectPosition = attackPoint.position;

            // 이소메트릭 위치 조정
            if (isometricPosition != null)
            {
                // 이펙트 생성 시 IsometricPositionHandler 추가
                GameObject effect = Instantiate(attackEffect, effectPosition, Quaternion.identity);
                if (effect.GetComponent<IsometricPositionHandler>() == null)
                {
                    effect.AddComponent<IsometricPositionHandler>();
                }
            }
            else if (useIsometricPosition)
            {
                // 수동으로 z 위치 조정
                effectPosition.z = effectPosition.y;
                Instantiate(attackEffect, effectPosition, Quaternion.identity);
            }
            else
            {
                Instantiate(attackEffect, effectPosition, Quaternion.identity);
            }
        }

        // 공격 소리 재생
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        // 데미지 적용 시도 (다양한 타겟 대응)
        bool damageApplied = false;

        // ResourceObject 컴포넌트 확인
        ResourceObject resource = target.GetComponent<ResourceObject>();
        if (resource != null)
        {
            resource.TakeDamage(attackDamage);
            Debug.Log($"{gameObject.name}이(가) {resource.ResourceName}에 {attackDamage}의 데미지를 입힘");
            damageApplied = true;
        }

        // EnemyHP 컴포넌트 확인
        if (!damageApplied)
        {
            EnemyHP enemyHP = target.GetComponent<EnemyHP>();
            if (enemyHP != null)
            {
                enemyHP.TakeDamage(attackDamage);
                Debug.Log($"{gameObject.name}이(가) {target.name}에 {attackDamage}의 데미지를 입힘 (EnemyHP)");
                damageApplied = true;
            }
        }

        // IDamageable 인터페이스 확인 (다른 타입의 대상)
        if (!damageApplied)
        {
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage((int)attackDamage);
                Debug.Log($"{gameObject.name}이(가) {target.name}에 {attackDamage}의 데미지를 입힘 (IDamageable)");
                damageApplied = true;
            }
        }

        if (!damageApplied)
        {
            Debug.LogWarning($"{target.name}에는 데미지를 받을 수 있는 컴포넌트가 없습니다.");
        }
    }

    // 원거리 공격
    private void RangedAttack(Transform target)
    {
        if (target == null) return;

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

        // 발사 위치 계산
        Vector3 spawnPosition = attackPoint.position;
        if (useIsometricPosition && isometricPosition == null)
        {
            spawnPosition.z = spawnPosition.y;
        }

        // 발사체 생성
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        ProjectileEnemy enemyProjectile = projectile.GetComponent<ProjectileEnemy>();
        if (enemyProjectile != null)
        {
            enemyProjectile.Setup(target, attackDamage);
            return;
        }

        ProjectileBase projectileBase = projectile.GetComponent<ProjectileBase>();
        if (projectileBase != null)
        {
            projectileBase.Setup(target, attackDamage);
            return;
        }

        ProjectileBook projectileBook = projectile.GetComponent<ProjectileBook>();
        if (projectileBook != null)
        {
            projectileBook.Setup(target, attackDamage);
            return;
        }

        Debug.LogWarning($"{projectilePrefab.name}에 지원되는 projectile Setup 컴포넌트가 없습니다.");
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

    // 에디터에서 공격 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
