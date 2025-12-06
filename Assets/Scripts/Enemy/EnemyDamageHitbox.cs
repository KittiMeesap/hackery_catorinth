using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyDamageHitbox : MonoBehaviour
{
    [SerializeField] private EnemyController owner;

    void Awake()
    {
        if (!owner) owner = GetComponentInParent<EnemyController>();
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // damage by trigger overlap
    }

    void OnTriggerEnter2D(Collider2D other) => owner?.Damage_OnTriggerEnter2D(other);
    void OnTriggerStay2D(Collider2D other) => owner?.Damage_OnTriggerStay2D(other);
}
