using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TutorialPopup : MonoBehaviour
{
    [Header("UI")]
    public GameObject popupPanel;
    public VideoPlayer videoPlayer;
    public Button closeButton;
    public GameObject firstSelectedButton;

    [Header("Input System")]
    public InputActionReference cancelAction;

    private bool isOpen = false;

    private void Awake()
    {
        popupPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePopup);
    }

    private void OnEnable()
    {
        if (cancelAction != null)
            cancelAction.action.performed += OnCancelPerformed;
    }

    private void OnDisable()
    {
        if (cancelAction != null)
            cancelAction.action.performed -= OnCancelPerformed;
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (isOpen)
            ClosePopup();
    }

    public void OpenPopup()
    {
        if (isOpen) return;
        isOpen = true;

        popupPanel.SetActive(true);

        var input = FindFirstObjectByType<PlayerInput>();
        if (input != null)
            input.SwitchCurrentActionMap("UI");

        GameManager.Instance.FreezeGame(true);

        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.Play();

        if (firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    public void ClosePopup()
    {
        if (!isOpen) return;
        isOpen = false;

        videoPlayer.Stop();
        popupPanel.SetActive(false);

        GameManager.Instance.FreezeGame(false);

        var input = FindFirstObjectByType<PlayerInput>();
        if (input != null)
            input.SwitchCurrentActionMap("Player");

        EventSystem.current.SetSelectedGameObject(null);
    }
}
