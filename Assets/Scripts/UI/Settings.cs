using System.Collections;
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

    void Start()
    {
        StartCoroutine(DelayedInit());
    }

    IEnumerator DelayedInit()
    {
        yield return null; 

        SetupDisplayMode();
        SetupResolutionDropdown();
        SetupVolumeSliders();
    }

    void SetupDisplayMode()
    {
        displayDropdown.ClearOptions();
        displayDropdown.AddOptions(new List<string> { "Fullscreen", "Windowed" });

        int savedMode = PlayerPrefs.GetInt("DisplayMode", 0);
        displayDropdown.value = savedMode;

        ApplyDisplayMode(savedMode);

        displayDropdown.onValueChanged.AddListener(ApplyDisplayMode);
    }

    void ApplyDisplayMode(int index)
    {
        Screen.fullScreenMode = index == 0
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        PlayerPrefs.SetInt("DisplayMode", index);
    }

    void SetupResolutionDropdown()
    {
        resolutionDropdown.ClearOptions();

        List<string> presetRes = new()
        {
            "1920x1080",
            "1600x900",
            "1366x768",
            "1280x720"
        };

        resolutionDropdown.AddOptions(presetRes);

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
            int.TryParse(parts[0], out int w) &&
            int.TryParse(parts[1], out int h))
        {
            Screen.SetResolution(w, h, Screen.fullScreenMode);
            PlayerPrefs.SetInt("ResolutionIndex", index);
        }
    }

    void SetupVolumeSliders()
    {
        
        masterSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();

        masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MasterVol", 1f));
        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVol", 1f));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVol", 1f));

        
        masterSlider.onValueChanged.AddListener(v =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMasterVolume(v);
        });

        musicSlider.onValueChanged.AddListener(v =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(v);
        });

        sfxSlider.onValueChanged.AddListener(v =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSFXVolume(v);
        });
    }
}
