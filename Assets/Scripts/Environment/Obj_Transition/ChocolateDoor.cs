using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChocolateDoor : MonoBehaviour, IHeatable, IOpenableDoor
{
    public enum OpenMode { Warp, LoadScene }

    [Header("Open Mode")]
    [SerializeField] private OpenMode openMode = OpenMode.Warp;

    [Header("Warp")]
    [SerializeField] private GameObject connectedDoor;
    [SerializeField] private Transform exitPoint;

    [Header("Scene")]
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private bool waitForSceneLoaded = true;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string meltParam = "IsMelt";

    [Header("Audio")]
    [SerializeField] private string sfxMeltKey = "SFX_ChocolateDoor_Break";
    [SerializeField] private string sfxOpenKey = "SFX_DoorSugarOpening";

    [Header("Behaviour")]
    [SerializeField] private bool startLocked = true;
    [SerializeField] private float meltThreshold = 2f;
    [SerializeField] private float teleportDelay = 0.1f;
    [SerializeField] private float reuseCooldown = 0.5f;

    private float temperature = 0f;
    private bool isLocked = true;
    private bool hasMelted = false;
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
        temperature = 0f;
        isLocked = startLocked;
        ApplyLockState(isLocked);
    }

    private void Update()
    {
        if (!hasMelted && isLocked && temperature >= meltThreshold)
            MeltDoor();
    }

    // ---- Heat ----
    public void ApplyHeat(float delta) => temperature += delta;
    public void ApplyCold(float delta) { }
    public void CoolDown(float delta) { }

    private void MeltDoor()
    {
        hasMelted = true;
        isLocked = false;

        ApplyLockState(false);

        if (!string.IsNullOrEmpty(sfxMeltKey))
            AudioManager.Instance?.PlaySFX(sfxMeltKey);
    }

    private void ApplyLockState(bool locked)
    {
        if (animator)
            animator.SetBool(meltParam, !locked);
    }

    // -------- Trigger --------
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        if (!isLocked)
            AudioManager.Instance?.PlaySFX(sfxOpenKey);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        if (isLocked || !canUseDoor) return;
        if (!CanOpenFor(other.gameObject)) return;

        StartCoroutine(TeleportRoutine(other.gameObject));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // nothing is required anymore (no interact UI)
    }

    // -------- Auto Teleport --------
    private IEnumerator TeleportRoutine(GameObject entity)
    {
        canUseDoor = false;

        yield return new WaitForSeconds(teleportDelay);

        var fader = UIManager.Instance?.screenFader;
        if (fader != null) yield return fader.FadeOut();

        if (openMode == OpenMode.Warp)
        {
            WarpEntity(entity);
            if (fader != null) yield return fader.FadeIn();
        }
        else
        {
            yield return LoadSceneRoutine();
            if (fader != null) yield return fader.FadeIn();
        }

        yield return new WaitForSeconds(reuseCooldown);
        canUseDoor = true;
    }

    public void WarpEntity(GameObject entity)
    {
        if (entity == null || connectedDoor == null) return;

        var nextDoor = connectedDoor.GetComponent<ChocolateDoor>();
        Transform targetExit = nextDoor ? nextDoor.exitPoint : null;
        if (targetExit == null) return;

        Vector3 oldPos = entity.transform.position;
        Vector3 targetPos = targetExit.position;
        Vector3 delta = targetPos - oldPos;

        entity.transform.position = targetPos;

        var vcam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null)
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
        if (!canUseDoor) return false;
        if (isLocked) return false;

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

    public void UnlockDoorFromSafe()
    {
        isLocked = false;
        hasMelted = true;
        ApplyLockState(false);
    }
}
