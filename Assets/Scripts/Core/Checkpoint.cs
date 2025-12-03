using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public bool isActive = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GameManager.Instance.SetCheckpoint(transform);

        isActive = true;

        Debug.Log("Checkpoint Activated: " + transform.position +
                  " | Saved Countdown Time = " +
                  GameManager.Instance.savedCountdownTime);
    }
}
