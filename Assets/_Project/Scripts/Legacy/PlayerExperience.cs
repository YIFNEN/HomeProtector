using UnityEngine;
using UnityEngine.Events;

public class PlayerExperience : MonoBehaviour
{
    [SerializeField] private int currentExp = 0; // 현재 경험치
    [SerializeField] private int[] expRequiredForLevel = { 0, 100, 250, 450, 700, 1000 }; // 각 레벨에 필요한 경험치 (0번 인덱스는 쓰지 않음)
    [SerializeField] private float[] damageMultipliers = { 1.0f, 1.2f, 1.5f, 1.8f, 2.2f, 2.7f }; // 각 레벨별 공격력 배수 (0번 인덱스는 쓰지 않음)

    private PlayerGold playerGold; // 플레이어 골드 참조
    private int level = 1; // 현재 레벨
    private int previousLevel = 1; // 이전 레벨 추적
    private const int MAX_LEVEL = 6; // 최대 레벨

    // 레벨업 이벤트
    [System.Serializable]
    public class LevelUpEvent : UnityEvent<int> { }
    public LevelUpEvent onLevelUp = new LevelUpEvent();

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
        // 추가 초기화
        previousLevel = level;
    }

    private void Update()
    {
        // 레벨이 변경되었는지 확인
        if (level > previousLevel)
        {
            // 레벨업 감지
            int levelsGained = level - previousLevel;
            HandleLevelUp(levelsGained);
            previousLevel = level;
        }
    }

    // 경험치 획득 메소드
    public void AddExperience(int expAmount)
    {
        // 이미 최대 레벨이면 경험치를 더하지 않음
        if (level >= MAX_LEVEL) return;

        // 현재 레벨 저장
        int oldLevel = level;

        // 경험치 추가
        currentExp += expAmount;
        Debug.Log($"경험치 획득: {expAmount}, 총 경험치: {currentExp}");

        // 레벨업 체크
        CheckLevelUp();

        // 레벨업 감지 (여러 레벨 동시에 오를 경우 대비)
        if (level > oldLevel)
        {
            int levelsGained = level - oldLevel;
            HandleLevelUp(levelsGained);
        }
    }

    // 몬스터 처치 시 경험치 획득
    public void AddExperienceForEnemy(EnemyDestroyType destroyType, int expValue)
    {
        // 몬스터가 목적지에 도달한 경우 경험치 획득 X
        if (destroyType == EnemyDestroyType.Arrive) return;

        // 지정된 경험치 값 사용
        int expAmount = expValue;
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
        }
    }

    // 레벨업 처리
    private void HandleLevelUp(int levelsGained)
    {
        Debug.Log($"레벨 업! 현재 레벨: {level} (+{levelsGained})");

        // 레벨업 이벤트 발생
        onLevelUp.Invoke(level);

        // 레벨업 알림 표시 (UI)
        ShowLevelUpNotification();
    }

    // 공격력 계산 메소드 - 화살에서 호출
    public float CalculateAttackDamage(float baseDamage)
    {
        return baseDamage * CurrentDamageMultiplier;
    }

    // 레벨업 알림 표시
    private void ShowLevelUpNotification()
    {
        // 레벨업 효과 (예: 파티클, 사운드 등)
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            // 레벨업 사운드 재생 (있는 경우)
            AudioClip levelUpSound = Resources.Load<AudioClip>("Sounds/LevelUp");
            if (levelUpSound != null)
            {
                audioSource.PlayOneShot(levelUpSound);
            }
        }

        // 레벨업 UI 알림 - UI 매니저가 있다면 메시지 전달
        TimeBasedUIManager uiManager = FindObjectOfType<TimeBasedUIManager>();
        if (uiManager != null)
        {
            uiManager.ShowLevelUpNotification(level);
        }
    }
}
