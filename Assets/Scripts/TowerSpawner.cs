using UnityEngine;

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

    public void SpawnTower(Vector2 hitPosition)
    {
        if (towerTemplate.weapon[0].cost > playerGold.CurrentGold)
        {
            return;
        }
        // Ŭ���� ���� ��ǥ�� �� ��ǥ�� ��ȯ
        Vector3Int cellPosition = grid.WorldToCell(hitPosition);

        // �� �߽��� ���� ��ǥ ���
        Vector3 worldPosition = grid.GetCellCenterWorld(cellPosition);

        // Y ��ǥ�� �������� Z ��ǥ ���� (Isometric z as y)
        worldPosition.z = worldPosition.y;

        // Ÿ�� ����
        GameObject clone = Instantiate(towerTemplate.towerPrefab, worldPosition, Quaternion.identity);

        playerGold.CurrentGold -= towerTemplate.weapon[0].cost;

        Debug.Log($"Tower placed at cell {cellPosition} -> world {worldPosition}");

        clone.GetComponent<TowerWeapon>().Setup(enemySpawner, playerGold);

        //Vector3 position = tileTransfrom.position + Vector3.back;
       // gameObject clone = Instantiate(towerPrefab, position, Quaternion.identity);
    }
}
