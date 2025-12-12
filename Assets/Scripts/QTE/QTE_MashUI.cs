using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class QTE_MashUI : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform root;
    public CanvasGroup canvasGroup;
    public Image barFill;
    public Image keyIcon;
    public TextMeshProUGUI labelText;

    [Header("Effects")]
    public float fadeInDuration = 0.15f;
    public float hitPunchScale = 1.15f;
    public float hitPunchDuration = 0.08f;

    [Header("SFX")]
    public string sfxMashHit = "SFX_Mixer_Mash";
    public string sfxSuccess = "SFX_Mixer_Success";

    private float currentFill;
    private float fillPerHit;
    private float drainPerSec;
    private float successTarget;

    private bool active;
    private bool hasStarted;

    private InputAction hitAction;
    private Coroutine fadeRoutine;
    private Coroutine punchRoutine;
    private Action<QTEResult> finishCallback;

    private string currentLogicalKey = "confirm";
    private Vector3 initialScale = Vector3.one;

    private void Awake()
    {
        if (root != null)
            initialScale = root.localScale;
    }

    private void OnDisable()
    {
        UnbindInput();
        StopAllCoroutines();

        if (root != null)
            root.localScale = initialScale;

        active = false;
        hasStarted = false;
    }

    public void Begin(
        string logicalKey,
        float perHit,
        float drain,
        float successTarget,
        Action<QTEResult> onFinished)
    {
        if (GameInput.Instance == null)
        {
            Debug.LogError("QTE_MashUI: GameInput.Instance is null");
            onFinished?.Invoke(QTEResult.Fail);
            return;
        }

        gameObject.SetActive(true);
        enabled = true;

        this.fillPerHit = perHit;
        this.drainPerSec = drain;
        this.successTarget = successTarget;
        this.finishCallback = onFinished;

        currentFill = 0f;
        hasStarted = false;
        active = true;

        if (barFill != null)
            barFill.fillAmount = 0f;

        if (root != null)
            root.localScale = initialScale;

        currentLogicalKey = string.IsNullOrEmpty(logicalKey) ? "confirm" : logicalKey;
        UpdateKeyIcon();

        if (labelText != null)
            labelText.text = "MASH!";

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            fadeRoutine = StartCoroutine(FadeCanvas(0f, 1f, fadeInDuration));
        }

        GameInput.Instance.SetModeQTE();

        hitAction = GameInput.Instance.QTEConfirmHitAction;
        if (hitAction == null)
        {
            Debug.LogError("QTE_MashUI: QTEConfirmHitAction is null");
            Finish(QTEResult.Fail);
            return;
        }

        hitAction.performed += OnHit;
    }

    private void Update()
    {
        if (!active) return;

        if (hasStarted)
        {
            currentFill -= drainPerSec * Time.unscaledDeltaTime;
            currentFill = Mathf.Clamp01(currentFill);

            if (barFill != null)
                barFill.fillAmount = currentFill;
        }

        if (currentFill >= successTarget)
            Finish(QTEResult.Success);
    }

    private void OnHit(InputAction.CallbackContext ctx)
    {
        if (!active) return;

        hasStarted = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUI(sfxMashHit);

        string logical = KeyIconDatabase.GetLogicalFromContext(ctx);
        if (!string.IsNullOrEmpty(logical))
        {
            currentLogicalKey = logical;
            UpdateKeyIcon();
        }

        currentFill = Mathf.Clamp01(currentFill + fillPerHit);

        if (barFill != null)
            barFill.fillAmount = currentFill;

        if (punchRoutine != null)
            StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(Punch());

        if (currentFill >= successTarget)
            Finish(QTEResult.Success);
    }

    private IEnumerator Punch()
    {
        if (root == null) yield break;

        Vector3 baseScale = initialScale;
        Vector3 target = baseScale * hitPunchScale;

        float t = 0f;
        while (t < hitPunchDuration)
        {
            t += Time.unscaledDeltaTime;
            root.localScale = Vector3.Lerp(baseScale, target, t / hitPunchDuration);
            yield return null;
        }

        t = 0f;
        while (t < hitPunchDuration)
        {
            t += Time.unscaledDeltaTime;
            root.localScale = Vector3.Lerp(target, baseScale, t / hitPunchDuration);
            yield return null;
        }

        root.localScale = baseScale;
    }

    private IEnumerator FadeCanvas(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private void UpdateKeyIcon()
    {
        if (keyIcon == null) return;

        var icon = KeyIconDatabase.GetIcon(currentLogicalKey);
        keyIcon.enabled = icon != null;
        keyIcon.sprite = icon;
    }

    private void Finish(QTEResult result)
    {
        if (!active) return;

        active = false;
        UnbindInput();

        if (result == QTEResult.Success && AudioManager.Instance != null)
            AudioManager.Instance.PlayUI(sfxSuccess);

        if (root != null)
            root.localScale = initialScale;

        gameObject.SetActive(false);
        finishCallback?.Invoke(result);
    }

    public void ForceStop()
    {
        Finish(QTEResult.Fail);
    }

    private void UnbindInput()
    {
        if (hitAction != null)
            hitAction.performed -= OnHit;
        hitAction = null;
    }
}
