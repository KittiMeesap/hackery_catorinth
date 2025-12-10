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
    public float mashFillPerHit = 0.08f;
    public float mashDrainPerSec = 0.6f;
    public float mashSuccessValue = 1.0f;

    [Header("Debug")]
    public bool logDebug = true;

    private QTEType currentType = QTEType.None;
    private bool isRunning = false;

    public InputManager input { get; private set; }

    public Action<QTEResult> OnQTEFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Force load KeyIconDatabase
        var db = KeyIconDatabase.Instance;

        input = new InputManager();

        input.Player.Disable();
        input.UI.Disable();
        input.QTE.Disable();
        input.GameControls.Disable();
    }

    private void Start()
    {
        if (mashUI) mashUI.gameObject.SetActive(false);
        if (timingUI) timingUI.gameObject.SetActive(false);
        if (sequenceUI) sequenceUI.gameObject.SetActive(false);
    }

    // ================= MASH QTE =================
    public void StartMashQTE(string logicalKey)
    {
        if (isRunning) return;

        isRunning = true;
        currentType = QTEType.Mash;

        input.Player.Disable();
        input.UI.Disable();
        input.QTE.Enable();

        mashUI.gameObject.SetActive(true);
        mashUI.Begin(logicalKey, mashFillPerHit, mashDrainPerSec, mashSuccessValue, OnMashFinished);
    }

    // ================= TIMING =================
    public void StartTimingQTE(float speed, float zoneSize)
    {
        if (isRunning) return;

        isRunning = true;
        currentType = QTEType.Timing;

        input.Player.Disable();
        input.UI.Disable();
        input.QTE.Enable();

        timingUI.gameObject.SetActive(true);
        timingUI.Begin(speed, zoneSize, OnTimingFinished);
    }

    // ================= SEQUENCE =================
    public void StartSequenceQTE(string[] sequence, float timePerKey = 2f)
    {
        if (isRunning) return;

        isRunning = true;
        currentType = QTEType.Sequence;

        input.Player.Disable();
        input.UI.Disable();
        input.QTE.Enable();

        sequenceUI.gameObject.SetActive(true);
        sequenceUI.Begin(sequence, timePerKey, OnSequenceFinished);
    }

    // ================= CANCEL =================
    public void CancelCurrentQTE()
    {
        if (!isRunning) return;

        if (currentType == QTEType.Mash) mashUI.ForceStop();
        if (currentType == QTEType.Timing) timingUI.ForceStop();
        if (currentType == QTEType.Sequence) sequenceUI.ForceStop();

        ResetState();
    }

    private void ResetState()
    {
        isRunning = false;
        currentType = QTEType.None;

        input.QTE.Disable();
        input.UI.Disable();
        input.Player.Enable();
    }

    private void OnMashFinished(QTEResult result)
    {
        ResetState();
        OnQTEFinished?.Invoke(result);
    }

    private void OnTimingFinished(QTEResult result)
    {
        ResetState();
        OnQTEFinished?.Invoke(result);
    }

    private void OnSequenceFinished(QTEResult result)
    {
        ResetState();
        OnQTEFinished?.Invoke(result);
    }
}
