using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsNavigationController : MonoBehaviour
{
    public enum RowType { DisplayMode, Resolution, MasterVolume, MusicVolume, SfxVolume }

    [Header("Rows")]
    public Image[] rowImages;
    public RowType[] rowTypes;

    [Header("Row Colors")]
    public Color normalRowColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Color selectedRowColor = new Color(1f, 0.9f, 0.6f, 1f);

    [Header("Scroll View")]
    public ScrollRect scrollRect;
    public bool autoScroll = true;

    [Header("Display Mode & Resolution")]
    public TextMeshProUGUI displayModeValueText;
    public TextMeshProUGUI resolutionValueText;

    [Header("Sliders")]
    public Slider masterSlider;
    public TextMeshProUGUI masterValueText;
    public Slider musicSlider;
    public TextMeshProUGUI musicValueText;
    public Slider sfxSlider;
    public TextMeshProUGUI sfxValueText;

    [Header("Volume Step")]
    public float volumeStep = 0.01f;

    [Header("Confirm Panel")]
    public GameObject confirmPanel;
    public Button confirmSaveButton;
    public Button cancelBackButton;

    // Input
    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    private int currentRowIndex = 0;

    private SettingsData data;
    private Resolution[] resolutions;
    private int displayModeIndex;
    private int resolutionIndex;

    private bool isDirty = false;

    private readonly string[] displayModeNames = { "Windowed", "Borderless", "Fullscreen" };

    // HOLD LOGIC
    private float holdTimer = 0f;
    private int holdDirection = 0;

    public float holdDelay = 0.25f;
    public float repeatRate = 0.06f;

    private void OnEnable()
    {
        GameInput.Instance.SetModeUI();

        var map = GameInput.Instance.PlayerInputComponent.actions.FindActionMap("UI");
        navigateAction = map.FindAction("Navigate");
        submitAction = map.FindAction("Submit");
        cancelAction = map.FindAction("Cancel");

        navigateAction.performed += OnNavigate;
        submitAction.performed += OnSubmit;
        cancelAction.performed += OnCancel;

        LoadSettings();
        RefreshAllUI();

        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        masterSlider.onValueChanged.AddListener(_ => OnVolumeChanged());
        musicSlider.onValueChanged.AddListener(_ => OnVolumeChanged());
        sfxSlider.onValueChanged.AddListener(_ => OnVolumeChanged());
    }

    private void OnDisable()
    {
        if (navigateAction != null) navigateAction.performed -= OnNavigate;
        if (submitAction != null) submitAction.performed -= OnSubmit;
        if (cancelAction != null) cancelAction.performed -= OnCancel;

        masterSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();
    }

    private void Update()
    {
        if (holdDirection != 0)
        {
            holdTimer -= Time.unscaledDeltaTime;
            if (holdTimer <= 0f)
            {
                AdjustCurrentRow(holdDirection);
                holdTimer = repeatRate;
            }
        }
    }

    private void LoadSettings()
    {
        resolutions = Screen.resolutions;
        if (resolutions.Length == 0)
            resolutions = new Resolution[] { Screen.currentResolution };

        data = SettingsSaveManager.Load() ?? new SettingsData
        {
            displayMode = 0,
            resolutionIndex = resolutions.Length - 1,
            masterVolume = 1,
            musicVolume = 1,
            sfxVolume = 1
        };

        displayModeIndex = Mathf.Clamp(data.displayMode, 0, displayModeNames.Length - 1);
        resolutionIndex = Mathf.Clamp(data.resolutionIndex, 0, resolutions.Length - 1);

        masterSlider.value = data.masterVolume;
        musicSlider.value = data.musicVolume;
        sfxSlider.value = data.sfxVolume;
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (confirmPanel != null && confirmPanel.activeSelf) return;

        Vector2 nav = ctx.ReadValue<Vector2>();

        // vertical navigation
        if (nav.y > 0.5f) MoveRow(-1);
        else if (nav.y < -0.5f) MoveRow(+1);

        // horizontal adjust
        if (Mathf.Abs(nav.x) > 0.5f)
        {
            holdDirection = nav.x > 0 ? +1 : -1;
            holdTimer = holdDelay;

            AdjustCurrentRow(holdDirection);
        }
        else
        {
            holdDirection = 0;
        }
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (confirmPanel != null && confirmPanel.activeSelf) return;

        SaveSettings();
        ApplyGraphicsSettings();
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        if (confirmPanel != null && confirmPanel.activeSelf)
        {
            confirmPanel.SetActive(false);
            return;
        }

        if (!isDirty)
        {
            CloseSettingsPanel();
            return;
        }

        confirmPanel.SetActive(true);

        confirmSaveButton.onClick.RemoveAllListeners();
        confirmSaveButton.onClick.AddListener(() =>
        {
            SaveSettings();
            ApplyGraphicsSettings();
            confirmPanel.SetActive(false);
            CloseSettingsPanel();
        });

        cancelBackButton.onClick.RemoveAllListeners();
        cancelBackButton.onClick.AddListener(() =>
        {
            confirmPanel.SetActive(false);
        });
    }

    private void MoveRow(int delta)
    {
        currentRowIndex = Mathf.Clamp(currentRowIndex + delta, 0, rowImages.Length - 1);
        RefreshRowHighlight();
        UpdateScrollPosition();
    }

    private void AdjustCurrentRow(int dir)
    {
        switch (rowTypes[currentRowIndex])
        {
            case RowType.DisplayMode:
                displayModeIndex = Mathf.Clamp(displayModeIndex + dir, 0, displayModeNames.Length - 1);
                isDirty = true;
                RefreshDisplayTexts();
                break;

            case RowType.Resolution:
                resolutionIndex = Mathf.Clamp(resolutionIndex + dir, 0, resolutions.Length - 1);
                isDirty = true;
                RefreshDisplayTexts();
                break;

            case RowType.MasterVolume:
                masterSlider.value = Mathf.Clamp01(masterSlider.value + dir * volumeStep);
                isDirty = true;
                RefreshVolumeTexts();
                break;

            case RowType.MusicVolume:
                musicSlider.value = Mathf.Clamp01(musicSlider.value + dir * volumeStep);
                isDirty = true;
                RefreshVolumeTexts();
                break;

            case RowType.SfxVolume:
                sfxSlider.value = Mathf.Clamp01(sfxSlider.value + dir * volumeStep);
                isDirty = true;
                RefreshVolumeTexts();
                break;
        }
    }

    private void RefreshRowHighlight()
    {
        for (int i = 0; i < rowImages.Length; i++)
            rowImages[i].color = (i == currentRowIndex) ? selectedRowColor : normalRowColor;
    }

    private void UpdateScrollPosition()
    {
        if (!autoScroll || rowImages.Length <= 1) return;

        float t = 1f - (float)currentRowIndex / (rowImages.Length - 1);
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(t);
    }

    private void RefreshDisplayTexts()
    {
        displayModeValueText.text = displayModeNames[displayModeIndex];

        Resolution r = resolutions[resolutionIndex];
        resolutionValueText.text = $"{r.width} x {r.height}";
    }

    private void OnVolumeChanged()
    {
        isDirty = true;
        RefreshVolumeTexts();
    }

    private void RefreshVolumeTexts()
    {
        masterValueText.text = Mathf.RoundToInt(masterSlider.value * 100).ToString();
        musicValueText.text = Mathf.RoundToInt(musicSlider.value * 100).ToString();
        sfxValueText.text = Mathf.RoundToInt(sfxSlider.value * 100).ToString();
    }

    private void RefreshAllUI()
    {
        RefreshDisplayTexts();
        RefreshVolumeTexts();
        RefreshRowHighlight();
        UpdateScrollPosition();
    }

    private void SaveSettings()
    {
        data.displayMode = displayModeIndex;
        data.resolutionIndex = resolutionIndex;
        data.masterVolume = masterSlider.value;
        data.musicVolume = musicSlider.value;
        data.sfxVolume = sfxSlider.value;

        SettingsSaveManager.Save(data);
        isDirty = false;
    }

    private void ApplyGraphicsSettings()
    {
        Resolution r = resolutions[resolutionIndex];
        FullScreenMode mode = displayModeIndex switch
        {
            0 => FullScreenMode.Windowed,
            1 => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.ExclusiveFullScreen
        };

        Screen.SetResolution(r.width, r.height, mode);
    }

    private void CloseSettingsPanel()
    {
        gameObject.SetActive(false);

        var main = FindFirstObjectByType<MainMenu>();
        if (main != null)
        {
            main.SetUILocked(false);
            main.mainPanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(main.firstSelectedMain);
        }
    }
}
