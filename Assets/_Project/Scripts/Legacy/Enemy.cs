using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyDestroyType { Kill = 0, Arrive }

public class Enemy : MonoBehaviour
{
    [Header("Basic Settings")]
    [SerializeField] public int gold = 10; // 적 사망시 획득 골드
    [SerializeField] public int expValue = 20; // 적 사망시 획득 경험치

    [Header("Target Selection")]
    [SerializeField, Tooltip("타겟 태그 우선순위 (순서대로 검색)")]
    public string[] targetTagPriority = { "Goods", "Food", "Human" }; // 타겟 태그 우선순위
    [SerializeField, Tooltip("타겟 검색 범위")]
    private float targetSearchRadius = 10f; // 타겟 검색 범위
    [SerializeField, Tooltip("타겟 갱신 주기 (초)")]
    private float targetUpdateInterval = 1f; // 타겟 갱신 주기
    [SerializeField, Tooltip("기본 타겟 태그")]
    private string defaultTargetTag = "Target"; // 기본 타겟 태그

    [Header("Attack Settings")]
    [SerializeField] private bool hasAttack = true; // 공격 기능 활성화 여부

    [Header("Isometric Settings")]
    [SerializeField] private bool useIsometricPosition = true; // 이소메트릭 위치 사용 여부

    [Header("Debug")]
    [SerializeField] private bool debugMode = false; // 디버그 모드

    private Transform target; // 현재 타겟
    private NavMeshAgent navMeshAgent;
    private EnemySpawner enemySpawner;
    private EnemyHP enemyHP;
    private EnemyAttack enemyAttack;
    private EnemyDirectionFlipper directionFlipper; // 방향 전환 컴포넌트 참조
    private IsometricPositionHandler isometricPosition; // 이소메트릭 위치 핸들러
    private Vector3 spawnOffset = Vector3.zero;
    private Transform customSpawnPoint = null;
    private string targetTag = "Target"; // 현재 사용 중인 타겟 태그
    private float lastTargetSearchTime; // 마지막 타겟 검색 시간
    private bool isSearchingForTarget = false; // 타겟 검색 중 여부(중복 검색 방지)

    // 현재 타겟에 대한 접근자 추가
    public Transform CurrentTarget => target;
    public string TargetTag => targetTag;
    public int GoldValue => gold;
    public int ExpValue => expValue;

    private void Awake()
    {
        // 필요한 컴포넌트 가져오기
        enemyHP = GetComponent<EnemyHP>();
        enemyAttack = GetComponent<EnemyAttack>();

        // 공격 컴포넌트가 없으면서 공격 기능이 활성화된 경우, 컴포넌트 추가
        if (hasAttack && enemyAttack == null)
        {
            enemyAttack = gameObject.AddComponent<EnemyAttack>();
        }

        // 방향 전환 컴포넌트 확인 및 추가
        directionFlipper = GetComponent<EnemyDirectionFlipper>();
        if (directionFlipper == null)
        {
            directionFlipper = gameObject.AddComponent<EnemyDirectionFlipper>();
        }

        // 이소메트릭 위치 핸들러 확인 및 추가
        if (useIsometricPosition)
        {
            isometricPosition = GetComponent<IsometricPositionHandler>();
            if (isometricPosition == null)
            {
                isometricPosition = gameObject.AddComponent<IsometricPositionHandler>();
            }
        }
    }

