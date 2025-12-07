using System;
using UnityEngine;
using TMPro;

public class TimeOfDaySystem : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI timeLabel;

    [Header("Config")]
    public float dayLengthGameHours = 8f;
    public float realSecondsPerGameHour = 37.5f;

    public bool IsRunning { get; private set; }

    public event Action OnDayTimeFinished;

    private float elapsedGameHours = 0f;
    private int startHour24 = 8;

    public float CurrentPlayedHours => elapsedGameHours;

    public void StartDay(int startHour24)
    {
        this.startHour24 = Mathf.Clamp(startHour24, 0, 23);
        elapsedGameHours = 0f;
        IsRunning = true;
        UpdateTimeLabel();
    }

    public void StopDay() => IsRunning = false;

    private void Update()
    {
        if (!IsRunning) return;

        elapsedGameHours += Time.deltaTime / realSecondsPerGameHour;

        if (elapsedGameHours >= dayLengthGameHours)
        {
            elapsedGameHours = dayLengthGameHours;
            UpdateTimeLabel();
            IsRunning = false;
            OnDayTimeFinished?.Invoke();
            return;
        }

        UpdateTimeLabel();
    }

    private void UpdateTimeLabel()
    {
        if (timeLabel == null) return;

        float currentHour = startHour24 + elapsedGameHours;
        int totalMinutes = Mathf.FloorToInt(currentHour * 60);

        int hour24 = (totalMinutes / 60) % 24;
        int minute = totalMinutes % 60;

        string ampm = hour24 >= 12 ? "PM" : "AM";
        int hour12 = hour24 % 12;
        if (hour12 == 0) hour12 = 12;

        timeLabel.text = $"{hour12:00}:{minute:00} {ampm}";
    }
}
