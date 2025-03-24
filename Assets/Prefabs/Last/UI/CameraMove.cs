using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public Vector3 startPosition;  // 카메라 시작 위치
    public Vector3 targetPosition; // 카메라 도착 위치
    public float moveSpeed = 2.0f;
    private bool isMoving = true;

    void Start()
    {
        // 씬이 로드될 때 startPosition에서 시작
        transform.position = startPosition;
    }

    void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                isMoving = false;
            }
        }
    }
}
