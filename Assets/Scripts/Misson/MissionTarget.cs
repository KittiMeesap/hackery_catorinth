using UnityEngine;

public class MissionTarget : MonoBehaviour
{
    [Tooltip("Must match MissionDataSO.targetMissionId")]
    public string missionId;

    private void OnEnable() => MissionManager.Instance?.RegisterTarget(missionId, this);
    private void OnDisable() => MissionManager.Instance?.UnregisterTarget(missionId, this);
}
