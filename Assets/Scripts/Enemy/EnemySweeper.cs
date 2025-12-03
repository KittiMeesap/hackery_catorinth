using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemySweeper : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public bool moveRight = true;

    [Header("End Settings")]
    public string endTag = "SweeperEnd";

    [Header("Damage Settings")]
    public LayerMask playerLayer;
    public int instantKillDamage = 9999;

    [Header("UI Pointer")]
    public GameObject uiPointerPrefab;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private bool canMove = false;
    private bool isDead = false;

    private float laneY;
    private IOpenableDoor lastDoorUsed = null;

    private UIEnemyPointer pointerUI;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        sprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        laneY = transform.position.y;
    }

    private void FixedUpdate()
    {
        if (!canMove || isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector3 pos = transform.position;
        pos.y = laneY;
        transform.position = pos;

        float dirX = moveRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dirX * moveSpeed, 0f);

        if (sprite != null)
            sprite.flipX = dirX < 0;
    }

    public void StartSweeping()
    {
        canMove = true;

        if (uiPointerPrefab != null && UIManager.Instance != null)
        {
            GameObject ui = Instantiate(uiPointerPrefab, UIManager.Instance.transform);
            pointerUI = ui.GetComponent<UIEnemyPointer>();
            if (pointerUI != null)
                pointerUI.enemyTarget = transform;
        }
    }

    public void StopSweeping()
    {
        canMove = false;
        rb.linearVelocity = Vector2.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if ((playerLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            var dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(instantKillDamage);
            return;
        }

        var heat = other.GetComponentInParent<IHeatable>();
        if (heat != null)
            heat.ApplyHeat(999f);

        if (!string.IsNullOrEmpty(endTag) && other.CompareTag(endTag))
        {
            DestroySelf();
            return;
        }

        var chocoDoor = other.GetComponentInParent<ChocolateDoor>();
        if (chocoDoor != null)
        {
            DestroySelf();
            return;
        }

        if (other.CompareTag("Transition"))
            return;

        var door = other.GetComponent<IOpenableDoor>();
        if (door == null)
            door = other.GetComponentInParent<IOpenableDoor>();

        if (door != null && door != lastDoorUsed)
        {
            door.OpenForSweeper(gameObject);
            lastDoorUsed = door;

            laneY = transform.position.y;
        }
    }

    private void DestroySelf()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (pointerUI != null)
            Destroy(pointerUI.gameObject);
    }

    public void ShakeCamera()
    {
        var impulse = GetComponent<CinemachineImpulseSource>();
        if (impulse != null)
            impulse.GenerateImpulse();
    }
}
