using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StorySceneController : MonoBehaviour
{
    [Header("Story Images")]
    public Sprite[] storySprites;
    public Image storyImage;

    [Header("Auto Slide")]
    public float slideDuration = 3f;

    [Header("Fade")]
    public Image fadeOverlay;
    public float fadeDuration = 0.6f;

    [Header("Audio")]
    public string storyBGMKey = "BGM_Story";
    public string skipSFXKey = "UI_Click";

    private int currentIndex = 0;
    private bool isSkipping = false;
    private Coroutine autoSlideRoutine;

    // INPUT
    private InputAction submitAction;

    private void Start()
    {
        // ===== INPUT MODE =====
        GameInput.Instance.SetModeUI();
        submitAction = GameInput.Instance.SubmitAction;

        // ===== INIT IMAGE =====
        currentIndex = 0;
        storyImage.sprite = storySprites[currentIndex];
        storyImage.color = Color.white;

        // ===== INIT FADE =====
        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
            fadeOverlay.raycastTarget = false;
        }

        // ===== PLAY BGM =====
        AudioManager.Instance.PlayBGM(storyBGMKey, crossfade: false);

        // ===== START AUTO SLIDE =====
        autoSlideRoutine = StartCoroutine(AutoSlideRoutine());
    }

    private void Update()
    {
        if (isSkipping)
            return;

        // ?? ??? UI Submit ??????
        if (submitAction != null && submitAction.WasPressedThisFrame())
        {
            SkipToMainMenu();
        }
    }

    // =====================================================
    // SKIP
    // =====================================================
    private void SkipToMainMenu()
    {
        if (isSkipping) return;
        isSkipping = true;

        AudioManager.Instance.PlaySFX(skipSFXKey);

        if (autoSlideRoutine != null)
            StopCoroutine(autoSlideRoutine);

        StartCoroutine(FadeAndGoToMainMenu());
    }

    // =====================================================
    // AUTO SLIDE
    // =====================================================
    private IEnumerator AutoSlideRoutine()
    {
        while (currentIndex < storySprites.Length - 1)
        {
            yield return new WaitForSeconds(slideDuration);
            yield return ChangeImage();
        }

        yield return new WaitForSeconds(slideDuration);
        yield return FadeAndGoToMainMenu();
    }

    private IEnumerator ChangeImage()
    {
        yield return Fade(0f, 1f);

        currentIndex++;
        storyImage.sprite = storySprites[currentIndex];

        yield return Fade(1f, 0f);
    }

    // =====================================================
    // SCENE TRANSITION
    // =====================================================
    private IEnumerator FadeAndGoToMainMenu()
    {
        yield return Fade(0f, 1f);

        AudioManager.Instance.StopBGM();
        SceneManager.LoadScene("MainMenu");
    }

    // =====================================================
    // FADE CORE
    // =====================================================
    private IEnumerator Fade(float from, float to)
    {
        fadeOverlay.raycastTarget = true;

        float t = 0f;
        Color c = fadeOverlay.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / fadeDuration);
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = to;
        fadeOverlay.color = c;

        fadeOverlay.raycastTarget = false;
    }
}
