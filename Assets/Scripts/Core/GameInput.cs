using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public PlayerInput PlayerInputComponent { get; private set; }
    public string CurrentControlScheme { get; private set; }

    public enum InputMode
    {
        Player,
        UI,
        QTE
    }

    public InputMode CurrentMode { get; private set; }

    public event System.Action ControlSchemeChanged;

    // =====================================================
    // PLAYER ACTIONS
    // =====================================================
    public InputAction MoveAction { get; private set; }
    public InputAction InteractAction { get; private set; }
    public InputAction PauseAction { get; private set; }

    // =====================================================
    // UI ACTIONS
    // =====================================================
    public InputAction NavigateAction { get; private set; }
    public InputAction SubmitAction { get; private set; }
    public InputAction CancelAction { get; private set; }

    // =====================================================
    // QTE ACTIONS
    // =====================================================
    public InputAction QTEConfirmAction { get; private set; }
    public InputAction QTEDirectionAction { get; private set; }
    public InputAction CancelQTEAction { get; private set; }

    // =====================================================
    // LIFECYCLE
    // =====================================================
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
        PlayerInputComponent.onControlsChanged += OnControlsChanged;

        CacheActions();
        ValidateActions();

        CurrentControlScheme = PlayerInputComponent.currentControlScheme;
    }

    private void Start()
    {
        PlayerInputComponent.actions.Enable();

        SetModePlayer();
    }

    private void OnDestroy()
    {
        if (PlayerInputComponent != null)
            PlayerInputComponent.onControlsChanged -= OnControlsChanged;

        if (Instance == this)
            Instance = null;
    }

    // =====================================================
    // CACHE ACTIONS
    // =====================================================
    private void CacheActions()
    {
        var actions = PlayerInputComponent.actions;

        // ---------- PLAYER ----------
        var player = actions.FindActionMap("Player", true);
        MoveAction = player.FindAction("Move", true);
        InteractAction = player.FindAction("Interact", true);
        PauseAction = player.FindAction("Pause", true);

        // ---------- UI ----------
        var ui = actions.FindActionMap("UI", true);
        NavigateAction = ui.FindAction("Navigate", true);
        SubmitAction = ui.FindAction("Submit", true);
        CancelAction = ui.FindAction("Cancel", true);

        // ---------- QTE ----------
        var qte = actions.FindActionMap("QTE", true);
        QTEConfirmAction = qte.FindAction("ConfirmHit", true);
        QTEDirectionAction = qte.FindAction("Directional", true);
        CancelQTEAction = qte.FindAction("CancelQTE", true);
    }

    // =====================================================
    // VALIDATION
    // =====================================================
    private void ValidateActions()
    {
        Debug.Assert(MoveAction != null, "MoveAction NOT FOUND");
        Debug.Assert(InteractAction != null, "InteractAction NOT FOUND");
        Debug.Assert(PauseAction != null, "PauseAction NOT FOUND");

        Debug.Assert(NavigateAction != null, "NavigateAction NOT FOUND");
        Debug.Assert(SubmitAction != null, "SubmitAction NOT FOUND");
        Debug.Assert(CancelAction != null, "CancelAction NOT FOUND");

        Debug.Assert(QTEConfirmAction != null, "QTEConfirmAction NOT FOUND");
        Debug.Assert(QTEDirectionAction != null, "QTEDirectionAction NOT FOUND");
        Debug.Assert(CancelQTEAction != null, "CancelQTEAction NOT FOUND");
    }

    // =====================================================
    // CONTROL SCHEME
    // =====================================================
    private void OnControlsChanged(PlayerInput input)
    {
        CurrentControlScheme = input.currentControlScheme;
        ControlSchemeChanged?.Invoke();
    }

    // =====================================================
    // MODE SWITCH
    // =====================================================
    public void SetModePlayer()
    {
        CurrentMode = InputMode.Player;
        PlayerInputComponent.SwitchCurrentActionMap("Player");

        Debug.Log("[INPUT MODE] Player");
        Debug.Log("[ACTION MAP] " + PlayerInputComponent.currentActionMap.name);
    }

    public void SetModeUI()
    {
        CurrentMode = InputMode.UI;
        PlayerInputComponent.SwitchCurrentActionMap("UI");

        Debug.Log("[INPUT MODE] UI");
        Debug.Log("[ACTION MAP] " + PlayerInputComponent.currentActionMap.name);
    }

    public void SetModeQTE()
    {
        CurrentMode = InputMode.QTE;
        PlayerInputComponent.SwitchCurrentActionMap("QTE");

        Debug.Log("[INPUT MODE] QTE");
        Debug.Log("[ACTION MAP] " + PlayerInputComponent.currentActionMap.name);
    }
}
