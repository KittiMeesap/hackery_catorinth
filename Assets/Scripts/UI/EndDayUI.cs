using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum EndDayResult
{
    WinDay,
    LoseDay,
    WeekComplete
}

public class EndDayUI : MonoBehaviour
{
    [Header("Root")]
    public CanvasGroup canvasGroup;
    public GameObject root;

    [Header("Title")]
    public TextMeshProUGUI titleText;

    [Header("Detail Text")]
    public TextMeshProUGUI dayCurrentText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI ordersText;

    [Header("Star Panel")]
    public Image[] starIcons;
    public Sprite starFull;
    public Sprite starEmpty;

    [Header("Status")]
    public TextMeshProUGUI statusText;
    public Image characterImage;

    [Header("Character Sprites")]
    public Sprite winSprite;
    public Sprite loseSprite;
    public Sprite weekCompleteSprite;

    [Header("Buttons")]
    public Button nextDayButton;
    public Button mainMenuButton;

    [Header("Navigation")]
    public GameObject firstSelected;

    [Header("Button Hover Scale")]
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1f);

    [Header("SFX Keys")]
    public string openKey = "UI_EndDay_Open";      // ?? ??? UI ???? (??????????)
    public string hoverKey = "UI_Hover";           // ?? Hover
    public string clickKey = "SFX_UI_Submit";      // ? ????????? (???????????)

    private EndDayResult currentResult;
    private GameObject lastSelected;
    private bool uiLocked;
    private bool hasPlayedOpenSFX;

    private void Awake()
    {
        HideImmediate();

        nextDayButton.onClick.AddListener(OnNextDayClicked);
        mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void Update()
    {
        if (!root.activeSelf || uiLocked)
            return;

        var current = EventSystem.current.currentSelectedGameObject;

        if (current != lastSelected)
        {
            if (lastSelected != null)
                lastSelected.transform.localScale = Vector3.one;

            if (current != null)
            {
                current.transform.localScale = hoverScale;
                PlayHover();
            }

            lastSelected = current;
        }
    }

    // =========================
    // SHOW
    // =========================
    public void ShowSummary(
        EndDayResult result,
        int dayIndex,
        int ordersDone,
        int ordersTarget,
        int starsLeft,
        int maxStars,
        float hoursPlayed
    )
    {
        currentResult = result;
        uiLocked = false;
        hasPlayedOpenSFX = false;

        root.SetActive(true);

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        // ?? UI OPEN (??????????????)
        PlayOpen();

        titleText.text = "Day Summary";
        dayCurrentText.text = dayIndex.ToString();
        timeText.text = $"{hoursPlayed:0.00} Hours";
        ordersText.text = $"{ordersDone}/{ordersTarget}";

        RefreshStars(starsLeft, maxStars);
        RefreshStatus(result);
        RefreshButtons(result);

        GameInput.Instance.SetModeUI();

        EventSystem.current.SetSelectedGameObject(null);

        Button targetButton = nextDayButton.interactable
            ? nextDayButton
            : mainMenuButton;

        EventSystem.current.SetSelectedGameObject(targetButton.gameObject);
    }

    // =========================
    // INTERNAL
    // =========================
    private void RefreshStars(int starsLeft, int maxStars)
    {
        for (int i = 0; i < starIcons.Length; i++)
        {
            starIcons[i].enabled = i < maxStars;
            starIcons[i].sprite = i < starsLeft ? starFull : starEmpty;
        }
    }

    private void RefreshStatus(EndDayResult result)
    {
        switch (result)
        {
            case EndDayResult.WinDay:
                statusText.text = "Great job! The customers loved it!";
                characterImage.sprite = winSprite;
                break;

            case EndDayResult.LoseDay:
                statusText.text = "The day didn’t go as planned...";
                characterImage.sprite = loseSprite;
                break;

            case EndDayResult.WeekComplete:
                statusText.text = "You made it through the week!";
                characterImage.sprite = weekCompleteSprite;
                break;
        }

        characterImage.enabled = characterImage.sprite != null;
    }

    private void RefreshButtons(EndDayResult result)
    {
        var label = nextDayButton.GetComponentInChildren<TextMeshProUGUI>();

        label.text = result switch
        {
            EndDayResult.WinDay => "Next Day",
            EndDayResult.LoseDay => "Retry Day",
            EndDayResult.WeekComplete => "Next Area",
            _ => label.text
        };

        nextDayButton.gameObject.SetActive(true);
        nextDayButton.interactable = true;
        mainMenuButton.interactable = true;
    }

    // =========================
    // BUTTONS
    // =========================
    private void OnNextDayClicked()
    {
        if (uiLocked) return;
        uiLocked = true;

        PlayClick();
        GameManager.Instance.OnEndDayNextButton(currentResult);
    }

    private void OnMainMenuClicked()
    {
        if (uiLocked) return;
        uiLocked = true;

        PlayClick();
        GameManager.Instance.OnEndDayQuitButton();
    }

    // =========================
    // VISIBILITY
    // =========================
    public void HideImmediate()
    {
        root.SetActive(false);
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        lastSelected = null;
        hasPlayedOpenSFX = false;
    }

    // =========================
    // ?? SFX
    // =========================
    private void PlayOpen()
    {
        if (hasPlayedOpenSFX) return;
        hasPlayedOpenSFX = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(openKey);
    }

    private void PlayHover()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(hoverKey);
    }

    private void PlayClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clickKey); // ? SFX_UI_Submit
    }
}
