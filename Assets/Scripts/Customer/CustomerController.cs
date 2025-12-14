using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public enum FailReason
{
    AcceptTimeout,
    ServeTimeout,
    WrongOrder
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
    public CustomerController CurrentCustomer { get; private set; }

    [Header("Settings")]
    public CustomerPersonality personality = CustomerPersonality.Normal;
    public float moveSpeed = 2f;
    public float baseWaitTime = 20f;

    [Header("Leave Delay")]
    public float leaveDelay = 0.5f;

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

    [Header("Customer Timer UI")]
    public Image customerTimerBG;
    public Image customerTimerFill;

    public Color timerNormalColor = new Color(0.2f, 1f, 0.3f);
    public Color timerWarningColor = new Color(1f, 0.85f, 0.2f);
    public Color timerDangerColor = new Color(1f, 0.3f, 0.3f);

    [Tooltip("0.4 = 40%")]
    [Range(0f, 1f)] public float warningThreshold = 0.4f;

    [Tooltip("0.2 = 20%")]
    [Range(0f, 1f)] public float dangerThreshold = 0.2f;


    [Header("Order Data")]
    public Sprite customerFaceIcon;
    [HideInInspector] public RecipeSO currentRecipe;
    [HideInInspector] public bool hasActiveOrder = false;

    [HideInInspector] public CustomerQueueManager queueManager;
    [HideInInspector] public string prefabId;

    private float waitAcceptTimer;
    private bool isWaitingAccept;

    private bool hasResolved = false;

    private State state = State.WalkingToQueue;
    private float waitTimer;
    private float waitDuration;

    private InputAction interactAction;
    private Coroutine leaveRoutine;


    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();

        if (emotionIcon != null)
            emotionIcon.enabled = false;

        if (interactUI)
            interactUI.SetActive(false);

        if (GameInput.Instance != null)
            interactAction = GameInput.Instance.InteractAction;

    }

    private void OnEnable()
    {
        hasResolved = false;
        hasActiveOrder = false;
        isWaitingAccept = false;

        if (emotionIcon != null)
            emotionIcon.enabled = false;

        SetInteractVisible(false);
        SetTimerVisible(false);

        SetState(State.WalkingToQueue);
    }

    private void OnDisable()
    {
        if (leaveRoutine != null)
        {
            StopCoroutine(leaveRoutine);
            leaveRoutine = null;
        }
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void Update()
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameState.Playing)
            return;

        HandleWaitingTimer();
        UpdateAnimator();
        HandleInteractInput();
        UpdateInteractVisibility();
    }


    // INITIALIZE
    public void Initialize(RecipeSO orderRecipe, Sprite faceIcon, Vector3 queueTarget, int index)
    {
        hasResolved = false;
        currentRecipe = orderRecipe;
        customerFaceIcon = faceIcon;
        targetPosition = queueTarget;
        queueIndex = index;

        hasActiveOrder = false;

        if (emotionIcon != null)
            emotionIcon.enabled = false;

        SetInteractVisible(false);
        SetState(State.WalkingToQueue);
    }

    // STATE MACHINE
    public void SetState(State newState)
    {
        state = newState;

        switch (newState)
        {
            case State.WalkingToQueue:
                SetInteractVisible(false);
                SetTimerVisible(false);

                if (emotionIcon != null)
                    emotionIcon.enabled = false;
                break;

            case State.InQueueIdle:
                SetInteractVisible(false);

                if (queueIndex == 0 && !hasActiveOrder)
                {
                    StartAcceptWaiting();
                }
                else
                {
                    isWaitingAccept = false;
                    SetTimerVisible(false);
                }

                if (emotionIcon != null)
                    emotionIcon.enabled = false;
                break;

            case State.WaitingOrder:
                isWaitingAccept = false;
                SetTimerVisible(false);

                if (emotionIcon != null)
                    emotionIcon.enabled = false;
                break;

            case State.WaitingServe:
                isWaitingAccept = false;

                waitDuration = GetWaitDurationByPersonality();
                waitTimer = waitDuration;

                SetTimerVisible(true);

                if (customerTimerFill != null)
                {
                    customerTimerFill.fillAmount = 1f;
                    customerTimerFill.color = timerNormalColor;
                }

                if (emotionIcon != null)
                    emotionIcon.enabled = true;

                CustomerOrderPanel.Instance?.Show(this, currentRecipe, customerFaceIcon);
                CustomerOrderPanel.Instance?.UpdateTimer(1f);
                break;

            case State.Leaving:
                SetInteractVisible(false);
                isWaitingAccept = false;
                SetTimerVisible(false);

                CustomerOrderPanel.Instance?.Hide();
                break;
        }
    }

    // MOVEMENT
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
                {
                    SetState(State.InQueueIdle);
                }
                else if (state == State.Leaving)
                {
                    queueManager?.OnCustomerLeft(this);
                    // NO Destroy() — pooled object
                }

                return;
            }

            float dir = Mathf.Sign(target.x - pos.x);
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);

            if (spriteRenderer != null)
                spriteRenderer.flipX = dir < 0;
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    public void SetQueueSlot(int newIndex, Vector3 newTarget)
    {
        int oldIndex = queueIndex;

        queueIndex = newIndex;
        targetPosition = newTarget;

        if (state != State.Leaving)
            SetState(State.WalkingToQueue);

        if (oldIndex != 0 && newIndex == 0)
        {
            if (!hasActiveOrder)
            {
                StartAcceptWaiting();
            }
        }
    }

    private void StartAcceptWaiting()
    {
        isWaitingAccept = true;
        waitAcceptTimer = GetWaitDurationByPersonality();

        SetTimerVisible(true);

        if (customerTimerFill != null)
        {
            customerTimerFill.fillAmount = 1f;
            customerTimerFill.color = timerNormalColor;
        }
    }


    private void UpdateAnimator()
    {
        if (animator != null)
            animator.SetBool("IsWalking", Mathf.Abs(rb.linearVelocity.x) > 0.05f);
    }

    private void UpdateTimerVisual(float normalized)
    {
        if (customerTimerFill == null) return;

        customerTimerFill.fillAmount = normalized;

        if (normalized <= dangerThreshold)
            customerTimerFill.color = timerDangerColor;
        else if (normalized <= warningThreshold)
            customerTimerFill.color = timerWarningColor;
        else
            customerTimerFill.color = timerNormalColor;
    }

    // INPUT & INTERACT
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
        if (interactAction == null) return;

        if (!interactAction.WasPerformedThisFrame()) return;

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

    private void ResolveFail(FailReason reason)
    {
        if (hasResolved || !enabled) return;
        hasResolved = true;

        ShowEmotion(emotionAngry);
        CustomerOrderPanel.Instance?.Hide();

        GameManager.Instance.RegisterOrderFail(personality);

        StartLeaveWithDelay();
    }

    // ORDER FLOW
    private void OnPlayerAcceptOrder()
    {
        if (hasActiveOrder) return;

        isWaitingAccept = false;
        SetTimerVisible(false);

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

        if (!player.HasServeBox())
        {
            NotificationUI.Instance?.Show(
                "You need the correct order to serve",
                NotifyType.Warning
            );
            return;
        }

        bool correct =
            player.serveBox != null &&
            player.serveBox.resultRecipe == currentRecipe;

        player.serveBox = null;
        player.OnInventoryChanged?.Invoke();

        var pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.RefreshCarryAnimation();
            pc.ForceIdle();
        }

        if (correct) HandleServeSuccess();
        else HandleServeFailWrongOrder();
    }

    private void HandleServeSuccess()
    {
        if (hasResolved) return;
        hasResolved = true;

        ShowEmotion(emotionHappy);
        CustomerOrderPanel.Instance?.Hide();

        GameManager.Instance.RegisterOrderSuccess();

        if (personality == CustomerPersonality.VIP)
            MichelinStarSystem.Instance.GainStar(1);

        StartLeaveWithDelay();
    }

    private void HandleServeFailWrongOrder()
    {
        NotificationUI.Instance?.Show(
            "This is not what I ordered",
            NotifyType.Info
        );

        ResolveFail(FailReason.WrongOrder);
    }

    private void HandleServeFailTimeout()
    {
        ResolveFail(FailReason.ServeTimeout);
    }

    // LEAVING (WITH DELAY)
    private void StartLeaveWithDelay()
    {
        if (!gameObject.activeInHierarchy) return;

        hasActiveOrder = false;
        SetInteractVisible(false);

        if (leaveRoutine != null)
            StopCoroutine(leaveRoutine);

        leaveRoutine = StartCoroutine(LeaveAfterDelay());
    }


    private IEnumerator LeaveAfterDelay()
    {
        yield return new WaitForSeconds(leaveDelay);

        if (spriteRenderer != null)
            spriteRenderer.flipX = !spriteRenderer.flipX;

        if (queueManager != null && queueManager.exitPoint != null)
            targetPosition = queueManager.exitPoint.position;
        else
            targetPosition = transform.position +
                             new Vector3(spriteRenderer != null && spriteRenderer.flipX ? -5f : 5f, 0, 0);

        SetState(State.Leaving);
        leaveRoutine = null;
    }

    // WAIT TIMER
    private void HandleWaitingTimer()
    {
        //  WAIT ACCEPT
        if (state == State.InQueueIdle && isWaitingAccept && queueIndex == 0)
        {
            waitAcceptTimer -= Time.deltaTime;

            float acceptNormalized =
                Mathf.Clamp01(waitAcceptTimer / GetWaitDurationByPersonality());

            UpdateTimerVisual(acceptNormalized);

            if (waitAcceptTimer <= 0f)
                HandleAcceptTimeout();

            return;
        }

        //  WAIT SERVE
        if (state != State.WaitingServe) return;

        waitTimer -= Time.deltaTime;

        float serveNormalized =
            Mathf.Clamp01(waitTimer / waitDuration);

        UpdateTimerVisual(serveNormalized);

        CustomerOrderPanel.Instance?.UpdateTimer(serveNormalized);

        if (waitTimer <= 0f)
            HandleServeFailTimeout();
    }

    private void HandleAcceptTimeout()
    {
        if (hasResolved) return;

        isWaitingAccept = false;
        ResolveFail(FailReason.AcceptTimeout);
    }


    private float GetWaitDurationByPersonality()
    {
        float personalityMultiplier = personality switch
        {
            CustomerPersonality.Chill => 1.3f,
            CustomerPersonality.Normal => 1f,
            CustomerPersonality.Impatient => 0.7f,
            CustomerPersonality.VIP => 0.85f,
            _ => 1f
        };

        float dayMultiplier = 1f;

        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentDayConfig != null)
        {
            dayMultiplier = GameManager.Instance.CurrentDayConfig.waitTimeMultiplier;
        }

        return baseWaitTime * personalityMultiplier * dayMultiplier;
    }

    // UI HELPERS
    private void ShowEmotion(Sprite sprite)
    {
        if (emotionIcon == null) return;

        emotionIcon.enabled = true;
        emotionIcon.sprite = sprite;
    }

    private void SetInteractVisible(bool visible)
    {
        if (interactUI != null)
            interactUI.SetActive(visible);
    }

    private void SetTimerVisible(bool visible)
    {
        if (customerTimerBG != null)
            customerTimerBG.gameObject.SetActive(visible);

        if (customerTimerFill != null)
            customerTimerFill.gameObject.SetActive(visible);
    }
    public void Freeze()
    {
        rb.linearVelocity = Vector2.zero;
        enabled = false;
    }

    public void ResetForPool()
    {
        // Stop movement
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Stop coroutine
        if (leaveRoutine != null)
        {
            StopCoroutine(leaveRoutine);
            leaveRoutine = null;
        }

        // Reset flags
        hasResolved = false;
        hasActiveOrder = false;
        isWaitingAccept = false;

        // Reset order data
        currentRecipe = null;
        queueIndex = -1;

        // Reset state
        state = State.WalkingToQueue;

        // Hide UI
        SetInteractVisible(false);
        SetTimerVisible(false);

        if (emotionIcon != null)
            emotionIcon.enabled = false;

        // Hide order panel if this customer was showing
        if (CustomerOrderPanel.Instance != null &&
            CustomerOrderPanel.Instance.CurrentCustomer == this)
        {
            CustomerOrderPanel.Instance.Hide();
        }

        // Disable script until reused
        enabled = false;
    }

}
