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

    public QTE_MashUI mashUI;
    public QTE_TimingUI timingUI;
    public QTE_SequenceUI sequenceUI;

    public float mashFillPerHit = 0.08f;
    public float mashDrainPerSec = 0.6f;
    public float mashSuccessValue = 1.0f;

    private bool isRunning = false;
    public Action<QTEResult> OnQTEFinished;

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
        mashUI?.gameObject.SetActive(false);
        timingUI?.gameObject.SetActive(false);
        sequenceUI?.gameObject.SetActive(false);
    }

    // MASH
    public void StartMashQTE(string logicalKey)
    {
        if (isRunning) return;
        isRunning = true;

        GameInput.Instance.SetModeQTE();

        mashUI.gameObject.SetActive(true);
        mashUI.Begin(logicalKey,
            mashFillPerHit,
            mashDrainPerSec,
            mashSuccessValue,
            OnMashFinished);
    }

    private void OnMashFinished(QTEResult result)
    {
        isRunning = false;
        GameInput.Instance.SetModePlayer();
        OnQTEFinished?.Invoke(result);
    }

    // TIMING
    public void StartTimingQTE(float speed, float zoneSize)
    {
        if (isRunning) return;
        isRunning = true;

        GameInput.Instance.SetModeQTE();

        timingUI.gameObject.SetActive(true);
        timingUI.Begin("space", speed, zoneSize, OnTimingFinished);
    }

    private void OnTimingFinished(QTEResult result)
    {
        isRunning = false;
        GameInput.Instance.SetModePlayer();
        OnQTEFinished?.Invoke(result);
    }

    // SEQUENCE
    public void StartSequenceQTE(string[] sequence, float timePerKey = 2f)
    {
        if (isRunning) return;
        isRunning = true;

        GameInput.Instance.SetModeQTE();

        sequenceUI.gameObject.SetActive(true);
        sequenceUI.Begin(sequence, timePerKey, OnSequenceFinished);
    }

    private void OnSequenceFinished(QTEResult result)
    {
        isRunning = false;
        GameInput.Instance.SetModePlayer();
        OnQTEFinished?.Invoke(result);
    }

    public void CancelCurrentQTE()
    {
        if (!isRunning) return;

        mashUI?.ForceStop();
        timingUI?.ForceStop();
        sequenceUI?.ForceStop();

        isRunning = false;
        GameInput.Instance.SetModePlayer();
    }
}