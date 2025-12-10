using UnityEngine;
using UnityEngine.UI;

public enum CustomerPersonality
{
    Chill,
    Normal,
    Impatient,
    VIP
}

public enum CustomerToolType
{
    Mixer,
    Oven,
    Fridge,
    Slice,
    FryPan
}

[RequireComponent(typeof(Rigidbody2D))]
public class CustomerController : MonoBehaviour
{
    public enum State
    {
        WalkingToQueue,
        InQueueIdle,
        WaitingOrder,
        WaitingServe,
        Leaving
    }

    [Header("Settings")]
    public CustomerPersonality personality = CustomerPersonality.Normal;
    public float moveSpeed = 2f;
    public float baseWaitTime = 20f;

    [Header("Queue")]
    public int queueIndex = -1;
    public Vector3 targetPosition;
    public float arrivedThreshold = 0.05f;

    [Header("Refs")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("World UI")]
    public Canvas canvasWorld;
    public Image emotionIcon;
    public GameObject interactUI;

    [Header("Emotion Sprites")]
    public Sprite emotionNeutral;
    public Sprite emotionHappy;
    public Sprite emotionAngry;

    [Header("Order Data")]
    public Sprite customerFaceIcon;
    [HideInInspector] public RecipeSO currentRecipe;
    [HideInInspector] public bool hasActiveOrder = false;

    private State state = State.WalkingToQueue;
    private float waitTimer;
    private float waitDuration;

    [HideInInspector] public CustomerQueueManager queueManager;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        queueManager = FindFirstObjectByType<CustomerQueueManager>();

        if (emotionIcon != null)
            emotionIcon.enabled = false;

