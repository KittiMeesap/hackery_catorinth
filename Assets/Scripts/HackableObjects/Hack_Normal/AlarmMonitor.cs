using System.Collections;
using UnityEngine;

public class AlarmMonitor : HackableObject, IInteractable
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float verticalTolerance = 1.5f;

    [Header("Alarm Settings")]
    [SerializeField] private float alarmDuration = 6f;
    [SerializeField] private float deactivateAfterReachDistance = 0.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string paramIsOn = "isOn";
    [SerializeField] private string paramIsOff = "isOff";

    [Header("Audio Keys")]
    [SerializeField] private string alarmSoundKey = "SFX_Alarm";

    [Header("Cooldown")]
    [SerializeField] private float spellCooldown = 2f;
    private bool isOnCooldown = false;

    [Header("Highlight UI")]
    [SerializeField] private SpriteRenderer highlightSprite;

    [Header("Prompt Point")]
    [SerializeField] private Transform promptPoint;

    [Header("Interact Radius")]
    [SerializeField] private float interactRadius = 0.9f;
    public float GetInteractRadius() => interactRadius;

    private bool isOn = false;
    private Coroutine alarmRoutine;
    private Collider2D col;

    private void Reset()
    {
        col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        if (!animator)
            animator = GetComponent<Animator>();

        allowRepeatHack = true;
        triggerType = HackTriggerType.ProximityInteract;

        gameObject.tag = "CanHack";
    }

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (!animator) animator = GetComponent<Animator>();

        if (highlightSprite)
            highlightSprite.enabled = false;

        if (promptPoint == null)
            promptPoint = transform;
    }

    public Transform GetPromptPoint() => promptPoint;

    public void Interact()
    {
        if (!isOn && !isOnCooldown)
            OnEnterHackingMode();
    }

    // ---------------- HIGHLIGHT ----------------
    public override bool ShouldShowHighlight
    {
        get
        {
            if (UIManager.Instance == null) return false;
            if (PlayerController.Instance == null) return false;

            float dist = Vector2.Distance(
                PlayerController.Instance.transform.position,
                promptPoint.position
            );

            return dist <= interactRadius &&
                   !UIManager.Instance.IsHacking &&
                   !isOn &&
                   !isOnCooldown;
        }
    }

    private void Update()
    {
        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        if (!highlightSprite) return;
        highlightSprite.enabled = ShouldShowHighlight;
    }

    private HackOptionSO GetCurrentOption()
    {
        if (hackOptions == null || hackOptions.Count == 0)
            return defaultHackOption;

        return hackOptions.Find(o => o.optionType == HackOptionSO.HackType.Enable);
    }

    public override void OnEnterHackingMode()
    {
        if (UIManager.Instance == null || UIManager.Instance.IsHacking) return;
        if (isOn || isOnCooldown) return;

        var selected = GetCurrentOption();
        if (selected == null) return;

        currentUI = UIManager.Instance.hackingUI;
        currentUI.SetCurrentHackTarget(this);

        PlayerController.Instance.SetPhoneOut(true);
        PlayerController.Instance.SetFrozen(true);
        GameManager.Instance.ToggleHackingMode(true);

        var seq = selected.isRandom ?
                  GenerateRandomSequence(selected.randomLength) :
                  selected.sequence;

        currentUI.ShowSingleOptionSequence(
            seq,
            transform,
            selected.icon,
            () => HandleHackOptionComplete(selected),
            OnHackFailed,
            useHackTimer,
            hackTimeLimit
        );
    }

    protected override void HandleHackOptionComplete(HackOptionSO option)
    {
        if (isOn || isOnCooldown) return;

        StartAlarm();
        RefreshHighlight();

        base.HandleHackOptionComplete(option);
    }

    private void StartAlarm()
    {
        if (isOn) return;

        isOn = true;

        if (animator)
        {
            animator.SetBool(paramIsOn, true);
            animator.SetBool(paramIsOff, false);
        }

        if (!string.IsNullOrEmpty(alarmSoundKey))
            AudioManager.Instance?.PlaySFXAt(alarmSoundKey, transform.position, use3D: false);

        alarmRoutine = StartCoroutine(AlarmRoutine());
    }

    private IEnumerator AlarmRoutine()
    {
        float timer = 0f;

        while (timer < alarmDuration)
        {
            AlertNearbyEnemies();
            timer += 1f;
            yield return new WaitForSeconds(1f);
        }

        StopAlarm();
    }

    private void AlertNearbyEnemies()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<EnemyController>(out var enemy))
            {
                float dy = Mathf.Abs(enemy.transform.position.y - transform.position.y);
                if (dy <= verticalTolerance)
                    enemy.GoToTarget(transform, deactivateAfterReachDistance, OnEnemyReached);
            }
        }
    }

    private void OnEnemyReached(EnemyController enemy)
    {
        if (!isOn) return;
        StopAlarm();
    }

    // ---------------- STOP ALARM ----------------
    public void StopAlarm()
    {
        if (!isOn) return;

        isOn = false;

        if (animator)
        {
            animator.SetBool(paramIsOn, false);
            animator.SetBool(paramIsOff, true);
        }

        if (alarmRoutine != null)
        {
            StopCoroutine(alarmRoutine);
            alarmRoutine = null;
        }

        StartCoroutine(AlarmCooldown());

        ResetHack();
        RefreshHighlight();
    }

    private IEnumerator AlarmCooldown()
    {
        isOnCooldown = true;
        RefreshHighlight();

        yield return new WaitForSeconds(spellCooldown);

        isOnCooldown = false;

        ResetHack();
        RefreshHighlight();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.cyan;
        if (promptPoint != null)
            Gizmos.DrawWireSphere(promptPoint.position, interactRadius);
    }
#endif

    public override bool IsFullyOpened => false;
    public override bool IsOnCooldown => isOnCooldown;
}
