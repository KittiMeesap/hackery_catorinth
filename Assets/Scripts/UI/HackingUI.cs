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

    [Header("Juice Settings")]
    [SerializeField] private float panelPopDuration = 0.4f;
    [SerializeField] private float arrowPopDelay = 0.08f;
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

    private bool isShowingSequence = false;
    public bool IsActive => hackPanel != null && hackPanel.activeSelf;

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

    // DISPLAY
    public void ShowMultiOptionUI(List<HackOptionSO> options, Transform worldTarget, System.Action<HackOptionSO> onSuccess)
    {
        ClearMultiOptions();
        currentInput.Clear();

        if (hackPanel != null)
        {
            hackPanel.SetActive(true);
            StartCoroutine(PopInPanelRoutine());
        }

        onOptionSelected = onSuccess;

        foreach (var option in options)
        {
            if (option == null || option.sequence == null || option.sequence.Count == 0) continue;

            GameObject optionGO = Instantiate(hackOptionUIPrefab, multiOptionParent);
            HackOptionUI optionUI = optionGO.GetComponent<HackOptionUI>();

            var iconImage = optionGO.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && option.icon != null)
                iconImage.sprite = option.icon;

            Transform arrowGroup = optionGO.transform.Find("ArrowGroup");

            foreach (var dir in option.sequence)
            {
                if (!arrowPrefabs.TryGetValue(dir, out var prefab) || prefab == null) continue;
                GameObject arrowGO = Instantiate(prefab, arrowGroup);
                arrowGO.transform.localScale = Vector3.zero;
                var arrowUI = arrowGO.GetComponent<ArrowUI>();
                arrowUI.Initialize(dir);
                optionUI.arrowUIs.Add(arrowUI);
            }

            optionUI.optionData = option;
            optionUI.Setup(option.sequence, () => onSuccess?.Invoke(optionUI.optionData));
            activeOptions.Add(optionUI);
        }

        StartCoroutine(AnimateArrowsRoutine());
        UpdateTimerPosition();
    }

    public void ShowMultiOptionUI(List<HackOptionSO> options, Transform worldTarget, System.Action<HackOptionSO> onSuccess, bool useTimer, float timerDuration)
    {
        ShowMultiOptionUI(options, worldTarget, onSuccess);
        if (useTimer)
            StartHackTimer(timerDuration, () => onSuccess?.Invoke(null));
        else
            StopHackTimer();
    }

    // INPUT
    public void SubmitInput(ArrowUI.Direction input)
    {
        if (isShowingSequence) return;

        currentInput.Add(input);
        HackOptionUI fullMatch = null;
        bool matchedPrefix = false;

        foreach (var option in activeOptions)
        {
            if (IsPrefixMatch(option.sequence, currentInput))
            {
                matchedPrefix = true;
                option.Highlight(currentInput.Count - 1);

                StartCoroutine(PulseArrow(option, currentInput.Count - 1));

                if (currentInput.Count == option.sequence.Count)
                    fullMatch = option;
            }
        }

        if (fullMatch != null)
        {
            StartCoroutine(SuccessFlash());
            onOptionSelected?.Invoke(fullMatch.optionData);
            HideHackingUI();
            return;
        }

        if (!matchedPrefix)
        {
            StartCoroutine(ShakePanelRoutine());
            FlashIncorrectAll();
        }
    }

    private bool IsPrefixMatch(List<ArrowUI.Direction> sequence, List<ArrowUI.Direction> input)
    {
        if (input.Count > sequence.Count) return false;
        for (int i = 0; i < input.Count; i++)
        {
            if (sequence[i] != input[i]) return false;
        }
        return true;
    }

    // TIMER
    public void StartHackTimer(float duration, System.Action onFail)
    {
        if (hackTimerSlider == null) return;

        onTimerFail = onFail;
        hackTimerSlider.gameObject.SetActive(true);
        hackTimerSlider.maxValue = duration;
        hackTimerSlider.value = duration;

        if (timerRoutine != null)
            StopCoroutine(timerRoutine);

        timerRoutine = StartCoroutine(HackCountdownRoutine(duration));
        UpdateTimerPosition();
    }

    private IEnumerator HackCountdownRoutine(float time)
    {
        while (time > 0f)
        {
            time -= Time.deltaTime;
            if (hackTimerSlider != null)
                hackTimerSlider.value = time;
            yield return null;
        }

        onTimerFail?.Invoke();
        HideHackingUI();
    }

    private void UpdateTimerPosition()
    {
        if (hackTimerSlider == null || !hackTimerSlider.gameObject.activeSelf) return;

        RectTransform timerRect = hackTimerSlider.transform as RectTransform;
        RectTransform panelRect = hackPanel.transform as RectTransform;

        if (timerRect != null && panelRect != null)
        {
            float offset = 30f;
            Vector2 pos = new Vector2(0f, -panelRect.rect.height * 0.5f - offset);
            timerRect.anchoredPosition = pos;
        }
    }

    public void StopHackTimer()
    {
        if (timerRoutine != null)
            StopCoroutine(timerRoutine);

        if (hackTimerSlider != null)
            hackTimerSlider.gameObject.SetActive(false);
    }

    public void ShowSingleOptionSequence(List<ArrowUI.Direction> sequence, Transform worldTarget, Sprite icon, System.Action onComplete, System.Action onFail, bool useTimer, float timerDuration)
    {
        HackOptionSO fakeOption = ScriptableObject.CreateInstance<HackOptionSO>();
        fakeOption.icon = icon;
        fakeOption.sequence = sequence;

        ShowMultiOptionUI(new List<HackOptionSO> { fakeOption }, worldTarget, _ => onComplete?.Invoke());

        if (useTimer)
            StartHackTimer(timerDuration, onFail);
        else
            StopHackTimer();
    }

    // EFFECTS
    private IEnumerator PopInPanelRoutine()
    {
        hackPanel.transform.localScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < panelPopDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / panelPopDuration;
            float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
            float overshoot = 1.05f + Mathf.Sin(t * Mathf.PI) * 0.05f;
            hackPanel.transform.localScale = Vector3.one * ease * overshoot;
            yield return null;
        }

        hackPanel.transform.localScale = Vector3.one;
    }

    private IEnumerator PulseArrow(HackOptionUI option, int index)
    {
        if (option == null || option.arrowUIs == null) yield break;
        if (index < 0 || index >= option.arrowUIs.Count) yield break;

        var arrowUI = option.arrowUIs[index];
        if (arrowUI == null || arrowUI.transform == null) yield break;

        Transform arrow = arrowUI.transform;
        Vector3 baseScale = Vector3.one;
        try { baseScale = arrow.localScale; } catch { yield break; }

        float t = 0f;

        while (t < 1f)
        {
            if (arrow == null || arrow.Equals(null)) yield break;

            t += Time.deltaTime * 6f;
            float s = 1f + Mathf.Sin(t * Mathf.PI) * (arrowPulseScale - 1f);

            try { arrow.localScale = baseScale * s; }
            catch { yield break; }

            yield return null;
        }

        if (arrow != null && !arrow.Equals(null))
        {
            try { arrow.localScale = baseScale; } catch { }
        }
    }

    private IEnumerator ShakePanelRoutine()
    {
        if (hackPanel == null) yield break;
        Vector3 originalPos = hackPanel.transform.localPosition;
        float elapsed = 0f;
        float magnitude = shakeIntensity * 3f;

        while (elapsed < shakeDuration)
        {
            if (hackPanel == null) yield break;

            elapsed += Time.deltaTime;
            float fade = 1f - (elapsed / shakeDuration);
            hackPanel.transform.localPosition = originalPos + (Vector3)Random.insideUnitCircle * magnitude * fade;
            yield return null;
        }

        if (hackPanel != null)
            hackPanel.transform.localPosition = originalPos;
    }

    private IEnumerator SuccessFlash()
    {
        if (hackPanel == null) yield break;

        CanvasGroup cg = hackPanel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = hackPanel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
        }

        float t = 0f;
        while (t < 0.3f)
        {
            if (cg == null) yield break;
            t += Time.deltaTime * 3f;
            cg.alpha = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
            yield return null;
        }
        cg.alpha = 1f;
    }

    // CLEANUP
    private void FlashIncorrectAll()
    {
        foreach (var option in activeOptions)
            option.SetIncorrect();

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

        StartCoroutine(WaitEndFrameCleanup());
    }

    private IEnumerator WaitEndFrameCleanup()
    {
        yield return new WaitForEndOfFrame();
        Resources.UnloadUnusedAssets();
    }

    private void ClearMultiOptions()
    {
        if (multiOptionParent == null) return;
        for (int i = multiOptionParent.childCount - 1; i >= 0; i--)
            Destroy(multiOptionParent.GetChild(i).gameObject);
        activeOptions.Clear();
    }

    private IEnumerator AnimateArrowsRoutine()
    {
        isShowingSequence = true;
        List<ArrowUI> allArrows = new();
        foreach (var opt in activeOptions)
            allArrows.AddRange(opt.arrowUIs);

        foreach (var arrow in allArrows)
        {
            if (arrow == null) continue;
            float elapsed = 0f;
            float duration = 0.15f;
            while (elapsed < duration)
            {
                if (arrow == null) yield break;
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(0f, arrowPopScale, elapsed / duration);
                arrow.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            if (arrow != null)
                arrow.transform.localScale = Vector3.one;
            yield return new WaitForSeconds(arrowPopDelay);
        }
        isShowingSequence = false;
    }
}
