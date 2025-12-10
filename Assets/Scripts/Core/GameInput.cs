using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public InputManager Actions { get; private set; }
    public string CurrentControlScheme { get; private set; }

    public enum InputMode { Player, UI, QTE }
    public InputMode CurrentMode { get; private set; } = InputMode.Player;

    private PlayerInput playerInput;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        playerInput = GetComponent<PlayerInput>();

        Actions = new InputManager();

        Actions.asset.bindingMask = playerInput.actions.bindingMask;
        Actions.asset.devices = playerInput.actions.devices;

        DisableAllMaps();
        SetModePlayer();

        playerInput.onControlsChanged += OnControlSchemeChanged;
        CurrentControlScheme = playerInput.currentControlScheme;
    }

    private void DisableAllMaps()
    {
        Actions.Player.Disable();
        Actions.UI.Disable();
        Actions.QTE.Disable();
        Actions.GameControls.Disable();
    }

    public void SetModePlayer()
    {
        DisableAllMaps();
        Actions.Player.Enable();
        CurrentMode = InputMode.Player;
        playerInput.SwitchCurrentActionMap("Player");
    }

    public void SetModeUI()
    {
        DisableAllMaps();
        Actions.UI.Enable();
        CurrentMode = InputMode.UI;
        playerInput.SwitchCurrentActionMap("UI");
    }

    public void SetModeQTE()
    {
        DisableAllMaps();
        Actions.QTE.Enable();
        CurrentMode = InputMode.QTE;
        playerInput.SwitchCurrentActionMap("QTE");
    }

    private void OnControlSchemeChanged(PlayerInput input)
    {
        CurrentControlScheme = input.currentControlScheme;
        Debug.Log("Control Scheme Switched -> " + CurrentControlScheme);
    }
}
