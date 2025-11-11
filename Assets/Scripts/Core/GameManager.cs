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

    [Header("Mission")]
    public MissionSetSO missionSetForScene;

    [Header("Countdown Settings")]
    public float countdownTime = 180f;

    public Color warningColor = Color.red;

    public float warningThreshold = 10f;

    [Header("UI Reference")]
    public TextMeshProUGUI countdownText;

    [Header("Enemy Spawn Settings")]
    public GameObject sweeperPrefab;

    public Transform sweeperSpawnPoint;

    public Transform[] sweeperDoorTargets;

    private bool timerRunning = false;
    private bool sweeperSpawned = false;
    private PlayerInput playerInput;
    private InputAction exitAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            exitAction = playerInput.actions["ExitGame"];
        }
    }

    private void OnEnable()
    {
        if (exitAction != null)
            exitAction.performed += OnExitPressed;

        ResetCountdown();
        StartCountdown();
    }

    private void OnDisable()
    {
        if (exitAction != null)
            exitAction.performed -= OnExitPressed;
    }

    private void Update()
    {
        time += Time.deltaTime;

        if (timerRunning && countdownTime > 0)
        {
            countdownTime -= Time.deltaTime;
            UpdateCountdownUI();

            if (countdownTime <= 0 && !sweeperSpawned)
            {
                countdownTime = 0;
                timerRunning = false;
                SpawnSweeper();
            }
        }
    }

    private void UpdateCountdownUI()
    {
        if (!countdownText) return;

        int minutes = Mathf.FloorToInt(countdownTime / 60);
        int seconds = Mathf.FloorToInt(countdownTime % 60);
        countdownText.text = $"{minutes:00}:{seconds:00}";

        if (countdownTime <= warningThreshold)
            countdownText.color = warningColor;
        else
            countdownText.color = Color.white;
    }

    private void SpawnSweeper()
    {
        if (!sweeperPrefab || !sweeperSpawnPoint)
        {
            Debug.LogWarning("[GameManager] Missing Sweeper Prefab or Spawn Point.");
            return;
        }

        GameObject sweeperObj = Instantiate(sweeperPrefab, sweeperSpawnPoint.position, Quaternion.identity);
        EnemySweeper sweeper = sweeperObj.GetComponent<EnemySweeper>();

        if (sweeper != null && sweeperDoorTargets != null && sweeperDoorTargets.Length > 0)
        {
            sweeper.doorTargets = sweeperDoorTargets;
        }

        sweeperSpawned = true;

        AudioManager.Instance?.PlaySFX("SFX_Sweeper_Spawn");
    }

    public void ResetCountdown()
    {
        sweeperSpawned = false;
        timerRunning = false;
    }

    public void StartCountdown() => timerRunning = true;
    public void StopCountdown() => timerRunning = false;

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

    public void SetPhoneOut(bool isOut) => IsPhoneOut = isOut;
    public void ToggleHackingMode(bool isActive) => IsInHackingMode = isActive;
}
