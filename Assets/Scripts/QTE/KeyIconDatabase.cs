using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class KeyIconDatabase
{
    private static Dictionary<string, Sprite> icons = new();

    static KeyIconDatabase()
    {
        void Map(LogicalInput key, string device, string spriteName)
        {
            Sprite s = Resources.Load<Sprite>("KeyIcons/" + spriteName);
            if (s != null)
                icons[$"{key}_{device}"] = s;
        }

        // ===== KEYBOARD =====
        Map(LogicalInput.Interact, "keyboard", "Keyboard_E");
        Map(LogicalInput.QTEConfirm, "keyboard", "Keyboard_SpaceBar");
        Map(LogicalInput.UISubmit, "keyboard", "Keyboard_Enter");
        Map(LogicalInput.UICancel, "keyboard", "Keyboard_Backspace");
        Map(LogicalInput.CancelQTE, "keyboard", "Keyboard_Esc");
        Map(LogicalInput.Pause, "keyboard", "Keyboard_Esc");

        Map(LogicalInput.Up, "keyboard", "Keyboard_ArrowUp");
        Map(LogicalInput.Down, "keyboard", "Keyboard_ArrowDown");
        Map(LogicalInput.Left, "keyboard", "Keyboard_ArrowLeft");
        Map(LogicalInput.Right, "keyboard", "Keyboard_ArrowRight");

        // ===== PLAYSTATION =====
        Map(LogicalInput.Interact, "ps", "PS_Cross");
        Map(LogicalInput.QTEConfirm, "ps", "PS_Cross");
        Map(LogicalInput.UISubmit, "ps", "PS_Cross");

        Map(LogicalInput.UICancel, "ps", "PS_Circle");
        Map(LogicalInput.CancelQTE, "ps", "PS_Circle");
        Map(LogicalInput.Pause, "ps", "PS_Options");

        Map(LogicalInput.Up, "ps", "PS_DPadUp");
        Map(LogicalInput.Down, "ps", "PS_DPadDown");
        Map(LogicalInput.Left, "ps", "PS_DPadLeft");
        Map(LogicalInput.Right, "ps", "PS_DPadRight");

        // ===== XBOX =====
        Map(LogicalInput.Interact, "xbox", "Xbox_A");
        Map(LogicalInput.QTEConfirm, "xbox", "Xbox_A");
        Map(LogicalInput.UISubmit, "xbox", "Xbox_A");

        Map(LogicalInput.UICancel, "xbox", "Xbox_B");
        Map(LogicalInput.CancelQTE, "xbox", "Xbox_B");
        Map(LogicalInput.Pause, "xbox", "Xbox_Start");

        Map(LogicalInput.Up, "xbox", "Xbox_DPadUp");
        Map(LogicalInput.Down, "xbox", "Xbox_DPadDown");
        Map(LogicalInput.Left, "xbox", "Xbox_DPadLeft");
        Map(LogicalInput.Right, "xbox", "Xbox_DPadRight");
    }

    private static string CurrentDevice
    {
        get
        {
            if (Gamepad.current != null)
            {
                if (Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad)
                    return "ps";
                return "xbox";
            }
            return "keyboard";
        }
    }

    public static Sprite GetIcon(LogicalInput key)
    {
        string id = $"{key}_{CurrentDevice}";
        icons.TryGetValue(id, out Sprite s);
        return s;
    }

    public static LogicalInput GetLogicalFromContext(InputAction.CallbackContext ctx)
    {
        if (ctx.control == null)
            return LogicalInput.QTEConfirm;

        string name = ctx.control.name.ToLower();

        if (name.Contains("space")) return LogicalInput.QTEConfirm;
        if (name == "enter") return LogicalInput.UISubmit;
        if (name == "escape") return LogicalInput.CancelQTE;

        if (name.Contains("up")) return LogicalInput.Up;
        if (name.Contains("down")) return LogicalInput.Down;
        if (name.Contains("left")) return LogicalInput.Left;
        if (name.Contains("right")) return LogicalInput.Right;

        return LogicalInput.QTEConfirm;
    }
}
