using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialPopup : MonoBehaviour
{
    [Header("UI")]
    public GameObject popupPanel;     // VideoPanel
    public VideoPlayer videoPlayer;
    public Button closeButton;

    [Header("Replay Button")]
    public GameObject replayButtonWorld; // small button above object

    private string prefKey;  // unique per object
    private bool initialized = false;

    public void Initialize(string key)
    {
        prefKey = key;
        initialized = true;
        popupPanel.SetActive(false);

        closeButton.onClick.AddListener(ClosePopup);
    }

    public void OpenPopup()
    {
        if (!initialized) return;

        // Hide replay button
        if (replayButtonWorld != null)
            replayButtonWorld.SetActive(false);

        popupPanel.SetActive(true);
        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    public void ClosePopup()
    {
        if (!initialized) return;

        popupPanel.SetActive(false);

        // Save that user has seen the tutorial for this object
        PlayerPrefs.SetInt(prefKey, 1);
        PlayerPrefs.Save();

        // Show replay button
        if (replayButtonWorld != null)
            replayButtonWorld.SetActive(true);
    }

    public bool HasSeenTutorial()
    {
        return PlayerPrefs.HasKey(prefKey);
    }
}
