using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ComicCutscene : MonoBehaviour
{
    [Header("Cutscene Pages")]
    [SerializeField] private GameObject[] pageObjects;

    // ============================
    // AUDIO (SoundLibrary Keys)
    // ============================
    [Header("Audio")]
    [SerializeField] private string sfxPageFlipKey = "SFX_PageFlip";      
    [SerializeField] private string sfxPanelRevealKey = "SFX_Panel";      
    [SerializeField] private string[] pageSFXKeys;                        

    [Header("Fade Overlay")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeDuration = 0.6f;

    [Header("Page Transition Fade")]
    [SerializeField] private float pageFadeDuration = 0.45f;

    [Header("Page Behavior")]
    [SerializeField] private bool[] pageUseFullFade;

    [Header("Options")]
    [SerializeField] private bool pauseGameTime = true;
    [SerializeField] private float panelFadeDuration = 0.4f;
    [SerializeField] private float subtitleFadeDuration = 0.35f;

    [Header("Scene Transition")]
    [SerializeField] private bool loadSceneAtEnd = false;
    [SerializeField] private string nextSceneName;

    private int currentIndex = 0;
    private bool allowNext = false;
    private bool isPlaying = false;

    // multi-panel system
    private bool waitingForPanelClick = false;
    private List<Image> currentPanels;

    private void Start()
    {
        if (fadeOverlay != null)
        {
            var c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
            fadeOverlay.gameObject.SetActive(true);
        }

        StartCutscene();
    }

    public void StartCutscene()
    {
        foreach (var page in pageObjects)
            page.SetActive(false);

        if (pauseGameTime)
            Time.timeScale = 0f;

        currentIndex = 0;
        isPlaying = true;

        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSecondsRealtime(0.20f);

        yield return FadeOverlaySmooth(1f, 0f, fadeDuration);

        yield return StartCoroutine(ShowPageRoutine());
    }

    private void Update()
    {
        if (!isPlaying) return;

        // reveal next panel
        if (waitingForPanelClick)
        {
            bool next =
                Keyboard.current?.spaceKey.wasPressedThisFrame == true ||
                Mouse.current?.leftButton.wasPressedThisFrame == true ||
                Gamepad.current?.buttonSouth.wasPressedThisFrame == true;

            if (next)
                waitingForPanelClick = false;

            return;
        }

        // next page
        if (!allowNext) return;

        bool nextPage =
            Keyboard.current?.spaceKey.wasPressedThisFrame == true ||
            Mouse.current?.leftButton.wasPressedThisFrame == true ||
            Gamepad.current?.buttonSouth.wasPressedThisFrame == true;

        if (nextPage)
            NextPage();
    }

    // ============================
    // MAIN PAGE ROUTINE
    // ============================
    IEnumerator ShowPageRoutine()
    {
        allowNext = false;

        bool useFade = PageShouldFade(currentIndex);

        
        PlayPageFlipSFX();

        if (useFade && currentIndex > 0)
        {
            yield return FullPageTransitionFade();
        }
        else
        {
            foreach (var p in pageObjects)
                p.SetActive(false);

            PreHidePagePanels(pageObjects[currentIndex]);

            pageObjects[currentIndex].SetActive(true);
        }

        
        PlayPageSFX(currentIndex);

        // Collect panels
        currentPanels = new List<Image>();
        foreach (Transform child in pageObjects[currentIndex].transform)
        {
            Image img = child.GetComponent<Image>();
            if (img != null)
                currentPanels.Add(img);
        }

        bool multiplePanels = currentPanels.Count > 1;

        // Subtitle
        TextMeshProUGUI tmpSub =
            pageObjects[currentIndex].GetComponentInChildren<TextMeshProUGUI>(true);

        if (tmpSub != null)
        {
            var col = tmpSub.color; col.a = 0;
            tmpSub.color = col;
            tmpSub.gameObject.SetActive(false);
        }

        // MULTI PANEL LOGIC
        if (multiplePanels)
        {
            // first panel
            yield return FadeInPanel(currentPanels[0]);
            PlayPanelSFX();

            // next panels
            for (int i = 1; i < currentPanels.Count; i++)
            {
                waitingForPanelClick = true;

                yield return new WaitUntil(() => waitingForPanelClick == false);

                yield return FadeInPanel(currentPanels[i]);
                PlayPanelSFX();
            }
        }
        else
        {
            if (currentPanels.Count == 1)
            {
                yield return FadeInPanel(currentPanels[0]);
                PlayPanelSFX();
            }
        }

        // subtitle reveal
        if (tmpSub != null)
        {
            tmpSub.gameObject.SetActive(true);
            yield return FadeInText(tmpSub);
        }

        allowNext = true;
    }

    // ============================
    // AUDIO FUNCTIONS
    // ============================
    private void PlayPageFlipSFX()
    {
        if (!string.IsNullOrEmpty(sfxPageFlipKey))
            AudioManager.Instance?.PlaySFX(sfxPageFlipKey);
    }

    private void PlayPanelSFX()
    {
        if (!string.IsNullOrEmpty(sfxPanelRevealKey))
            AudioManager.Instance?.PlaySFX(sfxPanelRevealKey);
    }

    private void PlayPageSFX(int index)
    {
        if (pageSFXKeys == null || index >= pageSFXKeys.Length) return;

        string key = pageSFXKeys[index];
        if (!string.IsNullOrEmpty(key))
            AudioManager.Instance?.PlaySFX(key);
    }

    // ============================
    // HELPERS
    // ============================
    void PreHidePagePanels(GameObject page)
    {
        foreach (Transform child in page.transform)
        {
            Image img = child.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color; c.a = 0;
                img.color = c;
            }
        }

        TextMeshProUGUI tmpSub =
            page.GetComponentInChildren<TextMeshProUGUI>(true);

        if (tmpSub != null)
        {
            var col = tmpSub.color; col.a = 0;
            tmpSub.color = col;
        }
    }

    bool PageShouldFade(int index)
    {
        if (pageUseFullFade == null || pageUseFullFade.Length == 0)
            return true;
        if (index >= pageUseFullFade.Length)
            return true;

        return pageUseFullFade[index];
    }

    IEnumerator FullPageTransitionFade()
    {
        yield return FadeOverlaySmooth(0f, 1f, pageFadeDuration);

        foreach (var p in pageObjects)
            p.SetActive(false);

        PreHidePagePanels(pageObjects[currentIndex]);

        pageObjects[currentIndex].SetActive(true);

        yield return FadeOverlaySmooth(1f, 0f, pageFadeDuration);
    }

    IEnumerator FadeOverlaySmooth(float start, float end, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / duration;
            p = p * p * (3 - 2 * p);

            float a = Mathf.Lerp(start, end, p);

            var c = fadeOverlay.color;
            c.a = a;
            fadeOverlay.color = c;

            yield return null;
        }
    }

    IEnumerator FadeInPanel(Image panel)
    {
        float t = 0f;

        while (t < panelFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / panelFadeDuration;
            p = p * p * (3 - 2 * p);

            panel.color = new Color(1, 1, 1, p);
            yield return null;
        }
    }

    IEnumerator FadeInText(Graphic textObj)
    {
        float t = 0f;

        while (t < subtitleFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / subtitleFadeDuration;
            p = p * p * (3 - 2 * p);

            var col = textObj.color;
            col.a = p;
            textObj.color = col;

            yield return null;
        }
    }

    // ============================
    // PAGE CHANGE + END CUTSCENE
    // ============================
    void NextPage()
    {
        currentIndex++;

        if (currentIndex >= pageObjects.Length)
            EndCutscene();
        else
            StartCoroutine(ShowPageRoutine());
    }

    void EndCutscene()
    {
        isPlaying = false;

        if (pauseGameTime)
            Time.timeScale = 1f;

        foreach (var p in pageObjects)
            p.SetActive(false);

        StartCoroutine(FadeOutAndExit());
    }

    IEnumerator FadeOutAndExit()
    {
        yield return FadeOverlaySmooth(0f, 1f, fadeDuration);

        if (loadSceneAtEnd && !string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Destroy(gameObject);
    }
}
