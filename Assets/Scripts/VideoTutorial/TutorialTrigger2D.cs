using UnityEngine;

public class TutorialTrigger2D : MonoBehaviour
{
    [Header("Popup Reference")]
    public TutorialPopup popup;

    private bool triggered = false;

    private void Start()
    {
        if (popup == null)
            Debug.LogWarning("TutorialTrigger2D: Popup reference is missing!");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggered) return;

        if (collision.CompareTag("Player"))
        {
            triggered = true;

            if (popup != null)
                popup.OpenPopup();
        }
    }
}
