using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QTE_MashUI : MonoBehaviour
{
    [Header("UI")]
    public Image barFill;
    public Image keyIcon;
    public RectTransform barRoot;

    [Header("Mash Difficulty")]
    public float graceAfterHit = 0.1f;
    public float drainMultiplier = 1.35f;
    public float comboBoost = 0.05f;
    public float comboWindow = 0.22f;

    [Header("Juice")]
    public float bounceScale = 1.15f;
    public float bounceSpeed = 16f;
    public float returnSpeed = 10f;

    private float currentFill;
    private float fillPerHit;
    private float drainPerSec;
    private float successTarget;

    private bool active;
    private float graceTimer;
    private float comboTimer;
    private int comboCount;

    private Vector3 baseScale;
    private Vector3 targetScale;

    private InputAction hitAction;
    private Action<QTEResult> finishCallback;

    private void Awake()
    {
        if (barRoot == null)
            barRoot = barFill.rectTransform;

        baseScale = barRoot.localScale;
        targetScale = baseScale;
    }

    private void OnDisable()
    {
        if (hitAction != null)
            hitAction.started -= OnHit;
    }

    public void Begin(
        float perHit,
        float drain,
        float success,
        Action<QTEResult> onFinished)
    {
        gameObject.SetActive(true);

        fillPerHit = perHit;
        drainPerSec = drain;
        successTarget = success;
        finishCallback = onFinished;

        currentFill = 0f;
        barFill.fillAmount = 0f;

        graceTimer = 0f;
        comboTimer = 0f;
        comboCount = 0;

        active = true;

        barRoot.localScale = baseScale;
        targetScale = baseScale;

        keyIcon.sprite = KeyIconDatabase.GetIcon(LogicalInput.QTEConfirm);

        hitAction = GameInput.Instance.QTEConfirmAction;
        hitAction.started += OnHit;
    }

    private void Update()
    {
        if (!active) return;

        // ===== COMBO TIMER =====
        if (comboTimer > 0f)
            comboTimer -= Time.unscaledDeltaTime;
        else
            comboCount = 0;

        // ===== DRAIN =====
        if (graceTimer > 0f)
        {
            graceTimer -= Time.unscaledDeltaTime;
        }
        else
        {
            float difficultyScale = Mathf.Lerp(0.9f, 1.6f, currentFill);
            currentFill -= drainPerSec * drainMultiplier * difficultyScale * Time.unscaledDeltaTime;
        }

        currentFill = Mathf.Clamp01(currentFill);
        barFill.fillAmount = currentFill;

        // ===== JUICE SCALE RETURN =====
        targetScale = Vector3.Lerp(targetScale, baseScale, returnSpeed * Time.unscaledDeltaTime);
        barRoot.localScale = Vector3.Lerp(barRoot.localScale, targetScale, bounceSpeed * Time.unscaledDeltaTime);

        if (currentFill >= successTarget)
            Finish(QTEResult.Success);
    }

    private void OnHit(InputAction.CallbackContext ctx)
    {
        if (!active) return;

        // ===== COMBO =====
        comboCount = (comboTimer > 0f) ? comboCount + 1 : 1;
        comboTimer = comboWindow;
        graceTimer = graceAfterHit;

        float bonus = Mathf.Min(comboBoost * comboCount, fillPerHit * 0.9f);
        float gain = fillPerHit + bonus;

        currentFill += gain;
        currentFill = Mathf.Clamp01(currentFill);
        barFill.fillAmount = currentFill;

        // ===== JUICE BOUNCE =====
        float intensity = Mathf.Clamp01(0.5f + comboCount * 0.15f);
        targetScale = baseScale * Mathf.Lerp(1f, bounceScale, intensity);
    }

    private void Finish(QTEResult result)
    {
        active = false;

        if (hitAction != null)
            hitAction.started -= OnHit;

        barRoot.localScale = baseScale;
        gameObject.SetActive(false);
        finishCallback?.Invoke(result);
    }

    public void ForceStop() => Finish(QTEResult.Canceled);
}
