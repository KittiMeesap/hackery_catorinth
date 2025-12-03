using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject SettingPanel;
    public GameObject creditPanel;

    [Header("UI Sound Keys (SoundLibrary)")]
    [SerializeField] private string uiClickKey = "UI_Click";
    [SerializeField] private string uiHoverKey = "UI_Hover";

    [Header("Fade Overlay")]
    public Image fadeOverlay;
    public float fadeDuration = 0.6f;

    [Header("First Selected")]
    public GameObject firstSelected;

    private bool isFading = false;

    private void Start()
    {
        var input = FindFirstObjectByType<PlayerInput>();
        if (input != null)
            input.SwitchCurrentActionMap("UI");

        if (firstSelected != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);

        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;

            fadeOverlay.raycastTarget = false;
            fadeOverlay.gameObject.SetActive(true);
        }
    }

    // ============================================================
    // START GAME
    // ============================================================
    public void PlayGame()
    {
        if (isFading) return;
        PlayClick();
        StartCoroutine(FadeOutAndLoad("IntroCutscene"));
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        isFading = true;

        if (fadeOverlay != null)
            fadeOverlay.raycastTarget = true;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / fadeDuration;
            p = p * p * (3 - 2 * p);

            if (fadeOverlay != null)
            {
                Color c = fadeOverlay.color;
                c.a = Mathf.Lerp(0f, 1f, p);
                fadeOverlay.color = c;
            }

            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }

<<<<<<< Updated upstream
    // OPEN SETTINGS
=======
    // ============================================================
    // SETTINGS
    // ============================================================
>>>>>>> Stashed changes
    public void Setting()
    {
        PlayClick();
        SettingPanel.SetActive(true);

        SetMainMenuButtonsInteractable(false);

        var input = FindFirstObjectByType<PlayerInput>();
        if (input != null)
            input.SwitchCurrentActionMap("UI");

        var first = SettingPanel.GetComponentInChildren<Button>();
        if (first != null)
            EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    // CLOSE SETTINGS
    public void BackToSetting()
    {
<<<<<<< Updated upstream
        PlayClickSound();
        StartCoroutine(BackToSettingRoutine());
    }

    private IEnumerator BackToSettingRoutine()
    {
=======
        PlayClick();
>>>>>>> Stashed changes
        SettingPanel.SetActive(false);

        yield return null;

        SetMainMenuButtonsInteractable(true);

        EventSystem.current.SetSelectedGameObject(firstSelected);

        var input = FindFirstObjectByType<PlayerInput>();
        if (input != null)
            input.SwitchCurrentActionMap("UI");
<<<<<<< Updated upstream
    }

    // CREDIT PANEL
=======

        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    // ============================================================
    // CREDIT
    // ============================================================
>>>>>>> Stashed changes
    public void ShowCredit()
    {
        PlayClick();
        creditPanel.SetActive(true);

        SetMainMenuButtonsInteractable(false);

        var first = creditPanel.GetComponentInChildren<Button>();
        if (first != null)
            EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    public void BackFromCredit()
    {
        PlayClick();
        creditPanel.SetActive(false);

        SetMainMenuButtonsInteractable(true);

        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

<<<<<<< Updated upstream
    // DISABLE / ENABLE MAIN MENU BUTTONS
    private void SetMainMenuButtonsInteractable(bool value)
    {
        Button[] buttons = GetComponentsInChildren<Button>(includeInactive: true);

        foreach (Button b in buttons)
        {
            if (SettingPanel != null && b.transform.IsChildOf(SettingPanel.transform))
                continue;

            if (creditPanel != null && b.transform.IsChildOf(creditPanel.transform))
                continue;

            b.interactable = value;
        }
    }

    // AUDIO
=======
    // ============================================================
    // QUIT GAME
    // ============================================================
    public void QuitGame()
    {
        PlayClick();
        Application.Quit();
    }

    // ============================================================
    // UI SOUND EVENTS
    // ============================================================
>>>>>>> Stashed changes
    public void OnHover()
    {
        if (!string.IsNullOrEmpty(uiHoverKey))
            AudioManager.Instance?.PlayUI(uiHoverKey);
    }

    private void PlayClick()
    {
        if (!string.IsNullOrEmpty(uiClickKey))
            AudioManager.Instance?.PlayUI(uiClickKey);
    }
}
