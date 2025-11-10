using System.Collections;
using UnityEngine;

public class BoxHide : HidingSpot, IInteractable
{
    public enum PlayerState { Idle, Walk }

    [Header("Animator & States")]
    [SerializeField] private Animator anim;
    [SerializeField] private string normalStateName = "Box_Normal";
    [SerializeField] private string idleStateName = "Box_PlayerIdle";
    [SerializeField] private string walkStateName = "Box_PlayerWalk";
    [SerializeField] private string getInTriggerParam = "GetIn";
    [SerializeField] private string getOutTriggerParam = "GetOut";
    [SerializeField] private string isWalkingBoolParam = "IsWalking";

    [Header("Timings")]
    [SerializeField] private float enterAnimTime = 0.35f;
    [SerializeField] private float exitAnimTime = 0.35f;
    [SerializeField] private float idleBlendTime = 0.05f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Mission")]
    [SerializeField] private bool completeMissionOnEnterHide = false;
    [SerializeField] private string missionIdOnEnterHide;

    [Header("Prompt UI")]
    [SerializeField] private Transform promptPoint;
    [SerializeField] private bool autoPlacePromptAbove = true;
    [SerializeField] private float promptMarginY = 0.15f;

    [Header("Audio")]
    [SerializeField] private string sfxOpenKey = "SFX_BoxOpen";
    [SerializeField] private string sfxCloseKey = "SFX_BoxClose";

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer highlightSprite;

    [Header("Cooldown")]
    [SerializeField] private float hideCooldown = 0.75f;

    private int hashNormal, hashIdle, hashWalk, hashGetIn, hashGetOut, hashIsWalking;
    private bool isPlayerNear;
    private bool isInside;
    private bool isBusy;
    private float lastHideTime = -999f;

    private PlayerHiding currentPlayer;
    private PlayerController playerController;
    private Vector2 cachedPlayerPosition;
    private SpriteRenderer sr;
    private PlayerState currentState = PlayerState.Idle;

    private float lastFacingDir = 1f;
    private Vector3 defaultScale;

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        defaultScale = transform.localScale;

        hashNormal = Animator.StringToHash(normalStateName);
        hashIdle = Animator.StringToHash(idleStateName);
        hashWalk = Animator.StringToHash(walkStateName);
        hashGetIn = Animator.StringToHash(getInTriggerParam);
        hashGetOut = Animator.StringToHash(getOutTriggerParam);
        hashIsWalking = Animator.StringToHash(isWalkingBoolParam);

        EnsurePromptPoint();
        if (autoPlacePromptAbove) UpdatePromptPointPosition();
        if (highlightSprite != null) highlightSprite.enabled = false;

