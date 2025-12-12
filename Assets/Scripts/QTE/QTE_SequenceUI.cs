using System;
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

    private InputAction directionalAction;

    private Coroutine punchRoutine;
    private Coroutine shakeRoutine;
    private Action<QTEResult> finishCallback;

    // =====================================================
    //  PUBLIC API
    // =====================================================
    public void Begin(string[] sequence, float timePerKey, Action<QTEResult> onFinished)
    {
        if (GameInput.Instance == null)
        {
            Debug.LogError("QTE_SequenceUI: GameInput.Instance is null!");
            return;
        }

        if (sequence == null || sequence.Length == 0)
        {
            Debug.LogError("QTE_SequenceUI: sequence is empty!");
            return;
        }

        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);
        enabled = true;

        this.sequence = sequence;
        this.timePerKey = timePerKey;
        this.finishCallback = onFinished;

        index = 0;
        timer = timePerKey;

        if (bgFill != null)
            bgFill.fillAmount = 1f;

        ShowCurrentKey();

        // Switch input mode
        GameInput.Instance.SetModeQTE();

        // Get directional action
        directionalAction = GameInput.Instance.QTEDirectionalAction;
        directionalAction.performed += OnArrow;

        active = true;
    }

    // =====================================================
    //  UPDATE
    // =====================================================
    private void Update()
    {
        if (!active) return;

        timer -= Time.unscaledDeltaTime;
        if (bgFill != null)
            bgFill.fillAmount = Mathf.Clamp01(timer / timePerKey);

        if (timer <= 0f)
            Finish(QTEResult.Fail);
    }

    // =====================================================
    //  INPUT CALLBACK
    // =====================================================
    private void OnArrow(InputAction.CallbackContext ctx)
    {
        if (!active) return;

        Vector2 v = ctx.ReadValue<Vector2>();
        string pressed = DirectionFromVector(v);
        if (pressed == null) return;

        string expected = sequence[index];

        if (pressed == expected)
        {
            // Correct
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            if (root != null)
                punchRoutine = StartCoroutine(StepPunch());

            index++;

            if (index >= sequence.Length)
            {
                Finish(QTEResult.Success);
            }
            else
            {
                timer = timePerKey;
                if (bgFill != null) bgFill.fillAmount = 1f;
                ShowCurrentKey();
            }
        }
        else
        {
            // Wrong -> shake + random new arrow
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            if (root != null)
                shakeRoutine = StartCoroutine(Shake());

            sequence[index] = RandomArrow();
            timer = timePerKey;
            if (bgFill != null) bgFill.fillAmount = 1f;

            ShowCurrentKey();
        }
    }

    private string DirectionFromVector(Vector2 v)
    {
        if (v.y > 0.5f) return "up";
        if (v.y < -0.5f) return "down";
        if (v.x > 0.5f) return "right";
        if (v.x < -0.5f) return "left";
        return null;
    }

    private string RandomArrow()
    {
        string[] pool = { "left", "right", "up", "down" };
        return pool[UnityEngine.Random.Range(0, pool.Length)];
    }

    // =====================================================
    //  VISUAL
    // =====================================================
    private void ShowCurrentKey()
    {
        if (sequence == null || sequence.Length == 0) return;

        string logical = sequence[index];
        var icon = KeyIconDatabase.GetIcon(logical);

        if (keyIcon != null)
        {
            keyIcon.enabled = icon != null;
            keyIcon.sprite = icon;
        }

        if (textLabel != null)
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
            root.anchoredPosition =
                basePos + UnityEngine.Random.insideUnitCircle * failShakeMagnitude;
            yield return null;
        }

        root.anchoredPosition = basePos;
    }

    // =====================================================
    //  FINISH / FORCE STOP
    // =====================================================
    private void Finish(QTEResult result)
    {
        if (!active) return;
        active = false;

        if (directionalAction != null)
            directionalAction.performed -= OnArrow;

        gameObject.SetActive(false);
        finishCallback?.Invoke(result);
    }

    public void ForceStop()
    {
        Finish(QTEResult.Fail);
    }
}
