using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;

[CreateAssetMenu(menuName = "Input/Key Icon Database")]
public class KeyIconDatabase : ScriptableObject
{
    // ---------- SINGLETON ----------
    public static KeyIconDatabase Instance;
    private void OnEnable() => Instance = this;

    // ---------- KEYBOARD ----------
    [Header("Keyboard - Face Buttons")]
    public Sprite key_Space;
    public Sprite key_Q;
    public Sprite key_E;

    [Header("Keyboard - Arrows")]
    public Sprite key_ArrowUp;
    public Sprite key_ArrowDown;
    public Sprite key_ArrowLeft;
    public Sprite key_ArrowRight;

    // ---------- XBOX ----------
    [Header("Xbox - Face Buttons")]
    public Sprite xbox_A;
    public Sprite xbox_B;
    public Sprite xbox_X;
    public Sprite xbox_Y;

    [Header("Xbox - DPad")]
    public Sprite xbox_DpadUp;
    public Sprite xbox_DpadDown;
    public Sprite xbox_DpadLeft;
    public Sprite xbox_DpadRight;

    // ---------- PS5 ----------
    [Header("PS5 - Face Buttons")]
    public Sprite ps_Cross;
    public Sprite ps_Circle;
    public Sprite ps_Square;
    public Sprite ps_Triangle;

    [Header("PS5 - DPad")]
    public Sprite ps_DpadUp;
    public Sprite ps_DpadDown;
    public Sprite ps_DpadLeft;
    public Sprite ps_DpadRight;

    // ---------- SWITCH ----------
    [Header("Switch Pro - Face Buttons")]
    public Sprite sw_B;
    public Sprite sw_A;
    public Sprite sw_X;
    public Sprite sw_Y;

    [Header("Switch Pro - DPad")]
    public Sprite sw_DpadUp;
    public Sprite sw_DpadDown;
    public Sprite sw_DpadLeft;
    public Sprite sw_DpadRight;

    // =============================================================
    //  PUBLIC STATIC API
    // =============================================================

    /// <summary>
    /// logicalKey: "confirm", "q", "space", "left", "right", "up", "down"
    /// </summary>
    public static Sprite GetIcon(string logicalKey)
    {
        var db = Instance;
        if (db == null) return null;

        logicalKey = logicalKey.ToLowerInvariant();

        Keyboard kb = Keyboard.current;
        Gamepad gp = Gamepad.current;

        // --- Keyboard only ---
        if (kb != null && gp == null)
        {
            switch (logicalKey)
            {
                case "confirm":
                case "space": return db.key_Space;
                case "q": return db.key_Q;
                case "e": return db.key_E;
                case "up": return db.key_ArrowUp;
                case "down": return db.key_ArrowDown;
                case "left": return db.key_ArrowLeft;
                case "right": return db.key_ArrowRight;
            }

            return db.key_Space;
        }

        // --- Gamepad (auto detect type) ---
        if (gp != null)
        {
            bool isXbox = gp is XInputController;
            bool isPS = gp is DualSenseGamepadHID || gp is DualShockGamepad;
            bool isSwitch = gp is SwitchProControllerHID;

            // Face / Confirm
            if (logicalKey == "confirm")
            {
                if (isXbox) return db.xbox_A;
                if (isPS) return db.ps_Cross;
                if (isSwitch) return db.sw_B; // Nintendo layout
                return db.xbox_A != null ? db.xbox_A : db.ps_Cross;
            }

            // Directions
            if (logicalKey == "up")
            {
                if (isXbox) return db.xbox_DpadUp;
                if (isPS) return db.ps_DpadUp;
                if (isSwitch) return db.sw_DpadUp;
            }
            if (logicalKey == "down")
            {
                if (isXbox) return db.xbox_DpadDown;
                if (isPS) return db.ps_DpadDown;
                if (isSwitch) return db.sw_DpadDown;
            }
            if (logicalKey == "left")
            {
                if (isXbox) return db.xbox_DpadLeft;
                if (isPS) return db.ps_DpadLeft;
                if (isSwitch) return db.sw_DpadLeft;
            }
            if (logicalKey == "right")
            {
                if (isXbox) return db.xbox_DpadRight;
                if (isPS) return db.ps_DpadRight;
                if (isSwitch) return db.sw_DpadRight;
            }

            // fallback
            return GetIcon("confirm");
        }

        return null;
    }

    public static string GetLogicalFromContext(InputAction.CallbackContext ctx)
    {
        if (ctx.control == null)
            return string.Empty;

        var control = ctx.control;
        var device = control.device;

        // ---------- Keyboard ----------
        if (device is Keyboard)
        {
            switch (control.name)
            {
                case "space": return "space";
                case "q": return "q";
                case "e": return "e";
                case "upArrow": return "up";
                case "downArrow": return "down";
                case "leftArrow": return "left";
                case "rightArrow": return "right";
            }

            return control.name.ToLowerInvariant();
        }

        // ---------- Gamepad ----------
        if (device is Gamepad gp)
        {
            if (control == gp.buttonSouth) return "confirm";
            if (control == gp.buttonEast) return "right";
            if (control == gp.buttonWest) return "left";
            if (control == gp.buttonNorth) return "up";

            if (control == gp.dpad.up) return "up";
            if (control == gp.dpad.down) return "down";
            if (control == gp.dpad.left) return "left";
            if (control == gp.dpad.right) return "right";
        }

        return control.name.ToLowerInvariant();
    }
}
