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

    [Header("Audio Sources (Auto-Created if Missing)")]
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

        // ---------- Audio Sources ----------
        if (musicSourceA == null)
        {
            musicSourceA = gameObject.AddComponent<AudioSource>();
            musicSourceA.playOnAwake = false;
            musicSourceA.loop = true;
        }

        if (musicSourceB == null)
        {
            musicSourceB = gameObject.AddComponent<AudioSource>();
            musicSourceB.playOnAwake = false;
            musicSourceB.loop = true;
        }

        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
        }

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

    // ============================================================
    // BGM
    // ============================================================
    public void PlayBGM(string key, bool crossfade = true)
    {
        if (string.IsNullOrEmpty(key)) return;

        AudioClip clip = GetClipSafe(key);
        if (clip == null) return;

        if (currentBGMKey == key && _currentMusicSource.isPlaying)
            return;

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
            _currentMusicSource.volume = musicVolume * masterVolume;
            _currentMusicSource.loop = true;
            _currentMusicSource.Play();
        }
    }

    private IEnumerator CrossfadeBGM(AudioClip newClip, float fadeTime)
    {
        AudioSource from = _currentMusicSource;
        AudioSource to = (from == musicSourceA) ? musicSourceB : musicSourceA;

        to.clip = newClip;
        to.volume = 0f;
        to.loop = true;
        to.Play();

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float p = t / fadeTime;
            p = p * p * (3 - 2 * p);

            to.volume = Mathf.Lerp(0f, musicVolume * masterVolume, p);
            from.volume = Mathf.Lerp(musicVolume * masterVolume, 0f, p);

            yield return null;
        }

        from.Stop();
        _currentMusicSource = to;
        bgmCrossfadeRoutine = null;
    }

    public void StopBGM()
    {
        if (musicSourceA != null) musicSourceA.Stop();
        if (musicSourceB != null) musicSourceB.Stop();

        currentBGMKey = "";

        if (bgmCrossfadeRoutine != null)
        {
            StopCoroutine(bgmCrossfadeRoutine);
            bgmCrossfadeRoutine = null;
        }
    }

    // ============================================================
    // SFX
    // ============================================================
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

    // ============================================================
    // UI Sound
    // ============================================================
    public void PlayUI(string key)
    {
        AudioClip clip = GetClipSafe(key);
        if (clip == null) return;

        uiSource.clip = clip;
        uiSource.volume = sfxVolume * masterVolume;
        uiSource.Play();
    }

    // ============================================================
    // STOP
    // ============================================================
    public void StopAllSFX()
    {
        foreach (var src in sfxPool)
            if (src != null)
                src.Stop();

        if (uiSource != null)
            uiSource.Stop();
    }

    public void StopAllAudio()
    {
        StopAllSFX();
        StopBGM();
    }

    // ============================================================
    // VOLUME
    // ============================================================
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        SetMixerSafe("MasterVol", masterVolume);
        PlayerPrefs.SetFloat("MasterVol", masterVolume);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        SetMixerSafe("MusicVol", musicVolume);
        PlayerPrefs.SetFloat("MusicVol", musicVolume);
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        SetMixerSafe("SFXVol", sfxVolume);
        PlayerPrefs.SetFloat("SFXVol", sfxVolume);
    }

    public void ApplyVolumePreview(float master, float music, float sfx)
    {
        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);
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

    private float VolumeToDb(float v)
        => Mathf.Log10(Mathf.Clamp(v, 0.0001f, 1f)) * 20f;

    // ============================================================
    // Utilities
    // ============================================================
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

    // ? FIX: ??? API ????????????????????????
    public AudioClip GetClipByKey(string key)
    {
        return GetClipSafe(key);
    }

    private void SetMixerSafe(string param, float value)
    {
        if (mainMixer == null) return;
        mainMixer.SetFloat(param, VolumeToDb(value));
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (AudioSource a in sfxPool)
            if (a != null && !a.isPlaying)
                return a;

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
