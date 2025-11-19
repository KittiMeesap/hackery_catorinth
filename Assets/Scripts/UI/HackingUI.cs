using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static HackableObject;

public class HackingUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject hackPanel;
    [SerializeField] private Transform multiOptionParent;
    [SerializeField] private GameObject hackOptionUIPrefab;
    [SerializeField] private Slider hackTimerSlider;

    [Header("Arrow Prefabs")]
    [SerializeField] private GameObject arrowUpPrefab;
    [SerializeField] private GameObject arrowDownPrefab;
    [SerializeField] private GameObject arrowLeftPrefab;
    [SerializeField] private GameObject arrowRightPrefab;

    
    [Header("SFX Keys (SoundLibrary keys)")]
    [SerializeField] private string sfxOpenUI = "Hack_Open";
    [SerializeField] private string sfxSuccess = "Hack_Success";
    [SerializeField] private string sfxFail = "Hack_Fail";
    [SerializeField] private string sfxInput = "Hack_Input";

    [Header("Juice Settings")]
    [SerializeField] private float panelPopDuration = 0.4f;
    [SerializeField] private float arrowPopScale = 1.15f;
    [SerializeField] private float shakeIntensity = 2f;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float arrowPulseScale = 1.25f;

    private Dictionary<ArrowUI.Direction, GameObject> arrowPrefabs;
    private List<HackOptionUI> activeOptions = new();
    private List<ArrowUI.Direction> currentInput = new();

    private System.Action<HackOptionSO> onOptionSelected;
    private Coroutine timerRoutine;
    private System.Action onTimerFail;
    public bool IsActive => hackPanel != null && hackPanel.activeSelf;

    private HackableObject currentHackTarget;

    public void SetCurrentHackTarget(HackableObject target)
    {
        currentHackTarget = target;
    }

    private void Awake()
    {
        arrowPrefabs = new()
        {
            { ArrowUI.Direction.Up, arrowUpPrefab },
            { ArrowUI.Direction.Down, arrowDownPrefab },
            { ArrowUI.Direction.Left, arrowLeftPrefab },
            { ArrowUI.Direction.Right, arrowRightPrefab }
        };

        if (hackPanel != null)
            hackPanel.SetActive(false);

        if (hackTimerSlider != null)
            hackTimerSlider.gameObject.SetActive(false);
    }

    
    private List<ArrowUI.Direction> GenerateRandomSequence(int length)
    {
        var dirs = new List<ArrowUI.Direction>
        {
            ArrowUI.Direction.Up,
            ArrowUI.Direction.Down,
            ArrowUI.Direction.Left,
            ArrowUI.Direction.Right
        };

        List<ArrowUI.Direction> seq = new();
        for (int i = 0; i < length; i++)
            seq.Add(dirs[Random.Range(0, dirs.Count)]);

        return seq;
    }

    
    public void ShowMultiOptionUI(List<HackOptionSO> options, Transform target, System.Action<HackOptionSO> onSuccess)
    {
        ClearMultiOptions();
        currentInput.Clear();

        if (hackPanel != null)
        {
            hackPanel.SetActive(true);

            
            AudioManager.Instance?.PlayUI(sfxOpenUI);

            StartCoroutine(PopInPanelRoutine());
        }

        onOptionSelected = onSuccess;

        foreach (var option in options)
        {
            if (option == null) continue;

            List<ArrowUI.Direction> seq =
                option.isRandom ? GenerateRandomSequence(option.randomLength)
                                : option.sequence;

            if (seq == null || seq.Count == 0) continue;

            GameObject optGO = Instantiate(hackOptionUIPrefab, multiOptionParent);
            HackOptionUI optUI = optGO.GetComponent<HackOptionUI>();

            var iconImage = optGO.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && option.icon != null)
                iconImage.sprite = option.icon;

            Transform arrowGroup = optGO.transform.Find("ArrowGroup");

            foreach (var dir in seq)
            {
                if (!arrowPrefabs.TryGetValue(dir, out var prefab) || prefab == null)
                    continue;

                GameObject arrowGO = Instantiate(prefab, arrowGroup);
                arrowGO.transform.localScale = Vector3.one * arrowPopScale;

                var arrowUI = arrowGO.GetComponent<ArrowUI>();
                arrowUI.Initialize(dir);

                optUI.arrowUIs.Add(arrowUI);
            }

            optUI.optionData = option;
            optUI.sequence = seq;
            optUI.Setup(seq, () => onSuccess?.Invoke(option));

            activeOptions.Add(optUI);
        }

        UpdateTimerPosition();
    }

    
    public void ShowMultiOptionUI(
        List<HackOptionSO> options,
        Transform worldTarget,
        System.Action<HackOptionSO> onSuccess,
        bool useTimer,
        float timerDuration)
    {
        ShowMultiOptionUI(options, worldTarget, onSuccess);

        if (useTimer)
            StartHackTimer(timerDuration, () => onSuccess?.Invoke(null));
        else
            StopHackTimer();
    }

    
    public void ShowSingleOptionSequence(
        List<ArrowUI.Direction> sequence,
        Transform worldTarget,
        Sprite icon,
        System.Action onComplete,
        System.Action onFail,
        bool useTimer,
        float timerDuration)
    {
        HackOptionSO temp = ScriptableObject.CreateInstance<HackOptionSO>();
        temp.icon = icon;
        temp.sequence = sequence;
        temp.isRandom = false;

        ShowMultiOptionUI(
            new List<HackOptionSO> { temp },
            worldTarget,
            _ => onComplete?.Invoke()
        );

        if (useTimer)
            StartHackTimer(timerDuration, onFail);
    }

    // INPUT
    public void SubmitInput(ArrowUI.Direction input)
    {
        currentInput.Add(input);

        
        AudioManager.Instance?.PlayUI(sfxInput);

        bool matchedPrefix = false;
        HackOptionUI fullMatch = null;

        foreach (var opt in activeOptions)
        {
            if (IsPrefixMatch(opt.sequence, currentInput))
            {
                matchedPrefix = true;
                opt.Highlight(currentInput.Count - 1);
                StartCoroutine(PulseArrow(opt, currentInput.Count - 1));

                if (currentInput.Count == opt.sequence.Count)
                    fullMatch = opt;
            }
        }

        if (fullMatch != null)
        {
            
            AudioManager.Instance?.PlayUI(sfxSuccess);

            StartCoroutine(SuccessFlash());
            onOptionSelected?.Invoke(fullMatch.optionData);
            HideHackingUI();
            return;
        }

        if (!matchedPrefix)
        {
           
            AudioManager.Instance?.PlayUI(sfxFail);

            StartCoroutine(ShakePanelRoutine());
            FlashIncorrectAll();

            if (currentHackTarget != null)
            {
                var timer = FindFirstObjectByType<CountdownTimer>();
                timer?.ReduceTime(currentHackTarget.reduceTimeOnFail);
            }
        }
    }

    private bool IsPrefixMatch(List<ArrowUI.Direction> seq, List<ArrowUI.Direction> input)
    {
        if (input.Count > seq.Count) return false;
        for (int i = 0; i < input.Count; i++)
            if (seq[i] != input[i]) return false;
        return true;
    }

    // TIMER
    private void UpdateTimerPosition()
    {
        if (!hackTimerSlider || !hackTimerSlider.gameObject.activeSelf) return;

        RectTransform timerRect = hackTimerSlider.transform as RectTransform;

        float baseOffset = -40f;
        float eachHeight = 70f;

        int count = multiOptionParent.childCount;
        float y = baseOffset - (eachHeight * count);

        timerRect.anchoredPosition = new Vector2(0f, y);
    }

    public void StartHackTimer(float duration, System.Action onFail)
    {
        onTimerFail = onFail;
        hackTimerSlider.gameObject.SetActive(true);

        hackTimerSlider.maxValue = duration;
        hackTimerSlider.value = duration;

        if (timerRoutine != null)
            StopCoroutine(timerRoutine);

        timerRoutine = StartCoroutine(HackTimerRoutine(duration));
        UpdateTimerPosition();
    }

    private IEnumerator HackTimerRoutine(float t)
    {
        while (t > 0f)
        {
            t -= Time.deltaTime;
            hackTimerSlider.value = t;
            yield return null;
        }

        onTimerFail?.Invoke();
        HideHackingUI();
    }

    public void StopHackTimer()
    {
        if (timerRoutine != null)
            StopCoroutine(timerRoutine);

        if (hackTimerSlider != null)
            hackTimerSlider.gameObject.SetActive(false);
    }

    // EFFECTS
    private IEnumerator PopInPanelRoutine()
    {
        hackPanel.transform.localScale = Vector3.zero;
        float t = 0f;

        while (t < panelPopDuration)
        {
            t += Time.deltaTime;
            float p = t / panelPopDuration;

            float s = Mathf.Sin(p * Mathf.PI * 0.5f);
            float overshoot = 1.05f + Mathf.Sin(p * Mathf.PI) * 0.05f;

            hackPanel.transform.localScale = Vector3.one * s * overshoot;
            yield return null;
        }

        hackPanel.transform.localScale = Vector3.one;
    }

    private IEnumerator PulseArrow(HackOptionUI opt, int idx)
    {
        if (idx < 0 || idx >= opt.arrowUIs.Count) yield break;

        Transform arrow = opt.arrowUIs[idx].transform;
        Vector3 baseScale = Vector3.one;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 6f;
            float s = 1f + Mathf.Sin(t * Mathf.PI) * (arrowPulseScale - 1f);
            arrow.localScale = baseScale * s;
            yield return null;
        }

        arrow.localScale = baseScale;
    }

    private IEnumerator ShakePanelRoutine()
    {
        Vector3 original = hackPanel.transform.localPosition;

        float e = 0f;
        while (e < shakeDuration)
        {
            e += Time.deltaTime;
            float fade = 1f - (e / shakeDuration);

            hackPanel.transform.localPosition =
                original + (Vector3)Random.insideUnitCircle * shakeIntensity * fade;

            yield return null;
        }

        hackPanel.transform.localPosition = original;
    }

    private IEnumerator SuccessFlash()
    {
        CanvasGroup cg = hackPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = hackPanel.AddComponent<CanvasGroup>();

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime * 3f;
            cg.alpha = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
            yield return null;
        }

        cg.alpha = 1f;
    }

    // CLEANUP
    private void FlashIncorrectAll()
    {
        foreach (var opt in activeOptions)
            opt.SetIncorrect();

        currentInput.Clear();
        Invoke(nameof(CloseAfterIncorrect), 0.4f);
    }

    private void CloseAfterIncorrect()
    {
        HideHackingUI();
        GameManager.Instance?.ToggleHackingMode(false);
    }

    public void HideHackingUI()
    {
        StopAllCoroutines();

        if (hackPanel != null)
            hackPanel.SetActive(false);

        ClearMultiOptions();
        currentInput.Clear();
        StopHackTimer();

        GameManager.Instance?.ToggleHackingMode(false);
        PlayerController.Instance?.SetFrozen(false);
        PlayerController.Instance?.SetPhoneOut(false);
        HackableObject.ActiveProximityHackable = null;

        StartCoroutine(EndFrameCleanup());
    }

    private IEnumerator EndFrameCleanup()
    {
        yield return new WaitForEndOfFrame();
        Resources.UnloadUnusedAssets();
    }

    private void ClearMultiOptions()
    {
        for (int i = multiOptionParent.childCount - 1; i >= 0; i--)
            Destroy(multiOptionParent.GetChild(i).gameObject);

        activeOptions.Clear();
    }
}
