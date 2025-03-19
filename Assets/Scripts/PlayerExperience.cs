using UnityEngine;

public class PlayerExperience : MonoBehaviour
{
    [SerializeField] private int currentExp = 0; // 현재 경험치
    [SerializeField] private int[] expRequiredForLevel = { 0, 100, 250, 450, 700, 1000 }; // 각 레벨에 필요한 경험치 (0번 인덱스는 쓰지 않음)
    [SerializeField] private float[] damageMultipliers = { 1.0f, 1.2f, 1.5f, 1.8f, 2.2f, 2.7f }; // 각 레벨별 공격력 배수 (0번 인덱스는 쓰지 않음)

    private PlayerGold playerGold; // 플레이어 골드 참조
    private int level = 1; // 현재 레벨
    private const int MAX_LEVEL = 6; // 최대 레벨

    // 레벨 프로퍼티
    public int Level => level;

    // 최대 레벨 프로퍼티
    public int MaxLevel => MAX_LEVEL;

    // 현재 경험치 프로퍼티
    public int CurrentExp => currentExp;

    // 현재 레벨에서 필요한 경험치 프로퍼티
    public int ExpRequiredForCurrentLevel => level < MAX_LEVEL ? expRequiredForLevel[level] : 0;

    // 현재 공격력 배수 프로퍼티
    public float CurrentDamageMultiplier => damageMultipliers[level];

    private void Awake()
    {
        playerGold = GetComponent<PlayerGold>();
        if (playerGold == null)
        {
            Debug.LogError("PlayerGold 컴포넌트를 찾을 수 없습니다.");
        }
    }

    // 경험치 획득 메소드
    public void AddExperience(int expAmount)
    {
        // 이미 최대 레벨이면 경험치를 더하지 않음
        if (level >= MAX_LEVEL) return;

        currentExp += expAmount;
        Debug.Log($"경험치 획득: {expAmount}, 총 경험치: {currentExp}");

        // 레벨업 체크
        CheckLevelUp();
    }

    // 몬스터 처치 시 경험치 획득
    public void AddExperienceForEnemy(EnemyDestroyType destroyType, int gold)
    {
        // 몬스터가 목적지에 도달한 경우 경험치 획득 X
        if (destroyType == EnemyDestroyType.Arrive) return;

        // 골드에 비례하여 경험치 부여 (예: 골드의 2배)
        int expAmount = gold * 2;
        AddExperience(expAmount);
    }

    // 웨이브 종료 후 경험치 정산
    public void AddExperienceForWaveCompletion(int enemiesKilled)
    {
        // 처치한 적의 수에 비례하여 보너스 경험치 부여
        int expAmount = enemiesKilled * 5; // 예: 적 1마리당 추가 5 경험치
        AddExperience(expAmount);

        Debug.Log($"웨이브 완료 보너스 경험치: {expAmount} (처치한 적: {enemiesKilled}마리)");
    }

    // 레벨업 체크 메소드
    private void CheckLevelUp()
    {
        while (level < MAX_LEVEL && currentExp >= expRequiredForLevel[level])
        {
            level++;
            Debug.Log($"레벨 업! 현재 레벨: {level}, 공격력 배수: {CurrentDamageMultiplier}");

            // 레벨업 보상 (예: 약간의 골드 지급)
            if (playerGold != null)
            {
                playerGold.CurrentGold += level * 10;
            }

            // 레벨업 이펙트나 사운드 재생 (필요시 추가)
            // PlayLevelUpEffect();
        }
    }

    // 공격력 계산 메소드 - 화살에서 호출
    public float CalculateAttackDamage(float baseDamage)
    {
        return baseDamage * CurrentDamageMultiplier;
    }
}