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

        if (panel != null)
            panel.SetActive(true);

        var input = FindFirstObjectByType<PlayerInput>();
        if (input != null)
            input.SwitchCurrentActionMap("UI");

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
        // Fade out screen
        if (screenFader != null)
            yield return screenFader.FadeOut();

        // Respawn Player
        var player = FindFirstObjectByType<PlayerController>();

        if (player != null)
        {
            var hp = player.GetComponent<PlayerHealth>();
            if (hp != null)
                hp.ResetHealth();

            // Restore position & countdown
            GameManager.Instance.RespawnPlayer(player.gameObject);

            // Reset movement, animations, states
            player.ResetStateAfterRespawn();
        }

        // Unfreeze game
        GameManager.Instance.FreezeGame(false);

        // Switch back to gameplay input
        var input = FindFirstObjectByType<PlayerInput>();
        if (input != null)
            input.SwitchCurrentActionMap("Player");

        shown = false;
        panel.SetActive(false);

        // Fade in again
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
