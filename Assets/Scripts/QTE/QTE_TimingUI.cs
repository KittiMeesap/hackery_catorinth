using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QTE_TimingUI : MonoBehaviour
{
    [Header("UI Refs")]
    public RectTransform pointer;
    public Image successZone;
    public Image keyIcon;

    // INTERNAL
    private float speed;
    private float zoneStart;
    private float zoneEnd;
    private float currentAngle;

    private InputAction confirmAction;
    private Action<QTEResult> finishCallback;
    private LogicalInput currentKey = LogicalInput.QTEConfirm;
    private bool active;

    // BEGIN QTE
    public void Begin(float rotateSpeed, float zoneSize, Action<QTEResult> callback)
    {
        gameObject.SetActive(true);

        speed = rotateSpeed;
        finishCallback = callback;

        //  RANDOM SUCCESS ZONE
        zoneStart = UnityEngine.Random.Range(0f, 360f - zoneSize);
        zoneEnd = (zoneStart + zoneSize) % 360f;

        successZone.fillAmount = zoneSize / 360f;
        successZone.rectTransform.localEulerAngles =
            new Vector3(0, 0, -zoneStart);

        // RESET POINTER
        currentAngle = 0f;
        pointer.localEulerAngles = Vector3.zero;

        UpdateKeyIcon();

        // INPUT
        GameInput.Instance.SetModeQTE();
        confirmAction = GameInput.Instance.QTEConfirmAction;
        confirmAction.performed += OnHit;

        active = true;
    }

    // UPDATE POINTER
    private void Update()
    {
        if (!active)
            return;

        currentAngle += speed * Time.unscaledDeltaTime;
        currentAngle %= 360f;

        pointer.localEulerAngles = new Vector3(0, 0, -currentAngle);
    }

    // CONFIRM INPUT
    private void OnHit(InputAction.CallbackContext ctx)
    {
        currentKey = KeyIconDatabase.GetLogicalFromContext(ctx);
        UpdateKeyIcon();

        bool success = IsAngleInZone(currentAngle);
        Finish(success ? QTEResult.Success : QTEResult.FailWrongInput);
    }

    // ZONE CHECK (IMPORTANT)
    private bool IsAngleInZone(float angle)
    {
        // Normal case
        if (zoneStart < zoneEnd)
            return angle >= zoneStart && angle <= zoneEnd;

        // Wrap-around case (e.g. 300 -> 40)
        return angle >= zoneStart || angle <= zoneEnd;
    }

    // ICON
    private void UpdateKeyIcon()
    {
        if (keyIcon != null)
            keyIcon.sprite = KeyIconDatabase.GetIcon(currentKey);
    }

    // FINISH
    private void Finish(QTEResult result)
    {
        active = false;

        if (confirmAction != null)
            confirmAction.performed -= OnHit;

        gameObject.SetActive(false);
        finishCallback?.Invoke(result);
    }

    // FORCE CANCEL
    public void ForceStop()
    {
        Finish(QTEResult.Canceled);
    }
}
