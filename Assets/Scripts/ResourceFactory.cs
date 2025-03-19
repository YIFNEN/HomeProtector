// ResourceFactory: 프리팹을 이용한 ResourceObject 생성을 담당하는 팩토리 클래스
using UnityEngine;

public class ResourceFactory : MonoBehaviour
{
    [SerializeField]
    private GameObject[] resourcePrefabs; // 다양한 종류의 재화 오브젝트 프리팹

    // 특정 유형의 ResourceObject 생성
    public GameObject CreateResource(int typeIndex, Vector3 position, string customName = null)
    {
        if (resourcePrefabs == null || resourcePrefabs.Length == 0)
        {
            Debug.LogError("리소스 프리팹이 설정되지 않았습니다!");
            return null;
        }

        if (typeIndex < 0 || typeIndex >= resourcePrefabs.Length)
        {
            Debug.LogError($"유효하지 않은 리소스 인덱스: {typeIndex}. 0~{resourcePrefabs.Length - 1} 범위여야 합니다.");
            return null;
        }

        // 프리팹으로부터 ResourceObject 생성
        GameObject resourceObj = Instantiate(resourcePrefabs[typeIndex], position, Quaternion.identity);

        if (!string.IsNullOrEmpty(customName))
        {
            resourceObj.name = customName;
        }

        // 이소메트릭 위치 처리 추가
        Vector3 isometricPos = resourceObj.transform.position;
        isometricPos.z = isometricPos.y;
        resourceObj.transform.position = isometricPos;

        // 생성된 오브젝트에 드래그 기능 추가
        if (resourceObj.GetComponent<DraggableResource>() == null)
        {
            resourceObj.AddComponent<DraggableResource>();
        }

        // ResourceManager 자동 업데이트 컴포넌트 추가
        if (resourceObj.GetComponent<ResourceManagerUpdater>() == null)
        {
            resourceObj.AddComponent<ResourceManagerUpdater>();
        }

        return resourceObj;
    }

    // 랜덤한 종류의 ResourceObject 생성
    public GameObject CreateRandomResource(Vector3 position, string namePrefix = "Resource")
    {
        int randomIndex = Random.Range(0, resourcePrefabs.Length);
        return CreateResource(randomIndex, position, $"{namePrefix}_{randomIndex}");
    }
}
