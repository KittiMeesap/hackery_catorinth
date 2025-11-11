using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Locker : HidingSpot, IInteractable
{
    [Header("Locker Visual")]
    [SerializeField] private Animator lockerAnimator;

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer highlightSprite;

    [Header("Animation Params")]
    [SerializeField] private string getInTriggerParam = "GetIn";
    [SerializeField] private string getOutTriggerParam = "GetOut";
    [SerializeField] private float enterAnimTime = 0.35f;
    [SerializeField] private float exitAnimTime = 0.35f;

    [Header("Mission")]
    [SerializeField] private bool completeMissionOnEnterHide = false;
    [SerializeField] private string missionIdOnEnterHide;

    [Header("Prompt UI")]
    [SerializeField] private Transform promptPoint;
    [SerializeField] private bool autoPlacePromptAbove = true;
    [SerializeField] private float promptMarginY = 0.15f;

    [Header("Audio (key SoundLibrary)")]
    [SerializeField] private string sfxOpenKey = "SFX_LockerOpen";
    [SerializeField] private string sfxCloseKey = "SFX_LockerClose";

    [Header("Cooldown")]
    [SerializeField] private float hideCooldown = 0.75f;

    private static int HashGetIn;
    private static int HashGetOut;
    private static int HashOpen;

    private bool isPlayerNear;
    private bool isInside;
    private bool isBusy;

    private PlayerHiding currentPlayer;
    private Vector2 cachedPlayerPosition;

    private void Awake()
    {
        HashGetIn = Animator.StringToHash(getInTriggerParam);
        HashGetOut = Animator.StringToHash(getOutTriggerParam);
        HashOpen = Animator.StringToHash("Open");

        EnsurePromptPoint();
        if (autoPlacePromptAbove) UpdatePromptPointPosition();

        if (highlightSprite != null)
            highlightSprite.enabled = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            return;

        if (Application.isPlaying) return;
        EnsurePromptPoint();
        if (autoPlacePromptAbove) UpdatePromptPointPosition();
    }
#endif

    private void LateUpdate()
    {
        if (autoPlacePromptAbove) UpdatePromptPointPosition();
    }

    private void EnsurePromptPoint()
    {
#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
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
        promptPoint.position = transform.position + new Vector3(0f, 1f + promptMarginY, 0f);
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

        if (!isInside)
            StartCoroutine(EnterRoutine(currentPlayer));
        else
            StartCoroutine(ExitRoutine(currentPlayer));

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

    // ENTER
    private IEnumerator EnterRoutine(PlayerHiding p)
    {
        isBusy = true;

        OnEnterHiding(p);
        currentPlayer = p;
        cachedPlayerPosition = p.transform.position;

        var controller = p.GetComponent<PlayerController>();
        if (controller != null)
            controller.SetFrozen(true);

        p.EnterHiding(this);
        UIManager.Instance?.HideInteractPrompt(this);

        if (lockerAnimator != null)
        {
            lockerAnimator.ResetTrigger(HashOpen);
            lockerAnimator.SetTrigger(!string.IsNullOrEmpty(getInTriggerParam) ? HashGetIn : HashOpen);
        }

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(sfxOpenKey))
            AudioManager.Instance.PlaySFX(sfxOpenKey);

        yield return new WaitForSeconds(enterAnimTime);

        if (controller != null)
            controller.SetFrozen(false);

        isBusy = false;
    }

    // EXIT
    private IEnumerator ExitRoutine(PlayerHiding p)
    {
        isBusy = true;

        if (lockerAnimator != null)
        {
            lockerAnimator.ResetTrigger(HashOpen);
            lockerAnimator.SetTrigger(!string.IsNullOrEmpty(getOutTriggerParam) ? HashGetOut : HashOpen);
        }

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(sfxCloseKey))
            AudioManager.Instance.PlaySFX(sfxCloseKey);

        yield return new WaitForSeconds(exitAnimTime);

        p.transform.position = cachedPlayerPosition;
        p.ExitHiding(this);

        var controller = p.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.SetFrozen(false);
            controller.TriggerMoveDelay(0.05f);
        }

        OnExitHiding(p);
        currentPlayer = p;

        if (isPlayerNear)
            UIManager.Instance?.ShowInteractPrompt(this);

        isBusy = false;
    }

    // POSITION OVERRIDES
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(promptPoint.position, 0.05f);
    }

    private void RefreshHighlight()
    {
        if (highlightSprite == null) return;
        highlightSprite.enabled = isPlayerNear && !isInside;
    }
}
