using UnityEngine;

public class SliderPositionAutoSetter : MonoBehaviour
{
   
    [SerializeField]
    private Vector3 distance = Vector3.up * 20.0f;
    private Transform targetTransform;
    private RectTransform rectTransform; // Slider의 RectTransform
    public void Setup(Transform target)
    {
        targetTransform = target; // 대상 설정
        rectTransform = GetComponent<RectTransform>(); // RectTransform 가져오기

    }

    private void LateUpdate()
    {
        if (targetTransform == null )
        {
            Destroy(gameObject);
            return;
        }

        // 적의 World Position을 Canvas의 UI Position으로 변환
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(targetTransform.position);

        // UI Canvas 공간으로 변환된 좌표를 RectTransform에 반영
        rectTransform.position = screenPosition + distance;
    }
}
