using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChocolateDoor : MonoBehaviour, IInteractable, IHeatable, IOpenableDoor
{
    public enum OpenMode { Warp, LoadScene }

    [Header("Open Mode")]
    [SerializeField] private OpenMode openMode = OpenMode.Warp;

    [Header("Warp Settings")]
    [SerializeField] private GameObject connectedDoor;
    [SerializeField] private Transform exitPoint;

    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private bool waitForSceneLoaded = true;

    [Header("Animator Params")]
    [SerializeField] private Animator animator;
    [SerializeField] private string meltParam = "IsMelt";

    [Header("Audio Keys")]
    [SerializeField] private string sfxMeltKey = "SFX_ChocolateDoor_Break";
    [SerializeField] private string sfxOpenKey = "SFX_DoorSugarOpening";

    [Header("Behaviour")]
    [SerializeField] private bool startLocked = true;
    [SerializeField] private float meltThreshold = 2f;
    [SerializeField] private float openToTeleportDelay = 0.1f;
    [SerializeField] private float reuseCooldown = 0.5f;

    [Header("Prompt UI")]
    [SerializeField] private Transform promptPoint;

    private float temperature = 0f;
    private bool isLocked = true;
    private bool hasMelted = false;
    private bool canUseDoor = true;

    private GameObject playerGO;
    private Collider2D triggerCol;


    private void Awake()
    {
        animator ??= GetComponent<Animator>();
        triggerCol ??= GetComponent<Collider2D>();
        if (triggerCol) triggerCol.isTrigger = true;

        EnsurePromptPoint();
    }

    private void EnsurePromptPoint()
    {
        if (promptPoint != null) return;

        GameObject go = new GameObject("InteractPosition");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        promptPoint = go.transform;
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

    // ---------------- Heat / Cold ----------------
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

    // ---------------- Trigger ----------------
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        if (other.CompareTag("Player"))
        {
            playerGO = other.gameObject;

            if (!isLocked)
                UIManager.Instance?.ShowInteractPrompt(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        UIManager.Instance?.HideInteractPrompt(this);
        playerGO = null;
    }

    // ---------------- Interact ----------------
    public void Interact()
    {
        if (isLocked || !canUseDoor) return;
        StartCoroutine(OpenAndGo());
    }

    private IEnumerator OpenAndGo()
    {
        if (isLocked || !canUseDoor || playerGO == null) yield break;

        canUseDoor = false;

        yield return new WaitForSeconds(openToTeleportDelay);

        var fader = UIManager.Instance?.screenFader;
        if (fader != null)
            yield return fader.FadeOut();

        if (openMode == OpenMode.Warp)
        {
            WarpEntity(playerGO);

            if (fader != null)
                yield return fader.FadeIn();
        }
        else
        {
            yield return DoLoadScene();

            if (fader != null)
                yield return fader.FadeIn();
        }

        yield return new WaitForSeconds(reuseCooldown);
        canUseDoor = true;
    }

    // ---------------- Warp / Scene Load ----------------
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

    private IEnumerator DoLoadScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[ChocolateDoor] targetSceneName is empty.");
            yield break;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);
        if (!waitForSceneLoaded) yield break;

        while (!op.isDone)
            yield return null;
    }

    // ---------------- Utility ----------------
    public void DisableInteractionTemporarily(float delay)
    {
        StartCoroutine(CoDisable(delay));
    }

    private IEnumerator CoDisable(float delay)
    {
        bool prev = canUseDoor;
        canUseDoor = false;

        yield return new WaitForSeconds(delay);

        canUseDoor = prev;
    }


    // ---------------- Interact Position ----------------
    public Transform GetPromptPoint()
    {
        return promptPoint != null ? promptPoint : transform;
    }


    // ---------------- Open Check ----------------
    public bool CanOpenFor(GameObject entity)
    {
        if (!canUseDoor) return false;

        if (entity != null && entity.CompareTag("Enemy"))
            return connectedDoor != null && exitPoint != null;

        if (isLocked) return false;

        if (startLocked && !hasMelted)
            return false;

        return connectedDoor != null && exitPoint != null;
    }

    public void OpenForEntity(GameObject entity)
    {
        if (!CanOpenFor(entity)) return;
        WarpEntity(entity);
    }

    public void UnlockDoorFromSafe()
    {
        isLocked = false;
        hasMelted = true;
        ApplyLockState(false);

        Debug.Log("[ChocolateDoor] Door unlocked by Safe.");
    }
}
