using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeDuration = 0.5f;

    private void Awake()
    {
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 1f);
            fadeImage.raycastTarget = true;
            fadeImage.gameObject.SetActive(true);
        }
    }

    // PUBLIC API

    public IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        yield return FadeRoutine(0f, 1f);
    }

    public IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        yield return FadeRoutine(1f, 0f);

        fadeImage.raycastTarget = false;
        fadeImage.gameObject.SetActive(false);
    }

    // CORE SMOOTHSTEP FADE

    private IEnumerator FadeRoutine(float start, float end)
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;

            float p = Mathf.Clamp01(t / fadeDuration);

            p = p * p * (3f - 2f * p);

            float a = Mathf.Lerp(start, end, p);

            Color c = fadeImage.color;
            fadeImage.color = new Color(c.r, c.g, c.b, a);

            yield return null;
        }

        Color fc = fadeImage.color;
        fadeImage.color = new Color(fc.r, fc.g, fc.b, end);
    }
}
