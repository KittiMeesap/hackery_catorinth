using UnityEngine;

[CreateAssetMenu(menuName = "Game/Day Config")]
public class DayConfigSO : ScriptableObject
{
    [Header("Display")]
    public int dayIndex = 1;

    [Header("Order Goal")]
    public int targetOrders = 10;

    [Header("Time of Day")]
    [Tooltip("24 hour like  8 = 8.00 AM, 20 = 8.00 PM")]
    [Range(0, 23)]
    public int startHour24 = 8;
}
