using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemySweeper : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public bool faceLeft = false;

    [Header("Damage Settings")]
    public LayerMask playerLayer;
    public int instantKillDamage = 9999;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private bool isDead = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        sprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        yield return null; // give one frame

        while (!isDead)
        {
            Vector2 dir = faceLeft ? Vector2.left : Vector2.right;

            rb.linearVelocity = dir * moveSpeed;

            if (sprite != null)
                sprite.flipX = (dir.x < 0);

            yield return null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            var dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(instantKillDamage);
            return;
        }

        if (other.CompareTag("Transition"))
        {
            return;
        }
    }

    public void ShakeCamera()
    {
        var impulse = GetComponent<CinemachineImpulseSource>();
        if (impulse != null)
        {
            impulse.GenerateImpulse();
        }
    }
}