        if (anim.HasState(0, hashNormal))
            anim.Play(hashNormal, 0, 0f);
    }

    private void Update()
    {
        if (isInside && currentPlayer != null && !isBusy)
        {
            playerController = currentPlayer.GetComponent<PlayerController>();

            HandleBoxMovement();
            FlipWithPlayer();
        }
    }

    private void LateUpdate()
    {
        if (autoPlacePromptAbove) UpdatePromptPointPosition();
    }

    // ======================================
    // Movement System for the Box
    // ======================================
    private void HandleBoxMovement()
    {
        if (playerController == null) return;

        Vector2 input = playerController.GetMoveInput();

        if (input.sqrMagnitude > 0.01f)
        {
            transform.Translate(input.normalized * moveSpeed * Time.deltaTime);
            SetPlayerState(PlayerState.Walk);
        }
        else
        {
            SetPlayerState(PlayerState.Idle);
        }
    }

    private void FlipWithPlayer()
    {
        if (playerController == null) return;
        Vector2 input = playerController.GetMoveInput();
        if (Mathf.Abs(input.x) > 0.05f)
        {
            lastFacingDir = Mathf.Sign(input.x);
            transform.localScale = new Vector3(defaultScale.x * lastFacingDir, defaultScale.y, defaultScale.z);
        }
    }

    // ======================================
    // Interaction Logic
    // ======================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerNear = true;
        currentPlayer = other.GetComponent<PlayerHiding>();
        UIManager.Instance?.ShowInteractPrompt(this);
        RefreshHighlight();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerNear = false;
        if (!isInside) currentPlayer = null;
        UIManager.Instance?.HideInteractPrompt(this);
        RefreshHighlight();
    }

    public void Interact()
    {
        if (isBusy) return;
        if (Time.time < lastHideTime + hideCooldown) return;
        if (!isPlayerNear && !isInside) return;
        if (currentPlayer == null) return;

        if (!isInside)
            StartCoroutine(EnterRoutine(currentPlayer));
        else
            StartCoroutine(ExitRoutine(currentPlayer));

        lastHideTime = Time.time;
    }

    // ======================================
    // Hiding System
    // ======================================
    public override void OnEnterHiding(PlayerHiding player)
    {
        isInside = true;
        RefreshHighlight();
        if (completeMissionOnEnterHide && !string.IsNullOrEmpty(missionIdOnEnterHide))
            MissionManager.Instance?.MarkInteractComplete(missionIdOnEnterHide);
    }

    public override void OnExitHiding(PlayerHiding player)
    {
        isInside = false;
        RefreshHighlight();
    }

    private IEnumerator EnterRoutine(PlayerHiding p)
    {
        isBusy = true;
        OnEnterHiding(p);
        currentPlayer = p;
        playerController = p.GetComponent<PlayerController>();
        cachedPlayerPosition = p.transform.position;

        p.EnterHiding(this);
        UIManager.Instance?.HideInteractPrompt(this);

        if (anim != null && hashGetIn != 0)
            anim.SetTrigger(hashGetIn);

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(sfxOpenKey))
            AudioManager.Instance.PlaySFX(sfxOpenKey);

        yield return new WaitForSeconds(enterAnimTime);

        PlayIdleBlend();
        isBusy = false;
    }

    private IEnumerator ExitRoutine(PlayerHiding p)
    {
        isBusy = true;

        if (anim != null && hashGetOut != 0)
            anim.SetTrigger(hashGetOut);

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(sfxCloseKey))
            AudioManager.Instance.PlaySFX(sfxCloseKey);

        yield return new WaitForSeconds(exitAnimTime);

        transform.position = cachedPlayerPosition;
        transform.localScale = new Vector3(defaultScale.x * lastFacingDir, defaultScale.y, defaultScale.z);

        p.transform.position = cachedPlayerPosition;
        p.ExitHiding(this);

        currentPlayer = p;
        if (isPlayerNear) UIManager.Instance?.ShowInteractPrompt(this);

        PlayNormalIdle();
        isBusy = false;
    }

    // ======================================
    // Animation Control
    // ======================================
    private void PlayNormalIdle()
    {
        if (anim == null) return;
        if (anim.HasState(0, hashNormal))
            anim.CrossFade(hashNormal, 0.1f, 0, 0f);
        if (hashIsWalking != 0) anim.SetBool(hashIsWalking, false);
    }

    private void PlayIdleBlend()
    {
        if (anim == null || hashIdle == 0) return;
        if (anim.HasState(0, hashIdle))
            anim.CrossFade(hashIdle, idleBlendTime, 0, 0f);
        if (hashIsWalking != 0) anim.SetBool(hashIsWalking, false);
    }

    public void SetPlayerState(PlayerState newState)
    {
        if (anim == null || isBusy) return;
        if (currentState == newState) return;

        currentState = newState;

        switch (newState)
        {
            case PlayerState.Idle:
                if (anim.HasState(0, hashIdle))
                    anim.CrossFade(hashIdle, 0.1f, 0, 0f);
                if (hashIsWalking != 0)
                    anim.SetBool(hashIsWalking, false);
                break;
            case PlayerState.Walk:
                if (anim.HasState(0, hashWalk))
                    anim.CrossFade(hashWalk, 0.1f, 0, 0f);
                if (hashIsWalking != 0)
                    anim.SetBool(hashIsWalking, true);
                break;
        }
    }

    // ======================================
    // Prompt & Visual
    // ======================================
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
            if (col != null) { b = col.bounds; hasBounds = true; }
        }

        if (!hasBounds)
        {
            promptPoint.position = transform.position + new Vector3(0, 1f + promptMarginY, 0);
            return;
        }

        Vector3 topCenter = new Vector3(b.center.x, b.max.y + promptMarginY, transform.position.z);
        promptPoint.position = topCenter;
    }

    public override Vector2 GetHidingPosition()
    {
        return currentPlayer != null ? (Vector2)currentPlayer.transform.position : (Vector2)transform.position;
    }

    public override Vector2 GetExitPosition() => cachedPlayerPosition;

    public Transform GetPromptPoint()
    {
        if (autoPlacePromptAbove) UpdatePromptPointPosition();
        return promptPoint != null ? promptPoint : transform;
    }

    private void OnDrawGizmosSelected()
    {
        if (promptPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(promptPoint.position, 0.05f);
    }

    private void RefreshHighlight()
    {
        if (highlightSprite == null) return;
        highlightSprite.enabled = isPlayerNear && !isInside;
    }
}
