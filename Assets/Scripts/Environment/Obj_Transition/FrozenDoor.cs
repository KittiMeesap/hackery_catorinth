using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FrozenDoor : MonoBehaviour, IHeatable, IOpenableDoor
{
    public enum OpenMode { Warp, LoadScene }

    [Header("Open Mode")]
    [SerializeField] private OpenMode openMode = OpenMode.Warp;
    [SerializeField] private GameObject connectedDoor;
    [SerializeField] private Transform exitPoint;

    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private bool waitForSceneLoaded = true;

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

    private void Awake()
    {
        animator ??= GetComponent<Animator>();
        triggerCol ??= GetComponent<Collider2D>();
        if (triggerCol) triggerCol.isTrigger = true;
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

    // -------- Heat System --------
    public void ApplyHeat(float delta) => temperature += delta;
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
        animator?.SetBool(freezeParam, frozen);
        animator?.SetBool(unfreezeParam, !frozen);
    }

    // -------- Trigger Logic --------
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        if (!isFrozen)
            animator?.SetBool(openParam, true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        if (isFrozen || !canUseDoor) return;
        if (!CanOpenFor(other.gameObject)) return;

        StartCoroutine(TeleportRoutine(other.gameObject));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!isFrozen)
            animator?.SetBool(openParam, false);
    }

    // -------- Auto Teleport --------
    private IEnumerator TeleportRoutine(GameObject entity)
    {
        canUseDoor = false;

        yield return new WaitForSeconds(teleportDelay);

        var fader = UIManager.Instance?.screenFader;
        if (fader != null) yield return fader.FadeOut();

        if (openMode == OpenMode.Warp)
            WarpEntity(entity);
        else
            yield return LoadSceneRoutine();

        if (fader != null) yield return fader.FadeIn();

        yield return new WaitForSeconds(reuseCooldown);
        canUseDoor = true;
    }

    public void WarpEntity(GameObject entity)
    {
        if (entity == null || connectedDoor == null) return;

        var nextDoor = connectedDoor.GetComponent<FrozenDoor>();
        Transform targetExit = nextDoor ? nextDoor.exitPoint : null;
        if (targetExit == null) return;

        Vector3 oldPos = entity.transform.position;
        Vector3 targetPos = targetExit.position;
        Vector3 delta = targetPos - oldPos;

        entity.transform.position = targetPos;

        var vcam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam)
            vcam.OnTargetObjectWarped(entity.transform, delta);

        nextDoor?.DisableInteractionTemporarily(reuseCooldown);
    }

    private IEnumerator LoadSceneRoutine()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);
        if (!waitForSceneLoaded) yield break;

        while (!op.isDone)
            yield return null;
    }

    public bool CanOpenFor(GameObject entity)
    {
        if (!canUseDoor || isFrozen) return false;
        return connectedDoor != null && exitPoint != null;
    }

    public void OpenForEntity(GameObject entity)
    {
        if (!CanOpenFor(entity))
            return;

        StartCoroutine(TeleportRoutine(entity));
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
}
