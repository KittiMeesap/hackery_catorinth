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

    [Header("Audio")]
    [SerializeField] private string bgmCutsceneKey = "BGM_Cutscene";
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

    private bool waitingForPanelClick = false;
    private List<Image> currentPanels;

    // INPUT SYSTEM
    private InputManager input;
    private InputAction continueAction;

    private void Awake()
    {
        input = new InputManager();
    }

    private void Start()
    {
        PlayCutsceneBGM();

        if (fadeOverlay != null)
        {
            var c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
            fadeOverlay.gameObject.SetActive(true);
        }

        StartCutscene();
    }

    private void PlayCutsceneBGM()
    {
        if (!string.IsNullOrEmpty(bgmCutsceneKey))
            AudioManager.Instance?.PlayBGM(bgmCutsceneKey, true);
    }

    public void StartCutscene()
    {
        foreach (var page in pageObjects)
            page.SetActive(false);

        if (pauseGameTime)
            Time.timeScale = 0f;

        currentIndex = 0;
        isPlaying = true;

        input.GameControls.Enable();

        continueAction = input.GameControls.Continue;
        continueAction.performed += OnContinuePerformed;

        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSecondsRealtime(0.20f);

        yield return FadeOverlaySmooth(1f, 0f, fadeDuration);
        yield return StartCoroutine(ShowPageRoutine());
    }

    private void OnContinuePerformed(InputAction.CallbackContext ctx)
    {
        if (!isPlaying)
            return;

        if (waitingForPanelClick)
        {
            waitingForPanelClick = false;
            return;
        }

        if (!allowNext)
            return;

        NextPage();
    }

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

        currentPanels = new List<Image>();
        foreach (Transform child in pageObjects[currentIndex].transform)
        {
            Image img = child.GetComponent<Image>();
            if (img != null)
                currentPanels.Add(img);
        }

        bool multiplePanels = currentPanels.Count > 1;

        TextMeshProUGUI tmpSub =
            pageObjects[currentIndex].GetComponentInChildren<TextMeshProUGUI>(true);

        if (tmpSub != null)
        {
            var col = tmpSub.color; col.a = 0;
            tmpSub.color = col;
            tmpSub.gameObject.SetActive(false);
        }

        // Panel animation
        if (multiplePanels)
        {
            yield return FadeInPanel(currentPanels[0]);
            PlayPanelSFX();

            for (int i = 1; i < currentPanels.Count; i++)
            {
                waitingForPanelClick = true;
                yield return new WaitUntil(() => waitingForPanelClick == false);

                yield return FadeInPanel(currentPanels[i]);
                PlayPanelSFX();
            }
        }
        else if (currentPanels.Count == 1)
        {
            yield return FadeInPanel(currentPanels[0]);
            PlayPanelSFX();
        }

        if (tmpSub != null)
        {
            tmpSub.gameObject.SetActive(true);
            yield return FadeInText(tmpSub);
        }

        allowNext = true;
    }

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

        if (continueAction != null)
            continueAction.performed -= OnContinuePerformed;

        input.GameControls.Disable();

        AudioManager.Instance?.StopBGM();

        if (pauseGameTime)
            Time.timeScale = 1f;

        foreach (var p in pageObjects)
            p.SetActive(false);

        StartCoroutine(FadeOutAndExit());
    }

    IEnumerator FadeOutAndExit()
    {
        yield return FadeOverlaySmooth(0f, 1f, fadeDuration);

        AudioManager.Instance?.StopBGM();

        if (loadSceneAtEnd && !string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Destroy(gameObject);
    }

    bool PageShouldFade(int index)
    {
        if (pageUseFullFade == null || pageUseFullFade.Length == 0) return true;
        if (index >= pageUseFullFade.Length) return true;
        return pageUseFullFade[index];
    }

    void PlayPageFlipSFX()
    {
        if (!string.IsNullOrEmpty(sfxPageFlipKey))
            AudioManager.Instance?.PlaySFX(sfxPageFlipKey);
    }

    void PlayPanelSFX()
    {
        if (!string.IsNullOrEmpty(sfxPanelRevealKey))
            AudioManager.Instance?.PlaySFX(sfxPanelRevealKey);
    }

    void PlayPageSFX(int index)
    {
        if (pageSFXKeys == null || index >= pageSFXKeys.Length) return;

        string key = pageSFXKeys[index];
        if (!string.IsNullOrEmpty(key))
            AudioManager.Instance?.PlaySFX(key);
    }

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
            fadeOverlay.color = new Color(c.r, c.g, c.b, a);

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
}
