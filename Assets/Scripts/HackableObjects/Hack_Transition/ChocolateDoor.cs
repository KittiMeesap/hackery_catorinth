using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChocolateDoor : MonoBehaviour, IInteractable, IHeatable
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
    [SerializeField] private string openParam = "IsOpen";
    [SerializeField] private string closeParam = "IsClose";

    [Header("Audio Keys")]
    [SerializeField] private string sfxMeltKey = "SFX_ChocolateDoor_Break";
    [SerializeField] private string sfxOpenKey = "SFX_DoorSugarOpening";
    [SerializeField] private string sfxCloseKey = "SFX_DoorSugarClosing";

    [Header("Behaviour")]
    [SerializeField] private bool startLocked = true;
    [SerializeField] private float meltThreshold = 2f;
    [SerializeField] private float openToTeleportDelay = 0.1f;
    [SerializeField] private float reuseCooldown = 0.5f;

    [Header("UI Offset")]
    [SerializeField] private float interactPromptYOffset = 0.8f;

    private float temperature = 0f;
    private bool isLocked = true;
    private bool canUseDoor = true;
    private GameObject playerGO;
    private Collider2D triggerCol;
    private Transform promptPoint;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        triggerCol = GetComponent<Collider2D>();
        if (triggerCol) triggerCol.isTrigger = true;
    }

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!triggerCol) triggerCol = GetComponent<Collider2D>();
        if (triggerCol) triggerCol.isTrigger = true;
    }

    private void Start()
    {
        isLocked = startLocked;
        ApplyLockState(isLocked);
    }

    private void Update()
    {
        if (temperature >= meltThreshold && isLocked)
        {
            MeltDoor();
        }
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
        ApplyLockState(false);

        if (!string.IsNullOrEmpty(sfxMeltKey))
            AudioManager.Instance?.PlaySFX(sfxMeltKey);
    }

    private void ApplyLockState(bool locked)
    {
        if (!animator) return;
        animator.SetBool(meltParam, !locked);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerGO = other.gameObject;

        if (isLocked)
        {
            animator?.SetBool(closeParam, true);
            UIManager.Instance?.HideInteractPrompt(this);
            return;
        }

        UIManager.Instance?.ShowInteractPrompt(this);
        animator?.SetBool(closeParam, false);
        animator?.SetBool(openParam, true);

        if (!string.IsNullOrEmpty(sfxOpenKey))
            AudioManager.Instance?.PlaySFX(sfxOpenKey);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (!isLocked)
        {
            UIManager.Instance?.HideInteractPrompt(this);
            animator?.SetBool(openParam, false);
            animator?.SetBool(closeParam, true);

            if (!string.IsNullOrEmpty(sfxCloseKey))
                AudioManager.Instance?.PlaySFX(sfxCloseKey);
        }
        else
        {
            animator?.SetBool(closeParam, true);
        }

        playerGO = null;
    }

    public void Interact()
    {
        if (isLocked || !canUseDoor) return;
        StartCoroutine(OpenAndGo());
    }

    private IEnumerator OpenAndGo()
    {
        if (isLocked || !canUseDoor || playerGO == null) yield break;
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

        var nextDoor = connectedDoor.GetComponent<ChocolateDoor>();
        Transform targetExit = nextDoor ? nextDoor.exitPoint : null;
        if (targetExit == null) return;

        Vector3 oldPos = playerGO.transform.position;
        Vector3 targetPos = targetExit.position;
        Vector3 delta = targetPos - oldPos;

        playerGO.transform.position = targetPos;

        var vcam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null) vcam.OnTargetObjectWarped(playerGO.transform, delta);

        if (nextDoor) nextDoor.DisableInteractionTemporarily(reuseCooldown);
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
            GameObject offsetGO = new GameObject("ChocolateDoorPromptPoint");
            offsetGO.transform.SetParent(transform);
            offsetGO.transform.localPosition = new Vector3(0f, interactPromptYOffset, 0f);
            promptPoint = offsetGO.transform;
        }
        return promptPoint;
    }
}
