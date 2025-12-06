using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialPopup : MonoBehaviour
{
    [Header("UI")]
    public GameObject popupPanel;
    public VideoPlayer videoPlayer;
    public Button closeButton;

    public void Initialize()
    {
        popupPanel.SetActive(false);
        closeButton.onClick.AddListener(ClosePopup);
    }

    public void OpenPopup()
    {
        popupPanel.SetActive(true);

        
        GameManager.Instance.FreezeGame(true);

        
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.time = 0f;
        videoPlayer.Play();
    }

    public void ClosePopup()
    {
        videoPlayer.Stop();
        popupPanel.SetActive(false);

        
        GameManager.Instance.FreezeGame(false);
    }
}
