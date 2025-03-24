using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel; // 반투명 창 (UI)
    public GameObject pausebtn;
    public GameObject gameOut;
    private bool isPaused = false; // 현재 일시정지 상태 여부

    void Update()
    {
        // ESC 키를 누르면 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        pausePanel.SetActive(true); // UI 활성화
        pausebtn.SetActive(true); // UI 활성화
        gameOut.SetActive(true); // UI 활성화
        Time.timeScale = 0; // 게임 정지
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false); // UI 숨김
        pausebtn.SetActive(false);
        gameOut.SetActive(false);
        Time.timeScale = 1; // 게임 재개
    }
}
