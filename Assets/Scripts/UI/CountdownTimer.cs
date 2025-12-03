using UnityEngine;
using TMPro;
using UnityEngine.Playables;

public class CountdownTimer : MonoBehaviour
{
    [Header("Countdown Settings")]
    public float startTime = 60f;

    [Header("UI Reference")]
    public TextMeshProUGUI timerText;

    [Header("Color Settings")]
    public Color normalColor = Color.white;
    public Color warningColor = Color.red;
    public float warningThreshold = 10f;

    [Header("Blink Effect")]
    public float blinkSpeed = 5f;
    public float blinkScale = 1.2f;

    [Header("Audio Settings")]
    public string warningSFXKey = "SFX_TimerWarning";
    public string timeOverSFXKey = "SFX_TimerEnd";

    [Header("Cinematic Settings")]
    public PlayableDirector sweeperIntroDirector;

    private float currentTime;
    private bool isRunning = false;
    private bool warned = false;
    private bool forceStopped = false;
    private Vector3 defaultScale;

    private void Start()
    {
        if (timerText != null)
            defaultScale = timerText.transform.localScale;

        ResetTimer();
    }

    private void Update()
    {
        if (!isRunning || forceStopped) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            if (currentTime < 0) currentTime = 0;
            UpdateTimerUI();
        }
        else
        {
            if (isRunning)
            {
                isRunning = false;
                OnTimeOver();
            }
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";

        if (forceStopped) return;

        if (currentTime <= warningThreshold)
        {
            timerText.color = warningColor;

            if (!warned)
            {
                warned = true;
                AudioManager.Instance?.PlaySFX(warningSFXKey);
                InvokeRepeating(nameof(PlayWarningBeep), 0f, 1f);
            }

            float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;
            float scale = Mathf.Lerp(1f, blinkScale, t);
            timerText.transform.localScale = defaultScale * scale;

            Color c = timerText.color;
            c.a = Mathf.Lerp(0.4f, 1f, t);
            timerText.color = c;
        }
        else
        {
            timerText.color = normalColor;
            timerText.transform.localScale = defaultScale;
        }
    }

    private void PlayWarningBeep()
    {
        if (forceStopped)  // ?? NEW
        {
            CancelInvoke(nameof(PlayWarningBeep));
            return;
        }

        if (currentTime > 0 && currentTime <= warningThreshold)
        {
            AudioManager.Instance?.PlaySFX(warningSFXKey);
        }
        else
        {
            CancelInvoke(nameof(PlayWarningBeep));
        }
    }

    private void OnTimeOver()
    {
        if (forceStopped) return;

        CancelInvoke(nameof(PlayWarningBeep));
        AudioManager.Instance?.PlaySFX(timeOverSFXKey);

        if (sweeperIntroDirector)
            sweeperIntroDirector.Play();
    }

    public void ResetTimer()
    {
        CancelInvoke(nameof(PlayWarningBeep));
        AudioManager.Instance?.StopAllSFX();

        forceStopped = false;

        currentTime = startTime;
        warned = false;

        if (timerText != null)
            timerText.transform.localScale = defaultScale;

        UpdateTimerUI();
    }

    public void ReduceTime(float amount)
    {
        currentTime -= amount;
        if (currentTime < 0)
            currentTime = 0;

        UpdateTimerUI();
        PlayDamageFlash();
    }

    public void StartCountdown()
    {
        forceStopped = false;
        isRunning = true;
    }

    public void StopCountdown()
    {
        CancelInvoke(nameof(PlayWarningBeep));
        AudioManager.Instance?.StopAllSFX();
        isRunning = false;
    }

    public void ForceStopAllTimerAudio()
    {
        forceStopped = true;
        CancelInvoke(nameof(PlayWarningBeep));
        AudioManager.Instance?.StopAllSFX();
        isRunning = false;
    }

    public void PlayDamageFlash()
    {
        if (timerText == null || forceStopped) return;
        StartCoroutine(DamageFlashRoutine());
    }

    private System.Collections.IEnumerator DamageFlashRoutine()
    {
        float shakeTime = 0.25f;

        Vector3 originalPos = timerText.rectTransform.localPosition;
        Color originalColor = timerText.color;

        float t = 0f;
        while (t < shakeTime)
        {
            t += Time.deltaTime * 4f;

            float shakeStrength = 5f * (1f - (t / shakeTime));
            timerText.rectTransform.localPosition =
                originalPos + (Vector3)Random.insideUnitCircle * shakeStrength;

            float f = Mathf.PingPong(t * 6f, 1f);
            Color c = Color.Lerp(originalColor, Color.red, f);
            timerText.color = c;

            yield return null;
        }

        timerText.rectTransform.localPosition = originalPos;
        timerText.color = originalColor;
    }

    public float GetCurrentTime()
    {
        return currentTime;
    }

    public void SetTime(float t)
    {
        currentTime = Mathf.Max(0, t);
        UpdateTimerUI();
    }


}
