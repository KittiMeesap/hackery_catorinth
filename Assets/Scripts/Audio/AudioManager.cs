using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Sound Library")]
    [SerializeField] private SoundLibrary soundLibrary;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    [SerializeField] private List<AudioSource> sfxPool = new();
    [SerializeField] private AudioSource uiSource;

    [Header("Volume (0-1)")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Crossfade Settings")]
    [SerializeField] private float bgmCrossfadeTime = 1.5f;

    private AudioSource _currentMusicSource;
    private string currentBGMKey = "";
    private Coroutine bgmCrossfadeRoutine;

    private Camera _mainCam;
    private Plane[] _frustumPlanes;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ensure UI source exists
        if (uiSource == null)
            uiSource = gameObject.AddComponent<AudioSource>();

        if (musicSourceA == null || musicSourceB == null)
            Debug.LogError("[AudioManager] Missing music sources!");

        _currentMusicSource = musicSourceA;

        LoadVolume();
        ApplyVolumes();
    }

    private void Update()
    {
        if (_mainCam == null) _mainCam = Camera.main;
        if (_mainCam != null)
            _frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCam);
    }

    // ======================================================================
    // BGM
    // ======================================================================
    public void PlayBGM(string key, bool crossfade = true)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (currentBGMKey == key && _currentMusicSource.isPlaying)
            return;

        AudioClip clip = GetClipSafe(key);
        if (clip == null) return;

        currentBGMKey = key;

        if (crossfade)
        {
            if (bgmCrossfadeRoutine != null)
                StopCoroutine(bgmCrossfadeRoutine);

            bgmCrossfadeRoutine = StartCoroutine(CrossfadeBGM(clip, bgmCrossfadeTime));
        }
        else
        {
            _currentMusicSource.Stop();
            _currentMusicSource.clip = clip;
            _currentMusicSource.loop = true;
            _currentMusicSource.volume = musicVolume * masterVolume;
            _currentMusicSource.Play();
        }
    }

    private IEnumerator CrossfadeBGM(AudioClip newClip, float fadeTime)
    {
        if (newClip == null) yield break;

        AudioSource from = _currentMusicSource;
        AudioSource to = (from == musicSourceA) ? musicSourceB : musicSourceA;

        to.clip = newClip;
        to.loop = true;
        to.volume = 0f;
        to.Play();

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float p = t / fadeTime;

            to.volume = Mathf.Lerp(0f, musicVolume * masterVolume, p);
            from.volume = Mathf.Lerp(musicVolume * masterVolume, 0f, p);
            yield return null;
        }

        from.Stop();
        _currentMusicSource = to;
        bgmCrossfadeRoutine = null;
    }

    // ======================================================================
    // SFX
    // ======================================================================
    public void PlaySFX(string key)
    {
        PlaySFXAt(key, Vector3.zero, false, false);
    }

    public void PlaySFXAt(string key, Vector3 pos, bool use3D = true, bool requireVisible = false, float radius = 0f)
    {
        AudioClip clip = GetClipSafe(key);
        if (clip == null) return;

        if (requireVisible && !IsOnScreen(pos, radius))
            return;

        AudioSource src = GetAvailableSFXSource();
        src.transform.position = pos;
        src.spatialBlend = use3D ? 1f : 0f;
        src.clip = clip;
        src.volume = sfxVolume * masterVolume;
        src.Play();
    }

    // ======================================================================
    // UI SOUND
    // ======================================================================
    public void PlayUI(string key)
    {
        AudioClip clip = GetClipSafe(key);
        if (clip == null) return;

        uiSource.clip = clip;
        uiSource.volume = sfxVolume * masterVolume;
        uiSource.Play();
    }

    // ======================================================================
    // Volume
    // ======================================================================
    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        SetMixerSafe("MasterVol", value);
        PlayerPrefs.SetFloat("MasterVol", value);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        SetMixerSafe("MusicVol", value);
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        SetMixerSafe("SFXVol", value);
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    private void LoadVolume()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVol", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVol", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVol", 1f);
    }

    private void ApplyVolumes()
    {
        SetMasterVolume(masterVolume);
        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);
    }

    private float VolumeToDb(float v) =>
        Mathf.Log10(Mathf.Clamp(v, 0.0001f, 1f)) * 20f;

    // ======================================================================
    // Utilities
    // ======================================================================
    private AudioClip GetClipSafe(string key)
    {
        if (soundLibrary == null)
        {
            Debug.LogWarning("[AudioManager] SoundLibrary not assigned!");
            return null;
        }

        AudioClip clip = soundLibrary.GetClip(key);
        if (clip == null)
            Debug.LogWarning($"[AudioManager] Clip not found for key: {key}");

        return clip;
    }

    // 🔄 Compatibility with older scripts (Oven.cs, Fridge.cs)
    public AudioClip GetClipByKey(string key)
    {
        return GetClipSafe(key);
    }

    private void SetMixerSafe(string param, float value)
    {
        if (mainMixer == null) return;

        float db = VolumeToDb(value);
        bool success = mainMixer.SetFloat(param, db);

        if (!success)
            Debug.LogWarning($"[AudioManager] Mixer parameter '{param}' not found! Check exposed parameters.");
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (AudioSource s in sfxPool)
            if (s != null && !s.isPlaying)
                return s;

        // Auto-create new SFX source if pool is full
        AudioSource newSrc = gameObject.AddComponent<AudioSource>();
        sfxPool.Add(newSrc);
        return newSrc;
    }

    public bool IsOnScreen(Vector3 pos, float radius = 0f)
    {
        if (_mainCam == null || _frustumPlanes == null) return true;

        Bounds b = new Bounds(pos, Vector3.one * radius * 2f);
        return GeometryUtility.TestPlanesAABB(_frustumPlanes, b);
    }
}
