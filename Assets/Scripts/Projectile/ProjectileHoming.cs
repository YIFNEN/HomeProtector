using UnityEngine;
using System.Collections;

public class ProjectileHoming : ProjectileBase
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float initialMoveSpeed = 7f; // 초기 속도(빠름)
    [SerializeField] private float finalMoveSpeed = 4f;   // 최종 속도(느림)
    [SerializeField] private float speedTransitionDuration = 1.5f; // 속도 변화 시간

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 100f;  // 회전 속도 감소
    [SerializeField] private float maxRotationAngle = 45f; // 최대 회전 각도 제한

    [Header("Leaf-like Movement")]
    [SerializeField] private float oscillationSpeed = 8f; // 좌우 흔들림 속도
    [SerializeField] private float oscillationAmount = 0.4f; // 좌우 흔들림 정도
    [SerializeField] private float fallSpeed = 1.5f;       // 낙하 속도
    [SerializeField] private bool randomizeMovement = true; // 랜덤한 움직임 여부

    [Header("Isometric Settings")]
    [SerializeField] private bool useIsometricMovement = true; // 이소메트릭 이동 사용 여부

    // 내부 변수
    private float lifeTime = 0f;
    private float initialDistance;
    private Vector3 initialDirection;
    private Vector3 targetPosition;
    private float randOffset;
    private float currentOscillationAmount;
    private bool hasReachedTarget = false;
    private float shootAngle;

    private void Start()
    {
        if (target != null)
        {
            // 초기 방향 및 거리 저장
            initialDirection = (target.position - transform.position).normalized;
            initialDistance = Vector3.Distance(transform.position, target.position);
            targetPosition = target.position;

            // 발사 각도 계산
            shootAngle = Mathf.Atan2(initialDirection.y, initialDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, shootAngle);

            // 약간의 랜덤성 추가
            if (randomizeMovement)
            {
                randOffset = Random.Range(-1f, 1f);
                oscillationAmount *= Random.Range(0.8f, 1.2f);
                currentOscillationAmount = oscillationAmount;
            }
            else
            {
                randOffset = 0;
                currentOscillationAmount = oscillationAmount;
            }

            // 타겟과의 거리에 따른 속도 조절
            moveSpeed = initialMoveSpeed;

            // 최대 5초 후에 자동 소멸
            Destroy(gameObject, 5f);
        }
        else
        {
            // 타겟이 없으면 앞으로 날아가다 소멸
            Destroy(gameObject, 2f);
        }
    }

    public override void Process()
    {
        // 타겟이 사라졌으면 마지막 위치로 날아감
        if (target == null)
        {
            MoveTowardPosition(targetPosition);
            return;
        }

        // 타겟 위치 업데이트
        targetPosition = target.position;

        // 생존 시간 증가
        lifeTime += Time.deltaTime;

        // 낙엽 같은 움직임 구현
        MoveWithLeaflikeMotion();

        // 타겟과의 거리 확인
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        // 타겟에 매우 근접하면 속도 증가해서 직행
        if (distanceToTarget < 0.5f && !hasReachedTarget)
        {
            hasReachedTarget = true;
            moveSpeed = initialMoveSpeed * 1.5f;
            currentOscillationAmount = 0f;
        }
    }

    private void MoveWithLeaflikeMotion()
    {
        // 시간에 따른 속도 감소 (초기엔 빠르다가 점점 느려짐)
        float speedFactor = Mathf.Lerp(1f, finalMoveSpeed / initialMoveSpeed,
                                      Mathf.Clamp01(lifeTime / speedTransitionDuration));
        moveSpeed = initialMoveSpeed * speedFactor;

        // 목표 방향 계산
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

        // 회전 각도 제한을 위한 현재 방향
        Vector3 currentDirection = transform.right;

        // 제한된 회전 각도로 방향 조정
        Vector3 limitedDirection = LimitRotation(currentDirection, directionToTarget, maxRotationAngle);

        // 흔들림 효과 적용 (사인파 사용)
        float oscillation = Mathf.Sin((lifeTime + randOffset) * oscillationSpeed) * currentOscillationAmount;

        // 흔들림 방향 (현재 진행 방향의 수직 방향)
        Vector3 oscillationDirection = new Vector3(-limitedDirection.y, limitedDirection.x, 0);

        // 약간의 하강 효과
        Vector3 fallDirection = new Vector3(0, -fallSpeed, 0);

        // 최종 이동 방향
        Vector3 moveDirection = limitedDirection + oscillationDirection * oscillation + fallDirection * Time.deltaTime;

        // 이소메트릭 타일맵 지원
        if (useIsometricMovement)
        {
            // 이소메트릭 변환 (Z 위치를 Y 위치와 동일하게 설정)
            Vector3 newPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;
            newPosition.z = newPosition.y;
            transform.position = newPosition;
        }
        else
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }

        // 투사체 회전 (진행 방향으로)
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                                                    rotationSpeed * Time.deltaTime);
    }

    private Vector3 LimitRotation(Vector3 current, Vector3 target, float maxAngle)
    {
        // 현재 방향과 목표 방향 사이의 각도 계산
        float angle = Vector3.Angle(current, target);

        // 각도가 제한 내에 있으면 목표 방향 반환
        if (angle <= maxAngle)
        {
            return target;
        }

        // 각도가 제한을 초과하면 최대 각도로 회전한 방향 반환
        return Vector3.RotateTowards(current, target, maxAngle * Mathf.Deg2Rad, 0f).normalized;
    }

    private void MoveTowardPosition(Vector3 position)
    {
        // 특정 위치로 이동 (타겟이 사라졌을 때 사용)
        Vector3 direction = (position - transform.position).normalized;

        // 흔들림 효과와 하강 적용
        float oscillation = Mathf.Sin((lifeTime + randOffset) * oscillationSpeed) * currentOscillationAmount;
        Vector3 oscillationDirection = new Vector3(-direction.y, direction.x, 0);
        Vector3 fallDirection = new Vector3(0, -fallSpeed, 0);

        Vector3 moveDirection = direction + oscillationDirection * oscillation + fallDirection * Time.deltaTime;

        // 이소메트릭 지원
        if (useIsometricMovement)
        {
            Vector3 newPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;
            newPosition.z = newPosition.y;
            transform.position = newPosition;
        }
        else
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }

        // 투사체 회전
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                                                    rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // 히트 이펙트 생성
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            // 데미지 적용
            EnemyHP enemyHP = collision.GetComponent<EnemyHP>();
            if (enemyHP != null)
            {
                enemyHP.TakeDamage(damage);
            }

            // 투사체 제거
            Destroy(gameObject);
        }
    }
}