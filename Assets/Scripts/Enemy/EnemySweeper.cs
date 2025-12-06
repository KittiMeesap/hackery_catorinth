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
    private SpriteRenderer sprite;
    private bool canMove = false;
    private bool isDead = false;

    private IOpenableDoor lastDoorUsed = null;

    private float laneY;

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

            while (Vector2.Distance(new Vector2(transform.position.x, laneY),
                                    new Vector2(target.position.x, laneY)) > stopDistanceToDoor
                   && !isDead)
            {
                Vector3 pos = transform.position;
                pos.y = laneY;
                transform.position = pos;

                float dx = target.position.x - transform.position.x;
                Vector2 dir = new Vector2(Mathf.Sign(dx), 0f);

                rb.linearVelocity = dir * moveSpeed;

                if (sprite != null)
                    sprite.flipX = dir.x < 0;

                yield return null;
            }

            rb.linearVelocity = Vector2.zero;

            // Trigger melting on door
            var heatObj = target.GetComponentInParent<IHeatable>();
            if (heatObj != null)
                heatObj.ApplyHeat(999f);

            // Check if door can open
            var door = target.GetComponent<IOpenableDoor>();
            if (door == null)
                door = target.GetComponentInParent<IOpenableDoor>();

            if (door != null && door.CanOpenFor(gameObject))
            {
                if (door != lastDoorUsed)
                {
                    door.OpenForEntity(gameObject);

                    laneY = transform.position.y;

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

        var heat = other.GetComponentInParent<IHeatable>();
        if (heat != null)
            heat.ApplyHeat(999f);

        if (other.CompareTag("Transition"))
            return;
    }

    public void ShakeCamera()
    {
        var impulse = GetComponent<CinemachineImpulseSource>();
        if (impulse != null)
            impulse.GenerateImpulse();
    }
}
