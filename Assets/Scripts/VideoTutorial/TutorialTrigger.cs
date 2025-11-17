using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    public Transform player;
    public float triggerDistance = 1.5f;

    public TutorialPopup popup;             // VideoPanel ? tutorial script
    public GameObject replayButtonWorld;    // button above this object
    public string tutorialID = "Object1";   // unique ID for each object

    private bool triggered = false;

    void Start()
    {
        popup.Initialize(tutorialID);

        // Hide replay button until watched first time
        if (!popup.HasSeenTutorial())
            replayButtonWorld?.SetActive(false);
        else
            replayButtonWorld?.SetActive(true);
    }

    void Update()
    {
        if (triggered) return;
        if (popup.HasSeenTutorial()) return;

        float dist = Vector2.Distance(player.position, transform.position);

        if (dist <= triggerDistance)
        {
            triggered = true;
            popup.replayButtonWorld = replayButtonWorld;
            popup.OpenPopup();
        }
    }
}
