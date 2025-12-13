using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum NotifyType
{
    Info,
    Warning,
    Error
}

public class NotificationUI : MonoBehaviour
{
    public static NotificationUI Instance;

    [Header("UI")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI messageText;
    public RectTransform messageRoot;

    [Header("Timing")]
    public float showDuration = 1.5f;
    public float fadeTime = 0.15f;

    [Header("Colors")]
    public Color infoColor = Color.white;
    public Color warningColor = new Color(1f, 0.85f, 0.3f);
    public Color errorColor = new Color(1f, 0.35f, 0.35f);

    [Header("Juice")]
    public float shakeStrength = 8f;
    public float shakeDuration = 0.25f;
    public int flashCount = 2;

    private Vector2 originalPos;

    private Coroutine displayRoutine;
    private Coroutine shakeRoutine;

    private Queue<(string, NotifyType)> queue = new();
    private string currentMessage = "";

    private void Awake()
    {
        Instance = this;

        if (messageRoot != null)
            originalPos = messageRoot.anchoredPosition;

        HideImmediate();
    }

    // PUBLIC API
    public void Show(string message, NotifyType type = NotifyType.Info)
    {
        if (canvasGroup.alpha > 0.9f && message == currentMessage)
        {
            RestartShake(type);
            return;
        }

        queue.Enqueue((message, type));

        if (displayRoutine == null)
            displayRoutine = StartCoroutine(DisplayQueue());
    }

    // CORE LOOP
    private IEnumerator DisplayQueue()
    {
        while (queue.Count > 0)
        {
            var (msg, type) = queue.Dequeue();
            currentMessage = msg;

            yield return ShowRoutine(msg, type);
        }

        currentMessage = "";
        displayRoutine = null;
    }

    private IEnumerator ShowRoutine(string message, NotifyType type)
    {
        messageText.text = message;
        messageText.color = GetColor(type);

        yield return Fade(0f, 1f);

        RestartShake(type);

        if (type == NotifyType.Error)
            StartCoroutine(Flash());

        yield return new WaitForSecondsRealtime(showDuration);

        yield return Fade(1f, 0f);
    }

    // JUICE
    private void RestartShake(NotifyType type)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        if (type == NotifyType.Warning || type == NotifyType.Error)
            shakeRoutine = StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float x = Random.Range(-1f, 1f) * shakeStrength;
            float y = Random.Range(-1f, 1f) * shakeStrength;
            messageRoot.anchoredPosition = originalPos + new Vector2(x, y);
            yield return null;
        }

        messageRoot.anchoredPosition = originalPos;
    }

    private IEnumerator Flash()
    {
        for (int i = 0; i < flashCount; i++)
        {
            messageText.enabled = false;
            yield return new WaitForSecondsRealtime(0.05f);
            messageText.enabled = true;
            yield return new WaitForSecondsRealtime(0.08f);
        }
    }

    // =================================================
    // HELPERS
    // =================================================
    private Color GetColor(NotifyType type)
    {
        return type switch
        {
            NotifyType.Warning => warningColor,
            NotifyType.Error => errorColor,
            _ => infoColor
        };
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        canvasGroup.blocksRaycasts = false;

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private void HideImmediate()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        if (messageRoot != null)
            messageRoot.anchoredPosition = originalPos;
    }
}
