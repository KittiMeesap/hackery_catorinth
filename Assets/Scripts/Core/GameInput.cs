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
    public InputAction PauseAction { get; private set; }

    // UI
    public InputAction NavigateAction { get; private set; }
    public InputAction SubmitAction { get; private set; }
    public InputAction CancelAction { get; private set; }

    // QTE
    public InputAction QTEConfirmHitAction { get; private set; }
    public InputAction QTEDirectionalAction { get; private set; }
    public InputAction CancelQTEAction { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

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
            PauseAction = player.FindAction("Pause");
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
            CancelQTEAction = qte.FindAction("CancelQTE");
        }
    }

    private void OnControlSchemeChanged(PlayerInput input)
    {
        CurrentControlScheme = input.currentControlScheme;
        ControlSchemeChanged?.Invoke();
    }

    // ==============================
    // MODE SWITCH
    // ==============================
    public void SetModePlayer()
    {
        CurrentMode = InputMode.Player;
        PlayerInputComponent.SwitchCurrentActionMap("Player");
    }

    public void SetModeUI()
    {
        CurrentMode = InputMode.UI;
        PlayerInputComponent.SwitchCurrentActionMap("UI");
    }

    public void SetModeQTE()
    {
        CurrentMode = InputMode.QTE;
        PlayerInputComponent.SwitchCurrentActionMap("QTE");
    }
}
