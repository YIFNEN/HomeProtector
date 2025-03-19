using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ResourceObject : MonoBehaviour
{
    [SerializeField] private float maxHP = 100f; // 최대 체력
    [SerializeField] private string resourceName = "Resource"; // 재화 오브젝트 이름
    [SerializeField] private GameObject destroyEffect; // 파괴 시 이펙트
    [SerializeField] private bool isInvincible = false; // 무적 여부 (선택적)

    [Header("Isometric Settings")]
    [SerializeField] private bool updateZPosition = true; // 이소메트릭 Z 위치 업데이트 여부

    [Header("Events")]
    public UnityEvent onDamaged; // 데미지 받을 때 이벤트
    public UnityEvent onDestroyed; // 파괴될 때 이벤트

    private float currentHP; // 현재 체력
    private SpriteRenderer spriteRenderer; // 스프라이트 렌더러
    private PlayerGold playerGold; // 플레이어 골드 참조
    private IsometricPositionHandler isometricPosition; // 이소메트릭 위치 핸들러

    // 최대 체력 프로퍼티
    public float MaxHP => maxHP;

    // 현재 체력 프로퍼티
    public float CurrentHP => currentHP;

    // 체력 비율 프로퍼티 (0~1)
    public float HealthRatio => currentHP / maxHP;

    // 이름 프로퍼티
    public string ResourceName => resourceName;

    // Awake: 초기화
    private void Awake()
    {
        currentHP = maxHP;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 플레이어 골드 및 HP 참조 찾기
        playerGold = FindObjectOfType<PlayerGold>();


        // 이소메트릭 위치 핸들러 가져오거나 생성
        isometricPosition = GetComponent<IsometricPositionHandler>();
        if (isometricPosition == null && updateZPosition)
        {
            isometricPosition = gameObject.AddComponent<IsometricPositionHandler>();
        }
    }

    // Start: 추가 초기화
    private void Start()
    {
        // 상태 표시 UI 초기화 등 필요시 여기에 추가
        if (updateZPosition && isometricPosition == null)
        {
            // Z 위치 수동 조정
            UpdateZPosition();
        }
    }

    // Update: Z 위치 업데이트
    private void Update()
    {
        if (updateZPosition && isometricPosition == null)
        {
            // Z 위치 수동 조정
            UpdateZPosition();
        }
    }

    // Z 위치 수동 업데이트 (이소메트릭 뷰)
    private void UpdateZPosition()
    {
        Vector3 position = transform.position;
        position.z = position.y;
        transform.position = position;
    }

    // 데미지 처리 메소드
    public void TakeDamage(float damage)
    {
        // 무적 상태면 데미지 무시
        if (isInvincible) return;

        // 데미지 적용
        currentHP = Mathf.Max(0, currentHP - damage);

        // 데미지 받았을 때 이벤트 발생
        onDamaged?.Invoke();

        // 데미지 효과 표시
        StartCoroutine(DamageEffect());

        Debug.Log($"{resourceName}이(가) {damage}의 데미지를 받음. 남은 체력: {currentHP}/{maxHP}");

        // 체력이 0이 되면 파괴
        if (currentHP <= 0)
        {
            DestroyResource();
        }
    }

    // 데미지 효과 코루틴 (깜빡임 효과)
    private IEnumerator DamageEffect()
    {
        if (spriteRenderer == null) yield break;

        // 원래 색상 저장
        Color originalColor = spriteRenderer.color;

        // 빨간색으로 변경
        spriteRenderer.color = Color.red;

        // 잠시 대기
        yield return new WaitForSeconds(0.1f);

        // 원래 색상으로 복구
        spriteRenderer.color = originalColor;
    }

    // 재화 오브젝트 파괴 메소드
    private void DestroyResource()
    {
        // 파괴 이벤트 발생
        onDestroyed?.Invoke();

        // 파괴 이펙트 생성
        if (destroyEffect != null)
        {
            // 이펙트 생성 시 Z 위치 조정
            Vector3 effectPosition = transform.position;
            GameObject effect = Instantiate(destroyEffect, effectPosition, Quaternion.identity);

            // 이펙트에 이소메트릭 위치 핸들러 추가
            if (effect.GetComponent<IsometricPositionHandler>() == null)
            {
                effect.AddComponent<IsometricPositionHandler>();
            }
        }

        // 오브젝트 파괴
        Destroy(gameObject);
    }

    // 치료 메소드 (필요시)
    public void Heal(float amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        Debug.Log($"{resourceName}이(가) {amount}만큼 회복됨. 현재 체력: {currentHP}/{maxHP}");
    }

    // 체력 설정 메소드
    public void SetHealth(float health)
    {
        currentHP = Mathf.Clamp(health, 0, maxHP);

        // 체력이 0이면 파괴
        if (currentHP <= 0)
        {
            DestroyResource();
        }
    }

    // 최대 체력 설정 메소드
    public void SetMaxHealth(float newMaxHP)
    {
        float ratio = currentHP / maxHP; // 현재 체력 비율 유지
        maxHP = newMaxHP;
        currentHP = maxHP * ratio;
    }

    // 위치 설정 메소드 (이소메트릭 Z 자동 조정)
    public void SetPosition(Vector3 newPosition)
    {
        if (isometricPosition != null)
        {
            isometricPosition.SetPosition(newPosition);
        }
        else
        {
            newPosition.z = newPosition.y;
            transform.position = newPosition;
        }
    }
}