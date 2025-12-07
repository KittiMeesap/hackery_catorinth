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
        currentDayIndex = 0;
        GameManager.Instance.StartDay(CurrentDay);
    }

    public void StartNextDay()
    {
        if (currentDayIndex + 1 < dayConfigs.Length)
        {
            currentDayIndex++;
            GameManager.Instance.StartDay(CurrentDay);
        }
        else
        {
            Debug.Log("No next day. Game completed.");
        }
    }

    public bool HasNextDay()
    {
        return currentDayIndex + 1 < dayConfigs.Length;
    }

    public void RestartCurrentDay()
    {
        GameManager.Instance.StartDay(CurrentDay);
    }
}
