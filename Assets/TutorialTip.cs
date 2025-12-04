using System.Collections;
using UnityEngine;
using TMPro;

public class TutorialTipUI : MonoBehaviour
{
    public static TutorialTipUI Instance;

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text tipText;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 10f;
    [SerializeField] private float fadeDuration = 0.4f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        Instance = this;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public static void Show(string message, float duration = -1f)
    {
        if (Instance == null) return;

        Instance.ShowTip(message, duration);
    }

    private void ShowTip(string msg, float duration)
    {
        if (duration <= 0) duration = displayDuration;

        tipText.text = msg;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        gameObject.SetActive(true);
        currentRoutine = StartCoroutine(TipRoutine(duration));
    }

    private IEnumerator TipRoutine(float duration)
    {
        // FADE IN
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        // WAIT
        yield return new WaitForSeconds(duration);

        // FADE OUT
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        currentRoutine = null;
    }
}
