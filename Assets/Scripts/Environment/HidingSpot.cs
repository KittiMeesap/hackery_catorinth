using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    [Header("Hiding Properties")]
    public bool isMovableContainer = false;

    [Header("Cooldown Settings")]
    [SerializeField]
    private float exitCooldown = 0.3f;

    [System.NonSerialized]
    protected float lastHideTime;

    protected bool isPlayerInside = false;
    protected PlayerHiding player;
    protected Vector2 playerOriginalPosition;
    private float lastHideStartTime;

    public bool IsPlayerInside => isPlayerInside;

    public virtual void OnEnterHiding(PlayerHiding player)
    {
        if (isPlayerInside) return;
        isPlayerInside = true;

        this.player = player;
        playerOriginalPosition = player.transform.position;
        lastHideStartTime = Time.time;

        player.EnterHiding(this);
        PlayHidingAnimation(true);
    }

    public virtual void OnExitHiding(PlayerHiding player)
    {
        if (player == null) return;
        if (!isPlayerInside) return;

        if (Time.time < lastHideStartTime + exitCooldown)
            return;

        isPlayerInside = false;
        this.player = null;

        player.ExitHiding(this);
        PlayHidingAnimation(false);
    }

    public virtual Vector2 GetHidingPosition() => transform.position;
    public virtual Vector2 GetExitPosition() => playerOriginalPosition;

    protected virtual void PlayHidingAnimation(bool isHiding) { }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            other.GetComponent<PlayerHiding>()?.SetHidingSpot(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            other.GetComponent<PlayerHiding>()?.ClearHidingSpot(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(GetHidingPosition(), 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetExitPosition(), 0.1f);
    }
#endif
}
