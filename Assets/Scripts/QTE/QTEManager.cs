using System;
using UnityEngine;

public enum QTEType
{
    None,
    Mash,
    Timing,
    Sequence
}

public enum QTEResult
{
    Success,
    Fail
}

public class QTEManager : MonoBehaviour
{
    public static QTEManager Instance { get; private set; }

    [Header("UI References")]
    public QTE_MashUI mashUI;
    public QTE_TimingUI timingUI;
    public QTE_SequenceUI sequenceUI;

    [Header("Mash Settings")]
    public float mashFillPerHit = 0.12f;
    public float mashDrainPerSec = 0.25f;
    public float mashSuccessValue = 1.0f;

    [Header("Debug")]
    public bool logDebug = true;

    // CURRENT STATE
    private QTEType currentType = QTEType.None;
    private bool isRunning = false;

    public InputManager input { get; private set; }

    // CALLBACKS
    public Action<QTEResult> OnQTEFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        input = new InputManager();
    }

    public void StartMashQTE(string logicalKey)
    {
        if (isRunning) return;

        currentType = QTEType.Mash;
        isRunning = true;

        mashUI.gameObject.SetActive(true);
        mashUI.Begin(
            logicalKey,
            mashFillPerHit,
            mashDrainPerSec,
            mashSuccessValue,
            OnMashFinished
        );
    }

    public void StartTimingQTE(float speed, float zoneSize)
    {
        if (isRunning) return;

        currentType = QTEType.Timing;
        isRunning = true;

        timingUI.gameObject.SetActive(true);
        timingUI.Begin(speed, zoneSize, OnTimingFinished);
    }

    public void StartSequenceQTE(string[] sequence, float timePerKey = 2f)
    {
        if (isRunning) return;

        currentType = QTEType.Sequence;
        isRunning = true;

        sequenceUI.gameObject.SetActive(true);
        sequenceUI.Begin(sequence, timePerKey, OnSequenceFinished);
    }

    public void CancelCurrentQTE()
    {
        if (!isRunning) return;

        if (currentType == QTEType.Mash) mashUI.ForceStop();
        if (currentType == QTEType.Timing) timingUI.ForceStop();
        if (currentType == QTEType.Sequence) sequenceUI.ForceStop();

        currentType = QTEType.None;
        isRunning = false;
    }

    // =============================================================
    //  CALLBACKS FROM UIs
    // =============================================================

    private void OnMashFinished(QTEResult result)
    {
        if (logDebug) Debug.Log($"Mash QTE: {result}");
        currentType = QTEType.None;
        isRunning = false;
        OnQTEFinished?.Invoke(result);
    }

    private void OnTimingFinished(QTEResult result)
    {
        if (logDebug) Debug.Log($"Timing QTE: {result}");
        currentType = QTEType.None;
        isRunning = false;
        OnQTEFinished?.Invoke(result);
    }

    private void OnSequenceFinished(QTEResult result)
    {
        if (logDebug) Debug.Log($"Sequence QTE: {result}");
        currentType = QTEType.None;
        isRunning = false;
        OnQTEFinished?.Invoke(result);
    }
}
