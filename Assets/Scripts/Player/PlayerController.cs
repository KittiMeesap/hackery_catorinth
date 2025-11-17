using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IDamageable, ITemperatureAffectable
{
    public static Transform PlayerTransform { get; private set; }

    [Header("Movement")]
    public float walkSpeed = 2f;
    [Range(0f, 1f)] public float hidingMoveMultiplier = 0.5f;

    [Header("Sound Effects")]
    public string footstepKey = "SFX_Footstep";
    public float footstepInterval = 0.4f;

    [Header("Temperature System")]
    [SerializeField] private float maxHeat = 5f;
    [SerializeField] private float maxCold = 5f;
    [SerializeField] private float decayRate = 1f;

    [Header("Heat Damage")]
    [SerializeField] private float overheatDamageInterval = 1f;
    [SerializeField] private int overheatDamage = 1;
    [SerializeField, Range(0f, 1f)] private float damageHeatThreshold = 0.9f;

    [Header("Cold Damage")]
    [SerializeField] private float coldDamageInterval = 2f;
    [SerializeField] private int coldDamage = 1;
    [SerializeField, Range(0f, 1f)] private float damageColdThreshold = 0.9f;

    [Header("Cold Slow")]
    [SerializeField] private float coldSlowThreshold = -1.5f;

    [Header("Control Delay Settings")]
    private float controlUnlockTime = 0f;

    [Header("Idle to Sleep Settings")]
    [SerializeField] private float afkDelay = 5f;

    private float temperature = 0f;
    private float visualTemp = 0f;
    private float lastHeatDamageTime = -999f;
    private float lastColdDamageTime = -999f;

    public float CurrentTemperature => temperature;

    private Rigidbody2D rb;
    private Animator anim;
    private PlayerInput playerInput;
    private SpriteRenderer[] sprites;

    private Vector2 moveInput;
    private bool isFrozen = false;
    private Vector3 defaultScale;

    private float lastFootstepTime;
    private IInteractable currentInteractable;

    private float idleTimer = 0f;
    private bool isAFKTriggered = false;
    private bool isSleeping = false;
    private bool isWaking = false;

    public bool IsIdle => moveInput.sqrMagnitude < 0.0001f;
    public bool IsPhoneOut { get; private set; } = false;

    public static PlayerController Instance { get; private set; }

    private readonly Dictionary<object, float> speedModifiers = new();


    // --------------------- Temperature ---------------------
    public void ApplyHeat(float amt) => temperature = Mathf.Clamp(temperature + amt, -maxCold, maxHeat);
    public void ApplyCold(float amt) => temperature = Mathf.Clamp(temperature - amt, -maxCold, maxHeat);

    public void CoolDown(float amt)
    {
        if (temperature < 0)
            temperature = Mathf.MoveTowards(temperature, 0, amt);
    }

    private void UpdateTemperature()
    {
        if (Mathf.Abs(temperature) > 0.01f)
        {
            float sign = Mathf.Sign(temperature);
            temperature -= sign * decayRate * Time.deltaTime;
            if (Mathf.Sign(temperature) != sign) temperature = 0f;
        }

        if (temperature >= maxHeat * damageHeatThreshold)
        {
            if (Time.time > lastHeatDamageTime + overheatDamageInterval)
            {
                PlayerHealth.TryDamagePlayer(overheatDamage, transform.position);
                lastHeatDamageTime = Time.time;
            }
        }

        if (temperature <= -maxCold * damageColdThreshold)
        {
            if (Time.time > lastColdDamageTime + coldDamageInterval)
            {
                PlayerHealth.TryDamagePlayer(coldDamage, transform.position);
                lastColdDamageTime = Time.time;
            }
        }

        UpdateTemperatureVisual();
    }

    private void UpdateTemperatureVisual()
    {
        if (sprites == null || sprites.Length == 0) return;

        visualTemp = Mathf.MoveTowards(visualTemp, temperature, Time.deltaTime);

        if (visualTemp > 0)
        {
            float t = Mathf.InverseLerp(0, maxHeat, visualTemp);
            Color c = Color.Lerp(Color.white, new Color(1f, 0.5f, 0f), t);
            foreach (var sr in sprites) sr.color = c;
        }
        else if (visualTemp < 0)
        {
            float t = Mathf.InverseLerp(0, -maxCold, visualTemp);
            Color c = Color.Lerp(Color.white, Color.cyan, t);
            foreach (var sr in sprites) sr.color = c;
        }
        else
        {
            foreach (var sr in sprites) sr.color = Color.white;
        }
    }


    private float CurrentSpeed
    {
        get
        {
            float mult = 1f;

            if (temperature <= coldSlowThreshold)
            {
                float t = Mathf.InverseLerp(0, -maxCold, temperature);
                mult *= Mathf.Lerp(1f, 0.2f, t);
            }

            if (PlayerHiding.Instance != null && PlayerHiding.Instance.IsHidingInContainer)
            {
                var spot = typeof(PlayerHiding)
                    .GetField("currentSpot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .GetValue(PlayerHiding.Instance) as HidingSpot;

                if (spot != null && spot.isMovableContainer)
                    mult *= hidingMoveMultiplier;
            }

            foreach (var kv in speedModifiers)
                mult *= Mathf.Clamp(kv.Value, 0.01f, 10f);

            return walkSpeed * mult;
        }
    }


    // --------------------- Unity Lifecycle ---------------------
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        Instance = this;
        PlayerTransform = transform;
        defaultScale = transform.localScale;
    }

    private void OnEnable()
    {
        playerInput.actions["Move"].performed += OnMovePerformed;
        playerInput.actions["Move"].canceled += OnMoveCanceled;
        playerInput.actions["Interact"].performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        playerInput.actions["Move"].performed -= OnMovePerformed;
        playerInput.actions["Move"].canceled -= OnMoveCanceled;
        playerInput.actions["Interact"].performed -= OnInteractPerformed;
    }


    // --------------------- Update Loop ---------------------
    private void Update()
    {
        UpdateTemperature();

        if (isFrozen || Time.time < controlUnlockTime)
        {
            StopMovement(true);
            return;
        }

        if (isSleeping || isWaking)
        {
            StopMovement(true);
            return;
        }

        if (moveInput.sqrMagnitude > 0.0001f)
            MoveCharacter();
        else
            StopMovement(false);

        UpdateInteractPrompt();
        FlipCharacter();
        HandleIdleSleepSystem();
    }

    private void LateUpdate() => UpdateAnimation();


    // --------------------- Animation ---------------------
    private void UpdateAnimation()
    {
        if (anim == null) return;

        if (isSleeping || isWaking)
        {
            anim.SetBool("IsIdle", false);
            anim.SetBool("IsWalking", false);
            anim.SetBool("IsPickupPhone", false);
            anim.SetBool("IsHacking", false);
            return;
        }

        if (IsPhoneOut && !UIManager.Instance.IsHacking)
        {
            anim.SetBool("IsPickupPhone", true);
            anim.SetBool("IsHacking", false);
            anim.SetBool("IsIdle", false);
            anim.SetBool("IsWalking", false);
            return;
        }

        if (UIManager.Instance.IsHacking)
        {
            anim.SetBool("IsPickupPhone", true);
            anim.SetBool("IsHacking", true);
            anim.SetBool("IsIdle", false);
            anim.SetBool("IsWalking", false);
            return;
        }

        if (!IsPhoneOut)
        {
            anim.SetBool("IsPickupPhone", false);
            anim.SetBool("IsHacking", false);
        }

        anim.SetBool("IsIdle", IsIdle);
        anim.SetBool("IsWalking", moveInput.sqrMagnitude > 0.001f);
    }


    // --------------------- Wake System ---------------------
    private void WakeUp()
    {
        if (!isSleeping || isWaking) return;

        anim.SetTrigger("Wake");
        StartCoroutine(RestoreIdleAfterWake());
    }

    private IEnumerator RestoreIdleAfterWake()
    {
        isWaking = true;
        isSleeping = false;

        rb.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero;

        yield return new WaitForSeconds(1.2f);

        isWaking = false;
        idleTimer = 0f;
        isAFKTriggered = false;

        anim.ResetTrigger("AFK");
        anim.ResetTrigger("Wake");
        anim.SetBool("IsIdle", true);
    }


    // --------------------- Movement ---------------------
    private void MoveCharacter()
    {
        Vector2 targetVel = moveInput.normalized * CurrentSpeed;
        rb.linearVelocity = targetVel;

        if (AudioManager.Instance != null
            && targetVel.sqrMagnitude > 0.01f
            && Time.time > lastFootstepTime + footstepInterval)
        {
            AudioManager.Instance.PlaySFX(footstepKey);
            lastFootstepTime = Time.time;
        }
    }

    private void StopMovement(bool resetInput)
    {
        rb.linearVelocity = Vector2.zero;
        if (resetInput) moveInput = Vector2.zero;
    }

    private void FlipCharacter()
    {
        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            transform.localScale = new Vector3(
                defaultScale.x * Mathf.Sign(moveInput.x),
                defaultScale.y,
                defaultScale.z
            );
        }
    }


    // --------------------- Input ---------------------
    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();

        if (isSleeping && !isWaking)
        {
            WakeUp();
            return;
        }

        if (isSleeping || isWaking)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = input;
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        if (!isSleeping && !isWaking)
            moveInput = Vector2.zero;
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
        => currentInteractable?.Interact();


    // --------------------- Sleep / AFK ---------------------
    private void HandleIdleSleepSystem()
    {
        if (isSleeping || isWaking)
            return;

        if (IsIdle)
        {
            idleTimer += Time.deltaTime;

            if (!isAFKTriggered && idleTimer >= afkDelay)
            {
                anim.SetTrigger("AFK");
                isAFKTriggered = true;
                isSleeping = true;
            }
        }
        else
        {
            idleTimer = 0f;

            if (isAFKTriggered)
            {
                anim.SetTrigger("Wake");
                isAFKTriggered = false;
            }
        }
    }


    // --------------------- Interactable ---------------------
    private void UpdateInteractPrompt()
    {
        if (isFrozen)
        {
            UIManager.Instance?.HideInteractPrompt(currentInteractable);
            currentInteractable = null;
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.6f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IInteractable>(out var interactable))
            {
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    UIManager.Instance?.ShowInteractPrompt(currentInteractable);
                }
                return;
            }
        }

        if (currentInteractable != null)
        {
            UIManager.Instance?.HideInteractPrompt(currentInteractable);
            currentInteractable = null;
        }
    }


    // --------------------- State (Frozen / Phone / Delay) ---------------------
    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;

        if (frozen)
        {
            rb.linearVelocity = Vector2.zero;
            moveInput = Vector2.zero;
        }
    }

    public void SetPhoneOut(bool isOut) => IsPhoneOut = isOut;

    public void ClearInputAndVelocity()
    {
        moveInput = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    public void TriggerMoveDelay(float delay)
    {
        controlUnlockTime = Time.time + delay;
        ClearInputAndVelocity();
    }

    public void DisableMoveInputTemporarily(float duration = 0.2f)
    {
        StartCoroutine(DisableMoveInputRoutine(duration));
    }

    private IEnumerator DisableMoveInputRoutine(float duration)
    {
        playerInput.actions["Move"].performed -= OnMovePerformed;
        playerInput.actions["Move"].canceled -= OnMoveCanceled;

        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(duration);

        playerInput.actions["Move"].performed += OnMovePerformed;
        playerInput.actions["Move"].canceled += OnMoveCanceled;
    }


    // --------------------- Damage & Utility ---------------------
    public void TakeDamage(int amount) => PlayerHealth.TryDamagePlayer(amount, transform.position);

    public void SetSpeedModifier(object key, float multiplier) => speedModifiers[key] = multiplier;

    public void RemoveSpeedModifier(object key)
    {
        if (speedModifiers.ContainsKey(key)) speedModifiers.Remove(key);
    }

    public Vector2 GetMoveInput() => moveInput;

    void IDamageable.TakeDamage(int amount) => TakeDamage(amount);
    void IHeatable.ApplyHeat(float amt) => ApplyHeat(amt);
    void IHeatable.CoolDown(float amt) => CoolDown(amt);
    void IFreezable.ApplyCold(float amt) => ApplyCold(amt);
    void IFreezable.CoolDown(float amt) => CoolDown(amt);
}