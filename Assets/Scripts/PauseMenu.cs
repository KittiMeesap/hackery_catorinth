using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("First Selected (Pause only)")]
    public GameObject firstSelectedPause;

    [Header("Button Hover Scale")]
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1f);
    private GameObject lastSelectedButton;

    [Header("SFX Keys")]
    public string hoverKey = "UI_Hover";
    public string clickKey = "UI_Click";

    private InputAction pauseAction;

    private void Start()
    {
        IsPaused = false;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (GameInput.Instance == null)
        {
            Debug.LogError("PauseMenu: GameInput.Instance is null");
            return;
        }

        pauseAction = GameInput.Instance.PauseAction;
        if (pauseAction != null)
            pauseAction.performed += OnPausePressed;
        else
            Debug.LogError("PauseMenu: PauseAction is null (check action name/map).");
    }

    private void OnDestroy()
    {
        if (pauseAction != null)
            pauseAction.performed -= OnPausePressed;
    }

    private void Update()
    {
        if (!IsPaused || pausePanel == null || !pausePanel.activeSelf)
            return;

        if (EventSystem.current == null)
            return;

        var current = EventSystem.current.currentSelectedGameObject;

        if (current != lastSelectedButton)
        {
            if (lastSelectedButton != null)
                lastSelectedButton.transform.localScale = Vector3.one;

            if (current != null)
            {
                current.transform.localScale = hoverScale;
                PlayHover();
            }

            lastSelectedButton = current;
        }
    }

    private void OnPausePressed(InputAction.CallbackContext ctx)
    {
        if (IsPaused && settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (!IsPaused) OpenPause();
        else ClosePause();
    }

    private void OpenPause()
    {
        IsPaused = true;

        if (pausePanel != null) pausePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Time.timeScale = 0f;
        GameInput.Instance.SetModeUI();

        if (EventSystem.current != null && firstSelectedPause != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedPause);
        }

        lastSelectedButton = null; // reset hover state
    }

    private void ClosePause()
    {
        IsPaused = false;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Time.timeScale = 1f;
        GameInput.Instance.SetModePlayer();

        // reset scale
        if (lastSelectedButton != null)
            lastSelectedButton.transform.localScale = Vector3.one;

        lastSelectedButton = null;
    }

    //  Buttons 

    public void OnResumePressed()
    {
        PlayClick();
        ClosePause();
    }

    public void OnMainMenuPressed()
    {
        PlayClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OpenSettings()
    {
        PlayClick();

        if (!IsPaused) OpenPause();

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);

    }

    public void CloseSettings()
    {
        PlayClick();

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);

        if (EventSystem.current != null && firstSelectedPause != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedPause);
        }

        lastSelectedButton = null;
    }

    //  SFX 
    private void PlayHover()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(hoverKey))
            AudioManager.Instance.PlaySFX(hoverKey);
    }

    private void PlayClick()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(clickKey))
            AudioManager.Instance.PlaySFX(clickKey);
    }
}
