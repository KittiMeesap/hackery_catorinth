using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using System.Collections;

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

    private InputAction hitAction;

    private Action<QTEResult> finishCallback;

    public void Begin(string logicalKey, float speed, float zoneSize, Action<QTEResult> callback)
    {
        finishCallback = callback;
        rotationSpeed = speed;

        pointerAngle = 0;

        zoneStart = UnityEngine.Random.Range(0f, 360f - zoneSize);
        zoneEnd = zoneStart + zoneSize;

        SetZoneVisual(zoneStart, zoneEnd);

        keyIcon.sprite = KeyIconDatabase.GetIcon(logicalKey);
        keyIcon.enabled = keyIcon.sprite != null;

        GameInput.Instance.SetModeQTE();
        hitAction = GameInput.Instance.QTEConfirmHitAction;
        if (hitAction != null)
            hitAction.performed += OnHit;

        active = true;
        gameObject.SetActive(true);
    }

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

        bool inside = pointerAngle >= zoneStart && pointerAngle <= zoneEnd;

        StartCoroutine(Shake());

        Finish(inside ? QTEResult.Success : QTEResult.Fail);
    }

    private void SetZoneVisual(float startDeg, float endDeg)
    {
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
