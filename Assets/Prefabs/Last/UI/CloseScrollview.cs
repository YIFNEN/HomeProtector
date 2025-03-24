using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CloseScrollview : MonoBehaviour
{
    public GameObject scrollView; // 스크롤 뷰 오브젝트
    void Start()
    {
        // 버튼이 현재 게임 오브젝트에 있을 경우 자동으로 가져옴
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(CloseView);
        }
    }

    public void CloseView()
    {
        if (scrollView != null)
        {
            scrollView.SetActive(false);
        }
    }
}
