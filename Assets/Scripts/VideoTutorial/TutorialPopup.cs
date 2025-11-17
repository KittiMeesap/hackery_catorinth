using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialPopup : MonoBehaviour
{
    [Header("UI")]
    public GameObject popupPanel;
    public VideoPlayer videoPlayer;
    public Button closeButton;

    private string prefKey;

    public void Initialize()
    {
       
        prefKey = "TutorialSeen_" + gameObject.GetInstanceID();

        popupPanel.SetActive(false);
        closeButton.onClick.AddListener(ClosePopup);
    }

    public bool HasSeenTutorial()
    {
        return PlayerPrefs.GetInt(prefKey, 0) == 1;
    }

    public void OpenPopup()
    {
        popupPanel.SetActive(true);
        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    public void ClosePopup()
    {
        popupPanel.SetActive(false);
        PlayerPrefs.SetInt(prefKey, 1);
        PlayerPrefs.Save();
    }
}
