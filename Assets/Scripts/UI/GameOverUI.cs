using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GameOverUI : MonoBehaviour
{
    [Header("UI Root")]
    public GameObject panel;

    [Header("Optional")]
    public ScreenFader screenFader;
    public string mainMenuSceneName = "MainMenu";

    [Header("Controller Support")]
    public GameObject firstSelectedButton;

    private bool shown = false;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        if (screenFader == null)
            screenFader = FindFirstObjectByType<ScreenFader>();
    }

    public void Show()
    {
        if (shown) return;
        shown = true;

        // ปลด Freeze เพื่อให้ UI รับ input
        GameManager.Instance.FreezeGame(false);

        if (panel != null)
            panel.SetActive(true);

        var input = FindFirstObjectByType<PlayerInput>();
        if (input != null)
            input.SwitchCurrentActionMap("UI");

        StartCoroutine(SelectFirstButton());
    }

    private System.Collections.IEnumerator SelectFirstButton()
    {
        yield return null;
        if (firstSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    public void OnClickRetry()
    {
        StartCoroutine(RespawnRoutine());
    }

    public void OnClickMainMenu()
    {
        StartCoroutine(MainMenuRoutine());
    }

    private System.Collections.IEnumerator RespawnRoutine()
    {
        if (screenFader != null)
            yield return screenFader.FadeOut();

        var player = FindFirstObjectByType<PlayerController>();

        if (player != null)
        {
            var hp = player.GetComponent<PlayerHealth>();
            if (hp != null)
                hp.ResetHealth();

            GameManager.Instance.RespawnPlayer(player.gameObject);
            player.ResetStateAfterRespawn();
        }

        GameManager.Instance.FreezeGame(false);

        var input = FindFirstObjectByType<PlayerInput>();
        if (input != null)
            input.SwitchCurrentActionMap("Player");

        shown = false;
        panel.SetActive(false);

        if (screenFader != null)
            yield return screenFader.FadeIn();
    }

    private System.Collections.IEnumerator MainMenuRoutine()
    {
        if (screenFader != null)
            yield return screenFader.FadeOut();

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
