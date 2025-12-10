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

    [Header("Timer Bar")]
    public Image bgFill;

    [Header("Effects")]
    public float stepPunchScale = 1.15f;
    public float stepPunchDuration = 0.08f;
    public float failShakeMagnitude = 40f;
    public float failShakeDuration = 0.08f;

    private string[] sequence;
    private int index;
    private float timePerKey;
    private float timer;

    private bool active = false;

    private InputManager input => QTEManager.Instance.input;
    private InputAction arrowAction;

    private Coroutine punchRoutine;
    private Coroutine shakeRoutine;
    private System.Action<QTEResult> finishCallback;

    private string DirectionFromVector(Vector2 v)
    {
        if (v.y > 0.5f) return "up";
        if (v.y < -0.5f) return "down";
        if (v.x > 0.5f) return "right";
        if (v.x < -0.5f) return "left";
        return null;
    }

    public void Begin(string[] sequence, float timePerKey, System.Action<QTEResult> onFinished)
    {
        this.sequence = sequence;
        this.timePerKey = timePerKey;
        this.finishCallback = onFinished;

        index = 0;
        timer = timePerKey;

        bgFill.fillAmount = 1f;

        ShowCurrentKey();

        input.QTE.Enable();
        arrowAction = input.QTE.Directional;
        arrowAction.performed += OnArrow;

        active = true;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!active) return;

        timer -= Time.unscaledDeltaTime;
        bgFill.fillAmount = Mathf.Clamp01(timer / timePerKey);

        if (timer <= 0f)
        {
            Finish(QTEResult.Fail);
        }
    }

    private void OnArrow(InputAction.CallbackContext ctx)
    {
        if (!active) return;

        Vector2 v = ctx.ReadValue<Vector2>();
        string pressed = DirectionFromVector(v);
        if (pressed == null) return;

        string expected = sequence[index];

        if (pressed == expected)
        {
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
                bgFill.fillAmount = 1f;
                ShowCurrentKey();
            }
        }
        else
        {
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(Shake());

            sequence[index] = RandomArrow();
            timer = timePerKey;
            bgFill.fillAmount = 1f;

            ShowCurrentKey();
        }
    }

    private string RandomArrow()
    {
        string[] pool = { "left", "right", "up", "down" };
        return pool[Random.Range(0, pool.Length)];
    }

    private void ShowCurrentKey()
    {
        string logical = sequence[index];

        var icon = KeyIconDatabase.GetIcon(logical);
        keyIcon.enabled = icon != null;
        keyIcon.sprite = icon;

        textLabel.text = "PRESS";
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

    private IEnumerator Shake()
    {
        Vector2 basePos = root.anchoredPosition;

        float t = 0f;
        while (t < failShakeDuration)
        {
            t += Time.unscaledDeltaTime;
            root.anchoredPosition = basePos + Random.insideUnitCircle * failShakeMagnitude;
            yield return null;
        }

        root.anchoredPosition = basePos;
    }

    private void Finish(QTEResult result)
    {
        if (!active) return;

        active = false;

        if (arrowAction != null)
            arrowAction.performed -= OnArrow;

        input.QTE.Disable();
        gameObject.SetActive(false);

        finishCallback?.Invoke(result);
    }

    public void ForceStop()
    {
        Finish(QTEResult.Fail);
    }
}
