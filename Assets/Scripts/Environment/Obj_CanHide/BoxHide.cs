using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class BoxHide : HidingSpot, IInteractable
{
    public enum PlayerState { Idle, Walk }

    [Header("Animator & States")]
    [SerializeField] private Animator anim;
    [SerializeField] private string idleStateName = "Box_PlayerIdle";
    [SerializeField] private string walkStateName = "Box_PlayerWalk";
    [SerializeField] private string getInTriggerParam = "GetIn";
    [SerializeField] private string getOutTriggerParam = "GetOut";
    [SerializeField] private string isWalkingBoolParam = "IsWalking";

    [Header("Timings")]
    [SerializeField] private float enterAnimTime = 0.35f;
    [SerializeField] private float exitAnimTime = 0.35f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Prompt UI")]
    [SerializeField] private Transform promptPoint;
    [SerializeField] private bool autoPlacePromptAbove = true;
    [SerializeField] private float promptMarginY = 0.2f;

    [Header("Audio")]
    [SerializeField] private string sfxOpenKey = "SFX_BoxOpen";
    [SerializeField] private string sfxCloseKey = "SFX_BoxClose";

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer highlightSprite;

    [Header("Cooldown")]
    [SerializeField] private float hideCooldown = 0.75f;

    private bool isPlayerNear;
    private bool isInside;
    private bool isBusy;

    private PlayerHiding currentPlayer;
    private PlayerController playerController;
    private SpriteRenderer[] playerSprites;

    private Vector3 defaultScale;
    private float lastFacingDir = 1f;

    private int hashIdle, hashWalk, hashGetIn, hashGetOut, hashIsWalking;
    private PlayerState currentState = PlayerState.Idle;
    private SpriteRenderer sr;

    private CinemachineCamera cam;
    private Transform originalFollowTarget;
    private Rigidbody2D rb;

    private void Awake()
    {
        isMovableContainer = true;
        defaultScale = transform.localScale;
        rb = GetComponent<Rigidbody2D>();

        if (anim == null) anim = GetComponent<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();

        hashIdle = Animator.StringToHash(idleStateName);
        hashWalk = Animator.StringToHash(walkStateName);
        hashGetIn = Animator.StringToHash(getInTriggerParam);
        hashGetOut = Animator.StringToHash(getOutTriggerParam);
        hashIsWalking = Animator.StringToHash(isWalkingBoolParam);

        if (highlightSprite) highlightSprite.enabled = false;
        EnsurePromptPoint();
        if (autoPlacePromptAbove) UpdatePromptPointPosition();

        cam = FindFirstObjectByType<CinemachineCamera>();
    }

    private void Update()
    {
        if (isInside && !isBusy)
            HandleBoxMovement();

        if (autoPlacePromptAbove)
            UpdatePromptPointPosition();
    }

    private void HandleBoxMovement()
    {
        if (playerController == null) return;

        Vector2 input = playerController.GetMoveInput();
        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 delta = input.normalized * moveSpeed * Time.deltaTime;

            if (rb != null)
                rb.MovePosition(rb.position + (Vector2)delta);
            else
                transform.Translate(delta);

            SetPlayerState(PlayerState.Walk);
            lastFacingDir = Mathf.Sign(input.x);
        }
        else
        {
            SetPlayerState(PlayerState.Idle);
        }

        transform.localScale = new Vector3(defaultScale.x * lastFacingDir, defaultScale.y, defaultScale.z);
    }

    private void SetPlayerState(PlayerState newState)
    {
        if (anim == null || currentState == newState) return;
        currentState = newState;
        anim.CrossFade(newState == PlayerState.Idle ? hashIdle : hashWalk, 0.1f);
        anim.SetBool(hashIsWalking, newState == PlayerState.Walk);
    }

    // ======================= INTERACT =======================
    public void Interact()
    {
        if (isBusy) return;

        if (currentPlayer == null)
            currentPlayer = PlayerHiding.Instance;
        if (currentPlayer == null) return;

        if (!isInside && Time.time < lastHideTime + hideCooldown) return;

        if (!isInside)
            StartCoroutine(EnterRoutine());
        else
            StartCoroutine(ExitRoutine());

        lastHideTime = Time.time;
    }

    private IEnumerator EnterRoutine()
    {
        isBusy = true;
        isInside = true;

        AudioManager.Instance?.PlaySFX(sfxOpenKey);
        anim?.SetTrigger(hashGetIn);
        highlightSprite?.gameObject.SetActive(false);
        UIManager.Instance?.HideInteractPrompt(this);

        playerController = PlayerController.Instance;

        playerSprites = playerController.GetComponentsInChildren<SpriteRenderer>();
        foreach (var s in playerSprites) s.enabled = false;

        currentPlayer.EnterHiding(this);
        playerController.SetFrozen(false);

        if (cam != null)
        {
            originalFollowTarget = cam.Follow;
            cam.Follow = this.transform;
        }

        yield return new WaitForSeconds(enterAnimTime);

        isBusy = false;
        RefreshHighlight();
    }

    private IEnumerator ExitRoutine()
    {
        isBusy = true;
        AudioManager.Instance?.PlaySFX(sfxCloseKey);
        anim?.SetTrigger(hashGetOut);

        yield return new WaitForSeconds(exitAnimTime);

        if (playerSprites != null)
        {
            foreach (var s in playerSprites)
                if (s != null) s.enabled = true;
        }

        currentPlayer.ExitHiding(this);
        playerController.transform.position = transform.position + Vector3.up * 0.2f;

        playerController = null;
        isInside = false;
        isBusy = false;

        if (cam != null && originalFollowTarget != null)
            cam.Follow = originalFollowTarget;

        isPlayerNear = true;
        UIManager.Instance?.ShowInteractPrompt(this);

        RefreshHighlight();
    }

    // ======================= TRIGGER =======================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        currentPlayer = other.GetComponent<PlayerHiding>();
        isPlayerNear = true;
        RefreshHighlight();
        if (!isInside) UIManager.Instance?.ShowInteractPrompt(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!isInside)
        {
            isPlayerNear = false;
            currentPlayer = null;
            UIManager.Instance?.HideInteractPrompt(this);
        }
        RefreshHighlight();
    }

    public override Vector2 GetExitPosition() => transform.position + Vector3.up * 0.2f;

    private void RefreshHighlight()
    {
        if (highlightSprite)
            highlightSprite.enabled = isPlayerNear && !isInside;
    }

    public Transform GetPromptPoint()
    {
        if (autoPlacePromptAbove) UpdatePromptPointPosition();
        return promptPoint != null ? promptPoint : transform;
    }

    private void EnsurePromptPoint()
    {
        if (promptPoint == null)
        {
            var go = new GameObject("PromptPoint");
            go.transform.SetParent(transform);
            promptPoint = go.transform;
        }
    }

    private void UpdatePromptPointPosition()
    {
        if (promptPoint == null) return;
        Bounds b = sr != null && sr.sprite != null ? sr.bounds : GetComponent<Collider2D>()?.bounds ?? new Bounds(transform.position, Vector3.one);
        Vector3 topCenter = new(b.center.x, b.max.y + promptMarginY, transform.position.z);
        promptPoint.position = topCenter;
    }
}
