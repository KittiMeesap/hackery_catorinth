using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI Menus")]
    public GameObject pausePanel;
    public GameObject defaultSelectedButton;

    public string homeSceneName = "MainMenu";

    private bool isPaused = false;

    [Header("Input References")]
    public InputActionReference pauseAction;

    private PlayerInput playerInput;
    private const string gameplayMap = "Player";
    private const string uiMap = "UI";

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
            Debug.LogError("PlayerInput is missing.");
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPausePerformed;
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (pausePanel == null) return;

        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        playerInput.SwitchCurrentActionMap(uiMap);

        StartCoroutine(SetDefaultButtonNextFrame());
    }

    private IEnumerator SetDefaultButtonNextFrame()
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(defaultSelectedButton);
    }


    public void ResumeGame()
    {
        if (pausePanel == null) return;

        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        playerInput.SwitchCurrentActionMap(gameplayMap);

    }

    public void ResetGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        isPaused = false;
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    public void GoHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }
}
