using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fridge : HackableObject
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

    private bool currentState;
    private float checkTimer;
    private AudioSource loopSource;
    private readonly HashSet<IFreezable> coldablesInRange = new();
    private float lastColdTickTime = -1f;


    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (ambientSoundGate != null)
            ambientSoundGate.EnableZone(startOn);
    }

    private void Start()
    {
        SetActiveInternal(startOn, playSfx: false);

        if (freezeMistVFX && hideParticleWhenOff && !currentState)
            freezeMistVFX.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!currentState) return;

        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            float now = Time.time;
            float dt = (lastColdTickTime < 0f) ? checkInterval : Mathf.Max(0.0001f, now - lastColdTickTime);
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

            if (col.TryGetComponent<PlayerHiding>(out var hiding))
                hiding.EnterSmoke();

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


    public void Hack_TurnOn() => SetActiveInternal(true, true);
    public void Hack_TurnOff() => SetActiveInternal(false, true);
    public void Toggle() => SetActiveInternal(!currentState, true);


    private void SetActiveInternal(bool on, bool playSfx)
    {
        if (currentState == on) return;
        currentState = on;

        if (on) lastColdTickTime = -1f;

        if (animator)
        {
            animator.SetBool(isOpenParam, on);
            if (!string.IsNullOrEmpty(isCloseParam))
                animator.SetBool(isCloseParam, !on);
        }

        if (freezeMistVFX)
        {
            var vfxGO = freezeMistVFX.gameObject;
            if (on)
            {
                if (hideParticleWhenOff && !vfxGO.activeSelf)
                    vfxGO.SetActive(true);

                freezeMistVFX.Clear(true);
                if (!freezeMistVFX.isPlaying)
                    freezeMistVFX.Play();
            }
            else
            {
                if (freezeMistVFX.isPlaying)
                {
                    freezeMistVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    if (hideParticleWhenOff)
                        StartCoroutine(DisableParticleAfterDelay(freezeMistVFX.main.startLifetime.constantMax));
                }
            }
        }

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
        }

        var clip = AudioManager.Instance.GetClipByKey(sfxLoopKey);
        if (clip != null)
        {
            loopSource.clip = clip;
            loopSource.volume = 1f;
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


    private IEnumerator DisableParticleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (freezeMistVFX && hideParticleWhenOff)
            freezeMistVFX.gameObject.SetActive(false);
    }


    protected override void HandleHackOptionComplete(HackOptionSO option)
    {
        if (isOnCooldown) return;
        StartCoroutine(HackCooldownTimer());

        if (option == null) return;

        switch (option.optionType)
        {
            case HackOptionSO.HackType.Disable:
                Hack_TurnOff();
                break;
            case HackOptionSO.HackType.Enable:
                Hack_TurnOn();
                break;
            default:
                Toggle();
                break;
        }
    }


    private IEnumerator HackCooldownTimer()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(hackCooldown);
        isOnCooldown = false;
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 center = freezePivot ? freezePivot.position : transform.position;
        Gizmos.DrawWireSphere(center, coldRadius);
    }
#endif
}
