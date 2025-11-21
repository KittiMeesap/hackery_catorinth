using UnityEngine;

public enum MissionType { CollectItem, HackTarget, KillTarget, ReachLocation, Interact }

[CreateAssetMenu(fileName = "Mission", menuName = "Mission/Single Mission")]
public class MissionDataSO : ScriptableObject
{
    [Header("Text")]
    [TextArea]
    public string description;
    public MissionType missionType = MissionType.CollectItem;
    [Min(1)] public int requiredAmount = 1;

    [Header("Identification (DO NOT change at runtime)")]
    public string missionId;

    [Header("Target (Optional)")]
    public string targetMissionId;
}
