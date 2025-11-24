using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FogZone : MonoBehaviour
{
    [Header("Fog Particle")]
    [SerializeField] private ParticleSystem fogParticles;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerHiding>(out var player))
        {
            player.EnterSmoke();
        }

        if (other.TryGetComponent<EnemyController>(out var enemy))
        {
            enemy.EnterSmoke();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerHiding>(out var player))
        {
            player.ExitSmoke();
        }

        if (other.TryGetComponent<EnemyController>(out var enemy))
        {
            enemy.ExitSmoke();
        }
    }
}