        if (interactUI)
            interactUI.SetActive(false);
    }

    private void OnEnable()
    {
        SetState(State.WalkingToQueue);
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void Update()
    {
        HandleWaitingTimer();
        UpdateAnimator();
        HandleInteractInput();
        UpdateInteractVisibility();
    }

    // ===== INITIALIZE =====
    public void Initialize(RecipeSO orderRecipe, Sprite faceIcon, Vector3 queueTarget, int index)
    {
        currentRecipe = orderRecipe;
        customerFaceIcon = faceIcon;
        targetPosition = queueTarget;
        queueIndex = index;
        hasActiveOrder = false;

        if (emotionIcon != null)
            emotionIcon.enabled = false;

        SetState(State.WalkingToQueue);
    }

    // ===== STATE MACHINE =====
    public void SetState(State newState)
    {
        state = newState;

        switch (newState)
        {
            case State.WalkingToQueue:
                SetInteractVisible(false);
                if (emotionIcon != null) emotionIcon.enabled = false;
                break;

            case State.InQueueIdle:
            case State.WaitingOrder:
                if (emotionIcon != null) emotionIcon.enabled = false;
                break;

            case State.WaitingServe:
                waitDuration = GetWaitDurationByPersonality();
                waitTimer = waitDuration;

                if (emotionIcon != null)
                    emotionIcon.enabled = true;

                CustomerOrderPanel.Instance.Show(this, currentRecipe, customerFaceIcon);
                CustomerOrderPanel.Instance.UpdateTimer(1f);
                break;

            case State.Leaving:
                SetInteractVisible(false);
                if (emotionIcon != null) emotionIcon.enabled = false;
                CustomerOrderPanel.Instance.Hide();
                break;
        }
    }

    // ===== MOVEMENT =====
    private void HandleMovement()
    {
        if (state == State.WalkingToQueue || state == State.Leaving)
        {
            Vector2 pos = rb.position;
            Vector2 target = new Vector2(targetPosition.x, pos.y);

            float dist = Mathf.Abs(target.x - pos.x);

            if (dist <= arrivedThreshold)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

                if (state == State.WalkingToQueue)
                    SetState(State.InQueueIdle);
                else if (state == State.Leaving)
                {
                    queueManager?.OnCustomerLeft(this);
                    Destroy(gameObject);
                }

                return;
            }

            float dir = Mathf.Sign(target.x - pos.x);
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
            if (spriteRenderer)
                spriteRenderer.flipX = dir < 0;
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    public void SetQueueSlot(int newIndex, Vector3 newTarget)
    {
        queueIndex = newIndex;
        targetPosition = newTarget;

        if (state != State.Leaving)
            SetState(State.WalkingToQueue);
    }

    private void UpdateAnimator()
    {
        if (animator)
            animator.SetBool("IsWalking", Mathf.Abs(rb.linearVelocity.x) > 0.05f);
    }

    // ===== INTERACT INPUT =====
    private bool IsCustomerAtServicePoint =>
        ServiceTrigger.Instance != null &&
        ServiceTrigger.Instance.currentCustomer == this;

    private bool PlayerIsHere =>
        ServiceTrigger.Instance != null &&
        ServiceTrigger.Instance.playerInside;

    private PlayerInventory CurrentPlayerInventory =>
        ServiceTrigger.Instance != null ? ServiceTrigger.Instance.playerInventory : null;

    private void HandleInteractInput()
    {
        if (!IsCustomerAtServicePoint) return;
        if (!PlayerIsHere) return;
        if (InteractStation.interactionLocked) return;

        var interact = GameInput.Instance.Actions.Player.Interact;
        if (!interact.WasPerformedThisFrame()) return;

        if (state == State.InQueueIdle || state == State.WaitingOrder)
            OnPlayerAcceptOrder();
        else if (state == State.WaitingServe)
            OnPlayerTryServe();
    }

    private void UpdateInteractVisibility()
    {
        var inv = CurrentPlayerInventory;

        bool canServe = state == State.WaitingServe &&
                        inv != null &&
                        inv.HasServeBox();

        bool visible =
            IsCustomerAtServicePoint &&
            PlayerIsHere &&
            !InteractStation.interactionLocked &&
            (state == State.InQueueIdle || state == State.WaitingOrder || canServe);

        SetInteractVisible(visible);
    }

    // ===== ORDER SYSTEM =====
    private void OnPlayerAcceptOrder()
    {
        if (hasActiveOrder) return;

        hasActiveOrder = true;

        ShowEmotion(emotionNeutral);

        SetInteractVisible(false);

        InteractStation.interactionLocked = true;
        Invoke(nameof(UnlockInteract), 0.15f);

        SetState(State.WaitingServe);
    }

    private void UnlockInteract()
    {
        InteractStation.interactionLocked = false;
    }

    private void OnPlayerTryServe()
    {
        if (!hasActiveOrder) return;

        var player = CurrentPlayerInventory;
        if (player == null) return;
        if (!player.HasServeBox()) return;

        bool correct = player.serveBox != null &&
                       player.serveBox.resultRecipe == currentRecipe;

        player.serveBox = null;
        player.OnInventoryChanged?.Invoke();
        player.GetComponent<PlayerController>()?.RefreshCarryAnimation();

        if (correct) HandleServeSuccess();
        else HandleServeFail();
    }

    private void HandleServeSuccess()
    {
        ShowEmotion(emotionHappy);
        CustomerOrderPanel.Instance.Hide();

        GameManager.Instance.RegisterOrderSuccess();

        if (personality == CustomerPersonality.VIP)
        {
            MichelinStarSystem.Instance.GainStar(1);
            GameManager.Instance.RegisterOrderSuccess();
        }

        StartLeave();
    }

    private void HandleServeFail()
    {
        ShowEmotion(emotionAngry);
        CustomerOrderPanel.Instance.Hide();

        if (personality == CustomerPersonality.VIP)
            MichelinStarSystem.Instance.LoseStar(1);

        GameManager.Instance.RegisterOrderFail();

        StartLeave();
    }

    // ===== LEAVING =====
    private void StartLeave()
    {
        hasActiveOrder = false;

        SetInteractVisible(false);
        if (emotionIcon)
            emotionIcon.enabled = false;

        if (spriteRenderer)
            spriteRenderer.flipX = !spriteRenderer.flipX;

        if (queueManager?.exitPoint != null)
            targetPosition = queueManager.exitPoint.position;
        else
            targetPosition = transform.position + new Vector3(spriteRenderer.flipX ? -5f : 5f, 0, 0);

        SetState(State.Leaving);
    }

    // ===== WAIT TIMER =====
    private void HandleWaitingTimer()
    {
        if (state != State.WaitingServe) return;

        waitTimer -= Time.deltaTime;
        float normalized = Mathf.Clamp01(waitTimer / waitDuration);

        CustomerOrderPanel.Instance.UpdateTimer(normalized);

        if (waitTimer <= 0f)
            HandleServeFail();
    }

    private float GetWaitDurationByPersonality()
    {
        return personality switch
        {
            CustomerPersonality.Chill => baseWaitTime * 1.3f,
            CustomerPersonality.Normal => baseWaitTime,
            CustomerPersonality.Impatient => baseWaitTime * 0.7f,
            CustomerPersonality.VIP => baseWaitTime * 0.85f,
            _ => baseWaitTime
        };
    }

    // ===== EMOTION UI =====
    private void ShowEmotion(Sprite sprite)
    {
        if (emotionIcon == null) return;

        emotionIcon.enabled = true;
        emotionIcon.sprite = sprite;
    }

    private void SetInteractVisible(bool visible)
    {
        if (interactUI)
            interactUI.SetActive(visible);
    }
}
