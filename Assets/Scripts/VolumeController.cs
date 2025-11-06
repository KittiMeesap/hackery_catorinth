using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    [Header("References")]
    public AudioMixer audioMixer;
    public Slider masterSlider;
    //public Slider musicSlider;
    //public Slider sfxSlider;
    //public Slider ambientSlider;

    private void Start()
    {
        InitSlider(masterSlider, "MasterVolume", 1f);
        //InitSlider(musicSlider, "MusicVolume", 0.8f);
        //InitSlider(sfxSlider, "SFXVolume", 0.8f);
        //InitSlider(ambientSlider, "AmbientVolume", 0.7f);

        if (masterSlider) masterSlider.onValueChanged.AddListener(v => SetVolume("MasterVolume", v));
        //if (musicSlider)  musicSlider.onValueChanged.AddListener(v => SetVolume("MusicVolume", v));
        //if (sfxSlider)    sfxSlider.onValueChanged.AddListener(v => SetVolume("SFXVolume", v));
        //if (ambientSlider)ambientSlider.onValueChanged.AddListener(v => SetVolume("AmbientVolume", v));
    }

    private void InitSlider(Slider slider, string key, float defaultValue)
    {
        if (!slider) return;
        float saved = PlayerPrefs.GetFloat(key, defaultValue);
        slider.value = saved;
        SetVolume(key, saved);
    }

    private void SetVolume(string exposedParam, float value)
    {
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(exposedParam, dB);
        PlayerPrefs.SetFloat(exposedParam, value);
    }
}
