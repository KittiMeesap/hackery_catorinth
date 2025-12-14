using UnityEngine;

[CreateAssetMenu(menuName = "DayData/DayConfig")]
public class DayConfigSO : ScriptableObject
{
    public int dayIndex;
    public int targetOrders;
    public int startHour24;

    [Header("Difficulty")]
    [Range(0f, 1f)]
    public float vipSpawnChance = 0.1f;

    [Tooltip("1 = netural, <1 = angry fast")]
    public float waitTimeMultiplier = 1f;
}
