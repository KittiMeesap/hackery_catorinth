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
    private float fillPerHit = 0.1f;
    private float drainPerSec = 0.25f;
    private float successTarget = 1f;

    private bool active = false;
    private bool hasStarted = false;

    private InputManager input => QTEManager.Instance.input;
    private InputAction hitAction;

    private Coroutine fadeRoutine;
    private Coroutine punchRoutine;

    private System.Action<QTEResult> finishCallback;

    public void Begin(string logicalKey, float perHit, float drain, float successTarget,
                      System.Action<QTEResult> onFinished)
    {
        this.fillPerHit = perHit;
        this.drainPerSec = drain;
        this.successTarget = successTarget;
        this.finishCallback = onFinished;

        currentFill = 0f;
        barFill.fillAmount = 0f;
        hasStarted = false;  // reset

        keyIcon.sprite = KeyIconDatabase.GetIcon(logicalKey);
        labelText.text = logicalKey.ToUpperInvariant();

        if (root == null) root = (RectTransform)transform;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeCanvas(0f, 1f, fadeInDuration));
        }

        input.QTE.Enable();
        hitAction = input.QTE.ConfirmHit;
        hitAction.performed += OnHit;

        active = true;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!active) return;

        if (hasStarted)
        {
            currentFill -= drainPerSec * Time.unscaledDeltaTime;
            currentFill = Mathf.Clamp01(currentFill);
            barFill.fillAmount = currentFill;

        }

        if (currentFill >= successTarget)
        {
            Finish(QTEResult.Success);
        }
    }

    private void OnHit(InputAction.CallbackContext ctx)
    {
        if (!active) return;

        hasStarted = true;

        currentFill += fillPerHit;
        currentFill = Mathf.Clamp01(currentFill);
        barFill.fillAmount = currentFill;

        PlayHitFeedback();

        if (currentFill >= successTarget)
        {
            Finish(QTEResult.Success);
        }
    }

    private void PlayHitFeedback()
    {
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        Vector3 baseScale = root.localScale;
        Vector3 target = baseScale * hitPunchScale;

        float t = 0f;
        while (t < hitPunchDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / hitPunchDuration;
            p = p * p * (3 - 2 * p);
            root.localScale = Vector3.Lerp(baseScale, target, p);
            yield return null;
        }

        t = 0f;
        while (t < hitPunchDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / hitPunchDuration;
            p = p * p * (3 - 2 * p);
            root.localScale = Vector3.Lerp(target, baseScale, p);
            yield return null;
        }

        root.localScale = baseScale;
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            p = p * p * (3 - 2 * p);
            canvasGroup.alpha = Mathf.Lerp(from, to, p);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private void Finish(QTEResult result)
    {
        if (!active) return;
        active = false;

        if (hitAction != null)
            hitAction.performed -= OnHit;

        input.QTE.Disable();
        gameObject.SetActive(false);

        finishCallback?.Invoke(result);
    }

    public void ForceStop()
    {
        Finish(QTEResult.Fail);
    }
}
