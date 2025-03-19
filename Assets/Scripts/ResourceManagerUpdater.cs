using UnityEngine;

// ResourceManagerUpdater: ResourceObject의 생성 및 변경사항을 ResourceManager에 알리는 편의 클래스
public class ResourceManagerUpdater : MonoBehaviour
{
    private ResourceManager resourceManager;
    private ResourceObject resourceObject;

    private void Awake()
    {
        resourceObject = GetComponent<ResourceObject>();

        // ResourceManager 찾기
        resourceManager = FindObjectOfType<ResourceManager>();

        if (resourceManager == null)
        {
            Debug.LogWarning("ResourceManager를 찾을 수 없습니다. ResourceObject가 자동으로 등록되지 않을 수 있습니다.");
        }
    }

    private void Start()
    {
        // ResourceManager에 이 ResourceObject 등록
        if (resourceManager != null && resourceObject != null)
        {
            resourceManager.AddResource(resourceObject);
        }
    }

    private void OnDestroy()
    {
        // ResourceManager에서 이 ResourceObject 제거
        if (resourceManager != null && resourceObject != null)
        {
            resourceManager.RemoveResource(resourceObject);
        }
    }
}