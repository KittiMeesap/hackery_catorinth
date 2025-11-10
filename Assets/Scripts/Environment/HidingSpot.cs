using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    [Header("Hiding Properties")]
    public bool isMovableContainer = false;

    protected bool isPlayerInside = false;
    protected PlayerHiding player;
    protected Vector2 playerOriginalPosition;

    public bool IsPlayerInside => isPlayerInside;

    public virtual void OnEnterHiding(PlayerHiding player)
    {
        isPlayerInside = true;
        this.player = player;
        playerOriginalPosition = player.transform.position;
        player.EnterHiding(this);
        PlayHidingAnimation(true);
    }

    public virtual void OnExitHiding(PlayerHiding player)
    {
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
}
