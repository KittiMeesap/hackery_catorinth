using System.Collections.Generic;
using UnityEngine;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance;

    [Header("Day Configs")]
    public List<DayConfigSO> days;

    private int currentDayIndex = 0;

    private void Awake()
    {
        Instance = this;
    }

    // RESET
    public void ResetToDayOne()
    {
        currentDayIndex = 0;
        Debug.Log("[DayManager] ResetToDayOne");
    }

    // START
    public void StartFirstDay()
    {
        if (days == null || days.Count == 0)
        {
            Debug.LogError(
                "[DayManager] No DayConfigSO assigned! Check Inspector."
            );
            return;
        }

        currentDayIndex = 0;
        StartCurrentDay();
    }

    public void StartNextDay()
    {
        currentDayIndex++;
        StartCurrentDay();
    }

    public void RestartCurrentDay()
    {
        StartCurrentDay();
    }

    private void StartCurrentDay()
    {
        if (days == null || days.Count == 0)
        {
            Debug.LogError("[DayManager] Days list is empty!");
            return;
        }

        if (currentDayIndex < 0 || currentDayIndex >= days.Count)
        {
            Debug.LogError(
                $"[DayManager] Invalid day index {currentDayIndex} / {days.Count}"
            );
            return;
        }

        Debug.Log($"[DayManager] Start Day {days[currentDayIndex].dayIndex}");
        GameManager.Instance.StartDay(days[currentDayIndex]);
    }

    public bool HasNextDay()
    {
        return days != null && currentDayIndex + 1 < days.Count;
    }
}
