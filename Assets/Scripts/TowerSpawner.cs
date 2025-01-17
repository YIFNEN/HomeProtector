using UnityEngine;

public class TowerSpawner : MonoBehaviour
{
    [SerializeField]
    private TowerTemplate towerTemplate;

    [SerializeField]
    private EnemySpawner enemySpawner; // 현재 맵에 존재하는 적 리스트 정보

    [SerializeField]
    private Grid grid; // 타일맵이 속한 Grid 컴포넌트

    [SerializeField]
    private PlayerGold playerGold;

    public void SpawnTower(Vector2 hitPosition)
    {
        if (towerTemplate.weapon[0].cost > playerGold.CurrentGold)
        {
            return;
        }
        // 클릭한 월드 좌표를 셀 좌표로 변환
        Vector3Int cellPosition = grid.WorldToCell(hitPosition);

        // 셀 중심의 월드 좌표 계산
        Vector3 worldPosition = grid.GetCellCenterWorld(cellPosition);

        // Y 좌표를 기준으로 Z 좌표 설정 (Isometric z as y)
        worldPosition.z = worldPosition.y;

        // 타워 생성
        GameObject clone = Instantiate(towerTemplate.towerPrefab, worldPosition, Quaternion.identity);

        playerGold.CurrentGold -= towerTemplate.weapon[0].cost;

        Debug.Log($"Tower placed at cell {cellPosition} -> world {worldPosition}");

        clone.GetComponent<TowerWeapon>().Setup(enemySpawner, playerGold);

        //Vector3 position = tileTransfrom.position + Vector3.back;
       // gameObject clone = Instantiate(towerPrefab, position, Quaternion.identity);
    }
}
