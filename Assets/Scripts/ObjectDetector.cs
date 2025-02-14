using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
public class ObjectDetector : MonoBehaviour
{
    [SerializeField]
    private TowerSpawner towerSpawner;
    [SerializeField]
    private TowerDataViewer towerDataViewer;

    private Camera mainCamera;
   // private Transform hitTransform = null;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject() == true)
        {
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            // 마우스 위치를 월드 좌표로 변환
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            worldPosition.z = 0f; // 2D 환경에서 Z 문제 방지
            // Raycast 발사
            RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);


            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Tower")) // 타워를 클릭한 경우
                {
                    Debug.Log("Tower clicked! Opening panel...");
                    towerDataViewer.OnPanel(hit.transform);
                    return; // 타워 클릭 시 타일맵 관련 로직 실행 X
                }
            }

            // Raycast 결과 처리
            Vector3Int cellPosition = towerSpawner.GetTilemap().WorldToCell(worldPosition);

            if (towerSpawner.IsTileOccupied(cellPosition))
            {
                // 타워가 이미 존재하는 경우 -> 제거
                Debug.Log($"Removing tower at {cellPosition}");

            }
            else
            {
                // 타워가 없는 경우 -> 생성
                Debug.Log($"Spawning tower at {cellPosition}");
                towerSpawner.SpawnTower(cellPosition);
            }
        }
       
    }

}
