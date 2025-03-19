using UnityEngine;

// 이소메트릭 뷰에서 z 위치를 y 위치와 일치시켜주는 컴포넌트
public class IsometricPositionHandler : MonoBehaviour
{
    [SerializeField] private bool updateContinuously = true; // 매 프레임마다 업데이트 여부
    [SerializeField] private bool updateOnStart = true; // 시작 시 업데이트 여부
    [SerializeField] private bool includeChildren = false; // 자식 오브젝트도 업데이트 여부

    private void Start()
    {
        if (updateOnStart)
        {
            UpdatePosition();
        }
    }

    private void Update()
    {
        if (updateContinuously)
        {
            UpdatePosition();
        }
    }

    // 현재 오브젝트의 위치 업데이트
    public void UpdatePosition()
    {
        Vector3 position = transform.position;
        position.z = position.y;
        transform.position = position;

        // 자식 오브젝트도 업데이트
        if (includeChildren)
        {
            foreach (Transform child in transform)
            {
                Vector3 childPos = child.position;
                childPos.z = childPos.y;
                child.position = childPos;
            }
        }
    }

    // 특정 위치로 오브젝트 이동 (이소메트릭 z 자동 설정)
    public void SetPosition(Vector3 newPosition)
    {
        newPosition.z = newPosition.y;
        transform.position = newPosition;
    }

    // 특정 위치로 오브젝트 이동 (이소메트릭 z 자동 설정) - Vector2 버전
    public void SetPosition(Vector2 newPosition)
    {
        transform.position = new Vector3(newPosition.x, newPosition.y, newPosition.y);
    }
}