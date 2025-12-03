using UnityEngine;

public class HackableQuestBinder : MonoBehaviour
{
    public string missionId;

    public void OnHacked()
    {
        MissionManager.Instance?.MarkHackComplete(missionId);
    }

    public bool IsUnlocked() => MissionManager.Instance && MissionManager.Instance.IsActiveMission(missionId);
}
