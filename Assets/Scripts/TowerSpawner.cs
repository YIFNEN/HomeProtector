using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Collections;
public class TowerSpawner : MonoBehaviour
{
    [SerializeField]
    private TowerTemplate towerTemplate;

    [SerializeField]
    private EnemySpawner enemySpawner; // ���� �ʿ� �����ϴ� �� ����Ʈ ����

    [SerializeField]
    private Grid grid; // Ÿ�ϸ��� ���� Grid ������Ʈ

    [SerializeField]
    private PlayerGold playerGold;
    [SerializeField]
    private SystemTextViewer systemTextViewer;
    private bool isOnTowerButton = false;// Ÿ�� �Ǽ� ��ư �Է� üũ
    private GameObject followTowerClone = null;
    [SerializeField]
    private Tilemap tilemap;

    private Dictionary<Vector3Int, GameObject> placedTowers = new Dictionary<Vector3Int, GameObject>(); // Ÿ�� ����

    public Tilemap GetTilemap() => tilemap;

    public void ReadyToSpawnTower()
    {
        if (isOnTowerButton)
        {
            return;
        }

        if (towerTemplate.weapon[0].cost > playerGold.CurrentGold)
        {
            systemTextViewer.PrintText(SystemType.Money);
            return;
        }

        isOnTowerButton = true;
        followTowerClone = Instantiate(towerTemplate.followTowerPrefab);// �ӽ� Ÿ�� ����
        StartCoroutine("OnTowerCancelSystem");
    }

    void Update()
    {
        /*if (Input.GetMouseButtonDown(0)) // ���콺 Ŭ�� (��ġ�� ����)
        {
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition); // Ŭ�� ��ġ
            Vector3Int cellPosition = tilemap.WorldToCell(worldPosition); // �� ��ǥ ��ȯ

            if (tilemap.HasTile(cellPosition)) // ������ ���� Ÿ���� �ִ��� Ȯ��
            {
                if (placedTowers.ContainsKey(cellPosition)) // �̹� Ÿ���� ��ġ�� ���
                {
                    Debug.Log("Tower already placed. Removing it...");
                }
                else // Ÿ���� ���� ���
                {
                    SpawnTower(cellPosition); // Ÿ�� ��ġ                  

                }
            }
            else
            {
                Debug.Log("No tile at selected position.");
            }
        }*/
    }

    public void SpawnTower(Vector3Int cellPosition)
    {
        if (isOnTowerButton == false)
        {
            return;
        }
        if (IsTileOccupied(cellPosition)) return; // �̹� Ÿ���� �����ϸ� ���� X

        if (towerTemplate.weapon[0].cost > playerGold.CurrentGold)
        {
            systemTextViewer.PrintText(SystemType.Money);
            return;
        }
        Vector3 towerPosition = tilemap.GetCellCenterWorld(cellPosition); // Ÿ�� �߽��� ���� ��ǥ
        GameObject newTower = Instantiate(towerTemplate.towerPrefab, towerPosition, Quaternion.identity); // Ÿ�� ����
        TowerWeapon towerWeapon = newTower.GetComponent<TowerWeapon>();
        if (towerWeapon != null)
        {
            towerWeapon.Setup(enemySpawner, playerGold, towerPosition); // Setup ȣ��
        }
        isOnTowerButton = false;
        placedTowers[cellPosition] = newTower; // �� ��ǥ�� Ÿ�� ����
        playerGold.CurrentGold -= towerTemplate.weapon[0].cost; // ��� ����

        Destroy(followTowerClone);
        StopCoroutine("OnTowerCancelSystem");

        Debug.Log($"Tower placed at {cellPosition}");
    }

    public void RemoveTower(Vector3Int cellPosition)
    {
        if (placedTowers.TryGetValue(cellPosition, out GameObject tower)) // Ÿ�� ã��
        {
            Destroy(tower); // Ÿ�� ������Ʈ ����
            placedTowers.Remove(cellPosition); // ���� ��Ͽ��� ����

            Debug.Log($"Tower removed from {cellPosition}");
        }
        else
        {
            Debug.Log("No tower found at the specified position.");
        }
    }

    private IEnumerator OnTowerCancelSystem()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                isOnTowerButton = false;
                Destroy(followTowerClone);
                break;
            }
            yield return null; 
        }
    }

    public bool IsTileOccupied(Vector3Int cellPosition)
    {
        return placedTowers.ContainsKey(cellPosition);
    }



}

