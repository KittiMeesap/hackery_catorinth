using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class HackableObject : MonoBehaviour
{
    public enum HackTriggerType { MouseHover, ProximityInteract }
    public HackTriggerType triggerType = HackTriggerType.MouseHover;

    [Header("Hack Settings")]
    public HackOptionSO defaultHackOption;
    public bool useHackOptions = false;
    public List<HackOptionSO> hackOptions;

    [Header("Timer Settings")]
    public bool useHackTimer = false;
    public float hackTimeLimit = 5f;

    [Header("Hack State")]
    public bool allowRepeatHack = false;

    [Header("Mission Settings")]
    public bool completeMissionOnHack = false;
    public string missionId;


    protected bool isHacked = false;
    public static HackableObject ActiveProximityHackable;
    protected HackingUI currentUI;

    public bool IsHacked => isHacked;

    private void Start()
    {
        isHacked = false;
    }


    public virtual void OnEnterHackingMode()
    {
        if ((!allowRepeatHack && isHacked) || UIManager.Instance == null || UIManager.Instance.IsHacking)
            return;

        currentUI = UIManager.Instance.hackingUI;

        PlayerController.Instance?.SetPhoneOut(true);
        PlayerController.Instance?.SetFrozen(true);

        GameManager.Instance?.ToggleHackingMode(true);

        var playerHacking = Object.FindFirstObjectByType<PlayerHacking>();
        playerHacking?.SetCurrentHackedObject(this);

        if (triggerType == HackTriggerType.ProximityInteract)
            ActiveProximityHackable = this;

        if (useHackOptions && hackOptions != null && hackOptions.Count > 1)
        {
            UIManager.Instance.StartMultiOptionHack(hackOptions, transform, HandleHackOptionComplete, useHackTimer, hackTimeLimit);
        }
        else
        {
            var selected = (hackOptions != null && hackOptions.Count == 1) ? hackOptions[0] : defaultHackOption;
            if (selected != null) OnOptionSelected(selected);
        }
    }

    protected virtual void OnOptionSelected(HackOptionSO selectedOption)
    {
        if (selectedOption == null) return;

        var sequence = selectedOption.isRandom
            ? GenerateRandomSequence(selectedOption.randomLength)
            : new List<ArrowUI.Direction>(selectedOption.sequence);

        if (!selectedOption.isRandom && (sequence == null || sequence.Count == 0))
        {
            return;
        }

        currentUI.ShowSingleOptionSequence(sequence, transform, selectedOption.icon,
            () => HandleHackOptionComplete(selectedOption),
            OnHackFailed,
            useHackTimer,
            hackTimeLimit);
    }

    protected virtual void HandleHackOptionComplete(HackOptionSO option)
    {
        if (!allowRepeatHack)
            isHacked = true;

        PerformHackedAction(option);

        HideHackingUI();

        if (completeMissionOnHack && !string.IsNullOrEmpty(missionId))
            MissionManager.Instance?.MarkHackComplete(missionId);
    }

    public void HideHackingUI()
    {
        currentUI?.HideHackingUI();

        if (triggerType == HackTriggerType.ProximityInteract && ActiveProximityHackable == this)
            ActiveProximityHackable = null;

        GameManager.Instance?.ToggleHackingMode(false);

        PlayerController.Instance?.ClearInputAndVelocity();

        StartCoroutine(UnfreezeAfterDelay(0.2f));
    }

    private IEnumerator UnfreezeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayerController.Instance?.SetFrozen(false);
        PlayerController.Instance?.SetPhoneOut(false);
        PlayerController.Instance?.ClearInputAndVelocity();
    }


    public virtual void OnHackFailed()
    {
        HideHackingUI();

        if (triggerType == HackTriggerType.ProximityInteract && ActiveProximityHackable == this)
            ActiveProximityHackable = null;

        PlayerController.Instance?.ClearInputAndVelocity();
    }

    protected virtual void PerformHackedAction(HackOptionSO option) { }

    public virtual void ResetHack() => isHacked = false;

    protected List<ArrowUI.Direction> GenerateRandomSequence(int length)
    {
        var options = new List<ArrowUI.Direction>
        {
            ArrowUI.Direction.Up,
            ArrowUI.Direction.Down,
            ArrowUI.Direction.Left,
            ArrowUI.Direction.Right
        };

        var result = new List<ArrowUI.Direction>();
        for (int i = 0; i < length; i++)
            result.Add(options[Random.Range(0, options.Count)]);
        return result;
    }

    private void OnDisable()
    {
        if (triggerType == HackTriggerType.ProximityInteract && ActiveProximityHackable == this)
            ActiveProximityHackable = null;
    }
}
