using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChocolateDoor : MonoBehaviour, IHeatable, IOpenableDoor, IHasExitPoint
{
    public enum OpenMode { Warp, LoadScene }

    [Header("Open Mode")]
    [SerializeField] private OpenMode openMode = OpenMode.Warp;

    [Header("Warp")]
    [SerializeField] private GameObject connectedDoor;
    [SerializeField] private Transform exitPoint;
    public Transform ExitPoint => exitPoint;

    [Header("Scene")]
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private bool waitForSceneLoaded = true;

    [Header("Checkpoint Settings")]
    [SerializeField] private bool isCheckpointDoor = false;

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
    private HashSet<GameObject> recentlyTeleported = new HashSet<GameObject>();


    private void Awake()
    {
        animator ??= GetComponent<Animator>();
        triggerCol ??= GetComponent<Collider2D>();
        triggerCol.isTrigger = true;

        Debug.Log($"[Door] Awake | Collider Trigger = {triggerCol.isTrigger}");
    }

    private void Start()
    {
        isLocked = startLocked;

        bool wasUnlocked = GameManager.Instance.unlockedDoors.Contains(gameObject.name);
        Debug.Log($"[Door] Start | startLocked = {startLocked}, FoundInSavedList = {wasUnlocked}");

        if (wasUnlocked)
        {
            isLocked = false;
            hasMelted = true;
        }

        ApplyLockState(isLocked);
        Debug.Log($"[Door] After ApplyLockState | isLocked = {isLocked}, hasMelted = {hasMelted}");
    }


    private void Update()
    {
        if (!hasMelted && isLocked && temperature >= meltThreshold)
        {
            Debug.Log("[Door] Melt threshold reached → unlock");
            MeltDoor();
        }
    }

    public void ApplyHeat(float delta)
    {
        temperature += delta;
        Debug.Log($"[Door] ApplyHeat: NewTemp = {temperature}");
    }

    public void ApplyCold(float delta) { }
    public void CoolDown(float delta) { }


    private void MeltDoor()
    {
        isLocked = false;
        hasMelted = true;

        Debug.Log("[Door] MeltDoor() | Unlocked");
        GameManager.Instance.unlockedDoors.Add(gameObject.name);

        ApplyLockState(false);

        if (!string.IsNullOrEmpty(sfxMeltKey))
            AudioManager.Instance?.PlaySFX(sfxMeltKey);
    }


    private void ApplyLockState(bool locked)
    {
        if (animator)
        {
            animator.SetBool(meltParam, !locked);
            Debug.Log($"[Door] Animator SetBool({meltParam}, {!locked})");
        }
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Door] Enter Trigger: {other.name}");
    }



    private void OnTriggerStay2D(Collider2D other)
    {
        Debug.Log($"[Door] Stay Trigger by {other.name} | isLocked={isLocked}, canUseDoor={canUseDoor}");

        if (!other.CompareTag("Player"))
        {
            Debug.Log("[Door] Not player → skip");
            return;
        }

        if (recentlyTeleported.Contains(other.gameObject))
        {
            Debug.Log("[Door] Recently teleported → skip");
            return;
        }

        if (isLocked)
        {
            Debug.Log("[Door] Door still locked → cannot teleport");
            return;
        }

        if (!canUseDoor)
        {
            Debug.Log("[Door] Door cooldown → cannot teleport");
            return;
        }

        Debug.Log("[Door] All conditions passed → Teleport!");

        StartCoroutine(TeleportRoutine(other.gameObject));
    }



    private IEnumerator TeleportRoutine(GameObject entity)
    {
        Debug.Log("[Door] TeleportRoutine Started");

        canUseDoor = false;

        yield return new WaitForSeconds(teleportDelay);

        ScreenFader screen = FindFirstObjectByType<ScreenFader>();
        if (screen != null)
        {
            Debug.Log("[Door] Fade Out...");
            yield return screen.FadeOut();
        }

        if (openMode == OpenMode.LoadScene)
        {
            Debug.Log($"[Door] Loading Scene: {targetSceneName}");
            yield return LoadSceneRoutine();
        }
        else
        {
            Debug.Log("[Door] Warp Mode — calling WarpEntity()");
            WarpEntity(entity);
        }

        if (screen != null)
        {
            Debug.Log("[Door] Fade In...");
            yield return screen.FadeIn();
        }

        yield return new WaitForSeconds(reuseCooldown);
        canUseDoor = true;

        Debug.Log("[Door] TeleportRoutine Finished");
    }



    private IEnumerator LoadSceneRoutine()
    {
        Debug.Log($"[Door] SceneManager.LoadSceneAsync({targetSceneName})");

        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);

        while (!op.isDone)
            yield return null;
    }



    // ⭐⭐⭐ Implement Interface functions ⭐⭐⭐

    public bool CanOpenFor(GameObject entity)
    {
        return isLocked == false && canUseDoor == true;
    }

    public void OpenForEntity(GameObject entity)
    {
        Debug.Log("[Door] OpenForEntity() called");
        if (CanOpenFor(entity))
            StartCoroutine(TeleportRoutine(entity));
    }

    public void OpenForSweeper(GameObject entity)
    {
        Debug.Log("[Door] OpenForSweeper() called");

        if (isLocked && !hasMelted)
            MeltDoor();

        WarpEntity(entity);
    }

    public void MarkRecentlyTeleported(GameObject entity)
    {
        recentlyTeleported.Add(entity);
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
        Debug.Log("[Door] UnlockDoorFromSafe() called by SAFE");

        isLocked = false;
        hasMelted = true;

        // Save permanent unlock status
        GameManager.Instance.unlockedDoors.Add(gameObject.name);

        ApplyLockState(false);

        if (!string.IsNullOrEmpty(sfxMeltKey))
            AudioManager.Instance?.PlaySFX(sfxMeltKey);
    }


    public void WarpEntity(GameObject entity)
    {
        Debug.Log("[Door] WarpEntity() called — but not used for LoadScene");
    }
}
