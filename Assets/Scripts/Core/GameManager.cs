using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    EndingDay,
    EndDayUI
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    [Header("References")]
    public TimeOfDaySystem timeSystem;
    public MichelinStarSystem starSystem;
    public EndDayUI endDayUI;
    public OrderGoalSystem orderGoalSystem;
    public OrderUI orderUI;
    [SerializeField] private CustomerQueueManager queueManager;

    [Header("HUD")]
    public TextMeshProUGUI dayLabel;

    private DayConfigSO currentDayConfig;
    private bool dayRunning = false;

    private int spawnedCustomersToday;
    private int failedOrdersToday;

    public DayConfigSO CurrentDayConfig => currentDayConfig;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        timeSystem.OnDayTimeFinished += OnDayTimeOver;
        DayManager.Instance.StartFirstDay();
    }

    // START DAY
    public void StartDay(DayConfigSO config)
    {
        Time.timeScale = 1f;
        queueManager?.ClearAllCustomers();
        InteractStation.interactionLocked = false;

        CurrentState = GameState.Playing;
        currentDayConfig = config;
        dayRunning = true;

        spawnedCustomersToday = 0;
        failedOrdersToday = 0;

        starSystem.ResetStars();
        orderGoalSystem.Initialize(config.targetOrders);
        orderUI?.Initialize(config.targetOrders);

        timeSystem.StartDay(config.startHour24);
        if (dayLabel != null) dayLabel.text = $"Day: {config.dayIndex}";

        endDayUI.HideImmediate();
        GameInput.Instance.SetModePlayer();

        SetupDayDifficulty();
    }

    public void SetupDayDifficulty()
    {
        if (queueManager == null)
        {
            Debug.LogError("GameManager: CustomerQueueManager not assigned");
            return;
        }

        queueManager.SetupForDay(
            currentDayConfig.targetOrders,
            currentDayConfig.dayIndex,
            timeSystem.dayLengthGameHours
        );
    }

    public void NotifyCustomerSpawned()
    {
        spawnedCustomersToday++;
        ApplyDynamicDifficulty();
    }

    private void ApplyDynamicDifficulty()
    {
        if (spawnedCustomersToday < 3) return;

        float playedHours = Mathf.Max(0.1f, timeSystem.CurrentPlayedHours);

        float playerOrdersPerHour = orderGoalSystem.CompletedOrders / playedHours;
        float requiredOrdersPerHour = currentDayConfig.targetOrders / timeSystem.dayLengthGameHours;

        queueManager.ApplyAdaptiveDifficulty(playerOrdersPerHour, requiredOrdersPerHour);
    }

    public void RegisterOrderSuccess()
    {
        if (CurrentState != GameState.Playing)
            return;

        orderGoalSystem.AddOrderSuccess();
        orderUI?.SetValue(orderGoalSystem.CompletedOrders);

        if (orderGoalSystem.IsGoalReached())
            EndDay(GetWinResult());
    }

    public void RegisterOrderFail(CustomerPersonality personality)
    {
        if (CurrentState != GameState.Playing)
            return;

        failedOrdersToday++;

        int baseLose = personality == CustomerPersonality.VIP ? 2 : 1;
        int dayPenalty = Mathf.FloorToInt(currentDayConfig.dayIndex * 0.2f);
        int loseAmount = Mathf.Clamp(baseLose + dayPenalty, 1, 3);

        starSystem.LoseStar(loseAmount);

        if (starSystem.CurrentStars <= 0)
            EndDay(EndDayResult.LoseDay);
    }

    private void EndDay(EndDayResult result)
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.EndingDay;
        dayRunning = false;

        Time.timeScale = 0f;

        timeSystem.StopDay();
        ServiceTrigger.Instance?.ClearCurrentCustomer();
        queueManager?.ClearAllCustomers();
        InteractStation.interactionLocked = false;

        GameInput.Instance.SetModeUI();

        endDayUI.ShowSummary(
            result,
            currentDayConfig.dayIndex,
            orderGoalSystem.CompletedOrders,
            orderGoalSystem.TargetOrders,
            starSystem.CurrentStars,
            starSystem.maxStars,
            timeSystem.CurrentPlayedHours
        );

        CurrentState = GameState.EndDayUI;
    }

    private void OnDayTimeOver()
    {
        if (!dayRunning) return;

        EndDay(orderGoalSystem.IsGoalReached()
            ? GetWinResult()
            : EndDayResult.LoseDay);
    }

    private EndDayResult GetWinResult()
    {
        return DayManager.Instance.HasNextDay()
            ? EndDayResult.WinDay
            : EndDayResult.WeekComplete;
    }

    public void OnEndDayNextButton(EndDayResult result)
    {
        Time.timeScale = 1f;

        switch (result)
        {
            case EndDayResult.WinDay:
                DayManager.Instance.StartNextDay();
                break;
            case EndDayResult.LoseDay:
                DayManager.Instance.RestartCurrentDay();
                break;
            case EndDayResult.WeekComplete:
                SceneManager.LoadScene("EndDemo");
                break;
        }
    }

    public void OnEndDayQuitButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
