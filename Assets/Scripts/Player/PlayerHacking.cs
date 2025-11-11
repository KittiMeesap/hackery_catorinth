using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHacking : MonoBehaviour
{
    private PlayerInput playerInput;
    private HackableObject currentHoverHackable;
    private HackableObject currentHackedObject;
    private bool isUsingController;

    [Header("Detection Settings")]
    [SerializeField] private float gamepadDetectionRadius = 0.3f;
    [SerializeField] private float clickCooldown = 0.25f;

    private bool hackingDisabled = false;
    private bool isClickLocked = false;

    public void SetHackingDisabled(bool disabled) => hackingDisabled = disabled;

    private void Awake() => playerInput = GetComponent<PlayerInput>();

    private void Update()
    {
        if (GameManager.Instance == null || UIManager.Instance == null) return;
        if (hackingDisabled) return;
        if (PlayerHiding.Instance != null && PlayerHiding.Instance.IsHidingInContainer) return;

        isUsingController = playerInput.currentControlScheme == "Gamepad";

        // Detect target only if not in hacking mode
        if (!GameManager.Instance.IsInHackingMode)
        {
            if (isUsingController)
                DetectHackableNearby_Gamepad();
            else
                DetectHoverHackable_Mouse();
        }

        HandleArrowInput();
    }

    // Mouse Detection
    private void DetectHoverHackable_Mouse()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldPos.z = 0f;

        Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorldPos);
        HackableObject hovered = null;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("CanHack")) continue;

            HackableObject hackable = hit.GetComponentInParent<HackableObject>();
            if (hackable != null && hackable.triggerType == HackableObject.HackTriggerType.MouseHover)
            {
                hovered = hackable;
                break;
            }
        }

        if (hovered == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && !isClickLocked)
        {
            StartCoroutine(ClickCooldownRoutine());

            if (!UIManager.Instance.IsHacking)
            {
                PlayerController.Instance.SetFrozen(true);
                hovered.OnEnterHackingMode();
                currentHackedObject = hovered;
            }
            else if (currentHackedObject != null)
            {
                currentHackedObject.HideHackingUI();
                currentHackedObject = null;
            }
        }
    }

    // Gamepad Detection
    private void DetectHackableNearby_Gamepad()
    {
        Vector2 pos = PlayerController.Instance.transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, gamepadDetectionRadius);

        HackableObject nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("CanHack")) continue;

            HackableObject hackable = hit.GetComponentInParent<HackableObject>();
            if (hackable != null && hackable.triggerType == HackableObject.HackTriggerType.ProximityInteract)
            {
                float dist = Vector2.Distance(pos, hackable.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = hackable;
                }
            }
        }

        currentHoverHackable = nearest;
        if (nearest == null) return;

        bool inputPressed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        if (inputPressed && !isClickLocked)
        {
            StartCoroutine(ClickCooldownRoutine());

            if (!UIManager.Instance.IsHacking)
            {
                PlayerController.Instance.SetFrozen(true);
                nearest.OnEnterHackingMode();
                currentHackedObject = nearest;
            }
            else if (currentHackedObject != null)
            {
                currentHackedObject.HideHackingUI();
                currentHackedObject = null;
            }
        }
    }

    // ==============================
    // Handle Arrow Inputs (WASD / D-Pad)
    // ==============================
    private void HandleArrowInput()
    {
        if (!GameManager.Instance.IsInHackingMode) return;
        if (UIManager.Instance == null || !UIManager.Instance.IsHacking) return;

        ArrowUI.Direction? input = null;

        // Keyboard input
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame) input = ArrowUI.Direction.Up;
            else if (Keyboard.current.sKey.wasPressedThisFrame) input = ArrowUI.Direction.Down;
            else if (Keyboard.current.aKey.wasPressedThisFrame) input = ArrowUI.Direction.Left;
            else if (Keyboard.current.dKey.wasPressedThisFrame) input = ArrowUI.Direction.Right;
        }

        // Gamepad input
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.up.wasPressedThisFrame) input = ArrowUI.Direction.Up;
            else if (Gamepad.current.dpad.down.wasPressedThisFrame) input = ArrowUI.Direction.Down;
            else if (Gamepad.current.dpad.left.wasPressedThisFrame) input = ArrowUI.Direction.Left;
            else if (Gamepad.current.dpad.right.wasPressedThisFrame) input = ArrowUI.Direction.Right;
        }

        if (input.HasValue)
            UIManager.Instance?.SubmitArrow(input.Value);
    }

    private IEnumerator ClickCooldownRoutine()
    {
        isClickLocked = true;
        yield return new WaitForSeconds(clickCooldown);
        isClickLocked = false;
    }

    public void SetCurrentHackedObject(HackableObject obj) => currentHackedObject = obj;
}
