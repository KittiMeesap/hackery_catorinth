using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    public Button firstSelected;

    private bool isFading = false;

    private void Start()
    {
        if (firstSelected != null)
            firstSelected.Select();

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

        var btn = SettingPanel.GetComponentInChildren<Button>();
        if (btn != null) btn.Select();
    }

    public void BackToSetting()
    {
        PlayClickSound();
        SettingPanel.SetActive(false);

        if (firstSelected != null) firstSelected.Select();
    }

    public void ShowCredit()
    {
        PlayClickSound();
        creditPanel.SetActive(true);

        var btn = creditPanel.GetComponentInChildren<Button>();
        if (btn != null) btn.Select();
    }

    public void BackFromCredit()
    {
        PlayClickSound();
        creditPanel.SetActive(false);

        if (firstSelected != null) firstSelected.Select();
    }

    public void QuitGame()
    {
        PlayClickSound();
        Debug.Log("Quit Game");
        Application.Quit();
    }

    public void ResetMainMenu()
    {
        if (audioSource == null)
            audioSource = FindFirstObjectByType<AudioSource>();

        if (SettingPanel != null)
            SettingPanel.SetActive(false);
        if (creditPanel != null)
            creditPanel.SetActive(false);

        if (firstSelected != null)
            firstSelected.Select();
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
