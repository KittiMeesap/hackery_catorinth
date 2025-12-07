using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QTE_TimingUI : MonoBehaviour
{
    [Header("UI")]
    public RectTransform root;
    public Image pointer;
    public Image successZone;

    [Header("Effect")]
    public Color successColor = Color.green;
    public Color failColor = Color.red;
    public float flashDuration = 0.15f;
    public float shakeMagnitude = 8f;
    public float shakeDuration = 0.15f;

    private float rotationSpeed;
    private float pointerAngle;
    private float zoneStart, zoneEnd;

    private bool active = false;

    private InputManager input => QTEManager.Instance.input;
    private InputAction hitAction;

    private System.Action<QTEResult> finishCallback;

    // =============================================================
    public void Begin(float speedDegPerSec, float zoneSizeDeg, System.Action<QTEResult> onFinished)
    {
        rotationSpeed = speedDegPerSec;
        finishCallback = onFinished;

        // random zone
        zoneStart = Random.Range(0f, 360f - zoneSizeDeg);
        zoneEnd = zoneStart + zoneSizeDeg;
        SetZoneVisual(zoneStart, zoneEnd);

        pointerAngle = 0f;
        pointer.transform.eulerAngles = Vector3.zero;

        // input
        input.QTE.Enable();
        hitAction = input.QTE.ConfirmHit;
        hitAction.performed += OnHit;

        active = true;
        gameObject.SetActive(true);
    }

    // =============================================================
    private void Update()
    {
        if (!active) return;

        pointerAngle += rotationSpeed * Time.unscaledDeltaTime;
        pointerAngle %= 360f;

        pointer.transform.eulerAngles = new Vector3(0, 0, -pointerAngle);
    }

    private void OnHit(InputAction.CallbackContext ctx)
    {
        if (!active) return;

        bool success = pointerAngle >= zoneStart && pointerAngle <= zoneEnd;

        if (success)
        {
            StartCoroutine(Flash(successColor));
            StartCoroutine(Shake());
            Finish(QTEResult.Success);
        }
        else
        {
            StartCoroutine(Flash(failColor));
            StartCoroutine(Shake());
            Finish(QTEResult.Fail);
        }
    }

    // =============================================================
    private void SetZoneVisual(float startDeg, float endDeg)
    {
        float size = endDeg - startDeg;
        successZone.fillAmount = size / 360f;
        successZone.transform.eulerAngles = new Vector3(0, 0, -startDeg);
    }

    private IEnumerator Flash(Color color)
    {
        Color baseColor = successZone.color;
        successZone.color = color;
        float t = 0f;

        while (t < flashDuration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        successZone.color = baseColor;
    }

    private IEnumerator Shake()
    {
        Vector3 basePos = root.anchoredPosition;
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float strength = shakeMagnitude * (1f - t / shakeDuration);
            root.anchoredPosition = basePos + (Vector3)Random.insideUnitCircle * strength;
            yield return null;
        }

        root.anchoredPosition = basePos;
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