    // Update 메서드에서 더 적극적으로 타겟을 찾도록 수정
    private void Update()
    {
        // 타겟이 없거나 유효하지 않은 경우 새 타겟 찾기
        if ((target == null || (TargetManager.Instance != null && !TargetManager.Instance.IsTargetValid(target))) && !isSearchingForTarget)
        {
            // 타겟이 없는 경우 더 빠르게 검색
            float interval = target == null ? targetUpdateInterval * 0.5f : targetUpdateInterval;

            // 마지막 검색으로부터 일정 시간이 지났는지 확인
            if (Time.time - lastTargetSearchTime >= interval)
            {
                SearchForNewTarget();
            }
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

    // 타겟 태그 우선순위 배열 가져오기
    public string[] GetTargetTagPriority()
    {
        // 비어있으면 기본 태그 반환
        if (targetTagPriority == null || targetTagPriority.Length == 0)
        {
            return new string[] { defaultTargetTag };
        }
        return targetTagPriority;
    }

    // 타겟 검색 범위 가져오기
    public float GetTargetSearchRadius()
    {
        return targetSearchRadius;
    }

    // 타겟을 변경하는 메서드
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

            // 방향 전환 컴포넌트가 있으면 타겟 방향으로 설정
            if (directionFlipper != null)
            {
                directionFlipper.SetFacingDirection(target.position);
            }

            if (debugMode)
            {
                Debug.Log($"Enemy {gameObject.name} target changed to {target.name} (Tag: {target.tag})");
            }
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

        // TargetManager 이벤트 구독
        if (TargetManager.Instance != null)
        {
            TargetManager.Instance.OnTargetAdded += HandleTargetAdded;
            TargetManager.Instance.OnTargetRemoved += HandleTargetRemoved;
        }

        // 초기 타겟이 없으면 새 타겟 찾기
        if (target == null)
        {
            SearchForNewTarget();
        }
        else
        {
            // 초기 타겟이 있으면 해당 태그 저장
            if (target != null)
            {
                targetTag = target.tag;
            }
        }

        // 방향 전환 설정 (초기 방향 설정)
        if (target != null && directionFlipper != null)
        {
            directionFlipper.SetFacingDirection(target.position);
        }

        // 이동 코루틴 시작
        StartCoroutine(OnMoveWithDirectionUpdate());
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
            // 이소메트릭 위치 핸들러 사용 여부에 따라 다르게 처리
            if (isometricPosition != null)
            {
                isometricPosition.SetPosition(hit.position);
            }
            else
            {
                // 수동으로 z 위치 조정
                Vector3 position = hit.position;
                position.z = position.y;
                transform.position = position;
            }
        }
        else
        {
            Debug.LogWarning("NavMesh 지점을 찾을 수 없습니다. 원래 위치에 스폰됩니다.");

            // 이소메트릭 위치 핸들러 사용 여부에 따라 다르게 처리
            if (isometricPosition != null)
            {
                isometricPosition.SetPosition(spawnPosition);
            }
            else
            {
                // 수동으로 z 위치 조정
                spawnPosition.z = spawnPosition.y;
                transform.position = spawnPosition;
            }
        }
    }

    // 타겟이 추가되었을 때 호출되는 핸들러
    private void HandleTargetAdded(string tag, Transform newTarget)
    {
        // 현재 타겟이 없는 경우
        if (target == null)
        {
            SearchForNewTarget();
        }
        // 현재 타겟이 유효하지 않고, 추가된 타겟의 태그가 우선순위에 있는 경우
        else if (!TargetManager.Instance.IsTargetValid(target))
        {
            string[] priorities = GetTargetTagPriority();
            for (int i = 0; i < priorities.Length; i++)
            {
                if (priorities[i] == tag)
                {
                    // 현재 태그보다 높은 우선순위면 즉시 타겟 변경
                    if (i < System.Array.IndexOf(priorities, targetTag))
                    {
                        SearchForNewTarget();
                    }
                    break;
                }
            }
        }
    }

    // 타겟이 제거되었을 때 호출되는 핸들러
    private void HandleTargetRemoved(string tag, Transform removedTarget)
    {
        // 현재 타겟이 제거된 타겟인 경우 새 타겟 찾기
        if (target == removedTarget)
        {
            SearchForNewTarget();
        }
        // 또는 현재 타겟의 태그와 같은 태그이고, 해당 태그의 오브젝트가 더 이상 없는 경우
        else if (tag == targetTag && TargetManager.Instance.GetTargetCountForTag(tag) == 0)
        {
            // 다음 우선순위 태그로 타겟 찾기
            SearchForNewTarget();
        }
    }

