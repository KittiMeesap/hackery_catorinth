using System;
using System.Collections;
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

    [Header("Mash Settings")]
    public float mashFillPerHit = 0.08f;
    public float mashDrainPerSec = 0.6f;
    public float mashSuccessValue = 1f;

    public bool IsRunning { get; private set; }
    public Action<QTEResult> OnQTEFinished;

    private InputAction cancelQTEAction;
    private bool cancelBound;

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

        StartCoroutine(BindCancelWhenReady());
    }

    private IEnumerator BindCancelWhenReady()
    {
        while (GameInput.Instance == null)
            yield return null;

        BindCancel();
    }

    private void OnDisable()
    {
        UnbindCancel();
    }

    private void OnDestroy()
    {
        UnbindCancel();
        if (Instance == this)
            Instance = null;
    }

    private void BindCancel()
    {
        if (cancelBound) return;

        cancelQTEAction = GameInput.Instance.CancelQTEAction;
        if (cancelQTEAction == null)
        {
            Debug.LogWarning("QTEManager: CancelQTEAction is null");
            return;
        }

        cancelQTEAction.performed += OnCancelQTE;
        cancelBound = true;
    }

    private void UnbindCancel()
    {
        if (!cancelBound) return;

        if (cancelQTEAction != null)
            cancelQTEAction.performed -= OnCancelQTE;

        cancelQTEAction = null;
        cancelBound = false;
    }

    private void OnCancelQTE(InputAction.CallbackContext ctx)
    {
        if (IsRunning)
            CancelCurrentQTE();
    }

    // ==============================
    // START QTE
    // ==============================
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
            FinishInternal
        );
    }

    public void StartTimingQTE(float speed, float zoneSize, string logicalKey = "confirm")
    {
        if (IsRunning) return;

        IsRunning = true;
        GameInput.Instance.SetModeQTE();

        timingUI.gameObject.SetActive(true);
        timingUI.Begin(logicalKey, speed, zoneSize, FinishInternal);
    }

    public void StartSequenceQTE(string[] sequence, float timePerKey = 2f)
    {
        if (IsRunning) return;

        IsRunning = true;
        GameInput.Instance.SetModeQTE();

        sequenceUI.gameObject.SetActive(true);
        sequenceUI.Begin(sequence, timePerKey, FinishInternal);
    }

    private void FinishInternal(QTEResult result)
    {
        IsRunning = false;
        GameInput.Instance.SetModePlayer();
        OnQTEFinished?.Invoke(result);
    }

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
