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

    private Rigidbody2D rb;
    private InputManager input;
    private float moveX;
    private float inactivityTimer;

    private bool isAFK;
    private bool isSleeping;
    private bool isSnoring;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = new InputManager();
    }

    private void OnEnable()
    {
        input.Player.Enable();

        input.Player.Move.performed += OnMovePerformed;
        input.Player.Move.canceled += OnMoveCanceled;

        input.Player.Interact.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        input.Player.Move.performed -= OnMovePerformed;
        input.Player.Move.canceled -= OnMoveCanceled;

        input.Player.Interact.performed -= OnInteractPerformed;

        input.Player.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        Vector2 v = ctx.ReadValue<Vector2>();
        moveX = v.x;
        ResetAFK();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        moveX = 0f;
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        ResetAFK();
        // TODO: Interact system
    }

    private void FixedUpdate()
    {
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
