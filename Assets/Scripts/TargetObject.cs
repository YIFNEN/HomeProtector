using UnityEngine;

public class TargetObject : MonoBehaviour
{
    [SerializeField]
    private string targetTag; // 태그 설정

    public string TargetTag => targetTag;

    private void Awake()
    {
        gameObject.tag = targetTag; // 태그 자동 설정
    }
}
