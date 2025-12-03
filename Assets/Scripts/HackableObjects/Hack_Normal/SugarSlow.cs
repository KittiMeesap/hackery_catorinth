using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SugarSlow : MonoBehaviour, IHeatable, IFreezable
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Slow Effect")]
    [SerializeField, Range(0f, 1f)] private float slowMultiplier = 0.4f;

    [Header("Heat & Cold Thresholds")]
    [SerializeField] private float freezeColdThreshold = -1f;
    [SerializeField] private float meltHeatThreshold = 0.5f;

    [Header("Transition Settings")]
    [SerializeField] private float heatDecayRate = 1f;
    [SerializeField] private float coldDecayRate = 1f;

    [Header("Audio Keys (from SoundLibrary)")]
    [SerializeField] private string freezeSFXKey = "SFX_SugarFreeze";
    [SerializeField] private string meltSFXKey = "SFX_SugarMelt";

    private bool isFrozen = false;
    private bool isMelted = true;
    private float heatLevel;
    private bool initialized = false;

    private PlayerController affectedPlayer;

    private void Start()
    {
        heatLevel = meltHeatThreshold + 0.1f;
        SetMelted(true);
        StartCoroutine(InitializeAfterFrame());
    }

    private System.Collections.IEnumerator InitializeAfterFrame()
    {
        yield return null;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        if (Mathf.Abs(heatLevel) > 0.01f)
        {
            heatLevel = Mathf.MoveTowards(
                heatLevel,
                0f,
                Time.deltaTime * (heatLevel > 0 ? heatDecayRate : coldDecayRate)
            );
        }

        ApplyStateByTemperature();
        UpdatePlayerSlowEffect();
    }

    private void ApplyStateByTemperature()
    {
        // ========== FREEZE ==========
        if (heatLevel <= freezeColdThreshold && !isFrozen)
        {
            SetFrozen(true);
            PlayFreezeSFX();
        }
        // ========== MELT ==========
        else if (heatLevel >= meltHeatThreshold && !isMelted)
        {
            SetMelted(true);
            PlayMeltSFX();
        }
    }

    private void PlayFreezeSFX()
    {
        if (AudioManager.Instance != null && !string.IsNullOrWhiteSpace(freezeSFXKey))
        {
            
            AudioManager.Instance.PlaySFXAt(freezeSFXKey, transform.position, false);
        }
    }

    private void PlayMeltSFX()
    {
        if (AudioManager.Instance != null && !string.IsNullOrWhiteSpace(meltSFXKey))
        {
            AudioManager.Instance.PlaySFXAt(meltSFXKey, transform.position, false);
        }
    }

    private void UpdatePlayerSlowEffect()
    {
        if (affectedPlayer == null) return;

        if (isMelted)
            affectedPlayer.SetSpeedModifier(this, slowMultiplier);
        else
            affectedPlayer.RemoveSpeedModifier(this);
    }

    private void SetMelted(bool melted)
    {
        isMelted = melted;
        isFrozen = !melted;

        if (animator)
        {
            animator.SetBool("IsMelted", melted);
            animator.SetBool("IsFrozen", !melted);
        }
    }

    private void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        isMelted = !frozen;

        if (animator)
        {
            animator.SetBool("IsFrozen", frozen);
            animator.SetBool("IsMelted", !frozen);
        }
    }

    public void ApplyHeat(float delta) => heatLevel += delta;
    public void ApplyCold(float delta) => heatLevel -= delta;
    public void CoolDown(float delta) =>
        heatLevel = Mathf.MoveTowards(heatLevel, 0f, delta);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            affectedPlayer = other.GetComponent<PlayerController>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            affectedPlayer?.RemoveSpeedModifier(this);
            affectedPlayer = null;
        }
    }
}
