using UnityEngine;
using TMPro;

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

    [Header("Enemy Spawn Settings")]
    public GameObject sweeperPrefab;

    public Transform sweeperSpawnPoint;

    public Transform[] sweeperDoorTargets;

    private float currentTime;
    private bool isRunning = false;
    private bool spawned = false;
    private bool warned = false;
    private Vector3 defaultScale;

    private void Start()
    {
        if (timerText != null)
            defaultScale = timerText.transform.localScale;

        ResetTimer();
        StartCountdown();
    }

    private void Update()
    {
        if (!isRunning) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            if (currentTime < 0) currentTime = 0;
            UpdateTimerUI();
        }
        else if (!spawned)
        {
            isRunning = false;
            spawned = true;
            OnTimeOver();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";

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
        CancelInvoke(nameof(PlayWarningBeep));
        AudioManager.Instance?.PlaySFX(timeOverSFXKey);

        Debug.Log("[CountdownTimer] Time's up!");

        if (sweeperPrefab && sweeperSpawnPoint)
        {
            GameObject obj = Instantiate(sweeperPrefab, sweeperSpawnPoint.position, Quaternion.identity);
            var sweeper = obj.GetComponent<EnemySweeper>();

            if (sweeper != null && sweeperDoorTargets != null && sweeperDoorTargets.Length > 0)
                sweeper.doorTargets = sweeperDoorTargets;

            Debug.Log("[CountdownTimer] EnemySweeper spawned at " + sweeperSpawnPoint.name);
        }
        else
        {
            Debug.LogWarning("[CountdownTimer] SweeperPrefab or SpawnPoint not assigned.");
        }
    }

    public void ResetTimer()
    {
        currentTime = startTime;
        spawned = false;
        warned = false;
        UpdateTimerUI();
    }

    public void StartCountdown() => isRunning = true;
    public void StopCountdown() => isRunning = false;
}
