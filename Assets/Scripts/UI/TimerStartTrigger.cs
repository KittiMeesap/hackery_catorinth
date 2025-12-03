using UnityEngine;

public class TimerStartTrigger : MonoBehaviour
{
    [Header("Reference")]
    public CountdownTimer timer;

    [Header("Trigger Settings")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered && triggerOnce) return;

        if (other.CompareTag("Player"))
        {
            if (timer != null)
            {
                timer.StartCountdown();
                Debug.Log("[TimerStartTrigger] Timer Started!");
            }

            hasTriggered = true;
        }
    }
}
