using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MissionSet", menuName = "Mission/Mission Set")]
public class MissionSetSO : ScriptableObject
{
    public List<MissionDataSO> missions;
}
