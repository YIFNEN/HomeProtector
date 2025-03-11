using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public enum WeaponState { SearchTarget = 0, AttackToTarget } //공격 대상 탐색 여부



public class TowerWeapon : MonoBehaviour
{
  
    [SerializeField]
    private GameObject projectilePrefab; // 발사체 프리팹
    [SerializeField]
    private Transform spawnPoint;
    [SerializeField] private ProjectileType projectileType = ProjectileType.Straight;
    [SerializeField] private GameObject[] projectilePrefabs; // Multiple projectile prefab types

    private TowerTemplate towerTemplate;
    private int level = 0;
    private WeaponState weaponState = WeaponState.SearchTarget;
    private Transform attackTarget = null;
    private SpriteRenderer spriteRenderer;
    private PlayerGold playerGold;
    private EnemySpawner enemySpawner;
    private Tile ownerTile;

    public Sprite TowerSprite => towerTemplate.weapons[level].sprite;
    public float Damage => towerTemplate.weapons[level].damage;
    public float Rate => towerTemplate.weapons[level].rate;
    public float Range => towerTemplate.weapons[level].range;
    public int Level => level + 1;
    public int MaxLevel => towerTemplate.weapons.Count;

    private void SpawnProjectile()
    {
        GameObject projectilePrefab = GetProjectilePrefabByType(projectileType);

        if (projectilePrefab == null)
        {
            Debug.LogError($"No prefab found for projectile type: {projectileType}");
            return;
        }

        Debug.Log($"Spawning projectile at {spawnPoint.position}");
        GameObject projectileObj = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);

        // Get the ProjectileBase component
        ProjectileBase projectileScript = projectileObj.GetComponent<ProjectileBase>();

        // Multiple projectile support with index-based variation
        int maxCount = 3; // Example: number of simultaneous projectiles
        for (int i = 0; i < maxCount; i++)
        {
            projectileScript.Setup(attackTarget, towerTemplate.weapons[level].damage, maxCount, i);
        }
    }

    private GameObject GetProjectilePrefabByType(ProjectileType type)
    {
        // This assumes projectilePrefabs array is ordered to match ProjectileType enum
        int index = (int)type;
        if (index >= 0 && index < projectilePrefabs.Length)
        {
            return projectilePrefabs[index];
        }
        return null;
    }

    public void Setup(TowerTemplate template, EnemySpawner enemySpawner, PlayerGold playerGold, Vector3 worldPosition)
    {
        towerTemplate = template;
        spriteRenderer = GetComponent<SpriteRenderer>();
        Debug.Log("TowerWeapon Setup called!");
        this.enemySpawner = enemySpawner;
        this.playerGold = playerGold;
        this.transform.position = worldPosition;
        spriteRenderer.sprite = towerTemplate.weapons[level].sprite;
        ChangeState(WeaponState.SearchTarget);
    }


    public void ChangeState(WeaponState newstate)
    {
        Debug.Log($"Changing state to {newstate}");
        StopCoroutine(weaponState.ToString());
        weaponState = newstate;
        StartCoroutine(weaponState.ToString());
    }
    // Update is called once per frame
    private void Update()
    {
        if (attackTarget != null)
        {
            RotateToTarget();
        }

    }

    private void RotateToTarget()
    {
        float dx = attackTarget.position.x - transform.position.x;
        float dy = attackTarget.position.y - transform.position.y;
        float degree = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, degree);
    }

    private IEnumerator SearchTarget()
    {
        while (true)
        {
            float closetDistSqr = Mathf.Infinity;
            for (int i = 0; i < enemySpawner.EnemyList.Count; i++) //모든 적 검사
            {
                float distance = Vector3.Distance(enemySpawner.EnemyList[i].transform.position, transform.position);
                if (distance <= towerTemplate.weapons[level].range && distance <= closetDistSqr)
                {
                    closetDistSqr = distance;
                    attackTarget = enemySpawner.EnemyList[i].transform;
                }
            }
            if (attackTarget != null)
            {
                Debug.Log($"Target found: {attackTarget.name}");
                ChangeState(WeaponState.AttackToTarget); // 해당 타겟 공격
            }

            yield return null;
        }
    }

    private IEnumerator AttackToTarget()
    {
        while (true)
        {
            if (attackTarget == null) // target 있는지 확인
            {
                ChangeState(WeaponState.SearchTarget);
                break;
            }

            float distance = Vector3.Distance(attackTarget.position, transform.position);
            if (distance > towerTemplate.weapons[level].range) //target이 공격 범위보다 멀 경우 새로운 적 탐색
            {
                attackTarget = null;
                ChangeState(WeaponState.SearchTarget);
                break;
            }

            yield return new WaitForSeconds(towerTemplate.weapons[level].rate);

            SpawnProjectile(); // 발사체 생성
        }
    }

    

    public bool Upgrade()
    {
        if (level + 1 >= towerTemplate.weapons.Count || playerGold.CurrentGold < towerTemplate.weapons[level + 1].cost)
        { 
            return false; 

        }
        level++;
        spriteRenderer.sprite = towerTemplate.weapons[level].sprite;
        playerGold.CurrentGold -= towerTemplate.weapons[level].cost;

        return true;
    }

    public void Sell()
    {
        playerGold.CurrentGold += towerTemplate.weapons[level].sell;
        
        Vector3Int cellposition = FindObjectOfType<Grid>().WorldToCell(transform.position);
        FindObjectOfType<TowerSpawner>().RemoveTower(cellposition);

        Destroy(gameObject);
    }
}

