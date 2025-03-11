using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class PlayerMovement : MonoBehaviour
{
    public Transform ItenPoint;
    public Transform ShotPoint; // 화살이 발사될 위치
    public GameObject ItemPrefab;
    public GameObject ThrowPrefab;
    public GameObject BowPrefab;
    public GameObject ArrowPrefab; // 화살 프리팹 추가
    public float arrowSpeed = 10f; // 화살 속도
    public float arrowCooldown = 0.5f; // 화살 발사 쿨다운 (초 단위)
    private bool useBowPrefab = false; // BowPrefab 사용 여부 설정
    private bool canShootArrow = true; // 화살 발사 가능 여부
    Rigidbody2D rb;
    Animator animator;
    public float moveSpeed;
    private Vector2 lastDirection = new Vector2(1, 0); // 기본적으로 오른쪽을 바라봄

    [SerializeField]
    private Transform shotPointTransform = null;

    void Start()
    {
        Application.targetFrameRate = 60;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = (x == 0) ? Input.GetAxisRaw("Vertical") : 0.0f;

        // 플레이어가 바라보는 방향 저장
        if (x != 0 || y != 0)
        {
            lastDirection = new Vector2(x, y).normalized;
            animator.SetFloat("x", x);
            animator.SetFloat("y", y);
            animator.SetBool("Walk", true);
        }
        else
        {
            animator.SetBool("Walk", false);
        }

        StartCoroutine(Action());
        StartCoroutine(Shot());
        transform.Translate(Input.GetAxis("Horizontal") * Time.deltaTime * moveSpeed, Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed, 0);
    }

    IEnumerator Action()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            animator.SetTrigger("Slash");
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            animator.SetTrigger("Guard");
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            animator.SetTrigger("Item");
            Instantiate(ItemPrefab, ItenPoint.position, transform.rotation);
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            animator.SetTrigger("Damage");
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            animator.SetTrigger("Dead");
            this.transform.position = new Vector2(0f, -0.12f);
            for (var i = 0; i < 64; i++)
            {
                yield return null;
            }
            this.transform.position = Vector2.zero;
        }
    }

    IEnumerator Shot()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            animator.SetTrigger("Throw");
            for (var i = 0; i < 30; i++)
            {
                yield return null;
            }
            Instantiate(ThrowPrefab, ShotPoint.position, Quaternion.identity);
        }
        if (Input.GetKeyDown(KeyCode.C) && canShootArrow)
        {
            // 화살 발사 가능할 때만 실행
            animator.SetTrigger("Bow");

            // 발사 쿨다운 시작
            canShootArrow = false;
            StartCoroutine(ArrowCooldown());

            for (var i = 0; i < 40; i++)
            {
                yield return null;
            }

            // useBowPrefab 설정에 따라 처리
            if (useBowPrefab)
            {
                // BowPrefab 생성 (원래의 로직)
                Instantiate(BowPrefab, ShotPoint.position, Quaternion.identity);
            }

            // 화살 발사
            ShootArrow();
        }
    }

    // 쿨다운 코루틴
    IEnumerator ArrowCooldown()
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

        // 화살 생성 위치 (ShotPoint 위치 사용)
        Vector3 shootPosition = ShotPoint.position;

        // 화살 게임 오브젝트 생성 - 부모 설정 제거
        GameObject arrow = Instantiate(ArrowPrefab, shootPosition, Quaternion.identity);

        // ArrowManager 컴포넌트가 있는지 확인하고 방향 설정
        ArrowManager arrowManager = arrow.GetComponent<ArrowManager>();
        if (arrowManager != null)
        {
            // 화살 방향 설정
            arrowManager.Setup(lastDirection);

            // 디버깅 로그 추가
            Debug.Log("화살 발사: 위치 " + shootPosition + ", 방향 " + lastDirection);
        }
        else
        {
            Debug.LogWarning("Arrow prefab does not have ArrowManager component!");
        }
    }
}