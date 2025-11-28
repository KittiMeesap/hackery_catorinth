using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HackableObject : MonoBehaviour
{
    public enum HackTriggerType { MouseHover, ProximityInteract }
    public HackTriggerType triggerType = HackTriggerType.MouseHover;

    [Header("Hack Settings")]
    public HackOptionSO defaultHackOption;
    public bool useHackOptions = false;
    public List<HackOptionSO> hackOptions;
    public float reduceTimeOnFail = 3f;

    [Header("Timer Settings")]
    public bool useHackTimer = false;
    public float hackTimeLimit = 5f;

    [Header("Hack State")]
    public bool allowRepeatHack = true;

    [Header("Mission Settings")]
    public bool completeMissionOnHack = false;
    public string missionId;

    protected bool isHacked = false;
    public bool IsHacked => isHacked;

    public static HackableObject ActiveProximityHackable;
    protected HackingUI currentUI;

    // let child classes control highlight
    public virtual bool ShouldShowHighlight => false;

    public virtual bool IsFullyOpened => false;
    public virtual bool IsOnCooldown => false;

    private void Start()
    {
        isHacked = false;
    }

    public virtual void RefreshHighlightExternal()
    {

    }

    public virtual void OnEnterHackingMode()
    {
        if (UIManager.Instance == null || UIManager.Instance.IsHacking)
            return;

        currentUI = UIManager.Instance.hackingUI;
        currentUI.SetCurrentHackTarget(this);

        PlayerController.Instance?.SetPhoneOut(true);
        PlayerController.Instance?.SetFrozen(true);
        GameManager.Instance?.ToggleHackingMode(true);

        var playerHacking = FindFirstObjectByType<PlayerHacking>();
        playerHacking?.SetCurrentHackedObject(this);

        if (triggerType == HackTriggerType.ProximityInteract)
            ActiveProximityHackable = this;

        HackOptionSO selected = defaultHackOption;

        if (useHackOptions && hackOptions != null && hackOptions.Count > 0)
            selected = hackOptions[0];

        if (selected != null)
            OnOptionSelected(selected);
    }

    protected virtual void OnOptionSelected(HackOptionSO selectedOption)
    {
        var sequence = selectedOption.isRandom
            ? GenerateRandomSequence(selectedOption.randomLength)
            : new List<ArrowUI.Direction>(selectedOption.sequence);

        currentUI.ShowSingleOptionSequence(
            sequence,
            transform,
            selectedOption.icon,
            () => HandleHackOptionComplete(selectedOption),
            OnHackFailed,
            useHackTimer,
            hackTimeLimit
        );
    }

    protected virtual void HandleHackOptionComplete(HackOptionSO option)
    {
        // mark hacked only for the moment of this action
        isHacked = true;

        PerformHackedAction(option);

        HideHackingUI();

        // allow next hack after exit
        StartCoroutine(ResetHackNextFrame());

        if (completeMissionOnHack && !string.IsNullOrEmpty(missionId))
            MissionManager.Instance?.MarkHackComplete(missionId);
    }

    private IEnumerator ResetHackNextFrame()
    {
        // wait 1 frame so highlight/Interact logic has time to update
        yield return null;
        isHacked = false;
    }

    protected virtual void PerformHackedAction(HackOptionSO option) { }

    public void HideHackingUI()
    {
        currentUI?.HideHackingUI();

        if (triggerType == HackTriggerType.ProximityInteract &&
            ActiveProximityHackable == this)
            ActiveProximityHackable = null;

        GameManager.Instance?.ToggleHackingMode(false);

        PlayerController.Instance?.ClearInputAndVelocity();
        StartCoroutine(UnfreezeAfterDelay(0.4f));
    }

    private IEnumerator UnfreezeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayerController.Instance?.SetFrozen(false);

        yield return new WaitForSeconds(0.15f);
        PlayerController.Instance?.SetPhoneOut(false);
        PlayerController.Instance?.ClearInputAndVelocity();
    }

    public virtual void OnHackFailed()
    {
        HideHackingUI();

        if (triggerType == HackTriggerType.ProximityInteract &&
            ActiveProximityHackable == this)
            ActiveProximityHackable = null;

        var timer = FindFirstObjectByType<CountdownTimer>();
        timer?.ReduceTime(reduceTimeOnFail);

        PlayerController.Instance?.ClearInputAndVelocity();
    }

    protected List<ArrowUI.Direction> GenerateRandomSequence(int length)
    {
        var dirs = new List<ArrowUI.Direction>
        {
            ArrowUI.Direction.Up,
            ArrowUI.Direction.Down,
            ArrowUI.Direction.Left,
            ArrowUI.Direction.Right
        };

        var result = new List<ArrowUI.Direction>();
        for (int i = 0; i < length; i++)
            result.Add(dirs[Random.Range(0, dirs.Count)]);

        return result;
    }

    public virtual void ResetHack() { isHacked = false; }

    private void OnDisable()
    {
        if (triggerType == HackTriggerType.ProximityInteract &&
            ActiveProximityHackable == this)
            ActiveProximityHackable = null;
    }
}
