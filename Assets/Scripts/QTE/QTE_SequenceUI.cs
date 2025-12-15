using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QTE_SequenceUI : MonoBehaviour
{
    public Image keyIcon;
    public Image timerFill;

    private LogicalInput[] sequence;
    private int index;
    private float timePerKey;
    private float timer;

    private InputAction directionAction;
    private Action<QTEResult> finishCallback;
    private bool active;

    public void Begin(LogicalInput[] seq, float time, Action<QTEResult> callback)
    {
        sequence = seq;
        timePerKey = time;
        finishCallback = callback;

        index = 0;
        timer = timePerKey;
        active = true;

        ShowKey();

        GameInput.Instance.SetModeQTE();

        directionAction = GameInput.Instance.QTEDirectionAction;
        if (directionAction != null)
            directionAction.performed += OnDirection;
    }

    private void Update()
    {
        if (!active) return;

        timer -= Time.unscaledDeltaTime;
        timerFill.fillAmount = Mathf.Clamp01(timer / timePerKey);

        if (timer <= 0f)
        {
            Finish(QTEResult.FailTimeout);
        }
    }

    private void OnDirection(InputAction.CallbackContext ctx)
    {
        if (!active) return;

        LogicalInput pressed = KeyIconDatabase.GetLogicalFromContext(ctx);

        if (pressed == sequence[index])
        {
            index++;

            if (index >= sequence.Length)
            {
                Finish(QTEResult.Success);
            }
            else
            {
                timer = timePerKey;
                ShowKey();
            }
        }
        else
        {
            Finish(QTEResult.FailWrongInput);
        }
    }

    private void ShowKey()
    {
        if (keyIcon != null && sequence != null && index < sequence.Length)
            keyIcon.sprite = KeyIconDatabase.GetIcon(sequence[index]);
    }

    private void Finish(QTEResult result)
    {
        if (!active) return;

        active = false;

        if (directionAction != null)
            directionAction.performed -= OnDirection;

        directionAction = null;

        gameObject.SetActive(false);
        finishCallback?.Invoke(result);
    }

    public void ForceStop()
    {
        Finish(QTEResult.Canceled);
    }
}
