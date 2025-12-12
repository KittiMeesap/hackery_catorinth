using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingPanel;
    public GameObject creditsPanel;

    [Header("Buttons")]
    public GameObject firstSelectedMain;

    [Header("Fade")]
    public Image fadeOverlay;
    public float fadeDuration = 0.5f;

    [Header("SFX Keys")]
    public string clickKey = "UI_Click";
    public string hoverKey = "UI_Hover";

    private bool isTransitioning = false;

    // UI Lock
    private bool uiLocked = false;

    // UI Cancel input
    private InputAction cancelAction;

    private void Start()
    {
        GameInput.Instance.SetModeUI();

        var map = GameInput.Instance.PlayerInputComponent.actions.FindActionMap("UI");
        cancelAction = map.FindAction("Cancel");
        cancelAction.performed += OnCancelPressed;

        if (fadeOverlay != null)
        {
            var c = fadeOverlay.color;
            c.a = 0;
            fadeOverlay.color = c;
        }

        ShowMainMenu();
    }

    private void OnDestroy()
    {
        if (cancelAction != null)
            cancelAction.performed -= OnCancelPressed;
    }

    // ------------------------------
    // UI LOCK SYSTEM
    // ------------------------------
    public void SetUILocked(bool locked)
    {
        uiLocked = locked;
    }

    // ------------------------------
    private void OnCancelPressed(InputAction.CallbackContext ctx)
    {
        if (uiLocked) return;

        if (creditsPanel.activeSelf)
        {
            CloseCredits();
            return;
        }

        if (settingPanel.activeSelf)
            return;
    }

    // ------------------------------
    public void ShowMainMenu()
    {
        PlayHover();

        mainPanel.SetActive(true);
        settingPanel.SetActive(false);
        creditsPanel.SetActive(false);

        GameInput.Instance.SetModeUI();

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectedMain);

        uiLocked = false;
    }

    public void ShowSetting()
    {
        PlayClick();

        uiLocked = true;

        mainPanel.SetActive(true);
        settingPanel.SetActive(true);
        creditsPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        PlayClick();

        uiLocked = true;

        mainPanel.SetActive(true);
        settingPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        PlayClick();
        uiLocked = false;
        ShowMainMenu();
    }

    public void PlayGame()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        PlayClick();
        StartCoroutine(FadeAndLoad("Tutorial 1"));
    }

    IEnumerator FadeAndLoad(string sceneName)
    {
        fadeOverlay.raycastTarget = true;

        float t = 0f;
        Color c = fadeOverlay.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0, 1, t / fadeDuration);
            fadeOverlay.color = c;
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        PlayClick();
        Application.Quit();
    }

    public void PlayHover() => AudioManager.Instance.PlaySFX(hoverKey);
    public void PlayClick() => AudioManager.Instance.PlaySFX(clickKey);
}