    // SearchForNewTarget 메서드 수정
    private void SearchForNewTarget()
    {
        // 중복 검색 방지
        if (isSearchingForTarget) return;
        isSearchingForTarget = true;

        // 마지막 검색 시간 갱신
        lastTargetSearchTime = Time.time;

        Transform newTarget = null;

        // 기본 검색 범위로 시도
        float searchRadius = targetSearchRadius;

        // TargetManager를 통해 우선순위별로 타겟 찾기
        if (TargetManager.Instance != null)
        {
            // 프리팹의 설정된 타겟 태그 우선순위 사용
            newTarget = TargetManager.Instance.FindTargetByPriority(
                GetTargetTagPriority(),
                transform.position,
                searchRadius
            );

            // 찾지 못했다면 검색 범위를 2배로 늘려서 재시도
            if (newTarget == null)
            {
                searchRadius *= 2;
                newTarget = TargetManager.Instance.FindTargetByPriority(
                    GetTargetTagPriority(),
                    transform.position,
                    searchRadius
                );
            }

            // 그래도 찾지 못했다면 전체 씬에서 검색 (제한 없음)
            if (newTarget == null)
            {
                newTarget = TargetManager.Instance.FindTargetByPriority(
                    GetTargetTagPriority(),
                    transform.position,
                    float.MaxValue
                );
            }

            // 찾은 타겟의 태그 저장 (나중에 참조용)
            if (newTarget != null)
            {
                targetTag = newTarget.tag;
            }
            // 여전히 타겟을 찾지 못했다면, 다음 검색 시간을 앞당김
            else
            {
                lastTargetSearchTime = Time.time - (targetUpdateInterval * 0.8f);
            }
        }

        // 타겟을 찾았으면 설정
        if (newTarget != null)
        {
            SetTarget(newTarget);
            if (debugMode)
            {
                Debug.Log($"적 {gameObject.name}이(가) 새 타겟 찾음: {newTarget.name} (태그: {newTarget.tag})");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning($"적 {gameObject.name}이(가) 타겟을 찾지 못했습니다! 더 빠르게 재검색합니다.");
        }

        isSearchingForTarget = false;
    }
    private void OnDisable()
    {
        // TargetManager 이벤트 구독 해제
        if (TargetManager.Instance != null)
        {
            TargetManager.Instance.OnTargetAdded -= HandleTargetAdded;
            TargetManager.Instance.OnTargetRemoved -= HandleTargetRemoved;
        }
    }

    // Enemy 클래스의 OnMoveWithDirectionUpdate 코루틴 수정
    private IEnumerator OnMoveWithDirectionUpdate()
    {
        int stuckCounter = 0;
        Vector3 lastPosition = transform.position;

        while (true)
        {
            // 타겟이 없거나 유효하지 않은 경우, 더 적극적으로 새 타겟 찾기
            if (target == null || (TargetManager.Instance != null && !TargetManager.Instance.IsTargetValid(target)))
            {
                // 즉시 새 타겟 찾기 시도
                SearchForNewTarget(targetSearchRadius * 2); // 더 넓은 범위로 검색

                // 그래도 타겟이 없다면
                if (target == null)
                {
                    // 방향을 랜덤하게 변경하여 제자리 맴돌기 방지
                    Vector2 randomDirection = Random.insideUnitCircle.normalized;
                    Vector3 tempDestination = transform.position + new Vector3(randomDirection.x, randomDirection.y, 0) * 3f;

                    if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
                    {
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(tempDestination, out hit, 5f, NavMesh.AllAreas))
                        {
                            navMeshAgent.SetDestination(hit.position);
                        }
                    }

                    Debug.Log("타겟을 찾지 못해 랜덤 이동합니다.");
                    yield return new WaitForSeconds(1f);
                    continue;
                }
            }

            // 타겟이 있으면 NavMeshAgent로 이동
            if (target != null && navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.SetDestination(target.position);

                // 이동 방향에 따라 스프라이트 방향 전환
                if (directionFlipper != null)
                {
                    Vector3 moveDirection = navMeshAgent.velocity;
                    if (moveDirection.sqrMagnitude > 0.01f)
                    {
                        // 이동 방향을 기준으로 방향 전환
                        directionFlipper.SetFacingDirection(transform.position + moveDirection);
                    }
                }

                // 제자리에 갇혔는지 확인
                if (Vector3.Distance(transform.position, lastPosition) < 0.05f)
                {
                    stuckCounter++;

                    // 일정 시간동안 제자리에 갇혔으면
                    if (stuckCounter > 30) // 약 1초 동안 제자리에 있으면
                    {
                        stuckCounter = 0;
                        // 새 타겟 검색 강제 실행 (더 넓은 범위)
                        SearchForNewTarget(targetSearchRadius * 3);

                        // 그래도 타겟이 없으면 랜덤 이동
                        if (target == null)
                        {
                            Vector2 randomDirection = Random.insideUnitCircle.normalized;
                            Vector3 tempDestination = transform.position + new Vector3(randomDirection.x, randomDirection.y, 0) * 5f;

                            NavMeshHit hit;
                            if (NavMesh.SamplePosition(tempDestination, out hit, 5f, NavMesh.AllAreas))
                            {
                                navMeshAgent.SetDestination(hit.position);
                            }

                            Debug.Log("제자리에 갇혀 랜덤으로 이동합니다.");
                            yield return new WaitForSeconds(1f);
                        }
                    }
                }
                else
                {
                    stuckCounter = 0;
                }

                lastPosition = transform.position;

                // 목표에 도달했는지 확인
                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                if (distanceToTarget < 0.5f)
                {
                    // ResourceObject인 경우
                    ResourceObject resource = target.GetComponent<ResourceObject>();
                    if (resource != null)
                    {
                        // 리소스 파괴 후 즉시 다음 타겟 검색 (더 넓은 범위)
                        yield return new WaitForSeconds(0.5f); // 잠시 대기
                        SearchForNewTarget(targetSearchRadius * 2);
                        continue;
                    }
                    else
                    {
                    
                        yield break;
                    }
                }
            }

            // 이소메트릭 위치 핸들러가 없는 경우 수동으로 Z 위치 조정
            if (isometricPosition == null && useIsometricPosition)
            {
                Vector3 position = transform.position;
                position.z = position.y;
                transform.position = position;
            }

            yield return null;
        }
    }

