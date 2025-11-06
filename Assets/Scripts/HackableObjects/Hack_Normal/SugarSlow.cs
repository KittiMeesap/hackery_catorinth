using System.Collections.Generic;
using UnityEngine;

public class SugarSlow : MonoBehaviour, IHeatable, IFreezable
{
    [Header("Slow Settings")]
    [Range(0.01f, 1f)]
    [SerializeField] private float slowMultiplier = 0.5f;

    [Header("Initial State")]
    [SerializeField] private bool startFrozen = false;

    [Header("Animator (Triggers)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string trigFreeze = "Freeze";
    [SerializeField] private string trigMelt = "Melt";

    [Header("Audio")]
    [SerializeField] private string sfxFreezeKey = "SFX_SugarFreeze";
    [SerializeField] private string sfxMeltKey = "SFX_SugarMelt";
    [SerializeField] private AudioSource loopAudio;

    [Header("State Change Threshold")]
    [SerializeField] private float meltThreshold = 2f;
    [SerializeField] private float freezeThreshold = -2f;

    [Header("Layer Settings")]
    [SerializeField] private string physicsLayerWhenMelt = "ObjectHack";
    [SerializeField] private string physicsLayerWhenFreeze = "Pillars";

    [Header("Sorting Layer Settings")]
    [SerializeField] private string sortingLayerWhenMelt = "HackObj";
    [SerializeField] private string sortingLayerWhenFreeze = "Pillars";

    private readonly HashSet<Collider2D> inside = new();
    private float temperature = 0f;
    private bool isFrozen;
    public float CurrentTemperature => temperature;

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!loopAudio) loopAudio = GetComponent<AudioSource>();

        isFrozen = startFrozen;
        UpdateLoopAudioImmediate();
        ApplyLayerState();
    }

    private void OnEnable() => ReapplyAll();

    private void OnDisable()
    {
        foreach (var c in inside) TryRemoveSlow(c);
        inside.Clear();
    }

    private void Update()
    {
        if (temperature >= meltThreshold && isFrozen)
        {
            SetFrozen(false, true, true);
            temperature = 0f;
        }
        else if (temperature <= freezeThreshold && !isFrozen)
        {
            SetFrozen(true, true, true);
            temperature = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        inside.Add(other);
        if (!isFrozen) TryApplySlow(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        inside.Remove(other);
        TryRemoveSlow(other);
    }

    // ----- Temperature System -----
    public void ApplyHeat(float delta) => temperature += delta;
    public void ApplyCold(float delta) => temperature -= delta;
    public void CoolDown(float delta) => temperature = Mathf.MoveTowards(temperature, 0f, delta);

    // ----- Freeze / Melt -----
    private void SetFrozen(bool frozen, bool playSfx, bool playAnim)
    {
        if (isFrozen == frozen)
        {
            ReapplyAll();
            return;
        }

        isFrozen = frozen;

        if (playAnim && animator)
            animator.SetTrigger(isFrozen ? trigFreeze : trigMelt);

        if (playSfx && AudioManager.Instance != null)
        {
            if (isFrozen && !string.IsNullOrEmpty(sfxFreezeKey))
                AudioManager.Instance.PlaySFX(sfxFreezeKey);
            if (!isFrozen && !string.IsNullOrEmpty(sfxMeltKey))
                AudioManager.Instance.PlaySFX(sfxMeltKey);
        }

        UpdateLoopAudio();
        ApplyLayerState();
        ReapplyAll();
    }

    // ---------- Layer System ----------
    private void ApplyLayerState()
    {
        // ?? Physics Layer
        int targetPhysicsLayer = LayerMask.NameToLayer(isFrozen ? physicsLayerWhenFreeze : physicsLayerWhenMelt);
        SetLayerRecursively(gameObject, targetPhysicsLayer);

        // ?? Sorting Layer (Sprite Renderer)
        string targetSortingLayer = isFrozen ? sortingLayerWhenFreeze : sortingLayerWhenMelt;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.sortingLayerName = targetSortingLayer;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }

    // ---------- Slow Effect ----------
    private void TryApplySlow(Collider2D col)
    {
        var pc = col.GetComponentInParent<PlayerController>();
        if (pc != null) pc.SetSpeedModifier(this, slowMultiplier);

        var ec = col.GetComponentInParent<EnemyController>();
        if (ec != null) ec.SetSpeedModifier(this, slowMultiplier);
    }

    private void TryRemoveSlow(Collider2D col)
    {
        var pc = col.GetComponentInParent<PlayerController>();
        if (pc != null) pc.RemoveSpeedModifier(this);

        var ec = col.GetComponentInParent<EnemyController>();
        if (ec != null) ec.RemoveSpeedModifier(this);
    }

    private void ReapplyAll()
    {
        foreach (var c in inside) TryRemoveSlow(c);
        if (!isFrozen) foreach (var c in inside) TryApplySlow(c);
    }

    private void UpdateLoopAudioImmediate()
    {
        if (!loopAudio) return;
        if (isFrozen && loopAudio.isPlaying) loopAudio.Stop();
        if (!isFrozen && !loopAudio.isPlaying) loopAudio.Play();
    }

    private void UpdateLoopAudio()
    {
        if (!loopAudio) return;
        if (isFrozen && loopAudio.isPlaying) loopAudio.Stop();
        if (!isFrozen && !loopAudio.isPlaying) loopAudio.Play();
    }
}
