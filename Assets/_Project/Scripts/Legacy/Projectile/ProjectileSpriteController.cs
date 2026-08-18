using UnityEngine;

public class ProjectileSpriteController : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    public Sprite[] projectileSprites; // 3개의 발사체 이미지
    private static int spriteIndex = 0; // 전역적으로 관리되는 인덱스

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 스프라이트 변경 (순차적으로)
        if (projectileSprites.Length > 0)
        {
            spriteRenderer.sprite = projectileSprites[spriteIndex];
            spriteIndex = (spriteIndex + 1) % projectileSprites.Length; // 다음 이미지로 순환
        }
        else
        {
            Debug.LogWarning("No projectile sprites assigned.");
        }
    }
}
