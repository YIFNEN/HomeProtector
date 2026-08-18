using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    private const string DefaultTargetSceneName = "isometric scene";

    [SerializeField] private FadeManager fadeManager;
    [SerializeField] private string targetSceneName = DefaultTargetSceneName;
    [SerializeField] private bool startOnAnyMouseClick = true;
    [SerializeField] private bool startOnSubmitKey = true;

    private bool isTransitioning;

    public string TargetSceneName =>
        string.IsNullOrWhiteSpace(targetSceneName) ? DefaultTargetSceneName : targetSceneName;

    private void Awake()
    {
        if (fadeManager == null)
        {
            fadeManager = FindObjectOfType<FadeManager>();
        }
    }

    private void Update()
    {
        if (!ShouldStart())
        {
            return;
        }

        StartGameFlow();
    }

    public void StartGameFlow()
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;

        if (fadeManager != null)
        {
            fadeManager.StartSceneTransition(TargetSceneName);
            return;
        }

        Debug.LogWarning("StartGame has no FadeManager. Loading target scene immediately.");
        SceneManager.LoadScene(TargetSceneName);
    }

    private bool ShouldStart()
    {
        bool mouseRequested = startOnAnyMouseClick && Input.GetMouseButtonDown(0);
        bool keyboardRequested = startOnSubmitKey
            && (Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Space));

        return mouseRequested || keyboardRequested;
    }
}