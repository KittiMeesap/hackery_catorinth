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

    private InputAction cancelQTEAction;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (GameInput.Instance != null)
        {
            cancelQTEAction = GameInput.Instance.CancelQTEAction;
            if (cancelQTEAction != null)
                cancelQTEAction.performed += OnCancelQTE;
        }
    }

    private void OnDisable()
    {
        if (cancelQTEAction != null)
            cancelQTEAction.performed -= OnCancelQTE;
    }

    private void OnCancelQTE(InputAction.CallbackContext ctx)
    {
        if (!IsRunning) return;
        CancelCurrentQTE();
    }

    private void Start()
    {
        if (mashUI != null) mashUI.gameObject.SetActive(false);
        if (timingUI != null) timingUI.gameObject.SetActive(false);
        if (sequenceUI != null) sequenceUI.gameObject.SetActive(false);
    }

    // MASH QTE
    public void StartMashQTE(string logicalKey = "confirm")
    {
        if (IsRunning) return;

        IsRunning = true;
        GameInput.Instance.SetModeQTE();

        mashUI.gameObject.SetActive(true);
        mashUI.Begin(
            logicalKey,
            mashFillPerHit,
            mashDrainPerSec,
            mashSuccessValue,
            OnQTEFinishedInternal
        );
    }

    // TIMING QTE
    public void StartTimingQTE(float speed, float zoneSize, string logicalKey = "confirm")
    {
        if (IsRunning) return;

        IsRunning = true;
        GameInput.Instance.SetModeQTE();

        timingUI.gameObject.SetActive(true);
        timingUI.Begin(logicalKey, speed, zoneSize, OnQTEFinishedInternal);
    }

    // SEQUENCE QTE
    public void StartSequenceQTE(string[] sequence, float timePerKey = 2f)
    {
        if (IsRunning) return;

        IsRunning = true;
        GameInput.Instance.SetModeQTE();

        sequenceUI.gameObject.SetActive(true);
        sequenceUI.Begin(sequence, timePerKey, OnQTEFinishedInternal);
    }

    private void OnQTEFinishedInternal(QTEResult result)
    {
        IsRunning = false;
        GameInput.Instance.SetModePlayer();
        OnQTEFinished?.Invoke(result);
    }

    // CANCEL ALL QTE
    public void CancelCurrentQTE()
    {
        if (!IsRunning) return;

        if (mashUI != null) mashUI.ForceStop();
        if (timingUI != null) timingUI.ForceStop();
        if (sequenceUI != null) sequenceUI.ForceStop();

        IsRunning = false;
        GameInput.Instance.SetModePlayer();
    }
}
