using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class BoxHide : HidingSpot
{
    [Header("Animator")]
    [SerializeField] private Animator anim;
    [SerializeField] private string getInTrigger = "GetIn";
    [SerializeField] private string getOutTrigger = "GetOut";
    [SerializeField] private float enterAnimTime = 0.35f;
    [SerializeField] private float exitAnimTime = 0.35f;

    [Header("Prompt UI")]
    [SerializeField] private Transform promptPoint;

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer highlightSprite;

    [Header("Audio")]
    [SerializeField] private string sfxOpenKey = "SFX_BoxOpen";
    [SerializeField] private string sfxCloseKey = "SFX_BoxClose";

    [Header("Cooldown")]
    [SerializeField] private float hideCooldown = 0.75f;

    [Header("Interact Radius")]
    [SerializeField] private float interactRadius = 0.9f;

    private bool isPlayerNear = false;
    private bool isInside = false;
    private bool isBusy = false;

    private PlayerHiding currentPlayer;
    private PlayerController controller;

    private Vector2 cachedPlayerPosition;
    private SpriteRenderer sr;
    private SpriteRenderer[] playerSprites;

    private CinemachineCamera cineCam;
    private Transform originalCameraFollow;

    private int hashGetIn, hashGetOut;

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        hashGetIn = Animator.StringToHash(getInTrigger);
        hashGetOut = Animator.StringToHash(getOutTrigger);

        sr = GetComponentInChildren<SpriteRenderer>();

        if (promptPoint == null)
        {
            GameObject go = new GameObject("PromptPoint");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            promptPoint = go.transform;
        }

        if (highlightSprite != null)
            highlightSprite.enabled = false;

        cineCam = FindFirstObjectByType<CinemachineCamera>();
        if (cineCam != null)
            originalCameraFollow = cineCam.Follow;
    }

    private void Update()
    {
        if (!isInside)
            UpdatePromptPoint();
    }

    //--------------------------------------------------
    //  OVERRIDE GET INTERACT RADIUS
    //--------------------------------------------------
    public override float GetInteractRadius() => interactRadius;

    //--------------------------------------------------
    //  OVERRIDE GET PROMPT POINT
    //--------------------------------------------------
    public override Transform GetPromptPoint()
        => promptPoint != null ? promptPoint : transform;

    //--------------------------------------------------
    //  OVERRIDE INTERACT
    //--------------------------------------------------
    public override void Interact()
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

    private IEnumerator EnterRoutine(PlayerHiding p)
    {
        isBusy = true;
        isInside = true;

        UIManager.Instance?.HideInteractPrompt(this);
        RefreshHighlight();

        controller = p.GetComponent<PlayerController>();
        cachedPlayerPosition = p.transform.position;

        if (controller)
        {
            controller.SetFrozen(true);
            controller.ClearInputAndVelocity();
        }

        p.EnterHiding(this);

        if (anim) anim.SetTrigger(hashGetIn);

        if (!string.IsNullOrEmpty(sfxOpenKey))
            AudioManager.Instance?.PlaySFX(sfxOpenKey);

        playerSprites = controller.GetComponentsInChildren<SpriteRenderer>();
        foreach (var s in playerSprites) s.enabled = false;

        if (cineCam) cineCam.Follow = this.transform;

        yield return new WaitForSeconds(enterAnimTime);

        if (controller) controller.SetFrozen(false);
        isBusy = false;
    }

    private IEnumerator ExitRoutine(PlayerHiding p)
    {
        isBusy = true;

        if (anim) anim.SetTrigger(hashGetOut);

        if (!string.IsNullOrEmpty(sfxCloseKey))
            AudioManager.Instance?.PlaySFX(sfxCloseKey);

        yield return new WaitForSeconds(exitAnimTime);

        foreach (var s in playerSprites)
            if (s != null) s.enabled = true;

        p.ExitHiding(this);
        isInside = false;

        if (cineCam && originalCameraFollow != null)
            cineCam.Follow = originalCameraFollow;

        if (controller) controller.SetFrozen(false);

        RefreshHighlight();

        if (isPlayerNear)
            UIManager.Instance?.ShowInteractPrompt(this);

        isBusy = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerNear = true;
        currentPlayer = other.GetComponent<PlayerHiding>();

        if (!isInside)
            UIManager.Instance?.ShowInteractPrompt(this);

        RefreshHighlight();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerNear = false;
        if (!isInside)
            currentPlayer = null;

        UIManager.Instance?.HideInteractPrompt(this);
        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        if (!highlightSprite) return;
        highlightSprite.enabled = isPlayerNear && !isInside && !isBusy;
    }

    private void UpdatePromptPoint()
    {
        if (promptPoint == null || sr == null) return;

        Bounds b = sr.bounds;
        promptPoint.position =
            new Vector3(b.center.x, b.max.y + 0.25f, transform.position.z);
    }

    public override Vector2 GetHidingPosition()
        => cachedPlayerPosition;

    public override Vector2 GetExitPosition()
        => cachedPlayerPosition;
}
