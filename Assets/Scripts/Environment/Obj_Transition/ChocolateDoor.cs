using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChocolateDoor : MonoBehaviour, IHeatable, IOpenableDoor
{
    [Header("Scene Settings")]
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

    private void Awake()
    {
        animator ??= GetComponent<Animator>();
        triggerCol ??= GetComponent<Collider2D>();
        if (triggerCol != null)
            triggerCol.isTrigger = true;
    }

    private void Start()
    {
        isLocked = startLocked;

        bool wasUnlocked = GameManager.Instance != null &&
                           GameManager.Instance.unlockedDoors.Contains(gameObject.name);

        if (wasUnlocked)
        {
            isLocked = false;
            hasMelted = true;
        }

        ApplyLockState(isLocked);
    }

    private void Update()
    {
        if (!hasMelted && isLocked && temperature >= meltThreshold)
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
        isLocked = false;
        hasMelted = true;

        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.unlockedDoors.Contains(gameObject.name))
                GameManager.Instance.unlockedDoors.Add(gameObject.name);
        }

        ApplyLockState(false);

        if (!string.IsNullOrEmpty(sfxMeltKey))
            AudioManager.Instance?.PlaySFX(sfxMeltKey);
    }

    private void ApplyLockState(bool locked)
    {
        if (animator != null)
            animator.SetBool(meltParam, !locked); // true = ละลายแล้ว
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (isLocked || !canUseDoor)
            return;

        StartCoroutine(TeleportRoutine(other.gameObject));
    }

    private IEnumerator TeleportRoutine(GameObject entity)
    {
        canUseDoor = false;

        yield return new WaitForSeconds(teleportDelay);

        ScreenFader screen = FindFirstObjectByType<ScreenFader>();
        if (screen != null)
            yield return screen.FadeOut();

        yield return LoadSceneRoutine();

        ScreenFader screenIn = FindFirstObjectByType<ScreenFader>();
        if (screenIn != null)
            yield return screenIn.FadeIn();

        yield return new WaitForSeconds(reuseCooldown);
        canUseDoor = true;
    }

    private IEnumerator LoadSceneRoutine()
    {
        if (string.IsNullOrEmpty(targetSceneName))
            yield break;

        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);

        if (!waitForSceneLoaded)
            yield break;

        while (!op.isDone)
            yield return null;
    }

    public bool CanOpenFor(GameObject entity)
    {
        if (entity == null) return false;
        if (!entity.CompareTag("Player")) return false;
        if (isLocked || !canUseDoor) return false;
        return !string.IsNullOrEmpty(targetSceneName);
    }

    public void OpenForEntity(GameObject entity)
    {
        if (!CanOpenFor(entity))
            return;

        StartCoroutine(TeleportRoutine(entity));
    }

    public void OpenForSweeper(GameObject entity)
    {

    }

    public void MarkRecentlyTeleported(GameObject entity)
    {

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
        if (!hasMelted)
        {
            isLocked = false;
            hasMelted = true;

            if (GameManager.Instance != null)
            {
                if (!GameManager.Instance.unlockedDoors.Contains(gameObject.name))
                    GameManager.Instance.unlockedDoors.Add(gameObject.name);
            }

            ApplyLockState(false);

            if (!string.IsNullOrEmpty(sfxMeltKey))
                AudioManager.Instance?.PlaySFX(sfxMeltKey);
        }
    }
}
