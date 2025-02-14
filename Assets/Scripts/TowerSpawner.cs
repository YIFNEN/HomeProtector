using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Collections;
public class TowerSpawner : MonoBehaviour
{
    [SerializeField]
    private List<TowerTemplate> towerTemplates;// 여러 종류의 타워 템플릿

    [SerializeField]
    private EnemySpawner enemySpawner; // 현재 맵에 존재하는 적 리스트 정보

    [SerializeField]
    private Grid grid; // 타일맵이 속한 Grid 컴포넌트

    [SerializeField]
    private PlayerGold playerGold;

    [SerializeField]
    private SystemTextViewer systemTextViewer;


    [SerializeField]
    private Tilemap tilemap;


    // 배치된 타워들을 월드 좌표(셀 좌표) 기준으로 관리
    private Dictionary<Vector3Int, GameObject> placedTowers = new Dictionary<Vector3Int, GameObject>();

    // 현재 선택된 타워 종류 인덱스 (타워 템플릿 리스트 내의 인덱스)
    private int selectedTowerIndex = 0;

    // 타워 배치 모드 활성화 여부
    private bool isOnTowerButton = false;
    private GameObject followTowerClone = null;

    public Tilemap GetTilemap() => tilemap;


    // 선택한 타워 종류를 설정하고 배치 모드에 들어감
    public void SelectAndReadyTower(int index)
    {
        if (index < 0 || index >= towerTemplates.Count)
        {
            Debug.LogError("잘못된 타워 인덱스");
            return;
        }

        selectedTowerIndex = index;

        // 배치 모드 진입 전에 골드 체크
        if (towerTemplates[selectedTowerIndex].weapons[0].cost > playerGold.CurrentGold)
        {
            systemTextViewer.PrintText(SystemType.Money);
            return;
        }
        isOnTowerButton = true;
        // 선택한 타워의 followTowerPrefab을 생성하여 미리 배치 미리보기 역할
        followTowerClone = Instantiate(towerTemplates[selectedTowerIndex].followTowerPrefab);
        // 필요하다면 followTowerClone의 위치 및 기타 세팅을 여기서 진행
        StartCoroutine(OnTowerCancelSystem());
    }

    public void ReadyToSpawnTower()
    {
        if (isOnTowerButton)
        {
            return;
        }
        TowerTemplate selectedTower = towerTemplates[selectedTowerIndex];

        if (selectedTower.weapons[0].cost > playerGold.CurrentGold)
        {
            systemTextViewer.PrintText(SystemType.Money);
            return;
        }

        isOnTowerButton = true;
        followTowerClone = Instantiate(selectedTower.followTowerPrefab);// 임시 타워 생성
        StartCoroutine("OnTowerCancelSystem");
    }

    void Update()
    {
        if (isOnTowerButton && followTowerClone != null)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            followTowerClone.transform.position = new Vector3(mousePosition.x, mousePosition.y, 0);
        }
    }

    public void SpawnTower(Vector3Int cellPosition)
    {
        if (isOnTowerButton == false)
        {
            return;
        }
        if (IsTileOccupied(cellPosition)) return; // 이미 타워가 존재하면 실행 X

        TowerTemplate selectedTower = towerTemplates[selectedTowerIndex];

        if (selectedTower.weapons[0].cost > playerGold.CurrentGold)
        {
            systemTextViewer.PrintText(SystemType.Money);
            return;
        }
        
        Vector3 towerPosition = tilemap.GetCellCenterWorld(cellPosition); // 타일 중심의 월드 좌표
        // Isometric z as y
       // towerPosition.z = towerPosition.y;

        GameObject newTower = Instantiate(selectedTower.towerPrefab, towerPosition, Quaternion.identity); // 타워 생성
        TowerWeapon towerWeapon = newTower.GetComponent<TowerWeapon>();
        
        if (towerWeapon != null)
        {
            towerWeapon.Setup(selectedTower, enemySpawner, playerGold, towerPosition); // Setup 호출
        }
        isOnTowerButton = false;
        placedTowers[cellPosition] = newTower; // 셀 좌표와 타워 연결
        playerGold.CurrentGold -= selectedTower.weapons[0].cost; // 골드 감소
        
        StopCoroutine("OnTowerCancelSystem");
        Destroy(followTowerClone);
       

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

