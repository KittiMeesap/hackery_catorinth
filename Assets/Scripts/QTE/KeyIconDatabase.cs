using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class KeyIconDatabase
{
    private static Dictionary<string, Sprite> icons;

    static KeyIconDatabase()
    {
        icons = new Dictionary<string, Sprite>();

        void Map(string key, string file)
        {
            Sprite s = Resources.Load<Sprite>("KeyIcons/" + file);
            if (s != null) icons[key] = s;
        }

        // =======================
        // KEYBOARD ICONS
        // =======================
        Map("confirm_keyboard", "Keyboard_E");
        Map("interact_keyboard", "Keyboard_E");
        Map("space_keyboard", "Keyboard_Space");
        Map("q_keyboard", "Keyboard_Q");

        Map("up_keyboard", "Keyboard_ArrowUp");
        Map("down_keyboard", "Keyboard_ArrowDown");
        Map("left_keyboard", "Keyboard_ArrowLeft");
        Map("right_keyboard", "Keyboard_ArrowRight");

        // =======================
        // PLAYSTATION ICONS
        // =======================
        Map("confirm_ps", "PS_Cross");
        Map("interact_ps", "PS_Cross");

        Map("up_ps", "PS_DPadUp");
        Map("down_ps", "PS_DPadDown");
        Map("left_ps", "PS_DPadLeft");
        Map("right_ps", "PS_DPadRight");

        // =======================
        // XBOX ICONS
        // =======================
        Map("confirm_xbox", "XBox_A");
        Map("interact_xbox", "XBox_A");

        Map("up_xbox", "XBox_Up");
        Map("down_xbox", "XBox_Down");
        Map("left_xbox", "XBox_Left");
        Map("right_xbox", "XBox_Right");
    }

    // ===========================
    //  AUTO DETECT CONTROLLER TYPE
    // ===========================
    private static string Prefix
    {
        get
        {
            if (Gamepad.current != null)
            {
                if (Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad)
                    return "_ps";

                if (Gamepad.current is UnityEngine.InputSystem.XInput.XInputController)
                    return "_xbox";

                return "_xbox";
            }
            return "_keyboard";
        }
    }

    // ===========================
    // GET ICON FOR A LOGICAL INPUT
    // ===========================
    public static Sprite GetIcon(string logicalKey)
    {
        if (string.IsNullOrEmpty(logicalKey)) return null;

        string finalKey = logicalKey.ToLower() + Prefix;

        if (icons.TryGetValue(finalKey, out Sprite s))
            return s;

        return null;
    }

    // ===========================
    // MAP INPUT CALLBACK -> LOGICAL KEY
    // ===========================
    public static string GetLogicalFromContext(InputAction.CallbackContext ctx)
    {
        if (ctx.control == null) return null;

        string n = ctx.control.name.ToLower();

        if (n.Contains("space")) return "space";
        if (n == "e" || n.Contains("interact")) return "confirm";
        if (n == "q") return "q";

        // ARROWS / DPAD
        if (n.Contains("up")) return "up";
        if (n.Contains("down")) return "down";
        if (n.Contains("left")) return "left";
        if (n.Contains("right")) return "right";

        return "confirm";
    }
}
