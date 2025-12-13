using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QTE_TimingUI : MonoBehaviour
{
    public RectTransform pointer;
    public Image successZone;
    public Image keyIcon;

    private float speed;
    private float zoneStart;
    private float zoneEnd;

    private InputAction confirmAction;
    private Action<QTEResult> finishCallback;
    private LogicalInput currentKey = LogicalInput.QTEConfirm;
    private bool active;

    public void Begin(float rotateSpeed, float zoneSize, Action<QTEResult> callback)
    {
        speed = rotateSpeed;
        finishCallback = callback;

        zoneStart = UnityEngine.Random.Range(0f, 360f - zoneSize);
        zoneEnd = zoneStart + zoneSize;

        successZone.fillAmount = zoneSize / 360f;
        successZone.transform.eulerAngles = new Vector3(0, 0, -zoneStart);

        UpdateKeyIcon();

        GameInput.Instance.SetModeQTE();
        confirmAction = GameInput.Instance.QTEConfirmAction;
        confirmAction.performed += OnHit;

        active = true;
    }

    private void Update()
    {
        if (!active) return;
        pointer.Rotate(0, 0, -speed * Time.unscaledDeltaTime);
    }

    private void OnHit(InputAction.CallbackContext ctx)
    {
        currentKey = KeyIconDatabase.GetLogicalFromContext(ctx);
        UpdateKeyIcon();

        float angle = pointer.eulerAngles.z;
        bool success = angle >= zoneStart && angle <= zoneEnd;

        Finish(success ? QTEResult.Success : QTEResult.Fail);
    }

    private void UpdateKeyIcon()
    {
        keyIcon.sprite = KeyIconDatabase.GetIcon(currentKey);
    }

    private void Finish(QTEResult result)
    {
        active = false;

        if (confirmAction != null)
            confirmAction.performed -= OnHit;

        gameObject.SetActive(false);
        finishCallback?.Invoke(result);
    }

    public void ForceStop() => Finish(QTEResult.Fail);
}
