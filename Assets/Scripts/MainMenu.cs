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

    [Header("First Selected")]
    public Button firstSelected;

    private void Start()
    {
        if (firstSelected != null)
            firstSelected.Select();
    }

    public void PlayGame()
    {
        PlayClickSound();
        SceneManager.LoadScene("Map 0");
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
        {
            audioSource = FindFirstObjectByType<AudioSource>();
        }

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
