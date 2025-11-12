using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemySweeper : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public Transform[] doorTargets;
    public float stopDistanceToDoor = 0.4f;

    [Header("Behaviour Settings")]
    public bool destroyOnFinish = true;

    [Header("Damage Settings")]
    public LayerMask playerLayer;
    public int instantKillDamage = 9999;

    private int currentIndex = 0;
    private Rigidbody2D rb;
    private bool canMove = false;
    private bool isDead = false;
    private SpriteRenderer sprite;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        sprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        StartCoroutine(SweepRoutine());
    }

    public void StartSweeping()
    {
        canMove = true;
    }

    private IEnumerator SweepRoutine()
    {
        yield return new WaitForSeconds(0.2f);

        while (!canMove)
            yield return null;

        while (currentIndex < doorTargets.Length && !isDead)
        {
            Transform doorPoint = doorTargets[currentIndex];

            while (Vector2.Distance(transform.position, doorPoint.position) > stopDistanceToDoor && !isDead)
            {
                Vector2 dir = (doorPoint.position - transform.position);
                dir.y = 0;
                dir = dir.normalized;
                rb.linearVelocity = dir * moveSpeed;

                if (sprite != null)
                    sprite.flipX = dir.x < 0;

                yield return null;
            }

            rb.linearVelocity = Vector2.zero;

            IOpenableDoor door = doorPoint.GetComponentInParent<IOpenableDoor>();
            if (door != null && door.CanOpenFor(gameObject))
            {
                door.OpenForEntity(gameObject);
            }

            SweeperDoorWarp warp = doorPoint.GetComponent<SweeperDoorWarp>();
            if (warp != null && warp.warpExitPoint != null)
            {
                if (sprite != null) sprite.enabled = false;

                yield return new WaitForSeconds(warp.appearDelay);

                transform.position = warp.warpExitPoint.position;

                if (sprite != null) sprite.enabled = true;

                yield return new WaitForSeconds(0.15f);
            }

            currentIndex++;
            yield return new WaitForSeconds(0.2f);
        }

        rb.linearVelocity = Vector2.zero;

        if (destroyOnFinish)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            var dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(instantKillDamage);
        }
    }

    public void ShakeCamera()
    {
        var impulse = GetComponent<CinemachineImpulseSource>();
        if (impulse != null)
        {
            impulse.GenerateImpulse();
            Debug.Log("[EnemySweeper] ShakeCamera triggered.");
        }
    }
}
