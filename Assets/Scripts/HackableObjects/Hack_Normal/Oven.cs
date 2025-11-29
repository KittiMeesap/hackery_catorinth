using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Oven : HackableObject
{
    [Header("Heat Settings")]
    [SerializeField] private float heatRadius = 2.5f;
    [SerializeField] private LayerMask heatLayer;
    [SerializeField] private float heatPower = 1f;
    [SerializeField] private float heatDecayRate = 0.5f;
    [SerializeField] private float checkInterval = 0.2f;

    [Header("Heat Pivot")]
    [SerializeField] private Transform heatPivot;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isOnParam = "isOn";
    [SerializeField] private string isOffParam = "isOff";

    [Header("FX")]
    [SerializeField] private ParticleSystem heatEffect;
    [SerializeField] private bool hideParticleWhenOff = true;

    [Header("Audio")]
    [SerializeField] private string sfxOnKey = "SFX_OvenOn";
    [SerializeField] private string sfxOffKey = "SFX_OvenOff";
    [SerializeField] private string sfxLoopKey = "SFX_OvenHum";

    [Header("Ambient Sound Zone")]
    [SerializeField] private SoundAreaGate ambientSoundGate;

    [Header("Behaviour")]
    [SerializeField] private bool startOn = false;

    [Header("Cooldown")]
    [SerializeField] private float hackCooldown = 2f;
    private bool isOnCooldown = false;

    [Header("Auto Close Settings")]
    [SerializeField] private float autoCloseDelay = 4f;
    private Coroutine autoCloseRoutine;

    [Header("Prompt Point")]
    [SerializeField] private Transform promptPoint;

    [Header("Interact Radius")]
    [SerializeField] private float interactRadius = 0.9f;

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer highlightSprite;

    private bool currentState;
    private float checkTimer;
    private AudioSource loopSource;

    private readonly HashSet<IHeatable> heatablesInRange = new();
    private float lastHeatTickTime = -1f;

    private void Awake()
    {
        allowRepeatHack = true;

        if (!animator) animator = GetComponent<Animator>();
        if (promptPoint == null)
            promptPoint = transform;

        if (highlightSprite != null)
            highlightSprite.enabled = false;

        loopSource = GetComponent<AudioSource>();
        if (loopSource)
        {
            loopSource.playOnAwake = false;
            loopSource.Stop();
        }
    }

    private void Start()
    {
        currentState = startOn;
        SetVisualState(currentState);

        if (!currentState)
        {
            StopLoopSound();
            ambientSoundGate?.EnableZone(false);
        }
    }

    private void Update()
    {
        RefreshHighlight();

        if (!currentState) return;

        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            float now = Time.time;
            float dt = (lastHeatTickTime < 0f)
                ? checkInterval
                : Mathf.Max(0.0001f, now - lastHeatTickTime);

            lastHeatTickTime = now;
            checkTimer = 0f;

            ApplyHeatSystem(dt);
        }
    }

    private void ApplyHeatSystem(float dt)
    {
        Vector2 center = heatPivot ? (Vector2)heatPivot.position : (Vector2)transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, heatRadius, heatLayer);

        HashSet<IHeatable> current = new();

        foreach (var col in hits)
        {
            if (col.TryGetComponent<IHeatable>(out var heatObj))
            {
                heatObj.ApplyHeat(heatPower * dt);
                current.Add(heatObj);
            }

            if (col.TryGetComponent<EnemyController>(out var enemy))
                enemy.EnterSmoke();
        }

        foreach (var prev in heatablesInRange)
            if (!current.Contains(prev))
                prev.CoolDown(heatDecayRate * dt);

        heatablesInRange.Clear();
        foreach (var h in current)
            heatablesInRange.Add(h);
    }

    public override bool ShouldShowHighlight =>
        !currentState &&
        !isOnCooldown &&
        PlayerController.Instance != null &&
        Vector2.Distance(PlayerController.Instance.transform.position, promptPoint.position)
            <= interactRadius;

    private void RefreshHighlight()
    {
        if (!highlightSprite) return;
        highlightSprite.enabled = ShouldShowHighlight;
    }

    public override void RefreshHighlightExternal()
    {
        if (highlightSprite != null)
            highlightSprite.enabled = ShouldShowHighlight;
    }

    public override Transform GetPromptPoint() => promptPoint;

    public override float GetInteractRadius() => interactRadius;

    public override void Interact()
    {
        if (!currentState && !isOnCooldown)
            OnEnterHackingMode();
    }

    private HackOptionSO GetCurrentOption()
    {
        if (hackOptions == null || hackOptions.Count == 0)
            return null;

        return hackOptions.Find(o => o.optionType == HackOptionSO.HackType.Enable);
    }

    public override void OnEnterHackingMode()
    {
        if (UIManager.Instance == null || UIManager.Instance.IsHacking)
            return;

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
        if (isOnCooldown) return;

        StartCoroutine(HackCooldownTimer());

        Hack_TurnOn();
        StartAutoClose();

        base.HandleHackOptionComplete(option);
    }

    private void StartAutoClose()
    {
        if (autoCloseRoutine != null)
            StopCoroutine(autoCloseRoutine);

        autoCloseRoutine = StartCoroutine(AutoCloseRoutine());
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);

        Hack_TurnOff();
        ResetHack();

        RefreshHighlight();
    }

    public void Hack_TurnOn() => SetActiveInternal(true, true);
    public void Hack_TurnOff() => SetActiveInternal(false, true);

    private void SetActiveInternal(bool on, bool playSfx)
    {
        if (currentState == on) return;

        currentState = on;
        SetVisualState(on);
        RefreshHighlight();

        if (on)
        {
            StartLoopSound();
            ambientSoundGate?.EnableZone(true);
            if (playSfx && !string.IsNullOrEmpty(sfxOnKey))
                AudioManager.Instance.PlaySFX(sfxOnKey);
        }
        else
        {
            StopLoopSound();
            ambientSoundGate?.EnableZone(false);
            if (playSfx && !string.IsNullOrEmpty(sfxOffKey))
                AudioManager.Instance.PlaySFX(sfxOffKey);
        }
    }

    private void SetVisualState(bool on)
    {
        if (animator)
        {
            animator.SetBool(isOnParam, on);

            if (!string.IsNullOrEmpty(isOffParam))
                animator.SetBool(isOffParam, !on);
        }

        if (heatEffect)
        {
            if (on)
            {
                if (hideParticleWhenOff && !heatEffect.gameObject.activeSelf)
                    heatEffect.gameObject.SetActive(true);

                heatEffect.Play();
            }
            else
            {
                heatEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                if (hideParticleWhenOff)
                    heatEffect.gameObject.SetActive(false);
            }
        }
    }

    private void StartLoopSound()
    {
        if (AudioManager.Instance == null || string.IsNullOrEmpty(sfxLoopKey))
            return;

        if (!loopSource)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.loop = true;
            loopSource.spatialBlend = 0f;
            loopSource.volume = 1f;
        }

        var clip = AudioManager.Instance.GetClipByKey(sfxLoopKey);

        if (clip != null)
        {
            loopSource.clip = clip;
            loopSource.Stop();
            loopSource.Play();
        }
    }

    private void StopLoopSound()
    {
        if (loopSource && loopSource.isPlaying)
        {
            loopSource.Stop();
            loopSource.clip = null;
        }
    }

    private IEnumerator HackCooldownTimer()
    {
        isOnCooldown = true;
        RefreshHighlight();

        yield return new WaitForSeconds(hackCooldown);

        isOnCooldown = false;
        RefreshHighlight();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Vector3 center = heatPivot ? heatPivot.position : transform.position;
        Gizmos.DrawWireSphere(center, heatRadius);

        Gizmos.color = Color.pink;
        if (promptPoint != null)
            Gizmos.DrawWireSphere(promptPoint.position, interactRadius);
    }
#endif
}
