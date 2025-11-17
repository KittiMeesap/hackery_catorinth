using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class BoxHide : HidingSpot, IInteractable
{
    public enum PlayerState { Idle, Walk }

    [Header("Animator")]
    [SerializeField] private Animator anim;
    [SerializeField] private string idleStateName = "Box_PlayerIdle";
    [SerializeField] private string walkStateName = "Box_PlayerWalk";
    [SerializeField] private string getInTrigger = "GetIn";
    [SerializeField] private string getOutTrigger = "GetOut";
    [SerializeField] private string isWalkingBool = "IsWalking";

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Prompt UI")]
    [SerializeField] private Transform promptPoint;
    [SerializeField] private float promptMarginY = 0.2f;

    [Header("Audio")]
    [SerializeField] private string sfxOpenKey = "SFX_BoxOpen";
    [SerializeField] private string sfxCloseKey = "SFX_BoxClose";

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer highlightSprite;

    private bool isInside = false;
    private bool isBusy = false;
    private bool isPlayerNear = false;

    private Rigidbody2D rb;
    private PlayerController playerController;
    private SpriteRenderer[] playerSprites;
    private SpriteRenderer sr;

    private CinemachineCamera cineCam;
    private Transform originalCameraFollow;

    private int hashIdle, hashWalk, hashGetIn, hashGetOut, hashIsWalking;
    private Vector3 defaultScale;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        sr = GetComponentInChildren<SpriteRenderer>();
        defaultScale = transform.localScale;

        // ANIM HASH
        if (anim == null) anim = GetComponent<Animator>();
        hashIdle = Animator.StringToHash(idleStateName);
        hashWalk = Animator.StringToHash(walkStateName);
        hashGetIn = Animator.StringToHash(getInTrigger);
        hashGetOut = Animator.StringToHash(getOutTrigger);
        hashIsWalking = Animator.StringToHash(isWalkingBool);

        // PROMPT POINT
        if (promptPoint == null)
        {
            var go = new GameObject("PromptPoint");
            go.transform.SetParent(transform);
            promptPoint = go.transform;
        }

        cineCam = FindFirstObjectByType<CinemachineCamera>();
        if (cineCam != null)
            originalCameraFollow = cineCam.Follow;
    }

    private void Update()
    {
        if (isInside && !isBusy)
            HandleMovement();

        UpdatePromptPointPosition();
    }

    private void HandleMovement()
    {
        if (playerController == null) return;

        moveInput = playerController.GetMoveInput();
        Vector2 delta = moveInput * moveSpeed * Time.deltaTime;

        rb.MovePosition(rb.position + delta);

        playerController.transform.position = transform.position;

        // Animation
        bool walking = moveInput.sqrMagnitude > 0.01f;
        anim.SetBool(hashIsWalking, walking);
        anim.Play(walking ? hashWalk : hashIdle);

        // Flip
        if (moveInput.x != 0)
        {
            float dir = Mathf.Sign(moveInput.x);
            transform.localScale = new Vector3(defaultScale.x * dir, defaultScale.y, defaultScale.z);
        }
    }

    // ------------------ INTERACT ----------------------
    public void Interact()
    {
        if (isBusy) return;

        if (!isInside)
            StartCoroutine(EnterRoutine());
        else
            StartCoroutine(ExitRoutine());
    }

    private IEnumerator EnterRoutine()
    {
        isBusy = true;
        isInside = true;

        UIManager.Instance?.HideInteractPrompt(this);
        isPlayerNear = false;

        if (highlightSprite) highlightSprite.enabled = false;

        AudioManager.Instance?.PlaySFX(sfxOpenKey);
        anim.SetTrigger(hashGetIn);

        playerController = PlayerController.Instance;

        // Hide player
        playerSprites = playerController.GetComponentsInChildren<SpriteRenderer>();
        foreach (var s in playerSprites) s.enabled = false;

        playerController.ClearInputAndVelocity();
        PlayerHiding.Instance.EnterHiding(this);

        if (cineCam != null)
            cineCam.Follow = this.transform;

        yield return new WaitForSeconds(0.3f);

        isBusy = false;
    }

    private IEnumerator ExitRoutine()
    {
        isBusy = true;

        AudioManager.Instance?.PlaySFX(sfxCloseKey);
        anim.SetTrigger(hashGetOut);

        yield return new WaitForSeconds(0.3f);

        // Show player
        foreach (var s in playerSprites)
            if (s != null) s.enabled = true;

        PlayerHiding.Instance.ExitHiding(this);

        isInside = false;
        isBusy = false;

        if (cineCam != null && originalCameraFollow != null)
            cineCam.Follow = originalCameraFollow;

        if (highlightSprite && isPlayerNear)
            highlightSprite.enabled = true;

        if (isPlayerNear)
            UIManager.Instance?.ShowInteractPrompt(this);
    }

    // ---------------- TRIGGERS ---------------------
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerNear = true;

        if (!isInside)
        {
            UIManager.Instance?.ShowInteractPrompt(this);

            if (highlightSprite)
                highlightSprite.enabled = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerNear = false;

        UIManager.Instance?.HideInteractPrompt(this);

        if (highlightSprite)
            highlightSprite.enabled = false;
    }

    // ---------------- HELPERS ---------------------
    public override Vector2 GetExitPosition()
        => transform.position + Vector3.up * 0.25f;

    public Transform GetPromptPoint()
        => promptPoint != null ? promptPoint : transform;

    private void UpdatePromptPointPosition()
    {
        if (promptPoint == null || sr == null) return;
        Bounds b = sr.bounds;

        promptPoint.position = new Vector3(b.center.x, b.max.y + promptMarginY, transform.position.z);
    }
}
