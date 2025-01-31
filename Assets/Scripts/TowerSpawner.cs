using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Collections;
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
    [SerializeField]
    private SystemTextViewer systemTextViewer;
    private bool isOnTowerButton = false;// 타워 건설 버튼 입력 체크
    private GameObject followTowerClone = null;
    [SerializeField]
    private Tilemap tilemap;

    private Dictionary<Vector3Int, GameObject> placedTowers = new Dictionary<Vector3Int, GameObject>(); // 타워 관리

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
        followTowerClone = Instantiate(towerTemplate.followTowerPrefab);// 임시 타워 생성
        StartCoroutine("OnTowerCancelSystem");
    }

    void Update()
    {
        /*if (Input.GetMouseButtonDown(0)) // 마우스 클릭 (터치도 가능)
        {
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition); // 클릭 위치
            Vector3Int cellPosition = tilemap.WorldToCell(worldPosition); // 셀 좌표 변환

            if (tilemap.HasTile(cellPosition)) // 선택한 셀에 타일이 있는지 확인
            {
                if (placedTowers.ContainsKey(cellPosition)) // 이미 타워가 배치된 경우
                {
                    Debug.Log("Tower already placed. Removing it...");
                }
                else // 타워가 없는 경우
                {
                    SpawnTower(cellPosition); // 타워 배치                  

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
        if (IsTileOccupied(cellPosition)) return; // 이미 타워가 존재하면 실행 X

        if (towerTemplate.weapon[0].cost > playerGold.CurrentGold)
        {
            systemTextViewer.PrintText(SystemType.Money);
            return;
        }
        Vector3 towerPosition = tilemap.GetCellCenterWorld(cellPosition); // 타일 중심의 월드 좌표
        GameObject newTower = Instantiate(towerTemplate.towerPrefab, towerPosition, Quaternion.identity); // 타워 생성
        TowerWeapon towerWeapon = newTower.GetComponent<TowerWeapon>();
        if (towerWeapon != null)
        {
            towerWeapon.Setup(enemySpawner, playerGold, towerPosition); // Setup 호출
        }
        isOnTowerButton = false;
        placedTowers[cellPosition] = newTower; // 셀 좌표와 타워 연결
        playerGold.CurrentGold -= towerTemplate.weapon[0].cost; // 골드 감소

        Destroy(followTowerClone);
        StopCoroutine("OnTowerCancelSystem");

        Debug.Log($"Tower placed at {cellPosition}");
    }

    public void RemoveTower(Vector3Int cellPosition)
    {
        if (placedTowers.TryGetValue(cellPosition, out GameObject tower)) // 타워 찾기
        {
            Destroy(tower); // 타워 오브젝트 제거
            placedTowers.Remove(cellPosition); // 관리 목록에서 제거

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

