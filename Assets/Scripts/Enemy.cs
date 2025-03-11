using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyDestroyType { Kill = 0, Arrive }

public class Enemy : MonoBehaviour
{
    [SerializeField]
    private int gold = 10; // 적 사망시 획득 골드

    private Transform target;
    private NavMeshAgent navMeshAgent;
    private EnemySpawner enemySpawner;
    private Vector3 spawnOffset = Vector3.zero;
    private Transform customSpawnPoint = null;
    private string targetTag = "Target"; // 기본 타겟 태그 저장

    // 현재 타겟에 대한 접근자 추가
    public Transform CurrentTarget => target;
    public string TargetTag => targetTag;

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
        // 현재 사용 중인 태그와 일치하고, 현재 타겟이 없는 경우에만 처리
        if (tag == targetTag && (target == null || !target.gameObject.activeInHierarchy))
        {
            // 현재 위치에서 가장 가까운 새 타겟으로 변경
            enemySpawner.ReassignTargetForEnemy(this, tag);
        }
    }

    // 타겟이 제거되었을 때 호출되는 핸들러
    private void HandleTargetRemoved(string tag, Transform removedTarget)
    {
        // 현재 사용 중인 태그가 일치하고, 현재 타겟이 제거된 타겟인 경우
        if (tag == targetTag && target == removedTarget)
        {
            // 새로운 타겟 찾기
            Transform newTarget = enemySpawner.ReassignTargetForEnemy(this, tag);

            // 유효한 타겟이 더 이상 없는 경우 처리
            if (newTarget == null)
            {
                Debug.LogWarning($"Enemy {gameObject.name} has no valid target after target removal");
                // 선택적: 적을 제거하거나 대기 상태로 변경
            }
        }
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
        if (target == null)
        {
            // 타겟이 없는 경우 새 타겟 찾기 시도
            if (enemySpawner != null)
            {
                Transform newTarget = enemySpawner.ReassignTargetForEnemy(this, targetTag);
                if (newTarget == null)
                {
                    Debug.LogError("Target is not assigned for enemy: " + gameObject.name);
                    yield break;
                }
            }
            else
            {
                Debug.LogError("EnemySpawner is not assigned for enemy: " + gameObject.name);
                yield break;
            }
        }

        // 적이 목표지점을 향해 이동
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.SetDestination(target.position);
        }

        while (true)
        {
            // 타겟이 파괴된 경우 확인
            if (target == null)
            {
                // 새 타겟 할당 시도
                Transform newTarget = enemySpawner.ReassignTargetForEnemy(this, targetTag);
                if (newTarget == null)
                {
                    // 유효한 타겟이 없으면 적 제거
                    OnDie(EnemyDestroyType.Kill);
                    yield break;
                }
            }

            // 목표에 도달했는지 확인
            if (Vector3.Distance(transform.position, target.position) < 0.5f)
            {
                gold = 0; // 목표 도달 시 골드는 0
                OnDie(EnemyDestroyType.Arrive);
                yield break;
            }

            // 목표가 움직일 수 있으므로 지속적으로 목표 위치 업데이트
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && target != null)
            {
                navMeshAgent.SetDestination(target.position);
            }

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

        enemySpawner.DestroyEnemy(type, this, gold);
    }
}