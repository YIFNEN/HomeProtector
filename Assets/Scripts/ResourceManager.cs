using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    private List<ResourceObject> allResources = new List<ResourceObject>();
    private List<ResourceObject> initialResources = new List<ResourceObject>(); // 초기 리소스 목록 저장
    private float initialTotalMaxHP = 0f; // 초기 최대 체력 합계

    [SerializeField] private bool debugMode = false; // 디버그 로그 출력 여부

    // 모든 재화 오브젝트의 총 체력 대비 남은 체력 비율 (0~1, 1이면 모든 오브젝트가 풀체력)
    public float TotalHealthRatio
    {
        get
        {
            float totalCurrentHP = 0f;

            // 현재 체력만 실시간 계산
            foreach (ResourceObject resource in allResources)
            {
                if (resource != null)
                {
                    totalCurrentHP += resource.CurrentHP;
                }
            }

            // 초기 최대 체력으로 나누기 (0으로 나누기 방지)
            return initialTotalMaxHP > 0 ? totalCurrentHP / initialTotalMaxHP : 1f;
        }
    }

    // 손실도 (0~1, 1이면 모든 오브젝트가 파괴됨)
    public float DamageRatio => 1f - TotalHealthRatio;

    private void Awake()
    {
        // 씬의 모든 ResourceObject 찾기
        RefreshResourceList();

        // 초기 최대 체력 계산 및 초기 리소스 목록 저장
        CalculateInitialMaxHP();
    }

    private void Start()
    {
        // ResourceObject에 파괴 이벤트 등록
        foreach (ResourceObject resource in allResources)
        {
            if (resource != null)
            {
                resource.onDestroyed.AddListener(() => RemoveResource(resource));
            }
        }

        if (debugMode)
        {
            Debug.Log($"ResourceManager: 초기 최대 체력 합계 = {initialTotalMaxHP}");
            Debug.Log($"ResourceManager: 초기 리소스 개수 = {initialResources.Count}");
        }
    }

    // 초기 최대 체력 계산 - 씬 시작 시 한 번만 호출
    private void CalculateInitialMaxHP()
    {
        initialTotalMaxHP = 0f;
        initialResources.Clear();

        // 모든 리소스의 최대 체력 합산 및 초기 리소스 목록 저장
        foreach (ResourceObject resource in allResources)
        {
            if (resource != null)
            {
                initialTotalMaxHP += resource.MaxHP;
                initialResources.Add(resource);
            }
        }

        if (debugMode)
        {
            Debug.Log($"ResourceManager: 초기 최대 체력 계산 완료 = {initialTotalMaxHP}");
        }
    }

    public void RefreshResourceList()
    {
        allResources.Clear();
        allResources.AddRange(FindObjectsOfType<ResourceObject>());

        if (debugMode)
        {
            Debug.Log($"ResourceManager: {allResources.Count}개의 재화 오브젝트 발견");
        }
    }

    // 새 자원 추가
    public void AddResource(ResourceObject resource)
    {
        if (resource != null && !allResources.Contains(resource))
        {
            allResources.Add(resource);
            // 파괴 이벤트 등록
            resource.onDestroyed.AddListener(() => RemoveResource(resource));

            if (debugMode)
            {
                Debug.Log($"ResourceManager: 재화 오브젝트 '{resource.ResourceName}' 추가");
            }

            // 초기화 이후에 추가된 리소스는 초기 최대 체력에 영향을 주지 않음
            // 필요시 아래 코드 주석 해제하여 동적으로 초기 최대 체력 업데이트 가능
            /*
            if (!initialResources.Contains(resource))
            {
                initialResources.Add(resource);
                initialTotalMaxHP += resource.MaxHP;
                
                if (debugMode)
                {
                    Debug.Log($"ResourceManager: 초기 최대 체력 업데이트 = {initialTotalMaxHP} (+{resource.MaxHP})");
                }
            }
            */
        }
    }

    // 자원 제거
    public void RemoveResource(ResourceObject resource)
    {
        if (resource != null && allResources.Contains(resource))
        {
            allResources.Remove(resource);

            if (debugMode)
            {
                Debug.Log($"ResourceManager: 재화 오브젝트 '{resource.ResourceName}' 제거됨, 남은 개수: {allResources.Count}");
                Debug.Log($"ResourceManager: 현재 체력 비율 = {TotalHealthRatio:P2}");
            }
        }
    }

    // 초기 상태 리셋 (필요시 호출)
    public void ResetInitialState()
    {
        RefreshResourceList();
        CalculateInitialMaxHP();

        if (debugMode)
        {
            Debug.Log("ResourceManager: 초기 상태 리셋됨");
        }
    }

    // 전체 재화 오브젝트 개수 가져오기
    public int GetTotalResourceCount()
    {
        return allResources.Count;
    }

    // 초기 재화 오브젝트 개수 가져오기
    public int GetInitialResourceCount()
    {
        return initialResources.Count;
    }

    // 현재 손상된 재화 오브젝트 개수 가져오기
    public int GetDamagedResourceCount()
    {
        int count = 0;
        foreach (ResourceObject resource in allResources)
        {
            if (resource != null && resource.CurrentHP < resource.MaxHP)
            {
                count++;
            }
        }
        return count;
    }

    // 파괴된 재화 오브젝트 개수 가져오기 (리스트에서 제거되지 않은 0체력 오브젝트)
    public int GetDestroyedResourceCount()
    {
        int count = 0;
        foreach (ResourceObject resource in allResources)
        {
            if (resource != null && resource.CurrentHP <= 0)
            {
                count++;
            }
        }
        return count;
    }

    // 초기 리소스 중 파괴된 비율 (0~1)
    public float GetDestroyedRatio()
    {
        int initialCount = initialResources.Count;
        if (initialCount == 0) return 0f;

        int destroyedCount = 0;
        foreach (ResourceObject resource in initialResources)
        {
            if (resource == null || resource.CurrentHP <= 0)
            {
                destroyedCount++;
            }
        }

        return (float)destroyedCount / initialCount;
    }

    // 전체 재화 오브젝트 상태 로그 출력
    public void LogResourceStatus()
    {
        Debug.Log($"=== 재화 오브젝트 상태 ===");
        Debug.Log($"초기 최대 체력: {initialTotalMaxHP}");
        Debug.Log($"초기 개수: {initialResources.Count}");
        Debug.Log($"현재 개수: {allResources.Count}");
        Debug.Log($"총 체력 비율: {TotalHealthRatio:P2}");
        Debug.Log($"손실도: {DamageRatio:P2}");
        Debug.Log($"손상된 개수: {GetDamagedResourceCount()}");
        Debug.Log($"파괴된 개수: {GetDestroyedResourceCount()}");

        Debug.Log($"--- 현재 리소스 상태 ---");
        foreach (ResourceObject resource in allResources)
        {
            if (resource != null)
            {
                Debug.Log($"- {resource.ResourceName}: {resource.CurrentHP}/{resource.MaxHP} ({resource.HealthRatio:P2})");
            }
        }
    }
}