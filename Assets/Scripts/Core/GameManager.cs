using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public TimeOfDaySystem timeSystem;
    public MichelinStarSystem starSystem;
    public EndDayUI endDayUI;
    public OrderGoalSystem orderGoalSystem;
    public OrderUI orderUI;

    [Header("HUD")]
    public TextMeshProUGUI dayLabel;

    private DayConfigSO currentDayConfig;
    private bool dayRunning = false;

    private void Awake()
    {
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
        currentDayConfig = config;
        dayRunning = true;

        // Reset stars
        starSystem.ResetStars();

        // Setup order goal
        orderGoalSystem.Initialize(config.targetOrders);

        // Setup UI
        if (orderUI != null)
            orderUI.Initialize(config.targetOrders);

        // Reset time
        timeSystem.StartDay(config.startHour24);

        // HUD
        dayLabel.text = $"Day: {config.dayIndex}";

        endDayUI.HideImmediate();

        // Input
        QTEManager.Instance.input.Player.Enable();
        QTEManager.Instance.input.UI.Disable();
    }

    // END DAY
    private void EndDay(bool success, string reason)
    {
        dayRunning = false;
        timeSystem.StopDay();

        QTEManager.Instance.input.Player.Disable();
        QTEManager.Instance.input.UI.Enable();

        bool hasNextDay = DayManager.Instance.HasNextDay();

        endDayUI.ShowSummary(
            success,
            currentDayConfig.dayIndex,
            orderGoalSystem.CompletedOrders,
            orderGoalSystem.TargetOrders,
            starSystem.CurrentStars,
            starSystem.maxStars,
            timeSystem.CurrentPlayedHours,
            hasNextDay
        );
    }

    // EVENT: Time finished
    private void OnDayTimeOver()
    {
        if (!dayRunning) return;

        if (orderGoalSystem.IsGoalReached())
            EndDay(true, "Completed orders just in time!");
        else
            EndDay(false, "Out of time");
    }

    // ORDER EVENTS
    public void RegisterOrderSuccess()
    {
        if (!dayRunning) return;

        orderGoalSystem.AddOrderSuccess();

        if (orderUI != null)
            orderUI.SetValue(orderGoalSystem.CompletedOrders);

        if (orderGoalSystem.IsGoalReached())
            EndDay(true, "All orders completed!");
    }

    public void RegisterOrderFail()
    {
        if (!dayRunning) return;

        starSystem.LoseStar(1);

        if (starSystem.CurrentStars <= 0)
            EndDay(false, "Lost all stars");
    }

    // END DAY BUTTONS
    public void OnEndDayNextButton()
    {
        bool success = orderGoalSystem.IsGoalReached();

        if (success)
        {
            if (DayManager.Instance.HasNextDay())
                DayManager.Instance.StartNextDay();
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // finish
        }
        else
        {
            DayManager.Instance.RestartCurrentDay(); // retry same day
        }
    }

    public void OnEndDayQuitButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
