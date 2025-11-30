using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject SettingPanel;
    public GameObject creditPanel;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    public AudioClip hoverSound;

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

    public void PlayGame()
    {
        if (isFading) return;
        PlayClickSound();
        StartCoroutine(FadeOutAndLoad("IntroCutscene"));
    }

    private System.Collections.IEnumerator FadeOutAndLoad(string sceneName)
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

    public void Setting()
    {
        PlayClickSound();
        SettingPanel.SetActive(true);

        var input = FindFirstObjectByType<PlayerInput>();
        if (input != null)
            input.SwitchCurrentActionMap("UI");

        var first = SettingPanel.GetComponentInChildren<Button>();
        if (first != null)
            EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    public void BackToSetting()
    {
        PlayClickSound();
        SettingPanel.SetActive(false);

        var input = FindFirstObjectByType<PlayerInput>();
        if (input != null)
            input.SwitchCurrentActionMap("UI");

        // RETURN FOCUS TO MAIN MENU
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    public void ShowCredit()
    {
        PlayClickSound();
        creditPanel.SetActive(true);

        var first = creditPanel.GetComponentInChildren<Button>();
        if (first != null)
            EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    public void BackFromCredit()
    {
        PlayClickSound();
        creditPanel.SetActive(false);

        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    public void QuitGame()
    {
        PlayClickSound();
        Application.Quit();
    }

    public void OnHover()
    {
        if (hoverSound != null && audioSource != null)
            audioSource.PlayOneShot(hoverSound);
    }

    private void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
            audioSource.PlayOneShot(clickSound);
    }
}
