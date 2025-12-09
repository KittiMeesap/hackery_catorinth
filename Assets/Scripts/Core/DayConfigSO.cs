using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DayData/DayConfig")]
public class DayConfigSO : ScriptableObject
{
    public int dayIndex;
    public int targetOrders;
    public int startHour24;
}
