using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    public Image fadeImage;
    public float fadeDuration = 0.6f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage != null)
            fadeImage.gameObject.SetActive(false);
    }

    public IEnumerator FadeOut()
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(0f, 1f, t / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, a);
            yield return null;
        }
    }

    public IEnumerator FadeIn()
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(1f, 0f, t / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, a);
            yield return null;
        }

        fadeImage.gameObject.SetActive(false);
        fadeImage.raycastTarget = false;
    }
}