    // 범위를 매개변수로 받는 새 타겟 찾기 메서드
    private void SearchForNewTarget(float searchRadius = -1)
    {
        // 중복 검색 방지
        if (isSearchingForTarget) return;
        isSearchingForTarget = true;

        // 기본 검색 범위 사용 여부
        float actualRadius = searchRadius > 0 ? searchRadius : targetSearchRadius;

        // 마지막 검색 시간 갱신
        lastTargetSearchTime = Time.time;

        Transform newTarget = null;

        // TargetManager를 통해 우선순위별로 타겟 찾기
        if (TargetManager.Instance != null)
        {
            // 프리팹의 설정된 타겟 태그 우선순위 사용
            newTarget = TargetManager.Instance.FindTargetByPriority(
                GetTargetTagPriority(),
                transform.position,
                actualRadius
            );

            // 찾은 타겟의 태그 저장 (나중에 참조용)
            if (newTarget != null)
            {
                targetTag = newTarget.tag;
                Debug.Log($"새 타겟 찾음: {newTarget.name} (태그: {targetTag}, 거리: {Vector3.Distance(transform.position, newTarget.position)})");
            }
            else
            {
                Debug.LogWarning($"타겟을 찾지 못했습니다. 검색 범위: {actualRadius}");
            }
        }

        // 타겟을 찾았으면 설정
        if (newTarget != null)
        {
            SetTarget(newTarget);

            // NavMeshAgent 리셋 추가
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.ResetPath();
                navMeshAgent.SetDestination(newTarget.position);
            }
        }

        isSearchingForTarget = false;
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