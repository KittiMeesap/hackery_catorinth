using UnityEngine;

public class SugarSlow : MonoBehaviour, IHeatable, IFreezable
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Visual State")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color meltedColor = new Color(1f, 0.7f, 0.7f);
    [SerializeField] private Color frozenColor = new Color(0.8f, 0.9f, 1f);

    [Header("Slow Effect")]
    [SerializeField, Range(0f, 1f)] private float slowMultiplier = 0.4f;

    [Header("Heat & Cold Thresholds")]
    [SerializeField] private float freezeColdThreshold = -1f;
    [SerializeField] private float meltHeatThreshold = 0.5f;

    [Header("Transition Settings")]
    [SerializeField] private float heatDecayRate = 1f;
    [SerializeField] private float coldDecayRate = 1f;

    [Header("Layer Settings")]
    [SerializeField] private int meltedLayer = 7;
    [SerializeField] private int frozenLayer = 6;

    private bool isFrozen = false;
    private bool isMelted = true;
    private float heatLevel = 1f;
    private PlayerController affectedPlayer;

    private void Start()
    {
        SetMelted(true);
    }

    private void Update()
    {
        if (Mathf.Abs(heatLevel) > 0.01f)
            heatLevel = Mathf.MoveTowards(
                heatLevel,
                0f,
                Time.deltaTime * (heatLevel > 0 ? heatDecayRate : coldDecayRate)
            );

        ApplyStateByTemperature();
        UpdatePlayerSlowEffect();
    }

    private void ApplyStateByTemperature()
    {
        if (heatLevel <= freezeColdThreshold && !isFrozen)
        {
            SetFrozen(true);
        }
        else if (heatLevel >= meltHeatThreshold && !isMelted)
        {
            SetMelted(true);
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

        if (animator) animator.SetBool("IsMelted", melted);
        if (spriteRenderer) spriteRenderer.color = melted ? meltedColor : frozenColor;

        int targetLayer = melted ? meltedLayer : frozenLayer;
        SetLayerRecursively(gameObject, targetLayer);
    }

    private void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        isMelted = !frozen;

        if (animator) animator.SetBool("IsFrozen", frozen);
        if (spriteRenderer) spriteRenderer.color = frozen ? frozenColor : meltedColor;

        int targetLayer = frozen ? frozenLayer : meltedLayer;
        SetLayerRecursively(gameObject, targetLayer);
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        int safeLayer = Mathf.Clamp(layer, 0, 31);
        obj.layer = safeLayer;

        foreach (Transform child in obj.transform)
        {
            if (child != null)
                SetLayerRecursively(child.gameObject, safeLayer);
        }
    }

    public void ApplyHeat(float delta)
    {
        heatLevel += delta;
    }

    public void ApplyCold(float delta)
    {
        heatLevel -= delta;
    }

    public void CoolDown(float delta)
    {
        heatLevel = Mathf.MoveTowards(heatLevel, 0f, delta);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            affectedPlayer = other.GetComponent<PlayerController>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (affectedPlayer != null)
                affectedPlayer.RemoveSpeedModifier(this);

            affectedPlayer = null;
        }
    }
}
