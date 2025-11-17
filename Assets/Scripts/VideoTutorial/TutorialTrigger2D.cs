using UnityEngine;

public class TutorialTrigger2D : MonoBehaviour
{
    [Header("Popup Reference")]
    public TutorialPopup popup;   

    private bool triggered = false;

    private void Start()
    {
        popup.Initialize();

        if (popup.HasSeenTutorial())
            triggered = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggered) return;

       
        if (collision.CompareTag("Player"))
        {
            triggered = true;
            popup.OpenPopup();
        }
    }
}
