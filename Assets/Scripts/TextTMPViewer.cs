using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextTMPViewer : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI textPlayerHP;
    [SerializeField]
    private GoodsBoxHP playerHP;
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
        textPlayerHP.text = "Hp"+playerHP.CurrentHP + "/" + playerHP.MaxHP;
        textPlayerGold.text = "Gold" + playerGold.CurrentGold.ToString();
        textWave.text = "Wave" + waveSystem.CurrentWave + "/" +waveSystem.MaxWave;
        textEnemyCount.text = "Count" + enemySpawner.CurrentEnemyCount + "/";// + enemyGroup.count;
    }
}
