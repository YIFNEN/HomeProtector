using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement2D : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 1.0f;
    [SerializeField]
    private Vector3 moveDirection = Vector3.zero;

    private float originalMoveSpeed; // 원래 이동 속도 저장용
    private bool isSlowed = false;   // 현재 감속 상태인지
    private float slowTimer = 0f;    // 감속 지속 시간 타이머
    private float currentSlowAmount = 0f; // 현재 적용된 감속 비율

    public float MoveSpeed => moveSpeed;

    private void Awake()
    {
        // 초기 이동 속도 저장
        originalMoveSpeed = moveSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // 감속 효과가 적용 중이라면 타이머 업데이트
        if (isSlowed)
        {
            slowTimer -= Time.deltaTime;

            // 타이머가 끝나면 이동 속도 복구
            if (slowTimer <= 0)
            {
                ResetMoveSpeed();
            }
        }
    }

    public void MoveTo(Vector3 direction)
    {
        moveDirection = direction;
    }

    // 이동 속도 감소 효과 적용
    public void ApplySlow(float slowAmount, float duration)
    {
        // 현재 적용된 감속보다 더 강한 감속이거나, 감속 효과가 곧 끝날 경우에만 적용
        if (slowAmount > currentSlowAmount || slowTimer < 0.5f)
        {
            // 감속 효과가 처음 적용되면 원래 속도 저장
            if (!isSlowed)
            {
                originalMoveSpeed = moveSpeed;
            }

            // 새로운 감속 효과 적용
            currentSlowAmount = slowAmount;
            moveSpeed = originalMoveSpeed * (1 - slowAmount);
            slowTimer = duration;
            isSlowed = true;
        }
    }

    // 이동 속도 원래대로 복구
    public void ResetMoveSpeed()
    {
        moveSpeed = originalMoveSpeed;
        isSlowed = false;
        currentSlowAmount = 0f;
    }
}