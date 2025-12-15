using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomerOrderPanel : MonoBehaviour
{
    public static CustomerOrderPanel Instance { get; private set; }
    public CustomerController CurrentCustomer { get; private set; }

    [Header("Root")]
    public GameObject panelRoot;

    [Header("Icons")]
    public Image customerIcon;
    public Image recipeIcon;

    [Header("Timer")]
    public Image timerFill;

    [Header("Timer Colors")]
    public Color timerNormalColor = new Color(0.3f, 1f, 0.4f);
    public Color timerWarningColor = new Color(1f, 0.85f, 0.3f);
    public Color timerDangerColor = new Color(1f, 0.35f, 0.35f);

    [Range(0f, 1f)] public float warningThreshold = 0.5f;
    [Range(0f, 1f)] public float dangerThreshold = 0.25f;

    [Header("Timer SFX Keys")]
    public string sfxCountdownLoop = "SFX_Timer_Countdown";
    public string sfxTimeUp = "SFX_Timer_Timeout";

    [Header("Groups")]
    public Transform recipeGroup;
    public Transform toolsGroup;

    [Header("Prefabs")]
    public GameObject recipeIconPrefab;
    public GameObject toolIconPrefab;

    [Header("Tool Icons")]
    public List<ToolIconEntry> toolIcons = new();

    [Header("Options")]
    public bool showMixerIcon = false;

    private Dictionary<CustomerToolType, Sprite> toolIconMap;

    // ?? AUDIO
    private AudioSource countdownSource;
    private bool timeUpPlayed;

    [System.Serializable]
    public class ToolIconEntry
    {
        public CustomerToolType type;
        public Sprite icon;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildToolIconMap();
        SetupAudio();
        Hide();
    }

    private void SetupAudio()
    {
        countdownSource = gameObject.AddComponent<AudioSource>();
        countdownSource.loop = true;
        countdownSource.playOnAwake = false;
        countdownSource.spatialBlend = 0f; // UI sound
    }

    private void BuildToolIconMap()
    {
        toolIconMap = new Dictionary<CustomerToolType, Sprite>();

        foreach (var e in toolIcons)
        {
            if (!toolIconMap.ContainsKey(e.type) && e.icon != null)
                toolIconMap.Add(e.type, e.icon);
        }
    }

    // =========================
    // PUBLIC API
    // =========================
    public void Show(CustomerController customer, RecipeSO recipe, Sprite customerSprite)
    {
        CurrentCustomer = customer;
        panelRoot.SetActive(true);

        customerIcon.sprite = customerSprite;
        recipeIcon.sprite = recipe.outputIcon;

        BuildRecipeIcons(recipe);
        BuildToolIcons(recipe);

        timeUpPlayed = false;
        StopCountdownLoop();

        UpdateTimer(1f);
    }

    public void Hide()
    {
        StopCountdownLoop();
        CurrentCustomer = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void UpdateTimer(float normalized)
    {
        normalized = Mathf.Clamp01(normalized);

        if (timerFill == null) return;

        timerFill.fillAmount = normalized;
        timerFill.color = GetTimerColor(normalized);

        HandleTimerSound(normalized);
    }

    // =========================
    // TIMER COLOR
    // =========================
    private Color GetTimerColor(float normalized)
    {
        if (normalized <= dangerThreshold)
            return timerDangerColor;

        if (normalized <= warningThreshold)
            return timerWarningColor;

        return timerNormalColor;
    }

    // =========================
    // ?? TIMER SOUND LOGIC
    // =========================
    private void HandleTimerSound(float normalized)
    {
        if (AudioManager.Instance == null) return;

        
        if (normalized <= dangerThreshold && normalized > 0f)
        {
            StartCountdownLoop();
        }
        else
        {
            StopCountdownLoop();
        }

        
        if (normalized <= 0f && !timeUpPlayed)
        {
            timeUpPlayed = true;
            StopCountdownLoop();
            AudioManager.Instance.PlayUI(sfxTimeUp);
        }
    }

    private void StartCountdownLoop()
    {
        if (countdownSource.isPlaying) return;

        var clip = AudioManager.Instance.GetClipByKey(sfxCountdownLoop);
        if (clip == null) return;

        countdownSource.clip = clip;
        countdownSource.volume =
            AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume;

        countdownSource.Play();
    }

    private void StopCountdownLoop()
    {
        if (countdownSource.isPlaying)
            countdownSource.Stop();
    }

    // =========================
    // ICON BUILDERS
    // =========================
    private void BuildRecipeIcons(RecipeSO recipe)
    {
        foreach (Transform c in recipeGroup)
            Destroy(c.gameObject);

        if (recipe == null) return;

        foreach (var ing in recipe.ingredients)
        {
            var obj = Instantiate(recipeIconPrefab, recipeGroup);
            var img = obj.GetComponent<Image>();
            if (img != null)
                img.sprite = ing.icon;
        }
    }

    private void BuildToolIcons(RecipeSO recipe)
    {
        foreach (Transform c in toolsGroup)
            Destroy(c.gameObject);

        if (recipe == null) return;

        List<CustomerToolType> tools = new();

        if (recipe.flow != ProcessFlow.None)
        {
            if (showMixerIcon)
                tools.Add(CustomerToolType.Mixer);

            switch (recipe.flow)
            {
                case ProcessFlow.BakeOnly:
                    tools.Add(CustomerToolType.Oven);
                    break;
                case ProcessFlow.CoolOnly:
                    tools.Add(CustomerToolType.Fridge);
                    break;
                case ProcessFlow.BakeThenCool:
                    tools.Add(CustomerToolType.Oven);
                    tools.Add(CustomerToolType.Fridge);
                    break;
                case ProcessFlow.CoolThenBake:
                    tools.Add(CustomerToolType.Fridge);
                    tools.Add(CustomerToolType.Oven);
                    break;
            }
        }

        foreach (var tool in tools)
        {
            if (!toolIconMap.TryGetValue(tool, out var sprite) || sprite == null)
                continue;

            var obj = Instantiate(toolIconPrefab, toolsGroup);
            var img = obj.GetComponent<Image>();
            if (img != null)
                img.sprite = sprite;
        }
    }
}
