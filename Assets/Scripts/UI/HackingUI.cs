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
    [SerializeField] private float shakeDuration = 0.3f;

    [Header("Audio Keys (from SoundLibrary)")]
    [SerializeField] private string hackingSoundKey = "SFX_HackInput";
    [SerializeField] private string failSoundKey = "SFX_HackFail";
    [SerializeField] private string popupSoundKey = "SFX_HackPopup";

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
        UpdateTimerPosition(); // ensure timer placed correctly if visible
    }

    public void ShowMultiOptionUI(List<HackOptionSO> options, Transform worldTarget, System.Action<HackOptionSO> onSuccess, bool useTimer, float timerDuration)
    {
        ShowMultiOptionUI(options, worldTarget, onSuccess);
        if (useTimer)
            StartHackTimer(timerDuration, () => onSuccess?.Invoke(null));
        else
            StopHackTimer();
    }

    public void SubmitInput(ArrowUI.Direction input)
    {
        if (isShowingSequence) return;

        currentInput.Add(input);
        HackOptionUI fullMatch = null;
        bool matchedPrefix = false;

        if (!string.IsNullOrEmpty(hackingSoundKey))
            AudioManager.Instance?.PlaySFX(hackingSoundKey);

        foreach (var option in activeOptions)
        {
            if (IsPrefixMatch(option.sequence, currentInput))
            {
                matchedPrefix = true;
                option.Highlight(currentInput.Count - 1);

                if (currentInput.Count == option.sequence.Count)
                    fullMatch = option;
            }
        }

        if (fullMatch != null)
        {
            onOptionSelected?.Invoke(fullMatch.optionData);
            HideHackingUI();
            return;
        }

        if (!matchedPrefix)
        {
            StartCoroutine(ShakeArrowsRoutine());
            FlashIncorrectAll();

            if (!string.IsNullOrEmpty(failSoundKey))
                AudioManager.Instance?.PlaySFX(failSoundKey);
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

    private void FlashIncorrectAll()
    {
        foreach (var option in activeOptions)
            option.SetIncorrect();

        currentInput.Clear();
        Invoke(nameof(CloseAfterIncorrect), 0.45f);
    }

    private void CloseAfterIncorrect()
    {
        HideHackingUI();
        GameManager.Instance?.ToggleHackingMode(false);
        StartCoroutine(DelayedFailUnfreeze());
    }

    private IEnumerator DelayedFailUnfreeze()
    {
        yield return new WaitForSeconds(0.05f);

        if (HackableObject.ActiveProximityHackable != null)
        {
            Debug.Log("[HackingUI] Wrong input — calling OnHackFailed()");
            HackableObject.ActiveProximityHackable.OnHackFailed();
        }
        else
        {
            PlayerController.Instance?.SetFrozen(false);
            PlayerController.Instance?.SetPhoneOut(false);
        }
    }

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
            hackTimerSlider.value = time;
            yield return null;
        }

        onTimerFail?.Invoke();
        HideHackingUI();
    }

    private void UpdateTimerPosition()
    {
        if (hackTimerSlider == null || !hackTimerSlider.gameObject.activeSelf) return;

        // วางให้ Timer อยู่ใต้ panel เสมอ
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

    public void HideHackingUI()
    {
        if (hackPanel != null)
            hackPanel.SetActive(false);

        ClearMultiOptions();
        currentInput.Clear();
        StopHackTimer();

        GameManager.Instance?.ToggleHackingMode(false);
        PlayerController.Instance?.SetFrozen(false);
        PlayerController.Instance?.SetPhoneOut(false);
        HackableObject.ActiveProximityHackable = null;
    }

    private void ClearMultiOptions()
    {
        if (multiOptionParent == null) return;

        for (int i = multiOptionParent.childCount - 1; i >= 0; i--)
            Destroy(multiOptionParent.GetChild(i).gameObject);

        activeOptions.Clear();
    }

    private IEnumerator PopInPanelRoutine()
    {
        if (!string.IsNullOrEmpty(popupSoundKey))
            AudioManager.Instance?.PlaySFX(popupSoundKey);

        hackPanel.transform.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < panelPopDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / panelPopDuration);
            hackPanel.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 0.8f, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
    }

    private IEnumerator AnimateArrowsRoutine()
    {
        isShowingSequence = true;

        PlayerHacking playerHack = FindFirstObjectByType<PlayerHacking>();
        if (playerHack != null) playerHack.SetHackingDisabled(true);

        List<ArrowUI> allArrows = new();
        foreach (var opt in activeOptions)
            allArrows.AddRange(opt.arrowUIs);

        foreach (var arrow in allArrows)
        {
            float elapsed = 0f;
            float duration = 0.15f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(0f, arrowPopScale, elapsed / duration);
                arrow.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            arrow.transform.localScale = Vector3.one;
            yield return new WaitForSeconds(arrowPopDelay);
        }

        if (playerHack != null) playerHack.SetHackingDisabled(false);
        isShowingSequence = false;
    }

    private IEnumerator ShakeArrowsRoutine()
    {
        float elapsed = 0f;
        float amplitude = shakeIntensity * 0.3f;
        float frequency = 30f;

        Dictionary<ArrowUI, Vector3> originalPositions = new();
        foreach (var opt in activeOptions)
            foreach (var arrow in opt.arrowUIs)
                originalPositions[arrow] = arrow.transform.localPosition;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shakeDuration;
            float fade = 1f - t;

            foreach (var opt in activeOptions)
            {
                foreach (var arrow in opt.arrowUIs)
                {
                    if (!originalPositions.ContainsKey(arrow)) continue;

                    Vector3 basePos = originalPositions[arrow];
                    float offsetX = Mathf.Sin(Time.time * frequency) * amplitude * fade;
                    float offsetY = Mathf.Cos(Time.time * frequency * 0.8f) * amplitude * 0.5f * fade;

                    arrow.transform.localPosition = basePos + new Vector3(offsetX, offsetY, 0f);
                }
            }

            yield return null;
        }

        foreach (var opt in activeOptions)
            foreach (var arrow in opt.arrowUIs)
                if (originalPositions.ContainsKey(arrow))
                    arrow.transform.localPosition = originalPositions[arrow];
    }
}
