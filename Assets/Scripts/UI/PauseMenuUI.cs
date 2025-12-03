using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public GameObject defaultPauseButton;
    public GameObject defaultSettingButton;

    [Header("Scene Navigation")]
    public string homeSceneName = "MainMenu";

    private bool isPaused = false;

    [Header("Input")]
    public InputActionReference pauseAction;

    private PlayerInput playerInput;
    private const string MAP_GAMEPLAY = "Player";
    private const string MAP_UI = "UI";

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
            Debug.LogError("PlayerInput missing on PauseMenuUI.");
    }

    private void OnEnable()
    {
        pauseAction.action.performed += OnPausePressed;
        pauseAction.action.Enable();
    }

    private void OnDisable()
    {
        pauseAction.action.performed -= OnPausePressed;
        pauseAction.action.Disable();
    }

    private void OnPausePressed(InputAction.CallbackContext ctx)
    {
        if (settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    // PAUSE / RESUME
    public void PauseGame()
    {
        Time.timeScale = 0f;
        isPaused = true;

        pausePanel.SetActive(true);
        settingsPanel.SetActive(false);

        playerInput.SwitchCurrentActionMap(MAP_UI);

        StartCoroutine(SelectButtonNextFrame(defaultPauseButton));
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        isPaused = false;

        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);

        playerInput.SwitchCurrentActionMap(MAP_GAMEPLAY);
    }

    // SETTINGS
    public void OpenSettings()
    {
        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);

        StartCoroutine(SelectButtonNextFrame(defaultSettingButton));
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        pausePanel.SetActive(true);

        StartCoroutine(SelectButtonNextFrame(defaultPauseButton));
    }

    // BUTTONS
    public void ResetLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }

    // UI Helper
    private IEnumerator SelectButtonNextFrame(GameObject button)
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button);
    }
}
