using System.Collections.Generic;
using UnityEngine;

public class MissionGate : MonoBehaviour
{
    [Tooltip("This object becomes usable only when this missionId is ACTIVE")]
    public string missionId;

    [Header("Toggle Targets")]
    public List<Behaviour> behavioursToEnable;
    public List<Collider2D> collidersToEnable;
    public List<GameObject> gameObjectsToEnable;

    [Header("Feedback")]
    public bool showLockedHint = true;

    private void Awake()
    {
        SetLocked(true);
        MissionManager.OnMissionChanged += HandleMissionChanged;
    }
    private void OnDestroy()
    {
        MissionManager.OnMissionChanged -= HandleMissionChanged;
    }

    private void HandleMissionChanged(string newActiveMissionId)
    {
        bool unlocked = !string.IsNullOrEmpty(missionId) && missionId == newActiveMissionId;
        SetLocked(!unlocked);
    }

    private void SetLocked(bool locked)
    {
        if (behavioursToEnable != null)
            foreach (var b in behavioursToEnable) if (b) b.enabled = !locked;

        if (collidersToEnable != null)
            foreach (var c in collidersToEnable) if (c) c.enabled = !locked;

        if (gameObjectsToEnable != null)
            foreach (var g in gameObjectsToEnable) if (g) g.SetActive(!locked);
    }
}
