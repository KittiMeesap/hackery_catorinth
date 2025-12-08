using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DayConfig")]
public class DayConfigSO : ScriptableObject
{
    public int dayIndex;
    public int targetOrders;
    public int startHour24;
}
