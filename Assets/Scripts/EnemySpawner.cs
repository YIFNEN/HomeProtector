using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
   // [SerializeField]
   // private GameObject enemyPrefab;
    [SerializeField]
    private GameObject enemyHPSliderPrefab;
    [SerializeField]
    private Transform canvasTransform;
   // [SerializeField]
   // private float spawnTime;
    [SerializeField]
    private Transform[] wayPoints;
    [SerializeField]
    private GoodsBoxHP playerHP;
    [SerializeField]
    private PlayerGold playerGold;
    private Wave currentWave;
    private int currentEnemyCount;
    private List<Enemy> enemyList;

    public List<Enemy> EnemyList => enemyList;
    public int CurrentEnemyCount => currentEnemyCount;
    public int MaxEnemyCount => currentWave.maxEnemyCount;


    private void Awake()
    {
        enemyList = new List<Enemy>();
        //StartCoroutine("SpawnEnemy");


    }

    private IEnumerator SpawnEnemy()
    {
        int spawnEnemyCount = 0;
        //while (true)
        while (spawnEnemyCount < currentWave.maxEnemyCount){
            
           /* GameObject clone = Instantiate(enemyPrefab);
            Enemy enemy = clone.GetComponent<Enemy>();

            enemy.Setup(this, wayPoints);
            enemyList.Add(enemy);
            Debug.Log($"Enemy registered: {enemy.name}");
            SpawnEnemyHPSlider(clone);

            yield return new WaitForSeconds(spawnTime);*/

            int enemyIndex = Random.Range(0, currentWave.enemyPrefabs.Length);
            GameObject clone = Instantiate(currentWave.enemyPrefabs[enemyIndex]);
            Enemy enemy = clone.GetComponent<Enemy>();
            enemy.Setup(this, wayPoints);
            enemyList.Add(enemy);
            SpawnEnemyHPSlider(clone);
            spawnEnemyCount++;
            yield return new WaitForSeconds(currentWave.spawnTime);
        }
    }
    // Update is called once per frame
    void Update()
    {

    }

    public void DestroyEnemy(EnemyDestroyType type, Enemy enemy, int gold)
    {

        if (type == EnemyDestroyType.Arrive)
        {
            playerHP.TakeDamage(1);
        }

        else if (type == EnemyDestroyType.Kill)
        {
            playerGold.CurrentGold += gold;
        }

        currentEnemyCount--;
        enemyList.Remove(enemy);
        Debug.Log($"Enemy unregistered: {enemy.name}");
        Destroy(enemy.gameObject);
    }

    private void SpawnEnemyHPSlider(GameObject enemy)
    {
        GameObject sliderclone = Instantiate(enemyHPSliderPrefab);
        sliderclone.transform.SetParent(canvasTransform, false);
        sliderclone.transform.localScale = Vector3.one;
        sliderclone.GetComponent<SliderPositionAutoSetter>().Setup(enemy.transform);
        sliderclone.GetComponent<EnemyHPViewer>().Setup(enemy.GetComponent<EnemyHP>());
    }
    public void StartWave(Wave wave) 
    {
        currentWave = wave;
        currentEnemyCount = currentWave.maxEnemyCount;
        StartCoroutine("SpawnEnemy");
    }

}