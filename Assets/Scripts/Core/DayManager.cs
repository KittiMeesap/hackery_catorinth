using UnityEngine;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Configs")]
    public DayConfigSO[] dayConfigs;

    private int currentDayIndex = 0;

    public DayConfigSO CurrentDay => dayConfigs[currentDayIndex];
    public int CurrentDayNumber => CurrentDay.dayIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartFirstDay()
    {
        UnlockSaveManager.Instance.Load();

        int dayToLoad = UnlockSaveManager.Instance.Data.lastUnlockedDay;
        currentDayIndex = dayToLoad - 1;

        UnlockManager.Instance.ApplyUnlocksForDay(dayToLoad);

        GameManager.Instance.StartDay(dayConfigs[currentDayIndex]);
    }

    public void StartNextDay()
    {
        if (currentDayIndex + 1 < dayConfigs.Length)
        {
            currentDayIndex++;

            UnlockManager.Instance.ApplyUnlocksForDay(CurrentDay.dayIndex);
            GameManager.Instance.StartDay(CurrentDay);
        }
        else
        {
            Debug.Log("Game completed.");
        }
    }

    public void RestartCurrentDay()
    {
        GameManager.Instance.StartDay(CurrentDay);
    }

    public bool HasNextDay()
    {
        return currentDayIndex + 1 < dayConfigs.Length;
    }

    public int GetNextDayIndex()
    {
        return HasNextDay() ? dayConfigs[currentDayIndex + 1].dayIndex : CurrentDay.dayIndex;
    }
}
