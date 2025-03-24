using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    private List<ResourceObject> allResources = new List<ResourceObject>();
    private List<ResourceObject> initialResources = new List<ResourceObject>(); // 초기 리소스 목록 저장
    private float initialTotalMaxHP = 0f; // 초기 최대 체력 합계

    // 파괴된 리소스 정보를 저장하는 클래스
    [System.Serializable]
    private class DestroyedResourceData
    {
        public GameObject prefab; // 리소스 프리팹
        public Vector3 position; // 위치
        public Quaternion rotation; // 회전
        public string resourceName; // 리소스 이름
        public float maxHP; // 최대 체력
        public string objectName; // 오브젝트 이름 (프리팹 이름에 활용)

        public DestroyedResourceData(GameObject prefab, Vector3 position, Quaternion rotation, string resourceName, float maxHP, string objectName)
        {
            this.prefab = prefab;
            this.position = position;
            this.rotation = rotation;
            this.resourceName = resourceName;
            this.maxHP = maxHP;
            this.objectName = objectName;
        }
    }

    // 파괴된 리소스 데이터 목록
    private List<DestroyedResourceData> destroyedResources = new List<DestroyedResourceData>();

    [SerializeField] private bool debugMode = false; // 디버그 로그 출력 여부
    [SerializeField] private List<GameObject> resourcePrefabs; // 리소스 오브젝트 프리팹 목록
    [SerializeField] private GameObject defaultResourcePrefab; // 기본 리소스 프리팹 (복구 실패 시 사용)

    // 리소스 초기 저장 데이터 (초기화 단계에서 설정)
    private Dictionary<string, GameObject> initialResourceData = new Dictionary<string, GameObject>();

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

        // 초기 리소스 데이터 복사 (복구용)
        CacheInitialResourceData();
    }

    private void Start()
    {
        // ResourceObject에 파괴 이벤트 등록
        RegisterResourceEvents();

        // TimeSystem 이벤트 구독
        TimeSystem timeSystem = FindObjectOfType<TimeSystem>();
        if (timeSystem != null)
        {
            timeSystem.onMorningStart.AddListener(OnMorningStart);

            if (debugMode)
            {
                Debug.Log("ResourceManager: TimeSystem의 onMorningStart 이벤트에 구독됨");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("ResourceManager: TimeSystem을 찾을 수 없습니다.");
        }

        if (debugMode)
        {
            Debug.Log($"ResourceManager: 초기 최대 체력 합계 = {initialTotalMaxHP}");
            Debug.Log($"ResourceManager: 초기 리소스 개수 = {initialResources.Count}");
            Debug.Log($"ResourceManager: 캐시된 리소스 프리팹 개수 = {initialResourceData.Count}");
        }
    }

    private void OnDestroy()
    {
        // TimeSystem 이벤트 구독 해제
        TimeSystem timeSystem = FindObjectOfType<TimeSystem>();
        if (timeSystem != null)
        {
            timeSystem.onMorningStart.RemoveListener(OnMorningStart);
        }
    }

    // 초기 리소스 데이터 캐싱 (복구용)
    private void CacheInitialResourceData()
    {
        initialResourceData.Clear();

        // 씬에 있는 모든 리소스 오브젝트의 프리팹 정보 저장
        foreach (ResourceObject resource in initialResources)
        {
            if (resource != null)
            {
                string objectName = resource.gameObject.name.Replace("(Clone)", "").Trim();

                // 이미 존재하지 않는 경우에만 추가
                if (!initialResourceData.ContainsKey(resource.ResourceName))
                {
                    // 먼저 프리팹 목록에서 찾기
                    GameObject prefab = FindPrefabByName(objectName);

                    // 찾지 못했다면 리소스 오브젝트 자체를 템플릿으로 저장
                    if (prefab == null)
                    {
                        if (debugMode)
                        {
                            Debug.LogWarning($"ResourceManager: '{objectName}' 프리팹을 찾을 수 없어 오브젝트 자체를 템플릿으로 사용합니다.");
                        }
                        prefab = resource.gameObject;
                    }

                    initialResourceData.Add(resource.ResourceName, prefab);

                    if (debugMode)
                    {
                        Debug.Log($"ResourceManager: '{resource.ResourceName}' 리소스 데이터 캐싱됨, 프리팹: {objectName}");
                    }
                }
            }
        }
    }

    // 리소스 이벤트 등록
    private void RegisterResourceEvents()
    {
        foreach (ResourceObject resource in allResources)
        {
            if (resource != null)
            {
                // 기존 리스너 제거 후 다시 등록 (중복 방지)
                resource.onDestroyed.RemoveListener(() => OnResourceDestroyed(resource));
                resource.onDestroyed.AddListener(() => OnResourceDestroyed(resource));
            }
        }
    }

    // 리소스 파괴 이벤트 핸들러
    private void OnResourceDestroyed(ResourceObject resource)
    {
        if (resource == null) return;

        // 파괴된 리소스 정보 저장
        string objectName = resource.gameObject.name.Replace("(Clone)", "").Trim();
        GameObject prefab = FindPrefabByName(objectName);

        // 프리팹을 찾지 못했다면 초기 데이터에서 찾기
        if (prefab == null && initialResourceData.ContainsKey(resource.ResourceName))
        {
            prefab = initialResourceData[resource.ResourceName];

            if (debugMode)
            {
                Debug.Log($"ResourceManager: '{objectName}' 프리팹을 찾지 못했지만 캐시된 데이터에서 찾음: {resource.ResourceName}");
            }
        }

        // 정보 저장
        DestroyedResourceData data = new DestroyedResourceData(
            prefab,
            resource.transform.position,
            resource.transform.rotation,
            resource.ResourceName,
            resource.MaxHP,
            objectName
        );

        destroyedResources.Add(data);

        if (debugMode)
        {
            Debug.Log($"ResourceManager: 리소스 '{resource.ResourceName}' 파괴 정보 저장됨 (복구 대기 리소스: {destroyedResources.Count}개)");
        }

        // 리스트에서 리소스 제거
        RemoveResource(resource);
    }

    // 아침 시작 시 호출되는 메소드
    private void OnMorningStart()
    {
        if (debugMode)
        {
            Debug.Log("ResourceManager: 아침 시작, 파괴된 리소스 복구 시작");
        }

        StartCoroutine(RestoreDestroyedResources());
    }

    // 파괴된 리소스 복구 코루틴
    private IEnumerator RestoreDestroyedResources()
    {
        // 복구할 리소스가 없으면 종료
        if (destroyedResources.Count == 0)
        {
            if (debugMode)
            {
                Debug.Log("ResourceManager: 복구할 리소스가 없습니다.");
            }
            yield break;
        }

        if (debugMode)
        {
            Debug.Log($"ResourceManager: {destroyedResources.Count}개의 리소스 복구 시작");
        }

        // 약간의 지연 후 복구 시작 (다른 시스템이 준비될 시간)
        yield return new WaitForSeconds(0.5f);

        // 모든 파괴된 리소스 복구
        List<DestroyedResourceData> resourcesToRestore = new List<DestroyedResourceData>(destroyedResources);
        int successCount = 0;

        foreach (DestroyedResourceData data in resourcesToRestore)
        {
            GameObject newObject = null;

            // 프리팹으로 복구 시도
            if (data.prefab != null)
            {
                newObject = Instantiate(data.prefab, data.position, data.rotation);
                successCount++;

                if (debugMode)
                {
                    Debug.Log($"ResourceManager: 리소스 '{data.resourceName}' 원래 프리팹으로 복구됨");
                }
            }
            // 프리팹을 찾지 못했다면 리소스 프리팹 배열에서 이름으로 다시 찾기
            else
            {
                GameObject matchingPrefab = null;

                // 리소스 이름으로 프리팹 찾기
                foreach (GameObject prefab in resourcePrefabs)
                {
                    if (prefab != null)
                    {
                        ResourceObject resourceObj = prefab.GetComponent<ResourceObject>();
                        if (resourceObj != null && resourceObj.ResourceName == data.resourceName)
                        {
                            matchingPrefab = prefab;
                            break;
                        }
                    }
                }

                // 찾은 프리팹으로 복구
                if (matchingPrefab != null)
                {
                    newObject = Instantiate(matchingPrefab, data.position, data.rotation);
                    successCount++;

                    if (debugMode)
                    {
                        Debug.Log($"ResourceManager: 리소스 '{data.resourceName}' 이름 일치 프리팹으로 복구됨");
                    }
                }
                // 기본 프리팹으로 복구 시도
                else if (defaultResourcePrefab != null)
                {
                    newObject = Instantiate(defaultResourcePrefab, data.position, data.rotation);

                    // 기본 정보 설정
                    ResourceObject resourceObj = newObject.GetComponent<ResourceObject>();
                    if (resourceObj != null)
                    {
                        // 프로퍼티가 있으면 설정
                        System.Type type = typeof(ResourceObject);
                        System.Reflection.PropertyInfo propName = type.GetProperty("ResourceName");
                        if (propName != null && propName.CanWrite)
                        {
                            propName.SetValue(resourceObj, data.resourceName);
                        }

                        // 체력 설정 메소드 호출
                        resourceObj.SetMaxHealth(data.maxHP);
                    }

                    successCount++;

                    if (debugMode)
                    {
                        Debug.Log($"ResourceManager: 리소스 '{data.resourceName}' 기본 프리팹으로 복구됨");
                    }
                }
                else
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"ResourceManager: 리소스 '{data.resourceName}' 복구 실패 (프리팹 없음, 기본 프리팹도 없음)");
                    }
                }
            }

            // 생성된 오브젝트가 있으면 리소스 등록
            if (newObject != null)
            {
                ResourceObject resourceComponent = newObject.GetComponent<ResourceObject>();
                if (resourceComponent != null)
                {
                    AddResource(resourceComponent);
                }
            }

            // 각 오브젝트 생성 사이에 약간의 지연
            yield return new WaitForSeconds(0.1f);
        }

        // 복구된 리소스 목록 비우기
        destroyedResources.Clear();

        if (debugMode)
        {
            Debug.Log($"ResourceManager: 리소스 복구 완료 ({successCount}/{resourcesToRestore.Count} 성공)");
            LogResourceStatus();
        }
    }

    // 리소스 프리팹 찾기 (이름 기준)
    private GameObject FindPrefabByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        foreach (GameObject prefab in resourcePrefabs)
        {
            if (prefab != null && prefab.name == name)
            {
                return prefab;
            }
        }

        return null;
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
            resource.onDestroyed.RemoveListener(() => OnResourceDestroyed(resource));
            resource.onDestroyed.AddListener(() => OnResourceDestroyed(resource));

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
        CacheInitialResourceData();

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

    // 복구 대기 중인 파괴된 리소스 개수 가져오기
    public int GetPendingRestoreCount()
    {
        return destroyedResources.Count;
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
        Debug.Log($"복구 대기 중인 개수: {destroyedResources.Count}");
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