using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class QTE_SequenceUI : MonoBehaviour
{
    [Header("UI")]
    public RectTransform root;
    public Image keyIcon;
    public TextMeshProUGUI textLabel;
    public Image background;

    [Header("Effects")]
    public float stepPunchScale = 1.15f;
    public float stepPunchDuration = 0.08f;
    public Color failFlashColor = Color.red;
    public float failFlashDuration = 0.15f;

    private string[] sequence;
    private int index;
    private float timePerKey;
    private float timer;

    private bool active = false;

    private InputManager input => QTEManager.Instance.input;
    private InputAction hitAction;

    private Coroutine punchRoutine;

    private System.Action<QTEResult> finishCallback;

    // =============================================================
    public void Begin(string[] sequence, float timePerKey, System.Action<QTEResult> onFinished)
    {
        this.sequence = sequence;
        this.timePerKey = timePerKey;
        this.finishCallback = onFinished;

        index = 0;
        timer = timePerKey;

        ShowCurrentKey();

        input.QTE.Enable();
        hitAction = input.QTE.ConfirmHit;
        hitAction.performed += OnHit;

        active = true;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!active) return;

        timer -= Time.unscaledDeltaTime;
        if (timer <= 0f)
        {
            // timeout -> fail
            StartCoroutine(FailFlash());
            Finish(QTEResult.Fail);
        }
    }

    private void OnHit(InputAction.CallbackContext ctx)
    {
        if (!active) return;

        string pressed = KeyIconDatabase.GetLogicalFromContext(ctx);
        string expected = sequence[index].ToLowerInvariant();

        if (pressed == expected || (expected == "confirm" && pressed == "space"))
        {
            // correct
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            punchRoutine = StartCoroutine(StepPunch());

            index++;
            if (index >= sequence.Length)
            {
                Finish(QTEResult.Success);
            }
            else
            {
                timer = timePerKey;
                ShowCurrentKey();
            }
        }
        else
        {
            // wrong key
            StartCoroutine(FailFlash());
            Finish(QTEResult.Fail);
        }
    }

    private void ShowCurrentKey()
    {
        if (index < 0 || index >= sequence.Length) return;

        string logical = sequence[index].ToLowerInvariant();
        keyIcon.sprite = KeyIconDatabase.GetIcon(logical);
        textLabel.text = logical.ToUpperInvariant();
    }

    private IEnumerator StepPunch()
    {
        Vector3 baseScale = root.localScale;
        Vector3 target = baseScale * stepPunchScale;

        float t = 0f;
        while (t < stepPunchDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / stepPunchDuration;
            p = p * p * (3 - 2 * p);
            root.localScale = Vector3.Lerp(baseScale, target, p);
            yield return null;
        }

        t = 0f;
        while (t < stepPunchDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / stepPunchDuration;
            p = p * p * (3 - 2 * p);
            root.localScale = Vector3.Lerp(target, baseScale, p);
            yield return null;
        }

        root.localScale = baseScale;
    }

    private IEnumerator FailFlash()
    {
        if (background == null) yield break;

        Color baseCol = background.color;
        background.color = failFlashColor;
        float t = 0f;
        while (t < failFlashDuration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        background.color = baseCol;
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
