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
    public float minStepInterval = 0.6f;   // เดินช้า
    public float maxStepInterval = 0.4f;   // เดินเร็ว
    public float stepIntervalMultiplier = 1.2f;
    public float minSpeedToPlay = 0.15f;

    private Rigidbody2D rb;

    private float moveX;
    private float inactivityTimer;
    private float footstepTimer;

    private bool isAFK;
    private bool isSleeping;
    private bool isSnoring;
    private bool movementEnabled = true;

    private bool wasWalkingLastFrame;

    private InputAction moveAction;
    private InputAction interactAction;

    private AudioSource sleepSource;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (GameInput.Instance == null)
        {
            Debug.LogError("PlayerController: No GameInput in scene!");
            return;
        }

        moveAction = GameInput.Instance.MoveAction;
        interactAction = GameInput.Instance.InteractAction;

        SetupAudio();
        ResetAnimator();
        RefreshCarryAnimation();
    }

    private void SetupAudio()
    {
        sleepSource = gameObject.AddComponent<AudioSource>();
        sleepSource.loop = true;
        sleepSource.playOnAwake = false;
        sleepSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;
        interactAction.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        moveAction.performed -= OnMovePerformed;
        moveAction.canceled -= OnMoveCanceled;
        interactAction.performed -= OnInteractPerformed;
    }

    private void FixedUpdate()
    {
        if (!movementEnabled || isSleeping || isSnoring) return;
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
    }

    private void Update()
    {
        UpdateAnimator();
        HandleAFK();
        UpdateFootstepByInterval();
    }

    // =========================
    // INPUT
    // =========================
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
        moveX = 0;
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        ResetAFK();
    }

    // =========================
    // ANIMATOR
    // =========================
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

    // =========================
    // 👣 FOOTSTEP (FIXED)
    // =========================
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

        // ✅ ก้าวแรก: ดังทันทีเมื่อเริ่มเดิน
        if (isWalking && !wasWalkingLastFrame)
        {
            footstepTimer = 0f;
            PlayFootstep();
        }

        wasWalkingLastFrame = isWalking;

        if (!isWalking)
            return;

        float speedRatio = Mathf.Clamp01(speed / moveSpeed);

        float stepInterval =
            Mathf.Lerp(minStepInterval, maxStepInterval, speedRatio)
            * stepIntervalMultiplier;

        // กันรัวเกิน
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

    // =========================
    // AFK / SLEEP
    // =========================
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

    public void Anim_SleepStart()
    {
        isSleeping = true;
        StartSleepSound();
    }

    public void Anim_SleepEnd()
    {
        isSleeping = false;
        StopSleepSound();
    }

    public void Anim_SnoreStart()
    {
        isSnoring = true;
        StartSleepSound();
    }

    public void Anim_SnoreEnd()
    {
        isSnoring = false;
        StopSleepSound();
    }

    public void Anim_WakeEnd()
    {
        isAFK = false;
        isSleeping = false;
        isSnoring = false;
        StopSleepSound();
    }

    // =========================
    // 😴 SLEEP SOUND
    // =========================
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

    // =========================
    // OTHER
    // =========================
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
