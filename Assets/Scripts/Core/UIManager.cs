using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI References")]
    public HackingUI hackingUI;

    [Header("Screen Fade")]
    public ScreenFader screenFader;

    [Header("Prompt UI")]
    public GameObject promptUI;

    [Header("Mission UI")]
    public TextMeshProUGUI missionText;

    private IInteractable currentPromptTarget;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowInteractPrompt(IInteractable target)
    {
        if (promptUI == null) return;

        currentPromptTarget = target;
        promptUI.SetActive(true);

        Transform followPoint = target.GetPromptPoint();
        if (followPoint != null)
        {
            promptUI.transform.position = followPoint.position + Vector3.up * 0.5f;
            promptUI.transform.rotation = Quaternion.identity;
        }
    }

    public void HideInteractPrompt(IInteractable target)
    {
        if (promptUI != null)
            promptUI.SetActive(false);

        currentPromptTarget = null;
    }

    public void ShowLockedMessage() { }

    public void StartMultiOptionHack(List<HackOptionSO> options, Transform worldTarget, System.Action<HackOptionSO> onOptionSelected)
    {
        hackingUI?.ShowMultiOptionUI(options, worldTarget, onOptionSelected);
    }

    public void StartMultiOptionHack(List<HackOptionSO> options, Transform worldTarget, System.Action<HackOptionSO> onOptionSelected, bool useTimer, float timerDuration)
    {
        hackingUI?.ShowMultiOptionUI(options, worldTarget, onOptionSelected, useTimer, timerDuration);
    }

    public void SubmitArrow(ArrowUI.Direction direction)
    {
        hackingUI?.SubmitInput(direction);
    }

    public void StopHacking()
    {
        hackingUI?.HideHackingUI();
    }

    public bool IsHacking => hackingUI != null && hackingUI.IsActive;

    public void ResetAllUI()
    {
        StopHacking();
    }
}
