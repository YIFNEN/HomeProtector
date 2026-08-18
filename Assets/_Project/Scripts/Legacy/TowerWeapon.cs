using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponState { SearchTarget = 0, AttackToTarget } //공격 대상 탐색 여부

public class TowerWeapon : MonoBehaviour
{
    [SerializeField]
    private GameObject projectilePrefab; // 단일 발사체 프리팹
    [SerializeField]
    private Transform spawnPoint;

    [Header("Time Settings")]
    [SerializeField] private bool attackEnabled = true; // 공격 가능 여부

    private TowerTemplate towerTemplate;
    private int level = 0;
    private WeaponState weaponState = WeaponState.SearchTarget;
    private Transform attackTarget = null;
    private SpriteRenderer spriteRenderer;
    private PlayerGold playerGold;
    private EnemySpawner enemySpawner;
    private TowerSpawner towerSpawner;
    private Tile ownerTile;
    private IsometricPositionHandler isometricPosition;
    private Coroutine stateRoutine;

    // 좌우반전 상태 관련 변수
    private bool isFlipped = false;
    private SpriteRenderer[] childRenderers;

    public Sprite TowerSprite => HasValidWeaponData() ? towerTemplate.weapons[level].sprite : null;
    public float Damage => HasValidWeaponData() ? towerTemplate.weapons[level].damage : 0f;
    public float Rate => HasValidWeaponData() ? towerTemplate.weapons[level].rate : 0f;
    public float Range => HasValidWeaponData() ? towerTemplate.weapons[level].range : 0f;
    public int Level => level + 1;
    public int MaxLevel => towerTemplate != null && towerTemplate.weapons != null ? towerTemplate.weapons.Count : 0;

    // 좌우반전 상태 프로퍼티
    public bool IsFlipped => isFlipped;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        childRenderers = GetComponentsInChildren<SpriteRenderer>();
        isometricPosition = GetComponent<IsometricPositionHandler>();

