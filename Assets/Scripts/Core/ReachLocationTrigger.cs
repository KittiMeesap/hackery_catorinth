using UnityEngine;

public class ReachLocationTrigger : MonoBehaviour
{
    [SerializeField] private string missionId;

    [SerializeField] private bool oneShot = true;
    private bool consumed;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        if (string.IsNullOrWhiteSpace(missionId))
            missionId = gameObject.name.ToUpperInvariant();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed && oneShot) return;
        if (!other.CompareTag("Player")) return;

        var mm = MissionManager.Instance;
        if (mm == null) return;

        if (mm.IsActiveMission(missionId))
        {
            consumed = true;
            mm.MarkReachComplete(missionId);
        }
    }
}
