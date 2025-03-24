using UnityEngine;

public class StartGame : MonoBehaviour
{
    public FadeManager fadeManager; // FadeManager 스크립트 참조

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 화면 클릭 시
        {
            fadeManager.StartSceneTransition("isometric scene"); // 씬 전환 실행
        }
    }
}
