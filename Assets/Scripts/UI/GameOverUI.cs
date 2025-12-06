using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class GameOverUI : MonoBehaviour
{
    [Header("UI Root")]
    public GameObject panel;

    [Header("Optional")]
    public ScreenFader screenFader;
    public string mainMenuSceneName = "UI - Menu";

    [Header("Controller Support")]
    public GameObject firstSelectedButton;

    private bool shown = false;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (screenFader == null) screenFader = FindFirstObjectByType<ScreenFader>();
    }

    public void Show()
    {
        if (shown) return;
        shown = true;
        if (panel != null) panel.SetActive(true);

        if (firstSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    public void OnClickRetry()
    {
        StartCoroutine(RetryRoutine());
    }

    public void OnClickMainMenu()
    {
        StartCoroutine(MainMenuRoutine());
    }

    private System.Collections.IEnumerator RetryRoutine()
    {
        if (screenFader != null) yield return screenFader.FadeOut();
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    private System.Collections.IEnumerator MainMenuRoutine()
    {
        if (screenFader != null) yield return screenFader.FadeOut();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
