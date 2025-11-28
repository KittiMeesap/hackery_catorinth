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

    [Header("Idle to Sleep Settings")]
    [SerializeField] private float afkDelay = 5f;

    [Header("Interaction")]
    [SerializeField] private float maxInteractScanRadius = 1.5f;
    public float MaxInteractScanRadius => maxInteractScanRadius;

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
    private bool isPreparingSleep = false;
    private bool isSleeping = false;
    private bool isWaking = false;

    public bool IsIdle => moveInput.sqrMagnitude < 0.0001f;
    public bool IsPhoneOut { get; private set; } = false;

    public IInteractable CurrentInteractable { get; private set; }

    public static PlayerController Instance { get; private set; }

    private readonly Dictionary<object, float> speedModifiers = new();


    // ---------------- TEMPERATURE ----------------
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
            if (Mathf.Sign(temperature) != sign)
                temperature = 0;
        }

        if (temperature >= maxHeat * damageHeatThreshold &&
            Time.time > lastHeatDamageTime + overheatDamageInterval)
        {
            PlayerHealth.TryDamagePlayer(overheatDamage, transform.position);
            lastHeatDamageTime = Time.time;
        }

        if (temperature <= -maxCold * damageColdThreshold &&
            Time.time > lastColdDamageTime + coldDamageInterval)
        {
            PlayerHealth.TryDamagePlayer(coldDamage, transform.position);
            lastColdDamageTime = Time.time;
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

            foreach (var kv in speedModifiers)
                mult *= Mathf.Clamp(kv.Value, 0.01f, 10f);

            return walkSpeed * mult;
        }
    }


    // ---------------- UNITY ----------------
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


    // ---------------- UPDATE LOOP ----------------
    private void Update()
    {
        UpdateTemperature();

        // Hiding / Hack Freeze
        if (isFrozen)
        {
            StopMovement(true);
            return;
        }

        if (PlayerHiding.Instance != null && PlayerHiding.Instance.IsHidingInContainer)
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

        UpdateInteractPromptPriority();  // <-- ใช้ Priority
        FlipCharacter();
        HandleIdleSleepSystem();
    }

    private void LateUpdate() => UpdateAnimation();


    // ---------------- ANIMATION ----------------
    private void UpdateAnimation()
    {
        if (anim == null) return;

        if (isSleeping)
        {
            anim.SetBool("IsIdle", false);
            anim.SetBool("IsWalking", false);
            anim.SetBool("IsPickupPhone", false);
            anim.SetBool("IsHacking", false);
            return;
        }

        if (isWaking)
        {
            anim.SetBool("IsIdle", false);
            anim.SetBool("IsWalking", false);
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

        // Default
        if (!IsPhoneOut)
        {
            anim.SetBool("IsPickupPhone", false);
            anim.SetBool("IsHacking", false);
        }

        anim.SetBool("IsIdle", IsIdle);
        anim.SetBool("IsWalking", moveInput.sqrMagnitude > 0.001f);
    }

    // PRIORITY INTERACT
    private int GetPriority(IInteractable obj)
    {
        if (obj is HidingSpot) return 1;
        if (obj is HackableObject) return 2;
        return 50;
    }

    private void UpdateInteractPromptPriority()
    {
        if (isFrozen)
        {
            UIManager.Instance?.HideInteractPrompt(currentInteractable);
            currentInteractable = null;
            CurrentInteractable = null;
            RefreshNearbyHackables();
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, maxInteractScanRadius);

        List<IInteractable> list = new();

        foreach (var h in hits)
        {
            if (h.TryGetComponent(out IInteractable obj))
            {
                float dist = Vector2.Distance(transform.position, obj.GetPromptPoint().position);

                if (dist <= obj.GetInteractRadius())
                    list.Add(obj);
            }
        }

        if (list.Count == 0)
        {
            if (currentInteractable != null)
                UIManager.Instance.HideInteractPrompt(currentInteractable);

            currentInteractable = null;
            CurrentInteractable = null;

            RefreshNearbyHackables();
            return;
        }

        IInteractable best = null;
        float bestScore = float.MaxValue;

        foreach (var obj in list)
        {
            float dist = Vector2.Distance(transform.position, obj.GetPromptPoint().position);
            float score = GetPriority(obj) * 10 + dist;

            if (score < bestScore)
            {
                bestScore = score;
                best = obj;
            }
        }

        if (best != currentInteractable)
        {
            if (currentInteractable != null)
                UIManager.Instance.HideInteractPrompt(currentInteractable);

            currentInteractable = best;
            CurrentInteractable = best;

            RefreshNearbyHackables();

            UIManager.Instance.ShowInteractPrompt(best);
        }
        else
        {
            RefreshNearbyHackables();
        }
    }


    private void StopMovement(bool resetInput)
    {
        rb.linearVelocity = Vector2.zero;
        if (resetInput)
            moveInput = Vector2.zero;
    }

    private void MoveCharacter()
    {
        Vector2 targetVel = moveInput.normalized * CurrentSpeed;
        rb.linearVelocity = targetVel;

        if (AudioManager.Instance != null &&
            targetVel.sqrMagnitude > 0.01f &&
            Time.time > lastFootstepTime + footstepInterval)
        {
            AudioManager.Instance.PlaySFX(footstepKey);
            lastFootstepTime = Time.time;
        }
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

    private void HandleIdleSleepSystem()
    {
        if (isSleeping || isWaking || isPreparingSleep)
            return;

        if (IsIdle)
        {
            idleTimer += Time.deltaTime;

            if (!isAFKTriggered && idleTimer >= afkDelay)
            {
                isPreparingSleep = true;
                anim.SetTrigger("AFK");
                isAFKTriggered = true;

                rb.linearVelocity = Vector2.zero;
                moveInput = Vector2.zero;

                StartCoroutine(EnterSleepAfterDelay());
            }
        }
        else
        {
            idleTimer = 0f;

            if (isAFKTriggered || isPreparingSleep)
            {
                anim.SetTrigger("Wake");
                isAFKTriggered = false;
                isPreparingSleep = false;
            }
        }
    }

    private IEnumerator EnterSleepAfterDelay()
    {
        yield return new WaitForSeconds(0.3f);

        isSleeping = true;
        isPreparingSleep = false;
    }

    private IEnumerator RestoreIdleAfterWake()
    {
        rb.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero;

        yield return new WaitForSeconds(1.2f);

        isWaking = false;
        isAFKTriggered = false;
        idleTimer = 0f;

        anim.ResetTrigger("AFK");
        anim.ResetTrigger("Wake");

        anim.SetBool("IsIdle", true);
        anim.SetBool("IsWalking", false);
    }

    private void WakeUp()
    {
        if (!isSleeping || isWaking) return;

        isSleeping = false;
        isWaking = true;

        anim.SetTrigger("Wake");
        StartCoroutine(RestoreIdleAfterWake());
    }

    // ---------------- INPUT ----------------
    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        if (isPreparingSleep)
        {
            moveInput = Vector2.zero;
            return;
        }

        if (isSleeping && !isWaking)
        {
            WakeUp();
            moveInput = Vector2.zero;
            return;
        }

        if (isSleeping || isWaking)
        {
            moveInput = Vector2.zero;
            return;
        }

        if (isFrozen)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        if (!isSleeping && !isWaking)
            moveInput = Vector2.zero;
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
        => currentInteractable?.Interact();


    // ---------------- FREEZE ----------------
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
        rb.linearVelocity = Vector2.zero;
    }


    // ---------------- DAMAGE & UTILITY ----------------
    public void TakeDamage(int amount)
        => PlayerHealth.TryDamagePlayer(amount, transform.position);

    public void SetSpeedModifier(object key, float multiplier)
        => speedModifiers[key] = multiplier;

    public void RemoveSpeedModifier(object key)
    {
        if (speedModifiers.ContainsKey(key))
            speedModifiers.Remove(key);
    }

    private void RefreshNearbyHackables()
    {
        float radius = 2f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (var h in hits)
        {
            if (h.TryGetComponent<HackableObject>(out var hackObj))
            {
                hackObj.RefreshHighlightExternal();
            }
        }
    }


    public Vector2 GetMoveInput() => moveInput;

    void IDamageable.TakeDamage(int amount) => TakeDamage(amount);
    void IHeatable.ApplyHeat(float amt) => ApplyHeat(amt);
    void IHeatable.CoolDown(float amt) => CoolDown(amt);
    void IFreezable.ApplyCold(float amt) => ApplyCold(amt);
    void IFreezable.CoolDown(float amt) => CoolDown(amt);

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, maxInteractScanRadius);
    }
#endif

}
