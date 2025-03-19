using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    private List<ResourceObject> allResources = new List<ResourceObject>();

    // 모든 재화 오브젝트의 총 체력 대비 남은 체력 비율 (0~1, 1이면 모든 오브젝트가 풀체력)
    public float TotalHealthRatio
    {
        get
        {
            float totalMaxHP = 0f;
            float totalCurrentHP = 0f;

            foreach (ResourceObject resource in allResources)
            {
                if (resource != null)
                {
                    totalMaxHP += resource.MaxHP;
                    totalCurrentHP += resource.CurrentHP;
                }
            }

            return totalMaxHP > 0 ? totalCurrentHP / totalMaxHP : 1f;
        }
    }

    // 손실도 (0~1, 1이면 모든 오브젝트가 파괴됨)
    public float DamageRatio => 1f - TotalHealthRatio;

    private void Awake()
    {
        // 씬의 모든 ResourceObject 찾기
        RefreshResourceList();
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
    }

    public void RefreshResourceList()
    {
        allResources.Clear();
        allResources.AddRange(FindObjectsOfType<ResourceObject>());
        Debug.Log($"ResourceManager: {allResources.Count}개의 재화 오브젝트 발견");
    }

    // 새 자원 추가
    public void AddResource(ResourceObject resource)
    {
        if (resource != null && !allResources.Contains(resource))
        {
            allResources.Add(resource);
            // 파괴 이벤트 등록
            resource.onDestroyed.AddListener(() => RemoveResource(resource));
            Debug.Log($"ResourceManager: 재화 오브젝트 '{resource.ResourceName}' 추가");
        }
    }

    // 자원 제거
    public void RemoveResource(ResourceObject resource)
    {
        if (resource != null && allResources.Contains(resource))
        {
            allResources.Remove(resource);
            Debug.Log($"ResourceManager: 재화 오브젝트 제거됨, 남은 개수: {allResources.Count}");
        }
    }

    // 전체 재화 오브젝트 개수 가져오기
    public int GetTotalResourceCount()
    {
        return allResources.Count;
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

    // 전체 재화 오브젝트 상태 로그 출력
    public void LogResourceStatus()
    {
        Debug.Log($"=== 재화 오브젝트 상태 ===");
        Debug.Log($"총 개수: {allResources.Count}");
        Debug.Log($"총 체력 비율: {TotalHealthRatio:P2}");
        Debug.Log($"손실도: {DamageRatio:P2}");
        Debug.Log($"손상된 개수: {GetDamagedResourceCount()}");
        Debug.Log($"파괴된 개수: {GetDestroyedResourceCount()}");

        foreach (ResourceObject resource in allResources)
        {
            if (resource != null)
            {
                Debug.Log($"- {resource.ResourceName}: {resource.CurrentHP}/{resource.MaxHP} ({resource.HealthRatio:P2})");
            }
        }
    }
}