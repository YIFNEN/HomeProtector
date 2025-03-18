using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private float attackRate = 1.0f;    // 초당 공격 횟수
    [SerializeField] private float attackRange = 1.5f;   // 공격 범위
    [SerializeField] private int attackDamage = 1;       // 공격 데미지
    [SerializeField] private string targetTag = "Target"; // 공격 대상 태그

    private float attackTimer = 0f;                // 공격 타이머
    private float originalAttackRate;             // 원래 공격 속도
    private bool isAttackSlowed = false;          // 공격 속도 감소 상태
    private float attackSlowTimer = 0f;           // 공격 속도 감소 타이머
    private float currentAttackSlowAmount = 0f;   // 현재 적용된 공격 속도 감소 비율

    private Transform target;                      // 현재 공격 대상

    private void Awake()
    {
        originalAttackRate = attackRate;
    }

    private void Start()
    {
        // 초기 타겟 찾기
        FindTarget();
    }

    private void Update()
    {
        // 타겟이 없으면 새로 찾기
        if (target == null)
        {
            FindTarget();
            return;
        }

        // 공격 범위 내에 있는지 확인
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget <= attackRange)
        {
            // 공격 타이머 업데이트
            attackTimer += Time.deltaTime;

            // 공격 주기에 도달하면 공격
            if (attackTimer >= 1f / attackRate)
            {
                Attack();
                attackTimer = 0f;
            }
        }

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

    private void FindTarget()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);
        float closestDistance = float.MaxValue;
        Transform closestTarget = null;

        foreach (GameObject potentialTarget in targets)
        {
            float distance = Vector3.Distance(transform.position, potentialTarget.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = potentialTarget.transform;
            }
        }

        target = closestTarget;
    }

    private void Attack()
    {
        // 대상이 데미지를 받을 수 있는지 확인
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(attackDamage);
        }

        // 공격 애니메이션이나 효과 (필요시 추가)
        // PlayAttackAnimation();

        // 공격 효과음 (필요시 추가)
        // AudioManager.Instance.PlaySound("EnemyAttack");
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
        }
    }

    // 공격 속도 원래대로 복구
    public void ResetAttackRate()
    {
        attackRate = originalAttackRate;
        isAttackSlowed = false;
        currentAttackSlowAmount = 0f;
    }

    // 발사체 타워를 위한 인터페이스 (필요시)
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