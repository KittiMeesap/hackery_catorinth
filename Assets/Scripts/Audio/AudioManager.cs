using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer & Groups")]
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

    [Header("BGM Crossfade Settings")]
    [SerializeField] private float bgmCrossfadeTime = 1.5f;

    private AudioSource _currentMusicSource;
    private string currentBGMKey = "";
    private Coroutine bgmCrossfadeRoutine;

    private Camera _mainCam;
    private Plane[] _frustumPlanes;

    // ===========================================================
    // 🧩 INITIALIZE
    // ===========================================================
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

        // ตรวจสอบ AudioSource
        if (musicSourceA == null || musicSourceB == null)
            Debug.LogError("[AudioManager] Missing music sources (A or B)!");

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

    // ===========================================================
    // 🎵 BGM ZONE CONTROL
    // ===========================================================
    public void PlayBGM(string key, bool crossfade = true)
    {
        if (string.IsNullOrEmpty(key)) return;

        // ถ้าเพลงเดิมเล่นอยู่แล้ว → ไม่ต้องเปลี่ยน
        if (currentBGMKey == key && _currentMusicSource.isPlaying)
            return;

        AudioClip clip = GetClipByKey(key);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] BGM clip not found for key: {key}");
            return;
        }

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

    // ===========================================================
    // 🎶 MANUAL MUSIC CONTROL (optional use)
    // ===========================================================
    public void PlayMusic(string key)
    {
        PlayBGM(key, true);
    }

    // ===========================================================
    // 🔊 SFX CONTROL
    // ===========================================================
    public void PlaySFX(string key)
    {
        PlaySFXAt(key, Vector3.zero, false, false);
    }

    public void PlaySFXAt(string key, Vector3 worldPos, bool use3D = true, bool requireVisible = false, float radius = 0f)
    {
        var clip = GetClipByKey(key);
        if (clip == null) return;

        if (requireVisible && !IsOnScreen(worldPos, radius)) return;

        var src = GetAvailableSFXSource();
        if (src == null)
        {
            Debug.LogWarning("[AudioManager] No available SFX source in pool!");
            return;
        }

        src.transform.position = worldPos;
        src.spatialBlend = use3D ? 1f : 0f;
        src.clip = clip;
        src.volume = sfxVolume * masterVolume;
        src.Play();
    }

    // ===========================================================
    // 🧩 UI SOUND
    // ===========================================================
    public void PlayUI(string key)
    {
        var clip = GetClipByKey(key);
        if (clip == null || uiSource == null) return;
        uiSource.clip = clip;
        uiSource.volume = sfxVolume * masterVolume;
        uiSource.Play();
    }

    // ===========================================================
    // ⚙️ VOLUME CONTROL
    // ===========================================================
    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        if (mainMixer)
            mainMixer.SetFloat("MasterVol", VolumeToDb(value));
        PlayerPrefs.SetFloat("MasterVol", value);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        if (mainMixer)
            mainMixer.SetFloat("MusicVol", VolumeToDb(value));
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        if (mainMixer)
            mainMixer.SetFloat("SFXVol", VolumeToDb(value));
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

    private float VolumeToDb(float v) => Mathf.Log10(Mathf.Clamp(v, 0.0001f, 1f)) * 20f;

    // ===========================================================
    // 🧠 UTILITIES
    // ===========================================================
    private AudioSource GetAvailableSFXSource()
    {
        foreach (var s in sfxPool)
            if (s && !s.isPlaying)
                return s;

        return (sfxPool != null && sfxPool.Count > 0) ? sfxPool[0] : null;
    }

    public bool IsOnScreen(Vector3 pos, float radius = 0f)
    {
        if (_mainCam == null || _frustumPlanes == null) return true;
        var bounds = new Bounds(pos, Vector3.one * (radius * 2f));
        return GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds);
    }

    // ===========================================================
    // 📚 SOUND LIBRARY ACCESS
    // ===========================================================
    public AudioClip GetClipByKey(string key)
    {
        if (soundLibrary == null)
        {
            Debug.LogWarning("[AudioManager] SoundLibrary is not assigned.");
            return null;
        }

        var clip = soundLibrary.GetClip(key);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] Clip not found for key: {key}");
            return null;
        }

        return clip;
    }
}
