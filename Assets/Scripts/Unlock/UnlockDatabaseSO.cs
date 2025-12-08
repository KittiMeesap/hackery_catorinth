using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Unlock/Unlock Database")]
public class UnlockDatabaseSO : ScriptableObject
{
    [System.Serializable]
    public class DayUnlocks
    {
        public int dayIndex;
        public List<UnlockEntrySO> unlocks;
    }

    public List<DayUnlocks> dayUnlockTable;
}
