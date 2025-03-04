using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyDestroyType { Kill = 0, Arrive }

public class Enemy : MonoBehaviour
{
    private Transform target;
    private NavMeshAgent navMeshAgent;
    private EnemySpawner enemySpawner;

    [SerializeField]
    private int gold = 10;// 적 사망시 획득 골드

    // 적 생성 위치 관련
    [SerializeField]
    private Vector3 spawnOffset = Vector3.zero; // 개별 적의 스폰 위치 오프셋
    private Transform customSpawnPoint = null;  // Added: Custom spawn point for this enemy

    public void Setup(EnemySpawner spawner, Transform target)
    {
        enemySpawner = spawner;
        this.target = target;

        // NavMeshAgent 설정
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.updateRotation = false;
        navMeshAgent.updateUpAxis = false;

        // 스폰 위치 설정
        SetSpawnPosition();
        StartCoroutine("OnMove");
    }

    // Added method to set custom spawn point
    public void SetCustomSpawnPoint(Transform spawnPoint)
    {
        customSpawnPoint = spawnPoint;
    }

    // 스폰 위치를 설정
    public void SetSpawnPosition()
    {
        // Get base spawn position using custom point or default logic
        Vector3 spawnPosition;
        if (customSpawnPoint != null)
        {
            spawnPosition = customSpawnPoint.position;
        }
        else
        {
            spawnPosition = enemySpawner.GetSpawnPosition();
        }

        // Apply individual offset
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

    // 스폰 오프셋 설정 함수 (외부에서 설정 가능)
    public void SetSpawnOffset(Vector3 offset)
    {
        spawnOffset = offset;
    }

    private IEnumerator OnMove()
    {
        if (target == null)
        {
            Debug.LogError("Target is not assigned for enemy: " + gameObject.name);
            yield break;
        }

        // 적이 목표지점을 향해 이동
        navMeshAgent.SetDestination(target.position);

        while (true)
        {
            // In case target was destroyed during gameplay
            if (target == null)
            {
                OnDie(EnemyDestroyType.Kill);
                yield break;
            }

            // 목표에 도달했는지 확인
            if (Vector3.Distance(transform.position, target.position) < 0.5f)
            {
                gold = 0;
                OnDie(EnemyDestroyType.Arrive);
                yield break;
            }

            // 목표가 움직일 수 있으므로 지속적으로 목표 위치 업데이트
            navMeshAgent.SetDestination(target.position);
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