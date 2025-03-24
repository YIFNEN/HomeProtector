using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public Image fadeImage; // UI Image (검은색 배경)
    public float fadeSpeed = 1.5f;

   // void Start()
  //  {
        // 씬 시작 시 페이드 인 효과 적용 (화면이 점점 밝아짐)
      //  StartCoroutine(FadeIn());
  //  }

    public void StartSceneTransition(string morning)
    {
        // 씬 전환 시작 (페이드 아웃 후 씬 이동)
        StartCoroutine(FadeOut(morning));
    }

  //  IEnumerator FadeIn()
  //  {
   //     float alpha = 1;
   //     while (alpha > 0)
   //     {
    //        alpha -= Time.deltaTime * fadeSpeed;
    //        fadeImage.color = new Color(0, 0, 0, alpha);
     //       yield return null;
     //   }
   // }

    IEnumerator FadeOut(string morning)
    {
        float alpha = 0;
        while (alpha < 1)
        {
            alpha += Time.deltaTime * fadeSpeed;
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // 씬 전환
        SceneManager.LoadScene(morning);
    }
}
