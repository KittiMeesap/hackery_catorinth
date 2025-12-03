using UnityEngine;

public class DoorDebug : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[DoorDebug] OnTriggerEnter2D → {other.name}");
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Debug.Log($"[DoorDebug] OnTriggerStay2D → {other.name}");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"[DoorDebug] OnTriggerExit2D → {other.name}");
    }
}