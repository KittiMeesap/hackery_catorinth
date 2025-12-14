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

    [Header("SFX Keys")]
    public string sfxFootstep = "SFX_Player_Footstep";
    public string sfxSleepLoop = "SFX_Player_Sleep";

    [Header("Footstep Timing")]
    public float minStepInterval = 0.6f;
    public float maxStepInterval = 0.4f;
    public float stepIntervalMultiplier = 1.2f;
    public float minSpeedToPlay = 0.15f;

    // RUNTIME
    private Rigidbody2D rb;

    private float moveX;
    private float inactivityTimer;
    private float footstepTimer;

    private bool isAFK;
    private bool isSleeping;
    private bool isSnoring;
    private bool movementEnabled = true;
    private bool wasWalkingLastFrame;

    // Input
    private InputAction moveAction;
    private InputAction interactAction;

    // Audio
    private AudioSource sleepSource;

    // LIFECYCLE
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (GameInput.Instance == null)
        {
            Debug.LogError("PlayerController: GameInput not found");
            enabled = false;
            return;
        }

        SetupAudio();
        ResetAnimator();
        RefreshCarryAnimation();
    }

    private void OnEnable()
    {
        BindInput();

        if (GameInput.Instance != null)
            GameInput.Instance.ControlSchemeChanged += RebindInput;
    }

    private void OnDisable()
    {
        UnbindInput();

        if (GameInput.Instance != null)
            GameInput.Instance.ControlSchemeChanged -= RebindInput;
    }

    // INPUT BINDING
    private void BindInput()
    {
        var input = GameInput.Instance;
        if (input == null) return;

        moveAction = input.MoveAction;
        interactAction = input.InteractAction;

        if (interactAction != null)
            interactAction.performed += OnInteractPerformed;
    }

    private void UnbindInput()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteractPerformed;

        moveAction = null;
        interactAction = null;
    }

    private void RebindInput()
    {
        UnbindInput();

        if (GameInput.Instance.CurrentMode == GameInput.InputMode.Player)
            BindInput();
    }

    // UPDATE LOOP
    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        ReadMovement();
        UpdateAnimator();
        HandleAFK();
        UpdateFootstepByInterval();
    }

    private void FixedUpdate()
    {
        if (!movementEnabled || isSleeping || isSnoring)
            return;

        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
    }

    // MOVEMENT (CORRECT PATTERN)
    private void ReadMovement()
    {
        if (!movementEnabled || isSleeping || isSnoring)
        {
            moveX = 0;
            return;
        }

        if (moveAction == null)
        {
            moveX = 0;
            return;
        }

        Vector2 v = moveAction.ReadValue<Vector2>();
        moveX = v.x;

        if (Mathf.Abs(moveX) > 0.01f)
            ResetAFK();
    }

    // INPUT CALLBACKS
    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        ResetAFK();
    }

    // ANIMATOR
    private void ResetAnimator()
    {
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsIdle", true);
        animator.SetBool("IsCarryingContainer", false);
        animator.SetBool("IsCarryingServeBox", false);
        animator.SetBool("AFK", false);
        animator.SetBool("IsCooking", false);
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

    // FOOTSTEP
    private void UpdateFootstepByInterval()
    {
        if (!movementEnabled || isSleeping || isSnoring)
        {
            footstepTimer = 0f;
            wasWalkingLastFrame = false;
            return;
        }

        float speed = Mathf.Abs(rb.linearVelocity.x);
        bool isWalking = speed >= minSpeedToPlay;

        if (isWalking && !wasWalkingLastFrame)
        {
            footstepTimer = 0f;
            PlayFootstep();
        }

        wasWalkingLastFrame = isWalking;
        if (!isWalking) return;

        float speedRatio = Mathf.Clamp01(speed / moveSpeed);
        float stepInterval =
            Mathf.Lerp(minStepInterval, maxStepInterval, speedRatio)
            * stepIntervalMultiplier;

        stepInterval = Mathf.Max(stepInterval, 0.35f);
        footstepTimer += Time.deltaTime;

        if (footstepTimer >= stepInterval)
        {
            footstepTimer = 0f;
            PlayFootstep();
        }
    }

    private void PlayFootstep()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.PlaySFXAt(
            sfxFootstep,
            transform.position,
            false
        );
    }

    // AFK / SLEEP
    private void HandleAFK()
    {
        inactivityTimer += Time.deltaTime;
        if (!isAFK && inactivityTimer >= afkTime)
            isAFK = true;
    }

    private void ResetAFK()
    {
        inactivityTimer = 0;

        if (isAFK)
            animator.SetTrigger("Wake");

        isAFK = false;
        StopSleepSound();
    }

    public void Anim_SleepStart() { isSleeping = true; StartSleepSound(); }
    public void Anim_SleepEnd() { isSleeping = false; StopSleepSound(); }
    public void Anim_SnoreStart() { isSnoring = true; StartSleepSound(); }
    public void Anim_SnoreEnd() { isSnoring = false; StopSleepSound(); }

    public void Anim_WakeEnd()
    {
        isAFK = false;
        isSleeping = false;
        isSnoring = false;
        StopSleepSound();
    }

    // AUDIO
    private void SetupAudio()
    {
        sleepSource = gameObject.AddComponent<AudioSource>();
        sleepSource.loop = true;
        sleepSource.playOnAwake = false;
        sleepSource.spatialBlend = 0f;
    }

    private void StartSleepSound()
    {
        if (sleepSource.isPlaying) return;
        if (AudioManager.Instance == null) return;

        var clip = AudioManager.Instance.GetClipByKey(sfxSleepLoop);
        if (clip == null) return;

        sleepSource.clip = clip;
        sleepSource.volume =
            AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume;
        sleepSource.Play();
    }

    private void StopSleepSound()
    {
        if (sleepSource.isPlaying)
            sleepSource.Stop();
    }

    // OTHER
    public void SetCooking(bool cooking)
    {
        animator.SetBool("IsCooking", cooking);
    }

    public void RefreshCarryAnimation()
    {
        bool hasBowl = inventory != null && inventory.HasBowl();
        bool hasServe = inventory != null && inventory.HasServeBox();
        if (hasServe) hasBowl = false;

        animator.SetBool("IsCarryingContainer", hasBowl);
        animator.SetBool("IsCarryingServeBox", hasServe);
    }

    public void ForceIdle()
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (animator != null)
            animator.SetBool("IsWalking", false);
    }

    public void DisableMovement()
    {
        movementEnabled = false;
        moveX = 0;
        rb.linearVelocity = Vector2.zero;
        footstepTimer = 0f;
        wasWalkingLastFrame = false;
    }

    public void EnableMovement()
    {
        movementEnabled = true;
    }
}
