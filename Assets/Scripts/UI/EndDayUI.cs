using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndDayUI : MonoBehaviour
{
    [Header("Root")]
    public CanvasGroup canvasGroup;
    public GameObject root;

    [Header("Title")]
    public TextMeshProUGUI titleText;

    [Header("Day Panel")]
    public TextMeshProUGUI dayText;          // "Day:"
    public TextMeshProUGUI dayCurrentText;   // e.g. "1"

    [Header("Time Panel")]
    public TextMeshProUGUI timePlayedText;   // "Time Played:"
    public TextMeshProUGUI timeText;         // "8:00 Hours"

    [Header("Order Panel")]
    public TextMeshProUGUI ordersCompletedText; // "Orders Completed:"
    public TextMeshProUGUI ordersText;          // "20/20"

    [Header("Star Panel")]
    public TextMeshProUGUI starsRemainingText; // "Stars Remaining:"
    public Image[] starIcons;                  // 5 slots (full or empty)

    [Header("Buttons")]
    public Button nextDayButton;
    public Button mainMenuButton;

    [Header("Star Sprites")]
    public Sprite starFull;
    public Sprite starEmpty;

    private void Awake()
    {
        HideImmediate();

        nextDayButton.onClick.AddListener(OnNextDayClicked);
        mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    /// <summary>
    /// Show summary at end of day.
    /// </summary>
    public void ShowSummary(
        bool success,
        int dayIndex,
        int ordersDone,
        int ordersTarget,
        int starsLeft,
        int maxStars,
        float hoursPlayed,
        bool hasNextDay
    )
    {
        root.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // ----- Title -----
        titleText.text = success ? "Day Summary" : "Day Failed";

        // ----- Day -----
        dayCurrentText.text = dayIndex.ToString();

        // ----- Time -----
        timeText.text = $"{hoursPlayed:0.00} Hours";

        // ----- Orders -----
        ordersText.text = $"{ordersDone}/{ordersTarget}";

        // ----- Stars -----
        RefreshStars(starsLeft, maxStars);

        // ----- Buttons -----
        if (success && hasNextDay)
            nextDayButton.GetComponentInChildren<TextMeshProUGUI>().text = "Next Day";
        else if (!success && hasNextDay)
            nextDayButton.GetComponentInChildren<TextMeshProUGUI>().text = "Retry";
        else
            nextDayButton.GetComponentInChildren<TextMeshProUGUI>().text = "Finish";

        nextDayButton.interactable = true;
        mainMenuButton.interactable = true;
    }

    private void RefreshStars(int starsLeft, int maxStars)
    {
        for (int i = 0; i < starIcons.Length; i++)
        {
            if (i < starsLeft)
                starIcons[i].sprite = starFull;
            else
                starIcons[i].sprite = starEmpty;

            starIcons[i].enabled = i < maxStars;
        }
    }

    public void HideImmediate()
    {
        root.SetActive(false);
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnNextDayClicked()
    {
        GameManager.Instance.OnEndDayNextButton();
    }

    private void OnMainMenuClicked()
    {
        GameManager.Instance.OnEndDayQuitButton();
    }
}
