using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextTMPViewer : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI textPlayerHP;
    [SerializeField]
    private ResourceManager HealthRatio;
    [SerializeField]
    private TextMeshProUGUI textPlayerGold;
    [SerializeField]
    private TextMeshProUGUI textWave;
    [SerializeField]
    private TextMeshProUGUI textEnemyCount;
    [SerializeField]
    private PlayerGold playerGold;
    [SerializeField]
    private WaveSystem waveSystem;
    [SerializeField]
    private EnemySpawner enemySpawner;


   
    // Update is called once per frame
    void Update()
    {
        // 체력 비율을 퍼센트로 표시 (예: "HP: 75%")
        textPlayerHP.text = "HP: " + (HealthRatio.TotalHealthRatio * 100).ToString("0") + "%";
        textPlayerGold.text = "Gold" + playerGold.CurrentGold.ToString();
        textWave.text = "Wave" + waveSystem.CurrentWave + "/" +waveSystem.MaxWave;
        textEnemyCount.text = "Count" + enemySpawner.CurrentEnemyCount + "/";// + enemyGroup.count;
    }
}
