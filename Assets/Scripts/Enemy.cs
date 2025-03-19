using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyDestroyType { Kill = 0, Arrive }

public class Enemy : MonoBehaviour
{
    [Header("Basic Settings")]
    [SerializeField] private int gold = 10; // 적 사망시 획득 골드
    [SerializeField] private int expValue = 20; // 적 사망시 획득 경험치

    [Header("Target Selection")]
    [SerializeField] public string[] targetTagPriority = { "Food", "Gooods", "Human" }; // 타겟 태그 우선순위
    [SerializeField] private float targetSearchRadius = 10f; // 타겟 검색 범위
    [SerializeField] private float targetUpdateInterval = 1f; // 타겟 갱신 주기 (초)

    [Header("Attack Settings")]
    [SerializeField] private bool hasAttack = true; // 공격 기능 활성화 여부

    private Transform target; // 현재 타겟
    private NavMeshAgent navMeshAgent;
    private EnemySpawner enemySpawner;
    private EnemyHP enemyHP;
    private EnemyAttack enemyAttack;
    private Vector3 spawnOffset = Vector3.zero;
    private Transform customSpawnPoint = null;
    private string targetTag = "Target"; // 기본 타겟 태그 저장
    private float lastTargetSearchTime; // 마지막 타겟 검색 시간

    // 현재 타겟에 대한 접근자 추가
    public Transform CurrentTarget => target;
    public string TargetTag => targetTag;
    public int GoldValue => gold;
    public int ExpValue => expValue;

    private void Awake()
    {
        enemyHP = GetComponent<EnemyHP>();
        enemyAttack = GetComponent<EnemyAttack>();

        // 공격 컴포넌트가 없으면서 공격 기능이 활성화된 경우, 컴포넌트 추가
        if (hasAttack && enemyAttack == null)
        {
            enemyAttack = gameObject.AddComponent<EnemyAttack>();
        }
    }

    public void SetSpawnOffset(Vector3 offset)
    {
        spawnOffset = offset;
    }

    public void SetCustomSpawnPoint(Transform spawnPoint)
    {
        customSpawnPoint = spawnPoint;
    }

    public void SetTargetTag(string tag)
    {
        if (!string.IsNullOrEmpty(tag))
        {
            targetTag = tag;
        }
    }

