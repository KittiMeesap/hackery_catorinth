using System;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    // Fires when the active mission changes (sends missionId or null)
    public static event Action<string> OnMissionChanged;
    // Fires when switching to a new mission set (current set index, total sets count)
    public static event Action<int, int> OnSetChanged;

    [Header("Mission Set (single)")]
    public MissionSetSO missionSet;

    [Header("Mission Sets (multiple)")]
    public List<MissionSetSO> initialSets = new();

    [Header("Behavior")]
    public bool pointExitWhenAllSetsDone = true;
    public float switchSetDelay = 0.4f;

    // Runtime state
    private MissionSetSO activeSet;
    private readonly Queue<MissionSetSO> setQueue = new();

    private int currentMissionIndex = 0;
    private readonly List<MissionProgress> missionStates = new();
    private readonly Dictionary<string, MissionTarget> targets = new();

    private int currentSetOrdinal = 0;

    private string ActiveMissionId =>
        (currentMissionIndex < missionStates.Count) ? missionStates[currentMissionIndex].data.missionId : null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private async void Start()
    {

        PrepareInitialQueue();

        // Fallback: if no initial sets provided, try the single missionSet / GameManager
        if (setQueue.Count == 0 && activeSet == null)
        {
            if (missionSet == null && GameManager.Instance != null)
                missionSet = GameManager.Instance.missionSetForScene;

            if (missionSet != null)
                EnqueueSet(missionSet);
        }

        if (!StartNextSetInternal(out string warn))
        {
            if (!string.IsNullOrEmpty(warn)) Debug.LogWarning(warn);
            return;
        }

        await RefreshUITextAsync();
        OnMissionChanged?.Invoke(ActiveMissionId);
    }

    private void PrepareInitialQueue()
    {
        if (initialSets != null && initialSets.Count > 0)
        {
            foreach (var s in initialSets)
                if (s != null) setQueue.Enqueue(s);
        }
    }

    // --- Mission Set control ---
    public void StartSetImmediately(MissionSetSO set)
    {
        if (set == null) return;
        setQueue.Clear();
        setQueue.Enqueue(set);
        StartNextSet(true);
    }

    public void EnqueueSet(MissionSetSO set)
    {
        if (set == null) return;
        setQueue.Enqueue(set);
    }

    public void StartNextSet(bool immediate = false)
    {
        if (immediate)
        {
            if (!StartNextSetInternal(out var warn) && !string.IsNullOrEmpty(warn))
                Debug.LogWarning(warn);
            return;
        }
        StartCoroutine(Co_StartNextSetDelayed());
    }

    private System.Collections.IEnumerator Co_StartNextSetDelayed()
    {
        if (switchSetDelay > 0f)
            yield return new WaitForSeconds(switchSetDelay);

        if (!StartNextSetInternal(out var warn) && !string.IsNullOrEmpty(warn))
            Debug.LogWarning(warn);
    }

    private bool StartNextSetInternal(out string warning)
    {
        warning = null;

        // No more sets -> clean and (optionally) point to exit
        if (setQueue.Count == 0)
        {
            missionStates.Clear();
            currentMissionIndex = 0;
            UpdateUI();
            OnMissionChanged?.Invoke(null);


            return false;
        }

        activeSet = setQueue.Dequeue();
        currentSetOrdinal++;

        if (activeSet == null || activeSet.missions == null || activeSet.missions.Count == 0)
        {
            warning = "[MissionManager] Next MissionSet is null or empty. Skipping.";
            return StartNextSetInternal(out warning); // skip to next available set
        }

        // Reset state for the new set
        missionStates.Clear();
        foreach (var m in activeSet.missions)
            missionStates.Add(new MissionProgress(m));

        currentMissionIndex = 0;

        // Refresh localized UI for the new set (async)
        _ = RefreshUITextAsync();

        OnMissionChanged?.Invoke(ActiveMissionId);
        OnSetChanged?.Invoke(currentSetOrdinal, currentSetOrdinal + setQueue.Count);

        return true;
    }

    // --- Target registration for arrows ---
    public void RegisterTarget(string missionId, MissionTarget target)
    {
        if (string.IsNullOrEmpty(missionId) || target == null) return;
        targets[missionId] = target;
    }

    public void UnregisterTarget(string missionId, MissionTarget target)
    {
        if (string.IsNullOrEmpty(missionId)) return;
        if (targets.TryGetValue(missionId, out var t) && t == target)
            targets.Remove(missionId);
    }

    public bool IsActiveMission(string missionId) => ActiveMissionId == missionId;

    private bool AllMissionsComplete()
    {
        for (int i = 0; i < missionStates.Count; i++)
            if (!missionStates[i].isCompleted) return false;
        return true;
    }

    public void AddProgressStrict(string missionId, int amount = 1)
    {
        if (currentMissionIndex >= missionStates.Count) return;

        var step = missionStates[currentMissionIndex];

        if (step.data.missionId != missionId)
        {
            Debug.Log($"[Mission] Ignored progress for {missionId}, active mission = {step.data.missionId}");
            return;
        }

        step.currentAmount += amount;
        if (step.currentAmount > step.data.requiredAmount)
            step.currentAmount = step.data.requiredAmount;

        if (step.currentAmount >= step.data.requiredAmount)
        {
            step.isCompleted = true;
            Debug.Log($"[Mission] Completed: {step.data.missionId}");

            // Show the completed state (e.g., 1/1) before switching to the next mission
            UpdateUI();

            // Advance to the next mission
            currentMissionIndex++;
            OnMissionChanged?.Invoke(ActiveMissionId);
        }
        else
        {
            // Partial progress: just refresh UI
            UpdateUI();
        }

        // If the whole set is done, switch or point to exit
        if (AllMissionsComplete())
        {
            Debug.Log($"[Mission] MissionSet completed: {activeSet?.name ?? "(null)"}");

            if (setQueue.Count > 0)
            {
                StartNextSet();
            }
        }
    }

    // Convenient wrappers
    public void MarkReachComplete(string missionId) => AddProgressStrict(missionId, 1);
    public void MarkHackComplete(string missionId) => AddProgressStrict(missionId, 1);
    public void MarkCollectComplete(string missionId, int amount = 1) => AddProgressStrict(missionId, amount);
    public void MarkKillComplete(string missionId, int amount = 1) => AddProgressStrict(missionId, amount);
    public void MarkInteractComplete(string missionId) => AddProgressStrict(missionId, 1);

    // --- UI ---
    private async System.Threading.Tasks.Task RefreshUITextAsync()
    {
        foreach (var s in missionStates)
            s.cachedText = await s.data.GetDescriptionAsync();

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (UIManager.Instance == null || UIManager.Instance.missionText == null) return;

        System.Text.StringBuilder sb = new();

        for (int i = 0; i < missionStates.Count; i++)
        {
            var m = missionStates[i];
            string color;

            if (m.isCompleted)
                color = "#00FF00";   // green = completed
            else if (i == currentMissionIndex)
                color = "#1E90FF";   // blue (DodgerBlue) = active
            else
                color = "#000000";   // black = locked/not yet

            string progress = (m.data.missionType == MissionType.ReachLocation) ? "" : $" {m.currentAmount}/{m.data.requiredAmount}";
            sb.Append($"<color={color}>• {m.cachedText}{progress}</color>\n");
        }

        UIManager.Instance.missionText.text = sb.ToString().TrimEnd();
    }


    public void ResetMissionState()
    {
        missionStates.Clear();
        currentMissionIndex = 0;
        currentSetOrdinal = 0;
        activeSet = null;
        setQueue.Clear();
        targets.Clear();
    }


    private class MissionProgress
    {
        public MissionDataSO data;
        public int currentAmount;
        public bool isCompleted;
        public string cachedText;

        public MissionProgress(MissionDataSO data)
        {
            this.data = data;
            currentAmount = 0;
            isCompleted = false;
            cachedText = data != null ? data.name : "(null)";
        }
    }
}
