using System.Collections;
using UnityEngine;

public class EnemySweeper : EnemyController
{
    [Header("Sweep Settings")]
    public Transform[] doorTargets;

    public float stopDistanceToDoor = 0.4f;

    public bool destroyOnFinish = true;

    private int currentIndex = 0;
    private Coroutine sweepRoutine;

    private void OnEnable()
    {
        StopPatrol();
        chasing = false;

        sweepRoutine = StartCoroutine(SweepLoop());
    }

    private void OnDisable()
    {
        if (sweepRoutine != null)
            StopCoroutine(sweepRoutine);

        ResumePatrol();
    }

    private IEnumerator SweepLoop()
    {
        while (currentIndex < doorTargets.Length && !isDead)
        {
            Transform target = doorTargets[currentIndex];
            if (target == null)
            {
                currentIndex++;
                continue;
            }

            bool reached = false;
            GoToTarget(target, stopDistanceToDoor, (e) => reached = true);

            while (!reached && !isDead)
                yield return null;

            IOpenableDoor door = target.GetComponentInParent<IOpenableDoor>();
            if (door != null && door.CanOpenFor(gameObject))
            {
                door.OpenForEntity(gameObject);
                yield return new WaitForSeconds(0.25f);
            }

            currentIndex++;
            yield return new WaitForSeconds(0.1f);
        }

        if (destroyOnFinish)
            Destroy(gameObject);
        else
            ResumePatrol();
    }

    protected override void TryDamagePlayerOnContact(Collider2D other)
    {
        if (isDead || !dealContactDamage) return;
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        damageable.TakeDamage(9999);
    }
}
