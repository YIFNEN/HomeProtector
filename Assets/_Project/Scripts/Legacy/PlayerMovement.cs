using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Transform ItenPoint; // 아이템 생성 위치
    public Transform ShotPoint; // 화살이 발사될 위치
    public GameObject ItemPrefab; // 아이템 프리팹
    public GameObject ThrowPrefab; // 던지기 오브젝트 프리팹
    public GameObject BowPrefab; // 활 프리팹
    public GameObject ArrowPrefab; // 화살 프리팹
    public float arrowSpeed = 10f; // 화살 속도
    public float arrowCooldown = 0.5f; // 화살 발사 쿨다운 (초 단위)
    private bool useBowPrefab = false; // BowPrefab 사용 여부 설정
    private bool canShootArrow = true; // 화살 발사 가능 여부

    [SerializeField] private float moveSpeed = 5f; // 이동 속도
    [SerializeField] private bool attackEnabled = true; // 공격 가능 여부

    private Rigidbody2D rb; // 리지드바디 컴포넌트
    private Animator animator; // 애니메이터 컴포넌트
    private Vector2 lastDirection = new Vector2(1, 0); // 기본적으로 오른쪽을 바라봄
    private PlayerExperience playerExperience; // 플레이어 경험치 시스템 참조
    // TimeSystem 참조 추가
    private TimeSystem timeSystem;
    private bool isEveningBowTriggered = false; // 밤 모드에서 Bow 애니메이션 실행 여부 추적
    // Start: 컴포넌트 초기화
    void Start()
    {
        Application.targetFrameRate = 60;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerExperience = GetComponent<PlayerExperience>();

        if (playerExperience == null)
        {
            playerExperience = FindObjectOfType<PlayerExperience>();
        }

        // TimeSystem 찾기
        timeSystem = FindObjectOfType<TimeSystem>();

        // TimeSystem 이벤트 구독
        if (timeSystem != null)
        {
            timeSystem.onEveningStart.AddListener(OnEveningStart);
            timeSystem.onMorningStart.AddListener(OnMorningStart);
        }
    }

    // Update: 매 프레임마다 호출
    void Update()
    {
        // 이동 입력 처리 (WASD 또는 화살표 키)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 움직임이 있을 때만 방향 업데이트
        if (horizontal != 0 || vertical != 0)
        {
            // 입력 방향 정규화
            Vector2 moveDirection = new Vector2(horizontal, vertical).normalized;
            lastDirection = moveDirection;

            // 애니메이터 파라미터 업데이트
            animator.SetFloat("x", horizontal);
            animator.SetFloat("y", vertical);
            animator.SetBool("Walk", true);
        }
        else
        {
            animator.SetBool("Walk", false);
        }

        // 액션 처리 - 공격 활성화 여부에 따라 다른 메서드 호출
        if (attackEnabled)
        {
            HandleActions();
        }
        else
        {
            // 공격이 비활성화된 경우 제한된 액션만 처리
            HandleActionsWithAttackToggle();
        }
        if (timeSystem != null && timeSystem.CurrentTime == TimeOfDay.Evening && !isEveningBowTriggered)
        {
            animator.SetTrigger("Bow");
            isEveningBowTriggered = true;
            Debug.Log("밤 모드 감지 - Bow 애니메이션 트리거됨");
        }

        // 이동 처리 (Isometric 뷰에 맞게 조정)
        MovePlayer(horizontal, vertical);
    }

    // 플레이어 이동 처리 메소드
    private void MovePlayer(float horizontal, float vertical)
    {
        // Isometric 이동 (z as y에 맞게 조정)
        Vector3 movement = new Vector3(horizontal, vertical, 0f).normalized * moveSpeed * Time.deltaTime;

        // 이동 적용
        transform.Translate(movement);

        // Isometric view에서 z 위치 조정 (y와 동일하게)
        Vector3 position = transform.position;
        position.z = position.y;
        transform.position = position;
    }

    // 액션 처리 메소드
    private void HandleActions()
    {
        // 공격 액션
        if (Input.GetKeyDown(KeyCode.Z))
        {
            animator.SetTrigger("Slash");
        }

        // 방어 액션
        if (Input.GetKeyDown(KeyCode.V))
        {
            animator.SetTrigger("Guard");
        }

        // 아이템 사용 액션
        if (Input.GetKeyDown(KeyCode.B))
        {
            animator.SetTrigger("Item");
            Instantiate(ItemPrefab, ItenPoint.position, transform.rotation);
        }

        // 데미지 받기 액션 (테스트용)
        if (Input.GetKeyDown(KeyCode.N))
        {
            animator.SetTrigger("Damage");
        }

        // 사망 및 부활 액션 (테스트용)
        if (Input.GetKeyDown(KeyCode.M))
        {
            StartCoroutine(DeathAndReviveCoroutine());
        }

        // 던지기 액션
        if (Input.GetKeyDown(KeyCode.X))
        {
            StartCoroutine(ThrowCoroutine());
        }

        // 화살 발사 액션
        if (Input.GetKeyDown(KeyCode.C) && canShootArrow)
        {
            StartCoroutine(ShootArrowCoroutine());
        }
    }

    // 공격이 비활성화된 경우의 액션 처리 메소드
    private void HandleActionsWithAttackToggle()
    {
        // 비공격 액션만 처리
        // 데미지 받기 액션 (테스트용)
        if (Input.GetKeyDown(KeyCode.N))
        {
            animator.SetTrigger("Damage");
        }
        // 사망 및 부활 액션 (테스트용)
        if (Input.GetKeyDown(KeyCode.M))
        {
            StartCoroutine(DeathAndReviveCoroutine());
        }
    }

    // 사망 및 부활 코루틴
    private IEnumerator DeathAndReviveCoroutine()
    {
        animator.SetTrigger("Dead");
        transform.position = new Vector2(0f, -0.12f);

        // 64프레임 대기
        for (var i = 0; i < 64; i++)
        {
            yield return null;
        }

        transform.position = Vector2.zero; // 원점으로 위치 초기화
    }

    // 던지기 코루틴
    private IEnumerator ThrowCoroutine()
    {
        animator.SetTrigger("Throw");

        // 30프레임 대기
        for (var i = 0; i < 30; i++)
        {
            yield return null;
        }

        Instantiate(ThrowPrefab, ShotPoint.position, Quaternion.identity);
    }

    // 화살 발사 코루틴
    private IEnumerator ShootArrowCoroutine()
    {
        // 발사 쿨다운 시작
        canShootArrow = false;
        animator.SetTrigger("Bow");

        // 발사 쿨다운 코루틴 시작
        StartCoroutine(ArrowCooldownCoroutine());

        // 40프레임 대기
        for (var i = 0; i < 40; i++)
        {
            yield return null;
        }

        // useBowPrefab 설정에 따라 처리
        if (useBowPrefab)
        {
            Instantiate(BowPrefab, ShotPoint.position, Quaternion.identity);
        }

        // 화살 발사
        ShootArrow();
    }

    // 쿨다운 코루틴
    private IEnumerator ArrowCooldownCoroutine()
    {
        yield return new WaitForSeconds(arrowCooldown);
        canShootArrow = true;
    }

    // 화살 발사 함수
    private void ShootArrow()
    {
        if (ArrowPrefab == null)
        {
            Debug.LogError("Arrow Prefab is not assigned!");
            return;
        }

        // 화살 생성 위치
        Vector3 shootPosition = ShotPoint.position;

        // 화살 게임 오브젝트 생성
        GameObject arrow = Instantiate(ArrowPrefab, shootPosition, Quaternion.identity);

        // ArrowManager 컴포넌트가 있는지 확인하고 방향 설정
        ArrowManager arrowManager = arrow.GetComponent<ArrowManager>();
        if (arrowManager != null)
        {
            // 화살 방향 설정
            arrowManager.Setup(lastDirection);

            // 레벨에 따른 데미지 증가 적용 (필요한 경우)
            if (playerExperience != null)
            {
                // ArrowManager에 데미지 배수 정보 전달 (클래스 확장 필요)
                arrowManager.SetDamageMultiplier(playerExperience.CurrentDamageMultiplier);
            }

            Debug.Log($"화살 발사: 위치 {shootPosition}, 방향 {lastDirection}, 데미지 배수: {(playerExperience != null ? playerExperience.CurrentDamageMultiplier : 1.0f)}");
        }
        else
        {
            Debug.LogWarning("Arrow prefab does not have ArrowManager component!");
        }
    }

    // 공격 활성화/비활성화 메소드
    public void SetAttackEnabled(bool enabled)
    {
        attackEnabled = enabled;
        // 공격 비활성화시 진행 중인 공격 코루틴 중지
        if (!enabled)
        {
            StopCoroutine("ShootArrowCoroutine");
            StopCoroutine("ThrowCoroutine");
            canShootArrow = true; // 공격 가능 상태로 리셋
        }
    }

    // OnDestroy: 이벤트 구독 해제
    private void OnDestroy()
    {
        if (timeSystem != null)
        {
            timeSystem.onEveningStart.RemoveListener(OnEveningStart);
            timeSystem.onMorningStart.RemoveListener(OnMorningStart);
        }
    }

    // 저녁 모드 시작 이벤트 핸들러
    private void OnEveningStart()
    {
        // 저녁(밤) 모드일 때 Bow 애니메이션 트리거
        if (animator != null)
        {
            animator.SetTrigger("Bow");
            isEveningBowTriggered = true;
            Debug.Log("밤 모드 시작 - Bow 애니메이션 트리거됨");
        }
    }

    // 아침 모드 시작 이벤트 핸들러
    private void OnMorningStart()
    {
        // 아침 모드로 돌아올 때 플래그 리셋
        isEveningBowTriggered = false;
    }
}