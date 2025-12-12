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

    private float currentFill = 0f;
    private float fillPerHit;
    private float drainPerSec;
    private float successTarget;

    private bool active = false;
    private bool hasStarted = false;

    private InputAction hitAction;

    private Coroutine fadeRoutine;
    private Coroutine punchRoutine;
    private Action<QTEResult> finishCallback;

    private string currentLogicalKey = "confirm";

    //  PUBLIC API
    public void Begin(string logicalKey, float perHit, float drain, float successTarget,
                      Action<QTEResult> onFinished)
    {
        if (GameInput.Instance == null)
        {
            Debug.LogError("QTE_MashUI: GameInput is NULL");
            return;
        }

        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        enabled = true;

        this.fillPerHit = perHit;
        this.drainPerSec = drain;
        this.successTarget = successTarget;
        this.finishCallback = onFinished;

        currentFill = 0f;
        if (barFill != null)
            barFill.fillAmount = 0f;

        hasStarted = false;
        active = true;

        if (root != null)
            root.localScale = Vector3.one;

        currentLogicalKey = string.IsNullOrEmpty(logicalKey) ? "confirm" : logicalKey;
        UpdateKeyIconSprite();

        if (labelText != null)
            labelText.text = "MASH!";

        // Fade-in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            fadeRoutine = StartCoroutine(FadeCanvas(0f, 1f, fadeInDuration));
        }

        // ENABLE QTE ACTION MAP
        GameInput.Instance.SetModeQTE();

        // GET INPUT ACTION
        hitAction = GameInput.Instance.QTEConfirmHitAction;
        hitAction.performed += OnHit;
    }

    //  UPDATE
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
        {
            Finish(QTEResult.Success);
        }
    }

    //  INPUT CALLBACK
    private void OnHit(InputAction.CallbackContext ctx)
    {
        if (!active) return;

        hasStarted = true;

        // Auto switch icon
        string logical = KeyIconDatabase.GetLogicalFromContext(ctx);
        if (!string.IsNullOrEmpty(logical))
        {
            currentLogicalKey = logical;
            UpdateKeyIconSprite();
        }

        currentFill += fillPerHit;
        currentFill = Mathf.Clamp01(currentFill);

        if (barFill != null)
            barFill.fillAmount = currentFill;

        if (punchRoutine != null)
            StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(HitRoutine());

        if (currentFill >= successTarget)
            Finish(QTEResult.Success);
    }

    //  VISUAL
    private void UpdateKeyIconSprite()
    {
        var icon = KeyIconDatabase.GetIcon(currentLogicalKey);

        if (icon != null)
        {
            keyIcon.enabled = true;
            keyIcon.sprite = icon;
        }
        else keyIcon.enabled = false;
    }

    private IEnumerator HitRoutine()
    {
        Vector3 baseScale = root.localScale;
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

    //  FINISH / FORCE STOP
    private void Finish(QTEResult result)
    {
        if (!active) return;
        active = false;

        if (hitAction != null)
            hitAction.performed -= OnHit;

        gameObject.SetActive(false);
        finishCallback?.Invoke(result);
    }

    public void ForceStop()
    {
        Finish(QTEResult.Fail);
    }
}
