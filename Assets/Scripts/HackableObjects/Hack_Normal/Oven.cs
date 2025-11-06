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
    [SerializeField] private float spellCooldown = 2f;
    private bool isOnCooldown = false;

    private bool currentState;
    private float checkTimer;
    private AudioSource loopSource;
    private readonly HashSet<IHeatable> heatablesInRange = new();
    private float lastHeatTickTime = -1f;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();

        loopSource = GetComponent<AudioSource>();
        if (loopSource != null)
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
        if (!currentState) return;

        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            float now = Time.time;
            float dt = (lastHeatTickTime < 0f) ? checkInterval : Mathf.Max(0.0001f, now - lastHeatTickTime);
            lastHeatTickTime = now;
            checkTimer = 0f;
            ApplyHeatSystem(dt);
        }
    }

    private void ApplyHeatSystem(float dt)
    {
        Vector2 center = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, heatRadius, heatLayer);

        HashSet<IHeatable> current = new();

        foreach (var col in hits)
        {
            if (col.TryGetComponent<IHeatable>(out var ht))
            {
                ht.ApplyHeat(heatPower * dt);
                current.Add(ht);
            }

            if (col.TryGetComponent<PlayerHiding>(out var hiding))
                hiding.EnterSmoke();

            if (col.TryGetComponent<EnemyController>(out var enemy))
                enemy.EnterSmoke();
        }

        foreach (var prev in heatablesInRange)
            if (!current.Contains(prev))
                prev.CoolDown(heatDecayRate * dt);

        heatablesInRange.Clear();
        foreach (var h in current) heatablesInRange.Add(h);
    }

    // ---------- Spell Actions ----------
    public void Spell_TurnOn() => SetActiveInternal(true, true);
    public void Spell_TurnOff() => SetActiveInternal(false, true);
    public void Toggle() => SetActiveInternal(!currentState, true);

    private void SetActiveInternal(bool on, bool playSfx)
    {
        if (currentState == on) return;
        currentState = on;

        SetVisualState(on);

        if (on)
        {
            StartLoopSound();
            ambientSoundGate?.EnableZone(true);
            if (playSfx && !string.IsNullOrEmpty(sfxOnKey))
                AudioManager.Instance?.PlaySFX(sfxOnKey);
        }
        else
        {
            StopLoopSound();
            ambientSoundGate?.EnableZone(false);
            if (playSfx && !string.IsNullOrEmpty(sfxOffKey))
                AudioManager.Instance?.PlaySFX(sfxOffKey);
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
                if (heatEffect.isPlaying)
                {
                    heatEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    if (hideParticleWhenOff)
                        heatEffect.gameObject.SetActive(false);
                }
            }
        }
    }

    private void StartLoopSound()
    {
        if (AudioManager.Instance == null || string.IsNullOrEmpty(sfxLoopKey))
            return;

        if (loopSource == null)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
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
        if (loopSource != null && loopSource.isPlaying)
        {
            loopSource.Stop();
            loopSource.clip = null;
        }
    }

    // ---------- Spellcasting ----------
    public override void OnEnterHackingMode()
    {
        base.OnEnterHackingMode();
    }

    protected override void HandleHackOptionComplete(HackOptionSO option)
    {
        if (isOnCooldown) return;
        StartCoroutine(SpellCooldownTimer());

        if (option == null) return;

        switch (option.optionType)
        {
            case HackOptionSO.HackType.Disable:
                Spell_TurnOff();
                break;
            case HackOptionSO.HackType.Enable:
                Spell_TurnOn();
                break;
            default:
                Toggle();
                break;
        }

        base.HandleHackOptionComplete(option);
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
        Gizmos.DrawWireSphere(transform.position, heatRadius);
    }
#endif
}
