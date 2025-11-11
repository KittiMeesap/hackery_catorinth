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
    [SerializeField] private float enterMoveDuration = 0.2f;

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
    private Vector3 defaultScale;
    private float lastFacingDir = 1f;

    private int hashIdle, hashWalk, hashGetIn, hashGetOut, hashIsWalking;
    private PlayerState currentState = PlayerState.Idle;
    private SpriteRenderer sr;

    private CinemachineCamera cam;
    private Transform originalFollowTarget;

    private void Awake()
    {
        isMovableContainer = true;
        defaultScale = transform.localScale;

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
            transform.Translate(input.normalized * moveSpeed * Time.deltaTime);
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

        switch (newState)
        {
            case PlayerState.Idle:
                anim.CrossFade(hashIdle, 0.1f);
                anim.SetBool(hashIsWalking, false);
                break;
            case PlayerState.Walk:
                anim.CrossFade(hashWalk, 0.1f);
                anim.SetBool(hashIsWalking, true);
                break;
        }
    }

    // MAIN INTERACT
    public void Interact()
    {
        if (isBusy) return;
        if (!isInside && Time.time < lastHideTime + hideCooldown) return;
        if (currentPlayer == null) return;

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

        if (AudioManager.Instance) AudioManager.Instance.PlaySFX(sfxOpenKey);
        if (anim) anim.SetTrigger(hashGetIn);

        if (highlightSprite) highlightSprite.enabled = false;
        UIManager.Instance?.HideInteractPrompt(this);

        Vector3 startPos = currentPlayer.transform.position;
        Vector3 targetPos = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / enterMoveDuration;
            currentPlayer.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        currentPlayer.EnterHiding(this);
        playerController = currentPlayer.GetComponent<PlayerController>();

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

        if (AudioManager.Instance) AudioManager.Instance.PlaySFX(sfxCloseKey);
        if (anim) anim.SetTrigger(hashGetOut);

        yield return new WaitForSeconds(exitAnimTime);

        currentPlayer.ExitHiding(this);
        playerController = null;
        isInside = false;
        isBusy = false;

        if (cam != null && originalFollowTarget != null)
        {
            cam.Follow = originalFollowTarget;
        }

        isPlayerNear = true;
        UIManager.Instance?.ShowInteractPrompt(this);

        RefreshHighlight();
    }

    // TRIGGERS
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        currentPlayer = other.GetComponent<PlayerHiding>();
        isPlayerNear = true;
        RefreshHighlight();

        if (!isInside)
            UIManager.Instance?.ShowInteractPrompt(this);
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

    // EXIT POSITION OVERRIDE
    public override Vector2 GetExitPosition() => transform.position + Vector3.up * 0.2f;

    // VISUAL
    private void RefreshHighlight()
    {
        if (highlightSprite)
            highlightSprite.enabled = isPlayerNear && !isInside;
    }

    // PROMPT POINT
    public Transform GetPromptPoint()
    {
        if (autoPlacePromptAbove) UpdatePromptPointPosition();
        return promptPoint != null ? promptPoint : transform;
    }

    private void EnsurePromptPoint()
    {
        if (promptPoint == null)
        {
            GameObject go = new GameObject("PromptPoint");
            go.transform.SetParent(transform);
            promptPoint = go.transform;
        }
    }

    private void UpdatePromptPointPosition()
    {
        if (promptPoint == null) return;

        Bounds b = default;
        bool hasBounds = false;

        if (sr != null && sr.sprite != null)
        {
            b = sr.bounds;
            hasBounds = true;
        }
        else
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                b = col.bounds;
                hasBounds = true;
            }
        }

        if (!hasBounds)
        {
            promptPoint.position = transform.position + new Vector3(0, 1f + promptMarginY, 0);
            return;
        }

        Vector3 topCenter = new Vector3(b.center.x, b.max.y + promptMarginY, transform.position.z);
        promptPoint.position = topCenter;
    }

    private void OnDrawGizmosSelected()
    {
        if (promptPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(promptPoint.position, 0.05f);
    }
}
