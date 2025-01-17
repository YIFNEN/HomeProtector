using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ObjectDetector : MonoBehaviour
{
    [SerializeField]
    private TowerSpawner towerSpawner;
    [SerializeField]
    private TowerDataViewer towerDataViewer;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 마우스 위치를 월드 좌표로 변환
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            // Raycast 발사
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            // Raycast 결과 처리
            if (hit.collider != null) // Raycast가 무언가를 맞췄다면
            {
                if (hit.collider.CompareTag("Tile")) // 맞춘 것이 "Tile" 태그라면
                {
                    Debug.Log($"Hit Tilemap at: {mousePosition}");
                    towerSpawner.SpawnTower(mousePosition); // 클릭한 좌표 전달
                }
                else if (hit.collider.CompareTag("Tower")) // 맞춘 것이 "Tower" 태그라면
                {
                    towerDataViewer.OnPanel(hit.transform);
                }
                else // 맞췄지만 조건에 부합하지 않는 경우
                {
                    Debug.Log("Hit something else.");
                }
            }
            else // Raycast가 아무것도 맞추지 못한 경우
            {
                Debug.Log("No hit detected");
            }
        }
    }

}
