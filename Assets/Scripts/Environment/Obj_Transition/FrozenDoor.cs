using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrozenDoor : MonoBehaviour, IHeatable, IOpenableDoor, IHasExitPoint
{
    [Header("Warp Settings")]
    [SerializeField] private GameObject connectedDoor;
    [SerializeField] private Transform exitPoint;
    public Transform ExitPoint => exitPoint;

    [Header("Checkpoint Settings")]
    [SerializeField] private bool isCheckpointDoor = false;

    [Header("Animator Params")]
    [SerializeField] private Animator animator;
    [SerializeField] private string freezeParam = "isLock";
    [SerializeField] private string unfreezeParam = "isUnlock";
    [SerializeField] private string openParam = "isOpen";
    [SerializeField] private string closeParam = "isClose";

    [Header("Audio Keys")]
    [SerializeField] private string sfxMeltKey = "SFX_DoorMelt";
    [SerializeField] private string sfxOpenKey = "SFX_DoorSugarOpening";
    [SerializeField] private string sfxCloseKey = "SFX_DoorSugarClosing";

    [Header("Behaviour")]
    [SerializeField] private bool startFrozen = true;
    [SerializeField] private float meltThreshold = 2f;
    [SerializeField] private float teleportDelay = 0.1f;
    [SerializeField] private float reuseCooldown = 0.5f;

    private float temperature = 0f;
    private bool isFrozen = true;
    private bool canUseDoor = true;

    private Collider2D triggerCol;
    private HashSet<GameObject> recentlyTeleported = new HashSet<GameObject>();

    private void Awake()
    {
        animator ??= GetComponent<Animator>();
        triggerCol ??= GetComponent<Collider2D>();
        if (triggerCol != null)
            triggerCol.isTrigger = true;
    }

    private void Start()
    {
        isFrozen = startFrozen;
        ApplyFrozenState(isFrozen);
    }

    private void Update()
    {
        if (temperature >= meltThreshold && isFrozen)
            MeltDoor();
    }

    public void ApplyHeat(float delta)
    {
        temperature += delta;
    }

    public void ApplyCold(float delta) { }
    public void CoolDown(float delta) { }

    private void MeltDoor()
    {
        isFrozen = false;
        ApplyFrozenState(false);
        AudioManager.Instance?.PlaySFX(sfxMeltKey);
    }

    private void ApplyFrozenState(bool frozen)
    {
        if (animator == null) return;

        animator.SetBool(freezeParam, frozen);
        animator.SetBool(unfreezeParam, !frozen);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!isFrozen)
        {
            if (animator != null)
                animator.SetBool(openParam, true);

            if (!string.IsNullOrEmpty(sfxOpenKey))
                AudioManager.Instance?.PlaySFX(sfxOpenKey);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        GameObject entity = other.gameObject;

        if (!other.CompareTag("Player"))
            return;

        if (recentlyTeleported.Contains(entity))
            return;

        if (isFrozen || !canUseDoor)
            return;

        if (!CanOpenFor(entity))
            return;

        StartCoroutine(TeleportRoutine(entity));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!isFrozen)
        {
            if (animator != null)
                animator.SetBool(openParam, false);

            if (!string.IsNullOrEmpty(sfxCloseKey))
                AudioManager.Instance?.PlaySFX(sfxCloseKey);
        }
    }

    private IEnumerator TeleportRoutine(GameObject entity)
    {
        canUseDoor = false;

        yield return new WaitForSeconds(teleportDelay);

        ScreenFader fader = FindFirstObjectByType<ScreenFader>();
        if (fader != null)
            yield return fader.FadeOut();

        WarpEntity(entity);

        ScreenFader faderIn = FindFirstObjectByType<ScreenFader>();
        if (faderIn != null)
            yield return faderIn.FadeIn();

        yield return new WaitForSeconds(reuseCooldown);
        canUseDoor = true;
    }

    public void WarpEntity(GameObject entity)
    {
        if (entity == null || connectedDoor == null)
            return;

        var nextDoorExit = connectedDoor.GetComponent<IHasExitPoint>();
        if (nextDoorExit == null)
            return;

        Transform targetExit = nextDoorExit.ExitPoint;
        if (targetExit == null)
            return;

        Vector3 oldPos = entity.transform.position;
        Vector3 targetPos = targetExit.position;
        Vector3 delta = targetPos - oldPos;

        entity.transform.position = targetPos;

        var vcam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null)
            vcam.OnTargetObjectWarped(entity.transform, delta);

        if (isCheckpointDoor && entity.CompareTag("Player"))
            GameManager.Instance.SetCheckpoint(targetExit);

        var openable = connectedDoor.GetComponent<IOpenableDoor>();
        if (openable != null)
        {
            openable.MarkRecentlyTeleported(entity);
            openable.DisableInteractionTemporarily(reuseCooldown);
        }
    }

    public bool CanOpenFor(GameObject entity)
    {
        if (entity == null)
            return false;

        if (!entity.CompareTag("Player"))
            return false;

        if (!canUseDoor || isFrozen)
            return false;

        return connectedDoor != null && exitPoint != null;
    }

    public void OpenForEntity(GameObject entity)
    {
        if (!CanOpenFor(entity))
            return;

        StartCoroutine(TeleportRoutine(entity));
    }

    public void OpenForSweeper(GameObject entity)
    {
        if (entity == null)
            return;

        if (isFrozen)
            MeltDoor();

        WarpEntity(entity);
    }

    public void DisableInteractionTemporarily(float delay)
    {
        StartCoroutine(CoDisable(delay));
    }

    private IEnumerator CoDisable(float delay)
    {
        canUseDoor = false;
        yield return new WaitForSeconds(delay);
        canUseDoor = true;
    }

    public void MarkRecentlyTeleported(GameObject entity)
    {
        if (entity == null)
            return;

        recentlyTeleported.Add(entity);
        StartCoroutine(ClearRecentTeleport(entity));
    }

    private IEnumerator ClearRecentTeleport(GameObject entity)
    {
        while (IsEntityInside(entity))
            yield return null;

        recentlyTeleported.Remove(entity);
    }

    private bool IsEntityInside(GameObject entity)
    {
        if (entity == null || triggerCol == null)
            return false;

        return triggerCol.bounds.Contains(entity.transform.position);
    }
}
