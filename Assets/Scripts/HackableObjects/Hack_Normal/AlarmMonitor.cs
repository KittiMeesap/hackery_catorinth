using System.Collections;
using UnityEngine;
using static HackableObject;

public class AlarmMonitor : HackableObject
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float verticalTolerance = 1.5f;

    [Header("Alarm Settings")]
    [SerializeField] private float alarmDuration = 6f;
    [SerializeField] private float deactivateAfterReachDistance = 0.5f;

    [Header("Animation (for alarm only)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string paramIsOn = "isOn";
    [SerializeField] private string paramIsOff = "isOff";

    [Header("Audio Keys")]
    [SerializeField] private string alarmSoundKey = "SFX_Alarm";

    [Header("Cooldown")]
    [SerializeField] private float spellCooldown = 2f;
    private bool isOnCooldown = false;

    private Coroutine alarmRoutine;
    private bool isOn = false;
    private Collider2D col;

    private void Reset()
    {
        col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        if (!animator) animator = GetComponent<Animator>();

        allowRepeatHack = true;
        gameObject.tag = "CanCast";
        triggerType = HackTriggerType.MouseHover;
    }

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (!animator) animator = GetComponent<Animator>();
    }

    public override void OnEnterHackingMode()
    {
        base.OnEnterHackingMode();
    }

    protected override void HandleHackOptionComplete(HackOptionSO option)
    {
        if (isOn || isOnCooldown) return;
        StartAlarm();
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

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(alarmSoundKey))
            AudioManager.Instance.PlaySFX(alarmSoundKey);

        alarmRoutine = StartCoroutine(AlarmRoutine());
    }

    private IEnumerator AlarmRoutine()
    {
        float timer = 0f;
        while (timer < alarmDuration)
        {
            AlertEnemiesNearby();
            timer += 1f;
            yield return new WaitForSeconds(1f);
        }
        StopAlarm();
    }

    private void AlertEnemiesNearby()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<EnemyController>(out var enemy))
            {
                float verticalDiff = Mathf.Abs(enemy.transform.position.y - transform.position.y);
                if (verticalDiff <= verticalTolerance)
                    enemy.GoToTarget(transform, deactivateAfterReachDistance, OnEnemyReached);
            }
        }
    }

    private void OnEnemyReached(EnemyController enemy)
    {
        if (!isOn) return;
        StopAlarm();
    }

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

        StartCoroutine(SpellCooldownTimer());
        ResetHack();
    }

    private IEnumerator SpellCooldownTimer()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(spellCooldown);
        isOnCooldown = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
#endif
}
