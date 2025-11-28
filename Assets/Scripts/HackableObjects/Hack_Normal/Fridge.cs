using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fridge : HackableObject, IInteractable
{
    [Header("Cold Settings")]
    [SerializeField] private float coldRadius = 2.5f;
    [SerializeField] private LayerMask coldLayer;
    [SerializeField] private float coldPower = 1f;
    [SerializeField] private float coldDecayRate = 0.5f;
    [SerializeField] private float checkInterval = 0.2f;

    [Header("Pivot (New Center Point)")]
    [SerializeField] private Transform freezePivot;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isOpenParam = "isOpen";
    [SerializeField] private string isCloseParam = "isClose";

    [Header("FX")]
    [SerializeField] private ParticleSystem freezeMistVFX;
    [SerializeField] private bool hideParticleWhenOff = true;

    [Header("Audio")]
    [SerializeField] private string sfxOnKey = "SFX_FridgeOn";
    [SerializeField] private string sfxOffKey = "SFX_FridgeOff";
    [SerializeField] private string sfxLoopKey = "SFX_FridgeHum";

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

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer highlightSprite;

    [Header("Prompt Point")]
    [SerializeField] private Transform promptPoint;

    [Header("Interact Radius")]
    [SerializeField] private float interactRadius = 0.9f;
    public float GetInteractRadius() => interactRadius;

    private bool currentState;
    private float checkTimer;
    private AudioSource loopSource;

    private readonly HashSet<IFreezable> coldablesInRange = new();
    private float lastColdTickTime = -1f;

    private void Awake()
    {
        allowRepeatHack = true;

        if (!animator) animator = GetComponent<Animator>();
        if (promptPoint == null) promptPoint = transform;
        if (highlightSprite) highlightSprite.enabled = false;

        loopSource = GetComponent<AudioSource>();
        if (loopSource)
        {
            loopSource.playOnAwake = false;
            loopSource.Stop();
        }

        ambientSoundGate?.EnableZone(startOn);
    }

    private void Start()
    {
        currentState = startOn;
        SetActiveInternal(startOn, false);

        if (!currentState && freezeMistVFX && hideParticleWhenOff)
            freezeMistVFX.gameObject.SetActive(false);
    }

    private void Update()
    {
        RefreshHighlight();

        if (!currentState) return;

        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            float now = Time.time;
            float dt = (lastColdTickTime < 0f)
                ? checkInterval
                : Mathf.Max(0.0001f, now - lastColdTickTime);

            lastColdTickTime = now;
            checkTimer = 0f;

            ApplyColdSystem(dt);
        }
    }

    private void ApplyColdSystem(float dt)
    {
        Vector2 center = freezePivot ? (Vector2)freezePivot.position : (Vector2)transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, coldRadius, coldLayer);

        HashSet<IFreezable> current = new();

        foreach (var col in hits)
        {
            if (col.TryGetComponent<IFreezable>(out var fz))
            {
                fz.ApplyCold(coldPower * dt * 2f);
                current.Add(fz);
            }

            if (col.TryGetComponent<EnemyController>(out var enemy))
                enemy.EnterSmoke();
        }

        foreach (var prev in coldablesInRange)
            if (!current.Contains(prev))
                prev.CoolDown(coldDecayRate * dt);

        coldablesInRange.Clear();
        foreach (var f in current)
            coldablesInRange.Add(f);
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

    public Transform GetPromptPoint() => promptPoint;

    public void Interact()
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

    public void Toggle() { }

    private void SetActiveInternal(bool on, bool playSfx)
    {
        if (currentState == on) return;

        currentState = on;
        RefreshHighlight();

        if (on) lastColdTickTime = -1f;

        if (animator)
        {
            animator.SetBool(isOpenParam, on);
            if (!string.IsNullOrEmpty(isCloseParam))
                animator.SetBool(isCloseParam, !on);
        }

        if (freezeMistVFX)
        {
            if (on)
            {
                if (hideParticleWhenOff && !freezeMistVFX.gameObject.activeSelf)
                    freezeMistVFX.gameObject.SetActive(true);
                freezeMistVFX.Play();
            }
            else
            {
                if (freezeMistVFX.isPlaying)
                {
                    freezeMistVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                    if (hideParticleWhenOff)
                    {
                        float delay = freezeMistVFX.main.startLifetime.constantMax;
                        StartCoroutine(DisableParticleAfterDelay(delay));
                    }
                }
            }
        }

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

    private void StartLoopSound()
    {
        if (AudioManager.Instance == null || string.IsNullOrEmpty(sfxLoopKey))
            return;

        if (!loopSource)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.loop = true;
            loopSource.spatialBlend = 0f;
        }

        var clip = AudioManager.Instance.GetClipByKey(sfxLoopKey);
        if (clip != null)
        {
            loopSource.clip = clip;
            loopSource.Play();
        }
    }

    private void StopLoopSound()
    {
        if (loopSource && loopSource.isPlaying)
            loopSource.Stop();
    }

    private IEnumerator DisableParticleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (freezeMistVFX && hideParticleWhenOff)
            freezeMistVFX.gameObject.SetActive(false);
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
        Gizmos.color = Color.cyan;

        Vector3 center = freezePivot ? freezePivot.position : transform.position;
        Gizmos.DrawWireSphere(center, coldRadius);

        Gizmos.color = Color.pink;
        if (promptPoint != null)
            Gizmos.DrawWireSphere(promptPoint.position, interactRadius);
    }
#endif
}
