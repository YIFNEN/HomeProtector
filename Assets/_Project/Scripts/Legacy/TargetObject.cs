using UnityEngine;

/// <summary>
/// 타겟 객체 컴포넌트 - 적의 표적이 될 수 있는 모든 게임 오브젝트에 추가
/// </summary>
[DisallowMultipleComponent]
public class TargetObject : MonoBehaviour
{
    [SerializeField, Tooltip("이 오브젝트의 타겟 태그 (비워두면 현재 오브젝트 태그 사용)")]
    private string targetTag = ""; // 기본값을 빈 문자열로 변경

    [SerializeField, Tooltip("TargetManager에 자동 등록 여부")]
    private bool autoRegister = true; // 자동 등록 여부

    // 타겟 태그 프로퍼티 - 설정된 targetTag가 없으면 현재 오브젝트 태그 반환
    public string TargetTag => string.IsNullOrEmpty(targetTag) ? gameObject.tag : targetTag;

    private void Awake()
    {
        // targetTag가 비어있지 않고 현재 태그와 다른 경우만 태그 설정
        if (!string.IsNullOrEmpty(targetTag) && gameObject.tag != targetTag)
        {
            gameObject.tag = targetTag;
        }
    }

    private void OnEnable()
    {
        // 활성화 시 자동 등록
        if (autoRegister && TargetManager.Instance != null)
        {
            TargetManager.Instance.RegisterTarget(TargetTag, transform);
        }
    }

    private void OnDisable()
    {
        // 비활성화 시 자동 해제
        if (autoRegister && TargetManager.Instance != null)
        {
            TargetManager.Instance.UnregisterTarget(TargetTag, transform);
        }
    }
}