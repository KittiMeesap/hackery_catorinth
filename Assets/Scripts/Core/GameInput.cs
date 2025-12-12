using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public PlayerInput PlayerInputComponent { get; private set; }
    public string CurrentControlScheme { get; private set; }

    public enum InputMode { Player, UI, QTE }
    public InputMode CurrentMode { get; private set; }

    public event System.Action ControlSchemeChanged;

    // PLAYER
    public InputAction MoveAction { get; private set; }
    public InputAction InteractAction { get; private set; }

    // UI
    public InputAction NavigateAction { get; private set; }
    public InputAction SubmitAction { get; private set; }
    public InputAction CancelAction { get; private set; }

    // QTE
    public InputAction QTEConfirmHitAction { get; private set; }
    public InputAction QTEDirectionalAction { get; private set; }

    private void Awake()
    {
        Instance = this;
        PlayerInputComponent = GetComponent<PlayerInput>();

        PlayerInputComponent.onControlsChanged += OnControlSchemeChanged;
        CurrentControlScheme = PlayerInputComponent.currentControlScheme;

        CacheInputActions();
    }

    private void Start()
    {
        PlayerInputComponent.actions.Enable();
        SetModeUI();
    }

    private void CacheInputActions()
    {
        var player = PlayerInputComponent.actions.FindActionMap("Player");
        if (player != null)
        {
            MoveAction = player.FindAction("Move");
            InteractAction = player.FindAction("Interact");
        }

        var ui = PlayerInputComponent.actions.FindActionMap("UI");
        if (ui != null)
        {
            NavigateAction = ui.FindAction("Navigate");
            SubmitAction = ui.FindAction("Submit");
            CancelAction = ui.FindAction("Cancel");
        }

        var qte = PlayerInputComponent.actions.FindActionMap("QTE");
        if (qte != null)
        {
            QTEConfirmHitAction = qte.FindAction("ConfirmHit");
            QTEDirectionalAction = qte.FindAction("Directional");
        }
    }

    private void OnControlSchemeChanged(PlayerInput input)
    {
        CurrentControlScheme = input.currentControlScheme;
        ControlSchemeChanged?.Invoke();
    }

    public void SetModePlayer() => PlayerInputComponent.SwitchCurrentActionMap("Player");
    public void SetModeUI() => PlayerInputComponent.SwitchCurrentActionMap("UI");
    public void SetModeQTE() => PlayerInputComponent.SwitchCurrentActionMap("QTE");
}