    // 타겟을 변경하는 메서드 추가
    public void SetTarget(Transform newTarget)
    {
        if (newTarget != null && newTarget != target)
        {
            target = newTarget;

            // NavMeshAgent 경로 재설정
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.SetDestination(target.position);
            }

            Debug.Log($"Enemy {gameObject.name} target changed to {target.name}");
        }
    }

    public void Setup(EnemySpawner spawner, Transform target)
    {
        enemySpawner = spawner;
        this.target = target;

        // NavMeshAgent 설정
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
        {
            navMeshAgent.updateRotation = false;
            navMeshAgent.updateUpAxis = false;
        }
        else
        {
            Debug.LogError("NavMeshAgent component not found on enemy!");
        }

        // 스폰 위치 설정
        SetSpawnPosition();

        // 타겟 추가/제거 이벤트 구독
        if (enemySpawner != null)
        {
            enemySpawner.OnTargetAdded += HandleTargetAdded;
            enemySpawner.OnTargetRemoved += HandleTargetRemoved;
        }

        // 초기 타겟이 없으면 새 타겟 찾기
        if (target == null)
        {
            target = FindTargetByPriority();
        }

        // 이동 코루틴 시작
        StartCoroutine("OnMove");
    }

    // 스폰 위치를 설정
    private void SetSpawnPosition()
    {
        // 기본 스폰 위치 가져오기
        Vector3 spawnPosition;
        if (customSpawnPoint != null)
        {
            spawnPosition = customSpawnPoint.position;
        }
        else
        {
            spawnPosition = enemySpawner.GetSpawnPosition();
        }

        // 개별 오프셋 적용
        spawnPosition += spawnOffset;

        // IsometricView를 위한 z 위치 조정 (y와 동일하게)
        spawnPosition.z = spawnPosition.y;

        // NavMesh 위치로 조정 (가장 가까운 NavMesh 지점 찾기)
        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPosition, out hit, 5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
        else
        {
            Debug.LogWarning("NavMesh 지점을 찾을 수 없습니다. 원래 위치에 스폰됩니다.");
            transform.position = spawnPosition;
        }
    }

    // 타겟이 추가되었을 때 호출되는 핸들러
    private void HandleTargetAdded(string tag, Transform newTarget)
    {
        // 현재 타겟이 없는 경우에만 처리
        if (target == null)
        {
            SearchForNewTarget();
        }
        // 또는 현재 타겟과 같은 태그의 새 타겟이 추가되었고, 현재 타겟이 유효하지 않다면
        else if (tag == targetTag && !IsTargetValid(target))
        {
            SearchForNewTarget();
        }
    }

    // 타겟이 제거되었을 때 호출되는 핸들러
    private void HandleTargetRemoved(string tag, Transform removedTarget)
    {
        // 현재 타겟이 제거된 타겟인 경우
        if (target == removedTarget)
        {
            SearchForNewTarget();
        }
    }

    // 새 타겟 찾기
    private void SearchForNewTarget()
    {
        // 우선순위에 따라 새 타겟 찾기
        Transform newTarget = FindTargetByPriority();

        if (newTarget != null)
        {
            SetTarget(newTarget);
        }
        else if (enemySpawner != null)
        {
            // EnemySpawner에 타겟 재할당 요청
            newTarget = enemySpawner.ReassignTargetForEnemy(this, targetTag);

            // 그래도 타겟이 없다면 경고
            if (newTarget == null)
            {
                Debug.LogWarning($"Enemy {gameObject.name} couldn't find any target after search");
            }
        }
    }

    // 우선순위에 따른 타겟 찾기
    private Transform FindTargetByPriority()
    {
        // 마지막 검색 시간 갱신
        lastTargetSearchTime = Time.time;

        // 우선순위 태그 배열이 비어있으면 기본 태그 사용
        if (targetTagPriority.Length == 0)
        {
            return FindClosestTargetWithTag(targetTag);
        }

        // 우선순위에 따라 타겟 검색
        foreach (string tag in targetTagPriority)
        {
            Transform foundTarget = FindClosestTargetWithTag(tag);
            if (foundTarget != null)
            {
                // 태그 저장 및 타겟 반환
                targetTag = tag;
                return foundTarget;
            }
        }

        // 아무 타겟도 못찾으면 null 반환
        return null;
    }

    // 특정 태그를 가진 가장 가까운 타겟 찾기
    private Transform FindClosestTargetWithTag(string tag)
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
        Transform closestTarget = null;
        float closestDistance = targetSearchRadius;

        foreach (GameObject obj in taggedObjects)
        {
            // 유효한지 확인
            if (!IsTargetValid(obj.transform))
                continue;

            float distance = Vector3.Distance(transform.position, obj.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = obj.transform;
            }
        }

        return closestTarget;
    }

    // 타겟이 유효한지 확인
    private bool IsTargetValid(Transform checkTarget)
    {
        // null이거나 비활성화된 경우
        if (checkTarget == null || !checkTarget.gameObject.activeInHierarchy)
            return false;

        // ResourceObject인 경우 체력 확인
        ResourceObject resource = checkTarget.GetComponent<ResourceObject>();
        if (resource != null && resource.CurrentHP <= 0)
            return false;

        return true;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (enemySpawner != null)
        {
            enemySpawner.OnTargetAdded -= HandleTargetAdded;
            enemySpawner.OnTargetRemoved -= HandleTargetRemoved;
        }
    }

    private IEnumerator OnMove()
    {
        while (true)
        {
            // 타겟이 없거나 유효하지 않은 경우
            if (target == null || !IsTargetValid(target))
            {
                // 일정 시간마다 새 타겟 검색
                if (Time.time - lastTargetSearchTime >= targetUpdateInterval)
                {
                    SearchForNewTarget();

                    // 그래도 타겟이 없다면
                    if (target == null)
                    {
                        Debug.LogWarning($"Enemy {gameObject.name} couldn't find any target, waiting...");
                        yield return new WaitForSeconds(1f); // 잠시 대기
                        continue;
                    }
                }
            }

            // 타겟이 있으면 NavMeshAgent로 이동
            if (target != null && navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.SetDestination(target.position);

                // 목표에 도달했는지 확인
                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                if (distanceToTarget < 0.5f)
                {
                    // ResourceObject인 경우
                    ResourceObject resource = target.GetComponent<ResourceObject>();
                    if (resource != null)
                    {
                        // 리소스 파괴 후 다음 타겟 검색
                        yield return new WaitForSeconds(0.5f); // 잠시 대기
                        SearchForNewTarget();
                        continue;
                    }
                    else
                    {
                        // 기본 목적지 도달 처리
                        gold = 0; // 목표 도달 시 골드는 0
                        expValue = 0; // 목표 도달 시 경험치도 0
                        OnDie(EnemyDestroyType.Arrive);
                        yield break;
                    }
                }
            }

            // IsometricView를 위한 z 위치 조정 (y와 동일하게)
            Vector3 position = transform.position;
            position.z = position.y;
            transform.position = position;

            yield return null;
        }
    }

    public void OnDie(EnemyDestroyType type)
    {
        if (enemySpawner == null)
        {
            Debug.LogError("EnemySpawner is not assigned! Check the Setup method.");
            return; // NullReferenceException 방지
        }

        // 플레이어 경험치 참조 가져오기
        PlayerExperience playerExperience = FindObjectOfType<PlayerExperience>();

        // KILL일 경우 경험치 부여
        if (type == EnemyDestroyType.Kill && playerExperience != null)
        {
            playerExperience.AddExperienceForEnemy(type, expValue);
        }

        enemySpawner.DestroyEnemy(type, this, gold);
    }
}