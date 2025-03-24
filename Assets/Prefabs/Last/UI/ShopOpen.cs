using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopOpen : MonoBehaviour
{
    public GameObject scrollView; 
    public Button toggleButton;   
    void Start()
    {
        // 처음에 ScrollView를 비활성화
        if (scrollView != null)
        {
            scrollView.SetActive(false);
        }

        // 버튼 클릭 이벤트에 ToggleScrollView 연결
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleScrollView);
        }
    }

    
        void ToggleScrollView()
        {
            if (scrollView != null)
            {
                scrollView.SetActive(!scrollView.activeSelf);
            }
        }
    
}
