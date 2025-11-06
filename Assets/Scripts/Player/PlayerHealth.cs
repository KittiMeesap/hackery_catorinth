using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Hearts")]
    [SerializeField] private int maxHearts = 3;
    [SerializeField] private int currentHearts = 3;

    public int MaxHearts => maxHearts;
    public int CurrentHearts => currentHearts;
    public event Action<int, int> OnHealthChanged;

    [Header("Invincibility Frames")]
    [SerializeField] private float invincibleDuration = 1.5f;
    [SerializeField] private float flashInterval = 0.1f;

    public enum FlashMode { Toggle, Pulse }

    [Header("Flash Settings")]
    [SerializeField] private FlashMode flashMode = FlashMode.Toggle;
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    [SerializeField] private Color flashColor = Color.white;
    [Range(0f, 1f)]
    [SerializeField] private float pulseIntensity = 1f;

    [SerializeField] private bool useFlashKick = false;
    [SerializeField] private float flashKickHold = 0.08f;
    [SerializeField] private float flashKickFade = 0.15f;

    [Header("Damage Reaction")]
    [SerializeField] private float knockbackForce = 5f;

    [Header("Audio Keys")]
    [SerializeField] private string hurtSFXKey = "SFX_PlayerHurt";
    [SerializeField] private string deathSFXKey = "SFX_PlayerDeath";

    [Header("UI")]
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private HeartUI heartUI;

    private bool isInvincible = false;
    private bool isDead = false;
    private Color[] originalColors;

    private Rigidbody2D rb;
    private Animator anim;

    private void Awake()
    {
        currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);
        CacheOriginalColors();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHearts, maxHearts);
        if (heartUI == null) heartUI = FindFirstObjectByType<HeartUI>();
        if (gameOverUI == null) gameOverUI = FindFirstObjectByType<GameOverUI>();
    }

    private void CacheOriginalColors()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHearts = Mathf.Min(maxHearts, currentHearts + amount);
        OnHealthChanged?.Invoke(currentHearts, maxHearts);
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, transform.position - Vector3.right);
    }

    public void TakeDamage(int amount, Vector2 damageSource)
    {
        if (isDead || isInvincible || amount <= 0) return;

        currentHearts = Mathf.Max(0, currentHearts - amount);
        OnHealthChanged?.Invoke(currentHearts, maxHearts);

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(hurtSFXKey))
            AudioManager.Instance.PlaySFX(hurtSFXKey);
        else
            Debug.LogWarning("[PlayerHealth] Hurt sound key missing or AudioManager not found.");

        if (currentHearts <= 0)
        {
            Die();
            return;
        }

        if (rb != null)
        {
            Vector2 knockDir = ((Vector2)transform.position - damageSource).normalized;
            rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);
        }

        if (useFlashKick)
            StartCoroutine(FlashOnceRoutine(flashKickHold, flashKickFade));

        StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        float t = 0f;
        bool toggle = false;

        if (flashMode == FlashMode.Toggle)
        {
            while (t < invincibleDuration)
            {
                toggle = !toggle;
                SetFlashToggle(toggle);
                t += flashInterval;
                yield return new WaitForSeconds(flashInterval);
            }
            SetFlashOff();
        }
        else
        {
            while (t < invincibleDuration)
            {
                float phase = Mathf.PingPong(t / Mathf.Max(0.0001f, flashInterval), 1f) * pulseIntensity;
                SetFlashPulse(phase);
                t += Time.deltaTime;
                yield return null;
            }
            SetFlashOff();
        }

        isInvincible = false;
    }

    private void SetFlashToggle(bool flashOn)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].color = flashOn ? flashColor : originalColors[i];
    }

    private void SetFlashPulse(float amount01)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].color = Color.Lerp(originalColors[i], flashColor, amount01);
    }

    private void SetFlashOff()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].color = originalColors[i];
    }

    private IEnumerator FlashOnceRoutine(float hold, float fade)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].color = flashColor;

        yield return new WaitForSeconds(Mathf.Max(0f, hold));

        float t = 0f;
        fade = Mathf.Max(0.0001f, fade);
        while (t < fade)
        {
            float a = t / fade;
            for (int i = 0; i < spriteRenderers.Length; i++)
                spriteRenderers[i].color = Color.Lerp(flashColor, originalColors[i], a);
            t += Time.deltaTime;
            yield return null;
        }

        SetFlashOff();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(deathSFXKey))
            AudioManager.Instance.PlaySFX(deathSFXKey);
        else
            Debug.LogWarning("[PlayerHealth] Death sound key missing or AudioManager not found.");

        PlayerController.Instance?.SetFrozen(true);
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (gameOverUI != null) gameOverUI.Show();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void TryDamagePlayer(int amount)
    {
        var ph = FindFirstObjectByType<PlayerHealth>();
        ph?.TakeDamage(amount);
    }

    public static void TryDamagePlayer(int amount, Vector2 damageSource)
    {
        var ph = FindFirstObjectByType<PlayerHealth>();
        ph?.TakeDamage(amount, damageSource);
    }
}
