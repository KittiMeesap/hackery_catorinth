using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHacking : MonoBehaviour
{
    private PlayerInput playerInput;
    private HackableObject currentHackedObject;

    private bool hackingDisabled = false;
    public void SetHackingDisabled(bool disabled) => hackingDisabled = disabled;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        if (GameManager.Instance == null || UIManager.Instance == null) return;
        if (hackingDisabled) return;

        if (GameManager.Instance.IsInHackingMode)
        {
            HandleArrowInput();
            return;
        }

        if (PlayerHiding.Instance != null && PlayerHiding.Instance.IsHidingInContainer)
            return;

        if (PlayerController.Instance != null)
        {
            var pl = PlayerController.Instance;

            bool isSleeping = (bool)typeof(PlayerController)
                .GetField("isSleeping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(pl);

            bool isAFK = (bool)typeof(PlayerController)
                .GetField("isAFKTriggered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(pl);
            if (!GameManager.Instance.IsInHackingMode && (isSleeping || isAFK))
                return;
        }

        HandleArrowInput();
    }

    private void HandleArrowInput()
    {
        if (!GameManager.Instance.IsInHackingMode) return;
        if (!UIManager.Instance.IsHacking) return;

        ArrowUI.Direction? input = null;

        // Keyboard Arrows
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                input = ArrowUI.Direction.Up;
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                input = ArrowUI.Direction.Down;
            else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                input = ArrowUI.Direction.Left;
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                input = ArrowUI.Direction.Right;
        }

        // Gamepad D-Pad
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.up.wasPressedThisFrame)
                input = ArrowUI.Direction.Up;
            else if (Gamepad.current.dpad.down.wasPressedThisFrame)
                input = ArrowUI.Direction.Down;
            else if (Gamepad.current.dpad.left.wasPressedThisFrame)
                input = ArrowUI.Direction.Left;
            else if (Gamepad.current.dpad.right.wasPressedThisFrame)
                input = ArrowUI.Direction.Right;
        }

        if (input.HasValue)
            UIManager.Instance.SubmitArrow(input.Value);
    }

    public void SetCurrentHackedObject(HackableObject obj)
    {
        currentHackedObject = obj;
    }
}
