using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI displayModeText;
    public TextMeshProUGUI resolutionText;
    public TextMeshProUGUI masterText;
    public TextMeshProUGUI musicText;
    public TextMeshProUGUI sfxText;

    [Header("Input System")]
    public InputActionReference submitAction;

    [Header("Hold Settings")]
    public float initialDelay = 0.3f;
    public float repeatRate = 0.05f;

    private float holdTimer = 0f;
    private bool isHolding = false;

    private string[] displayModes = { "Full screen", "Windowed" };
    private string[] resolutions =
    {
        "1920 x 1080",
        "1600 x 900",
        "1366 x 768",
        "1280 x 720"
    };

    private int displayIndex;
    private int resolutionIndex;
    private int masterVol;
    private int musicVol;
    private int sfxVol;

    private void Start()
    {
        LoadSettings();
        ApplyToUI();
    }

    private void Update()
    {
        HandleHoldSystem();
    }

    // HOLD SYSTEM
    private void HandleHoldSystem()
    {
        bool submitHeld = submitAction != null && submitAction.action.IsPressed();
        bool mouseHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;

        GameObject currentButton = EventSystem.current.currentSelectedGameObject;

        if (currentButton == null) return;

        if (submitHeld || mouseHeld)
        {
            if (!isHolding)
            {
                isHolding = true;
                holdTimer = 0f;
                ActivateButton(currentButton);
            }
            else
            {
                holdTimer += Time.unscaledDeltaTime;

                if (holdTimer >= initialDelay)
                {
                    ActivateButton(currentButton);
                    holdTimer -= repeatRate;
                }
            }
        }
        else
        {
            isHolding = false;
        }
    }

    private void ActivateButton(GameObject buttonObj)
    {
        Button b = buttonObj.GetComponent<Button>();
        if (b != null)
            b.onClick.Invoke();
    }

    // LOAD / SAVE
    public void LoadSettings()
    {
        displayIndex = PlayerPrefs.GetInt("DisplayMode", 0);
        resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", 0);

        masterVol = Mathf.RoundToInt(PlayerPrefs.GetFloat("MasterVol", 1f) * 100f);
        musicVol = Mathf.RoundToInt(PlayerPrefs.GetFloat("MusicVol", 1f) * 100f);
        sfxVol = Mathf.RoundToInt(PlayerPrefs.GetFloat("SFXVol", 1f) * 100f);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("DisplayMode", displayIndex);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);

        PlayerPrefs.SetFloat("MasterVol", masterVol / 100f);
        PlayerPrefs.SetFloat("MusicVol", musicVol / 100f);
        PlayerPrefs.SetFloat("SFXVol", sfxVol / 100f);

        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(masterVol / 100f);
            AudioManager.Instance.SetMusicVolume(musicVol / 100f);
            AudioManager.Instance.SetSFXVolume(sfxVol / 100f);
        }

        ApplyDisplayMode();
        ApplyResolution();
    }

    private void ApplyDisplayMode()
    {
        Screen.fullScreenMode = displayIndex == 0
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
    }

    private void ApplyResolution()
    {
        string[] parts = resolutions[resolutionIndex].Split('x');

        int w = int.Parse(parts[0]);
        int h = int.Parse(parts[1]);

        Screen.SetResolution(w, h, Screen.fullScreenMode);
    }

    // UI UPDATE
    private void ApplyToUI()
    {
        displayModeText.text = displayModes[displayIndex];
        resolutionText.text = resolutions[resolutionIndex];

        masterText.text = masterVol.ToString();
        musicText.text = musicVol.ToString();
        sfxText.text = sfxVol.ToString();
    }

    // SETTINGS BUTTON EVENTS
    public void DisplayLeft()
    {
        displayIndex--;
        if (displayIndex < 0) displayIndex = displayModes.Length - 1;
        displayModeText.text = displayModes[displayIndex];
    }

    public void DisplayRight()
    {
        displayIndex++;
        if (displayIndex >= displayModes.Length) displayIndex = 0;
        displayModeText.text = displayModes[displayIndex];
    }

    public void ResolutionLeft()
    {
        resolutionIndex--;
        if (resolutionIndex < 0) resolutionIndex = resolutions.Length - 1;
        resolutionText.text = resolutions[resolutionIndex];
    }

    public void ResolutionRight()
    {
        resolutionIndex++;
        if (resolutionIndex >= resolutions.Length) resolutionIndex = 0;
        resolutionText.text = resolutions[resolutionIndex];
    }

    private int ClampVol(int v) => Mathf.Clamp(v, 0, 100);

    public void MasterInc() { masterVol = ClampVol(masterVol + 1); masterText.text = masterVol.ToString(); }
    public void MasterDec() { masterVol = ClampVol(masterVol - 1); masterText.text = masterVol.ToString(); }

    public void MusicInc() { musicVol = ClampVol(musicVol + 1); musicText.text = musicVol.ToString(); }
    public void MusicDec() { musicVol = ClampVol(musicVol - 1); musicText.text = musicVol.ToString(); }

    public void SFXInc() { sfxVol = ClampVol(sfxVol + 1); sfxText.text = sfxVol.ToString(); }
    public void SFXDec() { sfxVol = ClampVol(sfxVol - 1); sfxText.text = sfxVol.ToString(); }
}
