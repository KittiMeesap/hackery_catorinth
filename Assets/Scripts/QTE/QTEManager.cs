using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum QTEType { None, Mash, Timing, Sequence }
public enum QTEResult { Success, Fail }

public class QTEManager : MonoBehaviour
{
    public static QTEManager Instance { get; private set; }

    [Header("QTE UI References")]
    public QTE_MashUI mashUI;
    public QTE_TimingUI timingUI;
    public QTE_SequenceUI sequenceUI;

    [Header("Mash QTE Settings")]
    public float mashFillPerHit = 0.08f;
    public float mashDrainPerSec = 0.6f;
    public float mashSuccessValue = 1.0f;

    public bool IsRunning { get; private set; }
    public Action<QTEResult> OnQTEFinished;

    // ------------------------
    //  NEW INPUT BINDINGS
    // ------------------------
    public PlayerInput Input => GameInput.Instance.PlayerInputComponent;

    public InputAction QTE_Hit => GameInput.Instance.QTEConfirmHitAction;
    public InputAction QTE_Arrow => GameInput.Instance.QTEDirectionalAction;

    // ------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (mashUI != null) mashUI.gameObject.SetActive(false);
        if (timingUI != null) timingUI.gameObject.SetActive(false);
        if (sequenceUI != null) sequenceUI.gameObject.SetActive(false);
    }

    // =====================================================
    // MASH QTE
    // =====================================================
    public void StartMashQTE(string logicalKey = "confirm")
    {
        if (IsRunning) return;
        if (mashUI == null)
        {
            Debug.LogError("QTEManager: MashUI not assigned!");
            return;
        }

        IsRunning = true;

        GameInput.Instance.SetModeQTE();

        mashUI.gameObject.SetActive(true);
        mashUI.Begin(
            logicalKey,
            mashFillPerHit,
            mashDrainPerSec,
            mashSuccessValue,
            OnMashFinished
        );
    }

    private void OnMashFinished(QTEResult result)
    {
        IsRunning = false;
        GameInput.Instance.SetModePlayer();
        OnQTEFinished?.Invoke(result);
    }

    // =====================================================
    // TIMING QTE
    // =====================================================
    public void StartTimingQTE(float speed, float zoneSize, string logicalKey = "confirm")
    {
        if (IsRunning) return;
        if (timingUI == null)
        {
            Debug.LogError("QTEManager: TimingUI not assigned!");
            return;
        }

        IsRunning = true;
        GameInput.Instance.SetModeQTE();

        timingUI.gameObject.SetActive(true);
        timingUI.Begin(logicalKey, speed, zoneSize, OnTimingFinished);
    }

    private void OnTimingFinished(QTEResult result)
    {
        IsRunning = false;
        GameInput.Instance.SetModePlayer();
        OnQTEFinished?.Invoke(result);
    }

    // =====================================================
    // SEQUENCE QTE
    // =====================================================
    public void StartSequenceQTE(string[] sequence, float timePerKey = 2f)
    {
        if (IsRunning) return;
        if (sequenceUI == null)
        {
            Debug.LogError("SequenceUI not assigned!");
            return;
        }
        if (sequence == null || sequence.Length == 0)
        {
            Debug.LogError("Sequence cannot be empty!");
            return;
        }

        IsRunning = true;
        GameInput.Instance.SetModeQTE();

        sequenceUI.gameObject.SetActive(true);
        sequenceUI.Begin(sequence, timePerKey, OnSequenceFinished);
    }

    private void OnSequenceFinished(QTEResult result)
    {
        IsRunning = false;
        GameInput.Instance.SetModePlayer();
        OnQTEFinished?.Invoke(result);
    }

    // =====================================================
    // CANCEL ALL QTE
    // =====================================================
    public void CancelCurrentQTE()
    {
        if (!IsRunning) return;

        mashUI?.ForceStop();
        timingUI?.ForceStop();
        sequenceUI?.ForceStop();

        IsRunning = false;
        GameInput.Instance.SetModePlayer();
    }
}
