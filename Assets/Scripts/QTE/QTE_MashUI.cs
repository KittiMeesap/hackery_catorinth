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
        hasStarted = false;
        active = true;

        if (root != null)
            root.localScale = Vector3.one;

        // icon
        Sprite icon = KeyIconDatabase.GetIcon(logicalKey);
        if (icon != null)
        {
            keyIcon.enabled = true;
            keyIcon.sprite = icon;
        }
        else
        {
            keyIcon.enabled = false;
        }

        labelText.text = "MASH!";

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeCanvas(0f, 1f, fadeInDuration));
        }

        gameObject.SetActive(true);

        GameInput.Instance.SetModeQTE();
        hitAction = GameInput.Instance.QTEConfirmHitAction;
        if (hitAction != null)
            hitAction.performed += OnHit;
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

        if (punchRoutine != null)
            StopCoroutine(punchRoutine);

        punchRoutine = StartCoroutine(HitRoutine());

        if (currentFill >= successTarget)
        {
            Finish(QTEResult.Success);
        }
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

    private IEnumerator FadeCanvas(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = t / dur;
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

        GameInput.Instance.SetModePlayer();

        gameObject.SetActive(false);
        finishCallback?.Invoke(result);
    }

    public void ForceStop()
    {
        Finish(QTEResult.Fail);
    }
}
