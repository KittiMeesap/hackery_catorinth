using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class VisionTriggerRelay : MonoBehaviour
{
    [SerializeField] private EnemyController owner;

    void Awake()
    {
        if (!owner) owner = GetComponentInParent<EnemyController>();
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // ensure trigger
    }

    void OnTriggerEnter2D(Collider2D other) => owner?.Vision_OnTriggerEnter2D(other);
    void OnTriggerStay2D(Collider2D other) => owner?.Vision_OnTriggerStay2D(other);
    void OnTriggerExit2D(Collider2D other) => owner?.Vision_OnTriggerExit2D(other);
}
