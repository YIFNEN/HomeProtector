using UnityEngine;

/// <summary>
/// 적의 이동 방향에 따라 스프라이트 방향을 전환하는 컴포넌트
/// </summary>
public class EnemyDirectionFlipper : MonoBehaviour
{
    [SerializeField] private bool flipX = true; // X축으로 뒤집기 (좌우 이동에 따라)
    [SerializeField] private bool flipY = false; // Y축으로 뒤집기 (상하 이동에 따라) - 필요시 활성화

    private SpriteRenderer spriteRenderer;
    private Vector3 lastFacingDirection = Vector3.right; // 기본 방향은 오른쪽

    private void Awake()
    {
        // 자신 또는 자식에서 SpriteRenderer 컴포넌트 찾기
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning("EnemyDirectionFlipper: SpriteRenderer를 찾을 수 없습니다!");
            enabled = false;
        }
    }

    /// <summary>
    /// 캐릭터가 바라볼 방향 설정
    /// </summary>
    /// <param name="targetPosition">바라볼 대상 위치</param>
    public void SetFacingDirection(Vector3 targetPosition)
    {
        if (spriteRenderer == null) return;

        // 현재 위치에서 대상 위치로의 방향 벡터
        Vector3 direction = targetPosition - transform.position;

        // 방향 벡터가 너무 작으면 무시
        if (direction.sqrMagnitude < 0.01f) return;

        // 현재 방향 저장
        lastFacingDirection = direction.normalized;

        // X축 뒤집기 (좌우 이동)
        if (flipX)
        {
            spriteRenderer.flipX = direction.x < 0;
        }

        // Y축 뒤집기 (상하 이동) - 필요시 사용
        if (flipY)
        {
            spriteRenderer.flipY = direction.y < 0;
        }
    }

    /// <summary>
    /// 현재 캐릭터가 바라보고 있는 방향 벡터 반환
    /// </summary>
    /// <returns>정규화된 방향 벡터</returns>
    public Vector3 GetFacingDirection()
    {
        // 스프라이트 뒤집기 상태에 따라 방향 결정
        Vector3 facingDir = lastFacingDirection;

        if (spriteRenderer != null)
        {
            if (flipX && spriteRenderer.flipX)
            {
                facingDir.x = -Mathf.Abs(facingDir.x);
            }
            else if (flipX)
            {
                facingDir.x = Mathf.Abs(facingDir.x);
            }

            if (flipY && spriteRenderer.flipY)
            {
                facingDir.y = -Mathf.Abs(facingDir.y);
            }
            else if (flipY)
            {
                facingDir.y = Mathf.Abs(facingDir.y);
            }
        }

        return facingDir.normalized;
    }

    /// <summary>
    /// 마지막으로 설정된 방향 각도 반환 (디버그용)
    /// </summary>
    public float GetFacingAngle()
    {
        return Mathf.Atan2(lastFacingDirection.y, lastFacingDirection.x) * Mathf.Rad2Deg;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 디버그 기즈모 - 현재 바라보는 방향 표시
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && enabled)
        {
            Gizmos.color = Color.blue;
            Vector3 direction = GetFacingDirection();
            Gizmos.DrawLine(transform.position, transform.position + direction * 1.5f);

            // 방향 화살표 그리기
            Vector3 arrowPos = transform.position + direction * 1.5f;
            Vector3 arrowLeft = arrowPos + Quaternion.Euler(0, 0, 135) * direction * 0.3f;
            Vector3 arrowRight = arrowPos + Quaternion.Euler(0, 0, -135) * direction * 0.3f;

            Gizmos.DrawLine(arrowPos, arrowLeft);
            Gizmos.DrawLine(arrowPos, arrowRight);
        }
    }
#endif
}