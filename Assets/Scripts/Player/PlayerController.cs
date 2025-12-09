using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float afkTime = 6f;

    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public PlayerInventory inventory;

    private Rigidbody2D rb;
    private InputManager input;

    private float moveX;
    private float inactivityTimer;

    private bool isAFK;
    private bool isSleeping;
    private bool isSnoring;

    private bool movementEnabled = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (QTEManager.Instance != null)
            input = QTEManager.Instance.input;

        ResetAnimatorParameters();

        RefreshCarryAnimation();
    }

    private void ResetAnimatorParameters()
    {
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsIdle", true);
        animator.SetBool("IsCarryingContainer", false);
        animator.SetBool("IsCarryingServeBox", false);
        animator.SetBool("AFK", false);
        animator.SetBool("IsCooking", false);
    }

    private void OnEnable()
    {
        if (input == null)
        {
            Debug.LogError("PlayerController: InputManager missing!");
            return;
        }

        input.Player.Enable();
        input.UI.Disable();

        input.Player.Move.performed += OnMovePerformed;
        input.Player.Move.canceled += OnMoveCanceled;
        input.Player.Interact.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        if (input == null) return;

        input.Player.Move.performed -= OnMovePerformed;
        input.Player.Move.canceled -= OnMoveCanceled;
        input.Player.Interact.performed -= OnInteractPerformed;

        input.Player.Disable();
    }

    public void DisableMovement()
    {
        movementEnabled = false;
        moveX = 0;
        rb.linearVelocity = Vector2.zero;
    }

    public void EnableMovement()
    {
        movementEnabled = true;
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        if (!movementEnabled)
        {
            moveX = 0;
            return;
        }

        moveX = ctx.ReadValue<Vector2>().x;
        ResetAFK();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        moveX = 0f;
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        ResetAFK();
    }

    private void FixedUpdate()
    {
        if (!movementEnabled) return;
        if (isSleeping || isSnoring) return;

        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
    }

    private void Update()
    {
        UpdateAnimator();
        HandleAFKTimer();
    }

    private void UpdateAnimator()
    {
        bool walking = Mathf.Abs(moveX) > 0.1f;

        animator.SetBool("IsWalking", walking);
        animator.SetBool("IsIdle", !walking);

        if (walking)
            spriteRenderer.flipX = moveX < 0;

        animator.SetBool("AFK", isAFK);
    }

    public void RefreshCarryAnimation()
    {
        bool hasBowl = inventory != null && inventory.HasBowl();
        bool hasServe = inventory != null && inventory.HasServeBox();

        if (hasServe) hasBowl = false;

        animator.SetBool("IsCarryingContainer", hasBowl);
        animator.SetBool("IsCarryingServeBox", hasServe);
    }

    public void SetCooking(bool cooking)
    {
        animator.SetBool("IsCooking", cooking);
    }

    private void HandleAFKTimer()
    {
        inactivityTimer += Time.deltaTime;
        if (!isAFK && inactivityTimer >= afkTime)
            isAFK = true;
    }

    private void ResetAFK()
    {
        inactivityTimer = 0f;

        if (isAFK)
            animator.SetTrigger("Wake");

        isAFK = false;
        isSleeping = false;
        isSnoring = false;
    }

    public void Anim_SleepStart() => isSleeping = true;
    public void Anim_SleepEnd() => isSleeping = false;
    public void Anim_SnoreStart() => isSnoring = true;
    public void Anim_SnoreEnd() => isSnoring = false;

    public void Anim_WakeEnd()
    {
        isAFK = false;
        isSleeping = false;
        isSnoring = false;
    }
}
