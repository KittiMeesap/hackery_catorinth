using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum KeyIconSizeType
{
    Square64,
    Wide128
}

public struct KeyIconData
{
    public Sprite sprite;
    public KeyIconSizeType sizeType;

    public KeyIconData(Sprite s, KeyIconSizeType t)
    {
        sprite = s;
        sizeType = t;
    }
}

public static class KeyIconDatabase
{
    private static Dictionary<string, KeyIconData> icons = new();

    // INIT
    static KeyIconDatabase()
    {
        void Map(
            LogicalInput key,
            string device,
            string spriteName,
            KeyIconSizeType sizeType
        )
        {
            Sprite s = Resources.Load<Sprite>("KeyIcons/" + spriteName);
            if (s != null)
                icons[$"{key}_{device}"] = new KeyIconData(s, sizeType);
        }

        // ===== KEYBOARD =====
        Map(LogicalInput.Interact, "keyboard", "Keyboard_E", KeyIconSizeType.Square64);
        Map(LogicalInput.QTEConfirm, "keyboard", "Keyboard_SpaceBar", KeyIconSizeType.Wide128);
        Map(LogicalInput.UISubmit, "keyboard", "Keyboard_Enter", KeyIconSizeType.Wide128);
        Map(LogicalInput.UICancel, "keyboard", "Keyboard_BackSpace", KeyIconSizeType.Wide128);
        Map(LogicalInput.CancelQTE, "keyboard", "Keyboard_Esc", KeyIconSizeType.Square64);
        Map(LogicalInput.Pause, "keyboard", "Keyboard_Esc", KeyIconSizeType.Square64);

        Map(LogicalInput.Up, "keyboard", "Keyboard_ArrowUp", KeyIconSizeType.Square64);
        Map(LogicalInput.Down, "keyboard", "Keyboard_ArrowDown", KeyIconSizeType.Square64);
        Map(LogicalInput.Left, "keyboard", "Keyboard_ArrowLeft", KeyIconSizeType.Square64);
        Map(LogicalInput.Right, "keyboard", "Keyboard_ArrowRight", KeyIconSizeType.Square64);

        // ===== PLAYSTATION =====
        Map(LogicalInput.Interact, "ps", "PS_Cross", KeyIconSizeType.Square64);
        Map(LogicalInput.QTEConfirm, "ps", "PS_Cross", KeyIconSizeType.Square64);
        Map(LogicalInput.UISubmit, "ps", "PS_Cross", KeyIconSizeType.Square64);

        Map(LogicalInput.UICancel, "ps", "PS_Circle", KeyIconSizeType.Square64);
        Map(LogicalInput.CancelQTE, "ps", "PS_Circle", KeyIconSizeType.Square64);
        Map(LogicalInput.Pause, "ps", "PS_Options", KeyIconSizeType.Wide128);

        Map(LogicalInput.Up, "ps", "PS_DPadUp", KeyIconSizeType.Square64);
        Map(LogicalInput.Down, "ps", "PS_DPadDown", KeyIconSizeType.Square64);
        Map(LogicalInput.Left, "ps", "PS_DPadLeft", KeyIconSizeType.Square64);
        Map(LogicalInput.Right, "ps", "PS_DPadRight", KeyIconSizeType.Square64);

        // ===== XBOX =====
        Map(LogicalInput.Interact, "xbox", "Xbox_A", KeyIconSizeType.Square64);
        Map(LogicalInput.QTEConfirm, "xbox", "Xbox_A", KeyIconSizeType.Square64);
        Map(LogicalInput.UISubmit, "xbox", "Xbox_A", KeyIconSizeType.Square64);

        Map(LogicalInput.UICancel, "xbox", "Xbox_B", KeyIconSizeType.Square64);
        Map(LogicalInput.CancelQTE, "xbox", "Xbox_B", KeyIconSizeType.Square64);
        Map(LogicalInput.Pause, "xbox", "Xbox_Start", KeyIconSizeType.Wide128);

        Map(LogicalInput.Up, "xbox", "Xbox_DPadUp", KeyIconSizeType.Square64);
        Map(LogicalInput.Down, "xbox", "Xbox_DPadDown", KeyIconSizeType.Square64);
        Map(LogicalInput.Left, "xbox", "Xbox_DPadLeft", KeyIconSizeType.Square64);
        Map(LogicalInput.Right, "xbox", "Xbox_DPadRight", KeyIconSizeType.Square64);
    }

    // DEVICE
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

    public static bool TryGetIcon(LogicalInput key, out KeyIconData data)
    {
        string id = $"{key}_{CurrentDevice}";
        return icons.TryGetValue(id, out data);
    }

    // BACKWARD COMPAT
    public static Sprite GetIcon(LogicalInput key)
    {
        if (TryGetIcon(key, out var data))
            return data.sprite;

        return null;
    }

    public static LogicalInput GetLogicalFromContext(InputAction.CallbackContext ctx)
    {
        if (ctx.control == null)
            return LogicalInput.QTEConfirm;

        string name = ctx.control.name.ToLower();

        if (name.Contains("space")) return LogicalInput.QTEConfirm;
        if (name == "enter") return LogicalInput.UISubmit;
        if (name.Contains("backspace")) return LogicalInput.UICancel;
        if (name.Contains("escape")) return LogicalInput.CancelQTE;

        if (name.Contains("up")) return LogicalInput.Up;
        if (name.Contains("down")) return LogicalInput.Down;
        if (name.Contains("left")) return LogicalInput.Left;
        if (name.Contains("right")) return LogicalInput.Right;

        return LogicalInput.QTEConfirm;
    }

    public static KeyIconSizeType GetSizeType(LogicalInput input)
    {
        switch (input)
        {
            case LogicalInput.UISubmit:
            case LogicalInput.UICancel:
                return KeyIconSizeType.Wide128;

            default:
                return KeyIconSizeType.Square64;
        }
    }


}
