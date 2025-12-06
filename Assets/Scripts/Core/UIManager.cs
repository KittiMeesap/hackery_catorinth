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
    private Camera mainCam;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        mainCam = Camera.main;
    }

    private void Update()
    {
        UpdatePromptFollow();
    }

    //INTERACT PROMPT
    public void ShowInteractPrompt(IInteractable target)
    {
        if (promptUI == null) return;

        currentPromptTarget = target;
        promptUI.SetActive(true);

        UpdatePromptFollow();
    }

    public void HideInteractPrompt(IInteractable target)
    {
        if (promptUI != null)
            promptUI.SetActive(false);

        currentPromptTarget = null;
    }

    private void UpdatePromptFollow()
    {
        if (currentPromptTarget == null || promptUI == null) return;

        Transform followPoint = currentPromptTarget.GetPromptPoint();
        if (followPoint == null) return;

        if (mainCam == null)
            mainCam = Camera.main;

        Vector3 screenPos = mainCam.WorldToScreenPoint(followPoint.position);

        promptUI.transform.position = screenPos;
        promptUI.transform.rotation = Quaternion.identity;
    }

    public void ShowLockedMessage() { }

    //HACKING UI
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
