using System.Collections;
using UnityEngine;

public class SugarSlow : MonoBehaviour, IHeatable, IFreezable
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Layer Settings")]
    [SerializeField] private int frozenLayer = 14;
    [SerializeField] private int normalLayer = 0;

    [Header("Slow Settings")]
    [SerializeField] private float slowMultiplier = 0.4f;

    [Header("Temperature Reaction")]
    [SerializeField] private float heatThreshold = 2f;

    [SerializeField] private float coldThreshold = -2f;

    [SerializeField] private float meltDelay = 1.5f;

    private float temperature = 0f;
    private bool isFrozen = false;
    private bool isMelting = false;

    private PlayerController affectedPlayer;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isFrozen && temperature >= heatThreshold && !isMelting)
        {
            StartCoroutine(MeltRoutine());
        }

        if (!isFrozen && temperature <= coldThreshold)
        {
            SetFrozen(true);
        }

        if (Mathf.Abs(temperature) > 0.01f)
        {
            temperature = Mathf.MoveTowards(temperature, 0, Time.deltaTime * 0.5f);
        }
    }

    // Interface Implementations
    public void ApplyHeat(float amount)
    {
        temperature += amount;

        if (temperature >= heatThreshold && isFrozen && !isMelting)
        {
            StartCoroutine(MeltRoutine());
        }
    }

    public void ApplyCold(float amount)
    {
        temperature -= amount;

        if (temperature <= coldThreshold && !isFrozen)
        {
            SetFrozen(true);
        }
    }

    public void CoolDown(float amount)
    {
        temperature = Mathf.MoveTowards(temperature, 0, amount);
    }

    // Freeze / Melt Logic
    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        isMelting = false;

        if (animator != null)
        {
            animator.SetBool("IsFrozen", isFrozen);
        }

        int targetLayer = isFrozen ? frozenLayer : normalLayer;
        SetLayerRecursively(gameObject, targetLayer);

        if (affectedPlayer != null)
        {
            if (isFrozen)
                affectedPlayer.SetSpeedModifier(this, slowMultiplier);
            else
                affectedPlayer.RemoveSpeedModifier(this);
        }
    }

    private IEnumerator MeltRoutine()
    {
        isMelting = true;

        if (animator != null)
            animator.SetTrigger("Melt");

        yield return new WaitForSeconds(meltDelay);

        SetFrozen(false);
        isMelting = false;
    }

    // Player Slow Trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        affectedPlayer = other.GetComponent<PlayerController>();

        if (affectedPlayer != null && isFrozen)
            affectedPlayer.SetSpeedModifier(this, slowMultiplier);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var controller = other.GetComponent<PlayerController>();
        if (controller != null)
            controller.RemoveSpeedModifier(this);

        affectedPlayer = null;
    }

    private void OnDisable()
    {
        if (affectedPlayer != null)
        {
            affectedPlayer.RemoveSpeedModifier(this);
            affectedPlayer = null;
        }
    }

    // Utility
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        if (obj.layer != layer)
            obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            if (child != null && child.gameObject.layer != layer)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