        // IsometricPositionHandler가 없으면 추가
        if (isometricPosition == null)
        {
            isometricPosition = gameObject.AddComponent<IsometricPositionHandler>();
        }
    }

    private void SpawnProjectile()
    {
        // 공격이 비활성화되어 있으면 발사체 생성 불가
        if (!attackEnabled) return;
        if (!HasValidWeaponData()) return;
        if (!IsTargetAttackable())
        {
            attackTarget = null;
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogError("No projectile prefab assigned to tower");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError($"TowerWeapon: {name}에 spawnPoint가 설정되지 않았습니다.");
            return;
        }

        Debug.Log($"Spawning projectile at {spawnPoint.position}");

        // 발사 위치의 z 위치 조정 (이소메트릭 뷰)
        Vector3 spawnPos = spawnPoint.position;
        spawnPos.z = spawnPos.y;

        GameObject projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // 발사체에 IsometricPositionHandler 추가 (없는 경우)
        IsometricPositionHandler projectileIsometric = projectileObj.GetComponent<IsometricPositionHandler>();
        if (projectileIsometric == null)
        {
            projectileIsometric = projectileObj.AddComponent<IsometricPositionHandler>();
        }

        // ProjectileBase 컴포넌트 가져오기
        ProjectileBase projectileScript = projectileObj.GetComponent<ProjectileBase>();

        if (projectileScript == null)
        {
            Debug.LogError($"No ProjectileBase component found on prefab: {projectilePrefab.name}");
            Destroy(projectileObj);
            return;
        }

        // 좌우반전 상태 적용
        if (isFlipped)
        {
            SpriteRenderer projRenderer = projectileObj.GetComponent<SpriteRenderer>();
            if (projRenderer != null)
            {
                projRenderer.flipX = true;
            }
            else
            {
                // 스프라이트 렌더러가 없으면 스케일로 반전
                Vector3 scale = projectileObj.transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                projectileObj.transform.localScale = scale;
            }

            // 발사 방향 반전 (필요시)
            ProjectileStraight straightProjectile = projectileObj.GetComponent<ProjectileStraight>();
            if (straightProjectile != null)
            {
                straightProjectile.SetFlipDirection(isFlipped);
            }
        }

        // 발사체 설정
        projectileScript.Setup(attackTarget, towerTemplate.weapons[level].damage);
    }

    public void Setup(TowerTemplate template, EnemySpawner enemySpawner, PlayerGold playerGold, TowerSpawner towerSpawner, Vector3 worldPosition)
    {
        towerTemplate = template;
        Debug.Log("TowerWeapon Setup called!");
        this.enemySpawner = enemySpawner;
        this.playerGold = playerGold;
        this.towerSpawner = towerSpawner;

        if (!HasValidWeaponData())
        {
            Debug.LogError($"TowerWeapon: {name}에 유효한 TowerTemplate/Weapon 데이터가 없습니다.");
            return;
        }

        // 이소메트릭 뷰에 맞게 z 위치 조정
        worldPosition.z = worldPosition.y;
        transform.position = worldPosition;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = towerTemplate.weapons[level].sprite;
        }

        ChangeState(WeaponState.SearchTarget);
    }

    public void Setup(TowerTemplate template, EnemySpawner enemySpawner, PlayerGold playerGold, Vector3 worldPosition)
    {
        Setup(template, enemySpawner, playerGold, null, worldPosition);
    }

    // 좌우반전 설정 메소드 (외부에서 호출 가능)
    public void SetFlipped(bool flipped)
    {
        isFlipped = flipped;

        // 모든 스프라이트 렌더러 반전 적용
        UpdateFlipState();
    }

    // 좌우반전 상태 토글
    public void ToggleFlip()
    {
        isFlipped = !isFlipped;
        UpdateFlipState();
    }

    // 좌우반전 상태 업데이트
    private void UpdateFlipState()
    {
        // 기본 스프라이트 렌더러가 있으면 반전
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = isFlipped;
        }

        // 모든 자식 스프라이트 렌더러도 반전
        foreach (SpriteRenderer renderer in childRenderers)
        {
            if (renderer != null && renderer != spriteRenderer) // 중복 방지
            {
                renderer.flipX = isFlipped;
            }
        }

        // 스프라이트 렌더러가 없거나 추가 반전이 필요한 경우 스케일도 조정
        if (spriteRenderer == null || !spriteRenderer.flipX)
        {
            Vector3 scale = transform.localScale;
            scale.x = isFlipped ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        // 스폰 포인트 위치 조정 (필요시)
        if (spawnPoint != null)
        {
            // 스폰 포인트가 로컬 위치에 있는 경우, x 반전이 필요할 수 있음
            // 상황에 따라 다음 코드 활성화
            /*
            Vector3 localPos = spawnPoint.localPosition;
            localPos.x = isFlipped ? -Mathf.Abs(localPos.x) : Mathf.Abs(localPos.x);
            spawnPoint.localPosition = localPos;
            */
        }
    }

    public void ChangeState(WeaponState newstate)
    {
        Debug.Log($"Changing state to {newstate}");
        StopStateRoutine();
        weaponState = newstate;

        if (!isActiveAndEnabled)
        {
            return;
        }

        switch (weaponState)
        {
            case WeaponState.SearchTarget:
                stateRoutine = StartCoroutine(SearchTarget());
                break;
            case WeaponState.AttackToTarget:
                stateRoutine = StartCoroutine(AttackToTarget());
                break;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (attackTarget != null)
        {
            // RotateToTarget(); // 기존 코드
            FlipToTarget(); // 새 코드 - 회전 대신 좌우반전
        }

        // 이소메트릭 뷰에 맞게 z 위치 조정 (매 프레임)
        Vector3 position = transform.position;
        position.z = position.y;
        transform.position = position;
    }

    // 기존 RotateToTarget 메서드를 FlipToTarget으로 대체
    private void FlipToTarget()
    {
        if (attackTarget == null) return;

        // 적의 위치와 타워의 위치를 비교하여 방향 결정
        float dx = attackTarget.position.x - transform.position.x;

        // dx가 음수면 적이 왼쪽에 있고, 양수면 오른쪽에 있음
        bool shouldFaceLeft = dx < 0;

        // 현재 타워가 왼쪽을 보고 있는지 확인 (flipX가 true면 왼쪽)
        bool isCurrentlyFacingLeft = false;

        // 스프라이트 렌더러로 확인
        if (spriteRenderer != null)
        {
            isCurrentlyFacingLeft = spriteRenderer.flipX;
        }
        else
        {
            // 스프라이트 렌더러가 없을 경우 localScale.x로 확인
            isCurrentlyFacingLeft = transform.localScale.x < 0;
        }

        // 방향이 다르면 반전
        if (shouldFaceLeft != isCurrentlyFacingLeft)
        {
            // 좌우반전 적용
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = shouldFaceLeft;
            }
            else
            {
                // 스프라이트 렌더러가 없는 경우 스케일 사용
                Vector3 scale = transform.localScale;
                scale.x = shouldFaceLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                transform.localScale = scale;
            }

            // 자식 스프라이트 렌더러도 반전
            foreach (SpriteRenderer renderer in childRenderers)
            {
                if (renderer != null && renderer != spriteRenderer)
                {
                    renderer.flipX = shouldFaceLeft;
                }
            }

            // isFlipped 변수 업데이트
            isFlipped = shouldFaceLeft;
        }

        // 회전은 수행하지 않음 - 기존 코드 제거
        // transform.rotation = Quaternion.Euler(0, 0, degree);
    }

    private IEnumerator SearchTarget()
    {
        while (true)
        {
            // 공격이 비활성화 되어 있으면 탐색만 하고 공격하지 않음
            if (!attackEnabled)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            if (!HasValidWeaponData() || enemySpawner == null)
            {
                attackTarget = null;
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            attackTarget = null;
            float closestDistSqr = Mathf.Infinity;
            float range = towerTemplate.weapons[level].range;
            float rangeSqr = range * range;
            List<Enemy> enemies = enemySpawner.EnemyList;

            for (int i = 0; i < enemies.Count; i++) //모든 적 검사
            {
                Enemy enemy = enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                float distanceSqr = (enemy.transform.position - transform.position).sqrMagnitude;
                if (distanceSqr <= rangeSqr && distanceSqr <= closestDistSqr)
                {
                    closestDistSqr = distanceSqr;
                    attackTarget = enemy.transform;
                }
            }

            if (attackTarget != null && attackEnabled)
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
            // 공격이 비활성화되어 있으면 탐색 상태로 돌아감
            if (!attackEnabled)
            {
                ChangeState(WeaponState.SearchTarget);
                break;
            }

            if (!HasValidWeaponData() || !IsTargetAttackable()) // target 있는지 확인
            {
                attackTarget = null;
                ChangeState(WeaponState.SearchTarget);
                break;
            }

            yield return new WaitForSeconds(Mathf.Max(0.05f, towerTemplate.weapons[level].rate));

            if (!attackEnabled || !IsTargetAttackable())
            {
                attackTarget = null;
                ChangeState(WeaponState.SearchTarget);
                break;
            }

            SpawnProjectile(); // 발사체 생성
        }
    }

    public bool Upgrade()
    {
        if (towerTemplate == null || towerTemplate.weapons == null ||
            level + 1 >= towerTemplate.weapons.Count ||
            playerGold == null ||
            playerGold.CurrentGold < towerTemplate.weapons[level + 1].cost)
        {
            return false;
        }
        level++;
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = towerTemplate.weapons[level].sprite;
        }
        playerGold.CurrentGold -= towerTemplate.weapons[level].cost;

        // 업그레이드 후 좌우반전 상태 유지
        if (isFlipped)
        {
            UpdateFlipState();
        }

        return true;
    }

    public void Sell()
    {
        if (playerGold != null && HasValidWeaponData())
        {
            playerGold.CurrentGold += towerTemplate.weapons[level].sell;
        }

        if (towerSpawner != null && towerSpawner.GetTilemap() != null)
        {
            Vector3Int cellposition = towerSpawner.GetTilemap().WorldToCell(transform.position);
            towerSpawner.RemoveTower(cellposition);
            return;
        }

        Debug.LogWarning("TowerWeapon: TowerSpawner 참조가 없어 타워 오브젝트만 제거합니다.");
        Destroy(gameObject);
    }

    // 공격 활성화/비활성화 메소드
    public void SetAttackEnabled(bool enabled)
    {
        attackEnabled = enabled;

        if (enabled)
        {
            // 공격 활성화시 타겟 탐색 시작
            ChangeState(WeaponState.SearchTarget);
        }
        else
        {
            attackTarget = null;
            StopStateRoutine();
        }
    }

    private bool HasValidWeaponData()
    {
        return towerTemplate != null &&
               towerTemplate.weapons != null &&
               level >= 0 &&
               level < towerTemplate.weapons.Count;
    }

    private bool IsTargetAttackable()
    {
        if (attackTarget == null || !HasValidWeaponData())
        {
            return false;
        }

        float range = towerTemplate.weapons[level].range;
        return (attackTarget.position - transform.position).sqrMagnitude <= range * range;
    }

    private void StopStateRoutine()
    {
        if (stateRoutine == null)
        {
            return;
        }

        StopCoroutine(stateRoutine);
        stateRoutine = null;
    }

    private void OnDisable()
    {
        StopStateRoutine();
    }
}
