using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FrozenDoor : MonoBehaviour, IInteractable, IHeatable, IOpenableDoor
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
    [SerializeField] private string freezeParam = "isLock";
    [SerializeField] private string unfreezeParam = "isUnlock";
    [SerializeField] private string openParam = "isOpen";
    [SerializeField] private string closeParam = "isClose";

    [Header("Audio")]
    [SerializeField] private string sfxMeltKey = "SFX_DoorMelt";
    [SerializeField] private string sfxOpenKey = "SFX_DoorSugarOpening";
    [SerializeField] private string sfxCloseKey = "SFX_DoorSugarClosing";

    [Header("Behaviour")]
    [SerializeField] private bool startFrozen = true;
    [SerializeField] private float meltThreshold = 2f;
    [SerializeField] private float openToTeleportDelay = 0.1f;
    [SerializeField] private float reuseCooldown = 0.5f;

    [Header("UI Offset")]
    [SerializeField] private float interactPromptYOffset = 0.8f;

    private float temperature = 0f;
    private bool isFrozen = true;
    private bool canUseDoor = true;
    private GameObject playerGO;
    private Collider2D triggerCol;
    private Transform promptPoint;

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
        {
            MeltDoor();
        }
    }

    public void ApplyHeat(float delta) { temperature += delta; }
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        // PLAYER
        if (other.CompareTag("Player"))
        {
            playerGO = other.gameObject;

            if (!isFrozen)
            {
                UIManager.Instance?.ShowInteractPrompt(this);
                animator?.SetBool(closeParam, false);
                animator?.SetBool(openParam, true);
                AudioManager.Instance?.PlaySFX(sfxOpenKey);
            }
        }

        // ENEMY AUTO USE
        if (other.CompareTag("Enemy") && !isFrozen && canUseDoor)
        {
            OpenForEntity(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (!isFrozen)
        {
            UIManager.Instance?.HideInteractPrompt(this);
            animator?.SetBool(openParam, false);
            animator?.SetBool(closeParam, true);
            AudioManager.Instance?.PlaySFX(sfxCloseKey);
        }

        playerGO = null;
    }

    public void Interact()
    {
        if (isFrozen || !canUseDoor) return;
        StartCoroutine(OpenAndGo());
    }

    private IEnumerator OpenAndGo()
    {
        if (isFrozen || !canUseDoor || playerGO == null) yield break;

        canUseDoor = false;

        animator?.SetBool(openParam, true);
        yield return new WaitForSeconds(openToTeleportDelay);

        var fader = UIManager.Instance?.screenFader;
        if (fader != null) yield return fader.FadeOut();

        if (openMode == OpenMode.Warp)
        {
            DoWarp();
            if (fader != null) yield return fader.FadeIn();
        }
        else
        {
            yield return DoLoadScene();
            if (fader != null) yield return fader.FadeIn();
        }

        yield return new WaitForSeconds(reuseCooldown);
        canUseDoor = true;
    }

    private void DoWarp()
    {
        if (playerGO == null || connectedDoor == null) return;

        var nextDoor = connectedDoor.GetComponent<FrozenDoor>();
        Transform targetExit = nextDoor ? nextDoor.exitPoint : null;
        if (targetExit == null) return;

        Vector3 oldPos = playerGO.transform.position;
        Vector3 targetPos = targetExit.position;
        Vector3 delta = targetPos - oldPos;

        playerGO.transform.position = targetPos;

        var vcam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null) vcam.OnTargetObjectWarped(playerGO.transform, delta);

        nextDoor?.DisableInteractionTemporarily(reuseCooldown);
    }

    private IEnumerator DoLoadScene()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);
        if (!waitForSceneLoaded) yield break;
        while (!op.isDone) yield return null;
    }

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

    public Transform GetPromptPoint()
    {
        if (promptPoint == null)
        {
            GameObject offsetGO = new GameObject("FrozenDoorPromptPoint");
            offsetGO.transform.SetParent(transform);
            offsetGO.transform.localPosition = new Vector3(0f, interactPromptYOffset, 0f);
            promptPoint = offsetGO.transform;
        }
        return promptPoint;
    }

    // === AI / Enemy ===
    public bool CanOpenFor(GameObject entity)
    {
        if (isFrozen) return false;
        if (!canUseDoor) return false;
        if (entity == null) return false;
        return connectedDoor != null && exitPoint != null;
    }

    public void OpenForEntity(GameObject entity)
    {
        if (!CanOpenFor(entity)) return;
        StartCoroutine(OpenForEntityRoutine(entity));
    }

    private IEnumerator OpenForEntityRoutine(GameObject entity)
    {
        canUseDoor = false;

        animator?.SetBool(openParam, true);
        yield return new WaitForSeconds(openToTeleportDelay);

        if (openMode == OpenMode.Warp)
            DoWarpForEntity(entity);

        yield return new WaitForSeconds(reuseCooldown);
        canUseDoor = true;
    }

    private void DoWarpForEntity(GameObject entity)
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
        if (vcam != null) vcam.OnTargetObjectWarped(entity.transform, delta);

        nextDoor?.DisableInteractionTemporarily(reuseCooldown);
    }
}
