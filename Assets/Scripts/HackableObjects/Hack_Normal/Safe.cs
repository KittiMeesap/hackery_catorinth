using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Safe : HackableObject
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openParam = "isOpen";

    [Header("Paper FX")]
    [SerializeField] private GameObject paperPrefab;
    [SerializeField] private Transform paperSpawnPoint;
    [SerializeField] private float flyDistance = 2f;
    [SerializeField] private float flyDuration = 1.5f;
    [SerializeField] private float spinSpeed = 720f;
    [SerializeField] private float disappearDelay = 0.3f;

    [Header("Audio")]
    [SerializeField] private string sfxOpenKey = "SFX_SafeOpen";
    [SerializeField] private string sfxPaperKey = "SFX_PaperFly";

    [Header("Unlock Doors After Hack")]
    [SerializeField] private List<ChocolateDoor> doorsToUnlock = new();

    [Header("Highlight UI")]
    [SerializeField] private SpriteRenderer highlightSprite;

    [Header("Prompt Point")]
    [SerializeField] private Transform promptPoint;

    [Header("Interact Radius")]
    [SerializeField] private float interactRadius = 0.9f;

    private bool isOpened = false;
    private bool isAnimating = false;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();

        triggerType = HackTriggerType.ProximityInteract;
        gameObject.tag = "CanHack";

        if (highlightSprite)
            highlightSprite.enabled = false;

        if (promptPoint == null)
            promptPoint = transform;
    }

    public override float GetInteractRadius() => interactRadius;

    public override Transform GetPromptPoint() => promptPoint;

    public override void Interact()
    {
        if (isOpened || isAnimating) return;
        OnEnterHackingMode();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        RefreshHighlight();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        if (!highlightSprite) return;
        highlightSprite.enabled = ShouldShowHighlight;
    }

    public override bool ShouldShowHighlight =>
        !isOpened &&
        !isAnimating &&
        !IsHacked &&
        !UIManager.Instance.IsHacking &&
        Vector2.Distance(PlayerController.Instance.transform.position, promptPoint.position)
            <= interactRadius;

    public override void OnEnterHackingMode()
    {
        if (UIManager.Instance == null || UIManager.Instance.IsHacking)
            return;

        var option = (hackOptions != null && hackOptions.Count > 0)
            ? hackOptions[0]
            : defaultHackOption;

        if (option == null) return;

        currentUI = UIManager.Instance.hackingUI;
        currentUI.SetCurrentHackTarget(this);

        PlayerController.Instance.SetPhoneOut(true);
        PlayerController.Instance.SetFrozen(true);
        GameManager.Instance.ToggleHackingMode(true);

        var seq = option.isRandom
            ? GenerateRandomSequence(option.randomLength)
            : option.sequence;

        currentUI.ShowSingleOptionSequence(
            seq,
            transform,
            option.icon,
            () => HandleHackOptionComplete(option),
            OnHackFailed,
            useHackTimer,
            hackTimeLimit
        );
    }

    protected override void HandleHackOptionComplete(HackOptionSO option)
    {
        base.HandleHackOptionComplete(option);

        if (isOpened || isAnimating) return;

        UnlockLinkedDoors(); 
        StartCoroutine(OpenSafeSequence());
    }

    private IEnumerator OpenSafeSequence()
    {
        isAnimating = true;
        RefreshHighlight();

        if (animator)
            animator.SetBool(openParam, true);

        if (!string.IsNullOrEmpty(sfxOpenKey))
            AudioManager.Instance?.PlaySFX(sfxOpenKey);

        yield return new WaitForSeconds(0.2f);

        if (paperPrefab && paperSpawnPoint)
        {
            GameObject paper = Instantiate(
                paperPrefab,
                paperSpawnPoint.position,
                Quaternion.identity
            );

            StartCoroutine(PaperFlyRoutine(paper));
        }

        isOpened = true;
        RefreshHighlight();

        yield return new WaitForSeconds(flyDuration + disappearDelay + 0.2f);

        isAnimating = false;
    }

    private void UnlockLinkedDoors()
    {
        foreach (var door in doorsToUnlock)
        {
            if (door != null)
            {
                GameManager.Instance.unlockedDoors.Add(door.name);

                door.UnlockDoorFromSafe();
            }
        }
    }

    private IEnumerator PaperFlyRoutine(GameObject paper)
    {
        if (!paper) yield break;

        Vector3 startPos = paper.transform.position;
        Vector3 targetPos = startPos + Vector3.up * flyDistance;

        float elapsed = 0f;

        if (!string.IsNullOrEmpty(sfxPaperKey))
            AudioManager.Instance?.PlaySFX(sfxPaperKey);

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;

            paper.transform.position = Vector3.Lerp(startPos, targetPos, t);
            paper.transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime);

            yield return null;
        }

        yield return new WaitForSeconds(disappearDelay);

        var sr = paper.GetComponent<SpriteRenderer>();
        if (sr)
        {
            float fade = 1f;
            float fadeTime = 0.3f;

            while (fade > 0f)
            {
                fade -= Time.deltaTime / fadeTime;
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, fade);
                yield return null;
            }
        }

        Destroy(paper);
    }

    public override bool IsFullyOpened => isOpened;
    public override bool IsOnCooldown => false;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.4f);

        Vector3 center = promptPoint ? promptPoint.position : transform.position;
        Gizmos.DrawWireSphere(center, interactRadius);
    }
#endif
}
