using System.Collections;
using System.Collections.Generic;
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
        this.enemyHP = enemyHP;
        hpSlider = GetComponent<Slider>();
        hpSlider.gameObject.SetActive(false);
        enemyHP.onTakeDamage += ShowHPBar;
        if (hpSlider == null)
        {
            Debug.LogError("Slider 컴포넌트가 할당되지 않았습니다. EnemyHPViewer는 Slider가 있는 오브젝트에 붙어 있어야 합니다.");
        }
    }

    // Update 메서드에서 Slider 값을 갱신
    public void Update()
    {
        if (enemyHP == null || hpSlider == null)
        {
            Debug.LogWarning("enemyHP 또는 hpSlider가 null 상태입니다. Setup이 호출되었는지 확인하세요.");
            return;
        }

        // Slider 값 업데이트
        hpSlider.value = enemyHP.CurrentHP / enemyHP.MaxHp;
    }
    private void ShowHPBar()
    {
        hpSlider.gameObject.SetActive(true);

        // 몇 초 후 비활성화
        StartCoroutine(HideHPBar());
    }

         private IEnumerator HideHPBar()
    {
        yield return new WaitForSeconds(3.0f); // 3초 후
        hpSlider.gameObject.SetActive(false);
    }
}

