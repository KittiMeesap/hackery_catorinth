using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemySweeper : MonoBehaviour
{
    public float moveSpeed = 2f;
    public Transform[] doorTargets;
    public float stopDistanceToDoor = 0.4f;

    public bool destroyOnFinish = true;

    public LayerMask playerLayer;
    public int instantKillDamage = 9999;

    private int currentIndex = 0;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private bool canMove = false;
    private bool isDead = false;

    private IOpenableDoor lastDoorUsed = null;

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

                if (sprite != null)
                    sprite.flipX = dir.x < 0;

                yield return null;
            }

            rb.linearVelocity = Vector2.zero;

            var heatObj = target.GetComponentInParent<IHeatable>();
            if (heatObj != null)
            {
                heatObj.ApplyHeat(999f);
            }

            var door = target.GetComponent<IOpenableDoor>();
            if (door == null)
                door = target.GetComponentInParent<IOpenableDoor>();

            if (door != null && door.CanOpenFor(gameObject))
            {
                if (door != lastDoorUsed)
                {
                    door.OpenForEntity(gameObject);
                    lastDoorUsed = door;
                    yield return new WaitForSeconds(0.25f);
                }
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
            return;
        }

        var door = other.GetComponentInParent<ChocolateDoor>();
        if (door != null && door.CanOpenFor(gameObject))
        {
            door.WarpEntity(gameObject);
            return;
        }

        var heat = other.GetComponentInParent<IHeatable>();
        if (heat != null)
        {
            heat.ApplyHeat(999f);
        }

        if (other.CompareTag("Transition"))
            return;
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
