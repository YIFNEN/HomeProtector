using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 싱글턴 패턴 구현
public class PlayerSingleton : MonoBehaviour
{
    // 싱글턴 인스턴스
    private static PlayerSingleton instance;

    // 외부에서 접근 가능한 인스턴스 프로퍼티
    public static PlayerSingleton Instance
    {
        get { return instance; }
    }

    // 플레이어 컴포넌트 참조
    private PlayerMovement playerMovement;

    // 플레이어가 현재 존재하는지 여부
    public static bool Exists
    {
        get { return instance != null; }
    }

    private void Awake()
    {
        // 싱글턴 패턴 구현
        if (instance != null && instance != this)
        {
            // 이미 인스턴스가 있으면 이 오브젝트 파괴
            Debug.Log("플레이어가 이미 존재합니다. 중복 생성된 플레이어를 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        // 인스턴스 설정
        instance = this;

        // 씬 전환 시에도 파괴되지 않도록 설정 (필요한 경우)
        // DontDestroyOnLoad(gameObject);

        // 플레이어 컴포넌트 캐싱
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnDestroy()
    {
        // 이 오브젝트가 인스턴스인 경우에만 null로 설정
        if (instance == this)
        {
            instance = null;
        }
    }

    // 플레이어 활성화/비활성화
    public void SetPlayerActive(bool active)
    {
        gameObject.SetActive(active);
    }

    // 플레이어 공격 활성화/비활성화
    public void SetAttackEnabled(bool enabled)
    {
        if (playerMovement != null)
        {
            playerMovement.SetAttackEnabled(enabled);
        }
    }

    // 플레이어 위치 설정
    public void SetPosition(Vector3 position)
    {
        transform.position = position;

        // 이소메트릭 뷰를 위한 Z 위치 조정
        Vector3 newPos = transform.position;
        newPos.z = newPos.y;
        transform.position = newPos;
    }
}