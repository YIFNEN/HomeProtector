using UnityEngine;
using UnityEngine.UI;

public class TowerSelectionUI : MonoBehaviour
{
    [SerializeField]
    private TowerSpawner towerSpawner;
    [SerializeField]
    private Button[] towerButtons; // 각 버튼은 타워 종류를 대표

    void Start()
    {
        // 버튼의 개수와 towerTemplates의 개수가 일치한다고 가정
        for (int i = 0; i < towerButtons.Length; i++)
        {
            int index = i; // 지역변수 캡처
            towerButtons[i].onClick.AddListener(() => OnTowerButtonClicked(index));
        }
    }

    private void OnTowerButtonClicked(int index)
    {
        // 선택한 타워 종류를 설정하고 배치 모드에 진입
        towerSpawner.SelectAndReadyTower(index);
    }
}
