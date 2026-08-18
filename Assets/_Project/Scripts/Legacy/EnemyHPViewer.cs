using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHPViewer : MonoBehaviour
{
    private EnemyHP enemyHP; // 적 HP 정보를 참조하는 변수
    private Slider hpSlider; // UI의 Slider 컴포넌트

    /// <summary>
    /// EnemyHPViewer 설정 메서드. EnemyHP와 Slider를 초기화합니다.
    /// </summary>
    /// <param name="enemyHP">적 HP를 관리하는 EnemyHP 객체</param>
    public void Setup(EnemyHP enemyHP)
    {
        // 기존 이벤트 구독 해제 (중복 방지)
        if (this.enemyHP != null)
        {
            this.enemyHP.onTakeDamage -= ShowHPBar;
        }

        this.enemyHP = enemyHP;
        hpSlider = GetComponent<Slider>();

        if (hpSlider == null)
        {
            Debug.LogError("Slider 컴포넌트가 할당되지 않았습니다. EnemyHPViewer는 Slider가 있는 오브젝트에 붙어 있어야 합니다.");
            return;
        }

        // 초기 슬라이더 설정
        hpSlider.maxValue = 1f;
        hpSlider.value = enemyHP.CurrentHP / enemyHP.MaxHp;
        hpSlider.gameObject.SetActive(false);

        // 데미지 이벤트에 핸들러 등록
        enemyHP.onTakeDamage += ShowHPBar;
    }

    // Update 메서드에서 Slider 값을 갱신
    private void Update()
    {
        if (enemyHP == null || hpSlider == null)
        {
            return; // 경고 메시지 제거, 조용히 무시
        }

        // Slider 값 업데이트
        hpSlider.value = enemyHP.CurrentHP / enemyHP.MaxHp;
    }

    private void ShowHPBar()
    {
        // 기존 코루틴 중지 (있다면)
        StopAllCoroutines();

        // HP 바 활성화
        hpSlider.gameObject.SetActive(true);

        // 3초 후 비활성화 코루틴 시작
        StartCoroutine(HideHPBar());
    }

    private IEnumerator HideHPBar()
    {
        yield return new WaitForSeconds(3.0f); // 3초 후
        hpSlider.gameObject.SetActive(false);
    }

    // 오브젝트 비활성화 시 이벤트 구독 해제
    private void OnDisable()
    {
        if (enemyHP != null)
        {
            enemyHP.onTakeDamage -= ShowHPBar;
        }
    }
}