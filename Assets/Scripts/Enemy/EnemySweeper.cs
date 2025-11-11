using System.Collections;
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
    private bool isMoving = false;
    private bool isDead = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    private void OnEnable()
    {
        StartCoroutine(SweepRoutine());
    }

    private IEnumerator SweepRoutine()
    {
        yield return new WaitForSeconds(0.2f);

        while (currentIndex < doorTargets.Length && !isDead)
        {
            Transform target = doorTargets[currentIndex];
            if (target == null)
            {
                currentIndex++;
                continue;
            }

            while (Vector2.Distance(transform.position, target.position) > stopDistanceToDoor && !isDead)
            {
                Vector2 dir = (target.position - transform.position).normalized;
                rb.linearVelocity = dir * moveSpeed;
                yield return null;
            }

            rb.linearVelocity = Vector2.zero;

            IOpenableDoor door = target.GetComponentInParent<IOpenableDoor>();
            if (door != null && door.CanOpenFor(gameObject))
            {
                door.OpenForEntity(gameObject);
                yield return new WaitForSeconds(0.25f);
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
}
