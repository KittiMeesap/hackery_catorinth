using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QTE_SequenceUI : MonoBehaviour
{
    [Header("UI")]
    public Image keyIcon;
    public Image timerFill;

    [Header("SFX Keys")]
    public string sfxArrowHit = "SFX_QTE_Arrow_Hit";        // ?????
    public string sfxArrowWrong = "SFX_QTE_Arrow_Wrong";    // ?????

    private LogicalInput[] sequence;
    private int index;
    private float timePerKey;
    private float timer;

    private InputAction directionAction;
    private Action<QTEResult> finishCallback;
    private bool active;

    // =========================
    // BEGIN
    // =========================
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
            Finish(QTEResult.FailTimeout); // ? ??????????
        }
    }

    // =========================
    // INPUT
    // =========================
    private void OnDirection(InputAction.CallbackContext ctx)
    {
        if (!active) return;

        LogicalInput pressed = KeyIconDatabase.GetLogicalFromContext(ctx);

        if (pressed == sequence[index])
        {
            PlayHit();

            index++;

            if (index >= sequence.Length)
            {
                Finish(QTEResult.Success); // ? ??????????
            }
            else
            {
                timer = timePerKey;
                ShowKey();
            }
        }
        else
        {
            PlayWrong();
            Finish(QTEResult.FailWrongInput);
        }
    }

    // =========================
    // UI
    // =========================
    private void ShowKey()
    {
        if (keyIcon != null && sequence != null && index < sequence.Length)
            keyIcon.sprite = KeyIconDatabase.GetIcon(sequence[index]);
    }

    // =========================
    // FINISH
    // =========================
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

    // =========================
    // ?? SFX
    // =========================
    private void PlayHit()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUI(sfxArrowHit);
    }

    private void PlayWrong()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUI(sfxArrowWrong);
    }
}
