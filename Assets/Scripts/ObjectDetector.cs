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
            // ���콺 ��ġ�� ���� ��ǥ�� ��ȯ
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            // Raycast �߻�
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            // Raycast ��� ó��
            if (hit.collider != null) // Raycast�� ���𰡸� ����ٸ�
            {
                if (hit.collider.CompareTag("Tile")) // ���� ���� "Tile" �±׶��
                {
                    Debug.Log($"Hit Tilemap at: {mousePosition}");
                    towerSpawner.SpawnTower(mousePosition); // Ŭ���� ��ǥ ����
                }
                else if (hit.collider.CompareTag("Tower")) // ���� ���� "Tower" �±׶��
                {
                    towerDataViewer.OnPanel(hit.transform);
                }
                else // �������� ���ǿ� �������� �ʴ� ���
                {
                    Debug.Log("Hit something else.");
                }
            }
            else // Raycast�� �ƹ��͵� ������ ���� ���
            {
                Debug.Log("No hit detected");
            }
        }
    }

}
