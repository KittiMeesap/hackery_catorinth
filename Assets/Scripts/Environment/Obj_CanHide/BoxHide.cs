using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BoxHide : HidingSpot, IInteractable
{
    [Header("Animator & States")]
    [SerializeField] private Animator anim;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string getInTriggerParam = "GetIn";
    [SerializeField] private string getOutTriggerParam = "GetOut";

    [Header("Timings")]
    [SerializeField] private float enterAnimTime = 0.35f;
    [SerializeField] private float exitAnimTime = 0.35f;
    [SerializeField] private float idleBlendTime = 0.05f;

    [Header("Mission")]
    [SerializeField] private bool completeMissionOnEnterHide = false;
    [SerializeField] private string missionIdOnEnterHide;

    [Header("Prompt UI")]
    [SerializeField] private Transform promptPoint;
    [SerializeField] private bool autoPlacePromptAbove = true;
    [SerializeField] private float promptMarginY = 0.15f;

    [Header("Audio ( key SoundLibrary)")]
    [Tooltip(" key SFX_BoxOpen")]
    [SerializeField] private string sfxOpenKey = "SFX_BoxOpen";
    [Tooltip("key SFX_BoxClose")]
    [SerializeField] private string sfxCloseKey = "SFX_BoxClose";

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer highlightSprite;

    [Header("Cooldown")]
    [SerializeField] private float hideCooldown = 0.75f;

    private int hashIdle, hashGetIn, hashGetOut;
    private bool isPlayerNear;
    private bool isInside;
    private bool isBusy;
    private float lastHideTime = -999f;

    private PlayerHiding currentPlayer;
    private Vector2 cachedPlayerPosition;
    private SpriteRenderer sr;

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();

        hashIdle = string.IsNullOrEmpty(idleStateName) ? 0 : Animator.StringToHash(idleStateName);
        hashGetIn = string.IsNullOrEmpty(getInTriggerParam) ? 0 : Animator.StringToHash(getInTriggerParam);
        hashGetOut = string.IsNullOrEmpty(getOutTriggerParam) ? 0 : Animator.StringToHash(getOutTriggerParam);

        EnsurePromptPoint();
        if (autoPlacePromptAbove) UpdatePromptPointPosition();

        if (highlightSprite != null) highlightSprite.enabled = false;

        PlayIdleImmediate();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (anim == null) anim = GetComponent<Animator>();
        EnsurePromptPoint();
        if (autoPlacePromptAbove) UpdatePromptPointPosition();
    }
#endif

    private void LateUpdate()
    {
        if (autoPlacePromptAbove) UpdatePromptPointPosition();
    }

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

        if (!isInside) StartCoroutine(EnterRoutine(currentPlayer));
        else StartCoroutine(ExitRoutine(currentPlayer));

        lastHideTime = Time.time;
    }

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

        p.transform.position = cachedPlayerPosition;
        p.ExitHiding(this);

        OnExitHiding(p);
        currentPlayer = p;

        if (isPlayerNear) UIManager.Instance?.ShowInteractPrompt(this);
        PlayIdleBlend();

        isBusy = false;
    }

    private void PlayIdleImmediate()
    {
        if (anim == null || hashIdle == 0) return;
        if (anim.HasState(0, hashIdle))
            anim.Play(hashIdle, 0, 0f);
    }

    private void PlayIdleBlend()
    {
        if (anim == null || hashIdle == 0) return;
        if (anim.HasState(0, hashIdle))
            anim.CrossFade(hashIdle, idleBlendTime, 0, 0f);
    }

    private void EnsurePromptPoint()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(this))
            return;
#endif
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
