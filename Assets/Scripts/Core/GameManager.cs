using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game States")]
    public bool IsPhoneOut { get; private set; } = false;
    public bool IsInHackingMode { get; private set; } = false;
    public float time = 0;

    [Header("Mission Settings")]
    public MissionSetSO missionSetForScene;

    [Header("UI References")]
    public TextMeshProUGUI missionText;

    [Header("External References")]
    public CountdownTimer countdownManager;

    [Header("Screen Fade")]
    public ScreenFader screenFader;

    private PlayerInput playerInput;
    private InputAction exitAction;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
            exitAction = playerInput.actions["ExitGame"];
    }

    private void Start()
    {
        if (screenFader != null)
            StartCoroutine(screenFader.FadeIn());
    }

    private void OnEnable()
    {
        if (exitAction != null)
            exitAction.performed += OnExitPressed;
    }

    private void OnDisable()
    {
        if (exitAction != null)
            exitAction.performed -= OnExitPressed;
    }

    private void Update()
    {
        time += Time.deltaTime;
    }

    public void StartCountdown()
    {
        countdownManager?.StartCountdown();
    }

    public void StopCountdown()
    {
        countdownManager?.StopCountdown();
    }

    private void OnExitPressed(InputAction.CallbackContext context)
    {
        QuitGame();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetPhoneOut(bool isOut)
    {
        IsPhoneOut = isOut;
    }

    public void ToggleHackingMode(bool isActive)
    {
        IsInHackingMode = isActive;
    }

    public void FreezeGame(bool freeze)
    {
        if (freeze)
        {
            Time.timeScale = 0f;

            if (playerInput != null)
                playerInput.enabled = false;
        }
        else
        {
            Time.timeScale = 1f;

            if (playerInput != null)
                playerInput.enabled = true;
        }
    }
}
