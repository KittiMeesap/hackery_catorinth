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
    private InputAction arrowAction;

    private Coroutine punchRoutine;
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
        if (timer <= 0f)
        {
            StartCoroutine(FailFlash());
            Finish(QTEResult.Fail);
        }
    }

    private void OnArrow(InputAction.CallbackContext ctx)
    {
        if (!active) return;

        Vector2 v = ctx.ReadValue<Vector2>();
        string pressed = DirectionFromVector(v);
        if (pressed == null) return;

        string expected = sequence[index].ToLowerInvariant();

        if (pressed == expected)
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
            // WRONG KEY — do NOT fail immediately
            StartCoroutine(FailFlash());

            // Pick a NEW RANDOM ARROW for this index
            sequence[index] = RandomArrow();

            // Reset timer
            timer = timePerKey;

            // Show new key
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
        Color baseCol = background.color;
        background.color = failFlashColor;

        yield return new WaitForSecondsRealtime(failFlashDuration);

        background.color = baseCol;
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
