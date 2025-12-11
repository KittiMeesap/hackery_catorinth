using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;

[CreateAssetMenu(menuName = "Input/Key Icon Database")]
public class KeyIconDatabase : ScriptableObject
{
    public static KeyIconDatabase Instance;
    private void OnEnable() => Instance = this;

    // ---------- KEYBOARD ----------
    public Sprite key_Space;
    public Sprite key_Enter;
    public Sprite key_Escape;
    public Sprite key_R;

    public Sprite key_ArrowUp;
    public Sprite key_ArrowDown;
    public Sprite key_ArrowLeft;
    public Sprite key_ArrowRight;

    // ---------- GAMEPAD ----------
    public Sprite xbox_A, xbox_B, xbox_X;
    public Sprite xbox_DpadUp, xbox_DpadDown, xbox_DpadLeft, xbox_DpadRight;

    public Sprite ps_Cross, ps_Circle, ps_Square;
    public Sprite ps_DpadUp, ps_DpadDown, ps_DpadLeft, ps_DpadRight;

    public Sprite sw_B, sw_A, sw_Y;
    public Sprite sw_DpadUp, sw_DpadDown, sw_DpadLeft, sw_DpadRight;


    // ---------- ICON LOOKUP ----------
    public static Sprite GetIcon(string logicalKey)
    {
        var db = Instance;
        if (db == null) return null;

        logicalKey = logicalKey.ToLowerInvariant();

        Keyboard kb = Keyboard.current;
        Gamepad gp = Gamepad.current;

        // KEYBOARD ONLY
        if (kb != null && gp == null)
        {
            switch (logicalKey)
            {
                case "confirm":
                case "enter": return db.key_Enter;

                case "cancel": return db.key_Escape;
                case "reset": return db.key_R;

                case "up": return db.key_ArrowUp;
                case "down": return db.key_ArrowDown;
                case "left": return db.key_ArrowLeft;
                case "right": return db.key_ArrowRight;
            }
        }

        // GAMEPAD
        if (gp != null)
        {
            bool xbox = gp is XInputController;
            bool ps = gp is DualSenseGamepadHID || gp is DualShockGamepad;
            bool sw = gp is SwitchProControllerHID;

            if (logicalKey == "confirm")
                return xbox ? db.xbox_A : ps ? db.ps_Cross : db.sw_B;

            if (logicalKey == "cancel")
                return xbox ? db.xbox_B : ps ? db.ps_Circle : db.sw_A;

            if (logicalKey == "reset")
                return xbox ? db.xbox_X : ps ? db.ps_Square : db.sw_Y;

            if (logicalKey == "up") return xbox ? db.xbox_DpadUp : ps ? db.ps_DpadUp : db.sw_DpadUp;
            if (logicalKey == "down") return xbox ? db.xbox_DpadDown : ps ? db.ps_DpadDown : db.sw_DpadDown;
            if (logicalKey == "left") return xbox ? db.xbox_DpadLeft : ps ? db.ps_DpadLeft : db.sw_DpadLeft;
            if (logicalKey == "right") return xbox ? db.xbox_DpadRight : ps ? db.ps_DpadRight : db.sw_DpadRight;
        }

        return db.key_Enter;
    }

    public static string GetLogicalFromContext(InputAction.CallbackContext ctx)
    {
        if (ctx.control == null) return "";

        var c = ctx.control;
        var device = c.device;

        if (device is Keyboard)
        {
            switch (c.name)
            {
                case "enter": return "confirm";
                case "escape": return "cancel";
                case "r": return "reset";

                case "upArrow": return "up";
                case "downArrow": return "down";
                case "leftArrow": return "left";
                case "rightArrow": return "right";
            }
        }

        if (device is Gamepad gp)
        {
            if (c == gp.buttonSouth) return "confirm";
            if (c == gp.buttonEast) return "cancel";
            if (c == gp.buttonWest) return "reset";

            if (c == gp.dpad.up) return "up";
            if (c == gp.dpad.down) return "down";
            if (c == gp.dpad.left) return "left";
            if (c == gp.dpad.right) return "right";
        }

        return "";
    }
}
