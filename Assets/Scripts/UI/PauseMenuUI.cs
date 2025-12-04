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
        // ดึง PlayerInput จากผู้เล่น ไม่ต้องใส่ไว้บน UI
        playerInput = FindFirstObjectByType<PlayerInput>();

        if (playerInput == null)
            Debug.LogError("No PlayerInput found in the scene.");
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPausePressed;
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePressed;
            pauseAction.action.Disable();
        }
    }

    private void OnPausePressed(InputAction.CallbackContext ctx)
    {
        // ถ้าอยู่ในหน้า Settings ให้กลับไปหน้า Pause ก่อน
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

    // -----------------------
    // PAUSE / RESUME LOGIC
    // -----------------------
    public void PauseGame()
    {
        Time.timeScale = 0f;
        isPaused = true;

        pausePanel.SetActive(true);
        settingsPanel.SetActive(false);

        // เปลี่ยน Action Map เป็น UI
        playerInput.SwitchCurrentActionMap(MAP_UI);

        StartCoroutine(SelectButtonNextFrame(defaultPauseButton));
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        isPaused = false;

        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);

        // กลับไป Action Map ของ Player
        playerInput.SwitchCurrentActionMap(MAP_GAMEPLAY);
    }

    // -----------------------
    // SETTINGS MENU
    // -----------------------
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

    // -----------------------
    // BUTTON EVENTS
    // -----------------------
    public void ResetLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoHome()
    {
        Time.timeScale = 1f;
        isPaused = false;

        SceneManager.LoadScene(homeSceneName);
    }

    // -----------------------
    // UI SELECTOR (Controller Support)
    // -----------------------
    private IEnumerator SelectButtonNextFrame(GameObject button)
    {
        yield return null;

        if (EventSystem.current != null && button != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(button);
        }
    }
}
