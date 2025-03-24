using UnityEngine;
using UnityEngine.UI;

public class LevelSpriteChanger : MonoBehaviour
{
    [Header("스프라이트 설정")]
    [Tooltip("경험치가 낮을 때 표시할 스프라이트 (0~33%)")]
    [SerializeField] private Sprite lowExpSprite;

    [Tooltip("경험치가 중간일 때 표시할 스프라이트 (34~66%)")]
    [SerializeField] private Sprite mediumExpSprite;

    [Tooltip("경험치가 높을 때 표시할 스프라이트 (67~100%)")]
    [SerializeField] private Sprite highExpSprite;

    [Header("비율 기준점")]
    [Tooltip("낮음에서 중간으로 전환되는 비율 (기본값: 0.33)")]
    [Range(0.1f, 0.5f)]
    [SerializeField] private float lowToMediumThreshold = 0.33f;

    [Tooltip("중간에서 높음으로 전환되는 비율 (기본값: 0.66)")]
    [Range(0.5f, 0.9f)]
    [SerializeField] private float mediumToHighThreshold = 0.66f;

    [Header("참조")]
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private Image targetImage;

    // 상태 추적
    private enum ExpLevel { Low, Medium, High }
    private ExpLevel currentLevel = ExpLevel.Low;

    private void Awake()
    {
        // 참조 확인 및 자동 할당
        if (playerExperience == null)
        {
            playerExperience = FindObjectOfType<PlayerExperience>();
            if (playerExperience == null)
            {
                Debug.LogError("PlayerExperience 참조를 찾을 수 없습니다!");
            }
        }

        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
            if (targetImage == null)
            {
                Debug.LogError("Image 컴포넌트를 찾을 수 없습니다!");
            }
        }

        // 스프라이트 확인
        if (lowExpSprite == null || mediumExpSprite == null || highExpSprite == null)
        {
            Debug.LogWarning("하나 이상의 스프라이트가 할당되지 않았습니다!");
        }
    }

    private void Start()
    {
        // 초기 스프라이트 설정
        UpdateSprite();
    }

    private void Update()
    {
        // 경험치 비율에 따라 스프라이트 업데이트
        UpdateSprite();
    }

    // 경험치 비율에 따라 스프라이트 업데이트
    private void UpdateSprite()
    {
        if (playerExperience == null || targetImage == null) return;

        // 현재 경험치 비율 계산 (0~1)
        float expRatio = GetExpRatio();

        // 비율에 따른 새 레벨 결정
        ExpLevel newLevel = DetermineExpLevel(expRatio);

        // 레벨이 변경된 경우에만 스프라이트 변경
        if (newLevel != currentLevel)
        {
            currentLevel = newLevel;

            // 새 스프라이트 설정
            switch (currentLevel)
            {
                case ExpLevel.Low:
                    targetImage.sprite = lowExpSprite;
                    break;
                case ExpLevel.Medium:
                    targetImage.sprite = mediumExpSprite;
                    break;
                case ExpLevel.High:
                    targetImage.sprite = highExpSprite;
                    break;
            }

            // 스프라이트 변경 로그
            Debug.Log($"경험치 레벨 변경: {currentLevel}, 비율: {expRatio:P0}");
        }
    }

    // 경험치 비율 계산 (0~1)
    private float GetExpRatio()
    {
        int currentExp = playerExperience.CurrentExp;
        int requiredExp = playerExperience.ExpRequiredForCurrentLevel;

        // 최대 레벨이거나 필요 경험치가 0인 경우
        if (requiredExp <= 0)
        {
            return 1.0f;
        }

        return (float)currentExp / requiredExp;
    }

    // 비율에 따른 경험치 레벨 결정
    private ExpLevel DetermineExpLevel(float ratio)
    {
        if (ratio < lowToMediumThreshold)
        {
            return ExpLevel.Low;
        }
        else if (ratio < mediumToHighThreshold)
        {
            return ExpLevel.Medium;
        }
        else
        {
            return ExpLevel.High;
        }
    }

    // 인스펙터에서 수동 업데이트 테스트용 메서드
    public void TestLowLevel()
    {
        if (targetImage != null && lowExpSprite != null)
        {
            targetImage.sprite = lowExpSprite;
            currentLevel = ExpLevel.Low;
        }
    }

    public void TestMediumLevel()
    {
        if (targetImage != null && mediumExpSprite != null)
        {
            targetImage.sprite = mediumExpSprite;
            currentLevel = ExpLevel.Medium;
        }
    }

    public void TestHighLevel()
    {
        if (targetImage != null && highExpSprite != null)
        {
            targetImage.sprite = highExpSprite;
            currentLevel = ExpLevel.High;
        }
    }
}