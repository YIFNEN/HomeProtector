using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 씬 변경 감지

public class FadeBlinkText : MonoBehaviour
{
    public Text textComponent; // UI 텍스트
    public float fadeDuration = 1f; // 페이드 인/아웃 지속 시간
    private bool isSceneActive = true; // 씬이 유지되는 동안 true
    void Start()
    {
        StartCoroutine(FadeBlink());
    }
    IEnumerator FadeBlink() // 씬이 유지되는 동안 실행
    {
        while (isSceneActive)
        {
            // 점점 투명해짐 (Alpha 1 -> 0)
            yield return StartCoroutine(FadeText(1, 0, fadeDuration));
            // 점점 선명해짐 (Alpha 0 -> 1)
            yield return StartCoroutine(FadeText(0, 1, fadeDuration));
        }
    }
    IEnumerator FadeText(float startAlpha, float endAlpha, float duration)
    {
        float time = 0;
        Color textColor = textComponent.color;

        while (time < duration) //지정한 시간(duration) 동안 Alpha 값 변경
        {
            time += Time.deltaTime; //한 프레임이 지나가는 시간(Time.deltaTime)을 통해 time 값 증가 시킴
            float alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration); //부드럽게 변화시키는 함수(Mathf.Lerp)
            textComponent.color = new Color(textColor.r, textColor.g, textColor.b, alpha); //텍스트의 색상, 투명도 변경
            yield return null; //한 프레임을 기다린 후 다시 실행,이걸 넣어야 부드럽게 서서히 변화함
        }

        textComponent.color = new Color(textColor.r, textColor.g, textColor.b, endAlpha);
    }
    void OnDisable()
    {
        isSceneActive = false; // 씬이 변경되거나 오브젝트가 비활성화되면 중지
    }
}
