using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [Header("Display Mode")]
    [SerializeField] private TMP_Dropdown displayDropdown;

    [Header("Resolution")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    //START
    void Start()
    {
        SetupDisplayMode();
        SetupResolutionDropdown();
        SetupVolumeSliders();
    }

    //DISPLAY MODE
    void SetupDisplayMode()
    {
        displayDropdown.ClearOptions();
        displayDropdown.AddOptions(new List<string>
        {
            "Fullscreen",
            "Windowed"
        });

        int savedMode = PlayerPrefs.GetInt("DisplayMode", 0);
        displayDropdown.value = savedMode;

        ApplyDisplayMode(savedMode);

        displayDropdown.onValueChanged.AddListener(ApplyDisplayMode);
    }

    void ApplyDisplayMode(int index)
    {
        if (index == 0)
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        else
            Screen.fullScreenMode = FullScreenMode.Windowed;

        PlayerPrefs.SetInt("DisplayMode", index);
    }

    //  RESOLUTION (CUSTOM PRESET)
    void SetupResolutionDropdown()
    {
        resolutionDropdown.ClearOptions();

        List<string> presetResolutions = new List<string>
        {
            "1920x1080",  // Full HD
            "1600x900",
            "1366x768",   // Laptop
            "1280x720",   // HD
        };

        resolutionDropdown.AddOptions(presetResolutions);

        int savedIndex = PlayerPrefs.GetInt("ResolutionIndex", 0);
        resolutionDropdown.value = savedIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(ApplyCustomResolution);

        ApplyCustomResolution(savedIndex);
    }

    void ApplyCustomResolution(int index)
    {
        string res = resolutionDropdown.options[index].text;

        string[] parts = res.Split('x', 'X');

        if (parts.Length == 2 &&
            int.TryParse(parts[0], out int width) &&
            int.TryParse(parts[1], out int height))
        {
            Screen.SetResolution(width, height, Screen.fullScreenMode);
            PlayerPrefs.SetInt("ResolutionIndex", index);

            Debug.Log($"Resolution Set: {width}x{height}");
        }
        else
        {
            Debug.LogError("Invalid resolution format: " + res);
        }
    }

    //VOLUME
    void SetupVolumeSliders()
    {
        masterSlider.value = PlayerPrefs.GetFloat("MasterVol", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVol", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVol", 1f);

        masterSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetMasterVolume(v));
        musicSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetMusicVolume(v));
        sfxSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetSFXVolume(v));
    }
}
