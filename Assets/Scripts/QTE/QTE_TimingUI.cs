using System;
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
    public Image keyIcon;

    [Header("Visual")]
    public float shakeMagnitude = 8f;
    public float shakeDuration = 0.15f;

    private float rotationSpeed;
    private float pointerAngle;
    private float zoneStart, zoneEnd;

    private bool active = false;

    private InputAction confirmHitAction;

    private Action<QTEResult> finishCallback;
    private string currentLogicalKey = "confirm";

    // =====================================================
    //  PUBLIC API
    // =====================================================
    public void Begin(string logicalKey, float speed, float zoneSize, Action<QTEResult> callback)
    {
        if (GameInput.Instance == null)
        {
            Debug.LogError("QTE_TimingUI: GameInput.Instance is null!");
            return;
        }

        // ensure active
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        enabled = true;

        finishCallback = callback;
        rotationSpeed = speed;
        currentLogicalKey = string.IsNullOrEmpty(logicalKey) ? "confirm" : logicalKey;

        pointerAngle = 0f;

        // random success zone
        zoneStart = UnityEngine.Random.Range(0f, 360f - zoneSize);
        zoneEnd = zoneStart + zoneSize;

        SetZoneVisual(zoneStart, zoneEnd);
        UpdateKeyIconSprite();

        // switch input map
        GameInput.Instance.SetModeQTE();

        // get action
        confirmHitAction = GameInput.Instance.QTEConfirmHitAction;
        confirmHitAction.performed += OnHit;

        active = true;
    }

    // =====================================================
    //  UPDATE
    // =====================================================
    private void Update()
    {
        if (!active) return;

        pointerAngle += rotationSpeed * Time.unscaledDeltaTime;
        pointerAngle %= 360f;

        if (pointer != null)
            pointer.transform.eulerAngles = new Vector3(0, 0, -pointerAngle);
    }

    // =====================================================
    //  INPUT CALLBACK
    // =====================================================
    private void OnHit(InputAction.CallbackContext ctx)
    {
        if (!active) return;

        // Dynamic key icon update
        string logical = KeyIconDatabase.GetLogicalFromContext(ctx);
        if (!string.IsNullOrEmpty(logical))
        {
            currentLogicalKey = logical;
            UpdateKeyIconSprite();
        }

        bool inside = pointerAngle >= zoneStart && pointerAngle <= zoneEnd;

        if (root != null)
            StartCoroutine(Shake());

        Finish(inside ? QTEResult.Success : QTEResult.Fail);
    }

    // =====================================================
    //  VISUAL HELPERS
    // =====================================================
    private void UpdateKeyIconSprite()
    {
        if (keyIcon == null) return;

        var icon = KeyIconDatabase.GetIcon(currentLogicalKey);
        if (icon != null)
        {
            keyIcon.enabled = true;
            keyIcon.sprite = icon;
        }
        else keyIcon.enabled = false;
    }

    private void SetZoneVisual(float startDeg, float endDeg)
    {
        if (successZone == null) return;

        float size = endDeg - startDeg;
        successZone.fillAmount = size / 360f;
        successZone.transform.eulerAngles = new Vector3(0, 0, -startDeg);
    }

    private IEnumerator Shake()
    {
        Vector2 basePos = root.anchoredPosition;

        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            root.anchoredPosition =
                basePos + UnityEngine.Random.insideUnitCircle * shakeMagnitude;

            yield return null;
        }

        root.anchoredPosition = basePos;
    }

    //  FINISH / FORCE STOP
    private void Finish(QTEResult result)
    {
        if (!active) return;
        active = false;

        if (confirmHitAction != null)
            confirmHitAction.performed -= OnHit;

        gameObject.SetActive(false);
        finishCallback?.Invoke(result);
    }

    public void ForceStop()
    {
        Finish(QTEResult.Fail);
    }
}
