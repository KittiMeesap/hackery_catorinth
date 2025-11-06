using UnityEngine;
using UnityEngine.Localization;

public enum MissionType { CollectItem, HackTarget, KillTarget, ReachLocation, Interact }

[CreateAssetMenu(fileName = "Mission", menuName = "Mission/Single Mission")]
public class MissionDataSO : ScriptableObject
{
    [Header("Text")]
    public LocalizedString description;
    public MissionType missionType = MissionType.CollectItem;
    [Min(1)] public int requiredAmount = 1;

    [Header("Identification (DO NOT change at runtime)")]
    [Tooltip("Stable ID for this mission step (e.g., HACK_FRIDGE_1).")]
    public string missionId;

    [Header("Target (Optional)")]
    [Tooltip("Link to a MissionTarget in scene via the same missionId")]
    public string targetMissionId;

    public async System.Threading.Tasks.Task<string> GetDescriptionAsync()
    {
        var handle = description.GetLocalizedStringAsync();
        await handle.Task;
        return handle.Result;
    }
}
