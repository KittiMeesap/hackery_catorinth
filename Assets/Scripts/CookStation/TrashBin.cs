using UnityEngine;

public class TrashBin : InteractStation
{
    public Animator animator;

    private static readonly int AnimUse = Animator.StringToHash("Use");
    private static readonly int AnimOpen = Animator.StringToHash("Open");
    private static readonly int AnimClose = Animator.StringToHash("Close");

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);

        if (other.CompareTag("Player"))
            animator?.SetTrigger(AnimOpen);
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);

        if (other.CompareTag("Player"))
            animator?.SetTrigger(AnimClose);
    }

    public override void Interact(PlayerInventory player)
    {
        animator?.SetTrigger(AnimUse);

        PlayerController pc = player.GetComponent<PlayerController>();

        if (player.HasServeBox())
        {
            player.serveBox = null;
            player.OnInventoryChanged?.Invoke();
            pc?.RefreshCarryAnimation();

            Debug.Log("TrashBin: Serve box discarded.");
            return;
        }

        if (player.HasBowl())
        {
            if (player.bowl.contents.Count > 0)
            {
                player.bowl.Clear();
                player.OnInventoryChanged?.Invoke();
                pc?.RefreshCarryAnimation();

                Debug.Log("TrashBin: Ingredients cleared.");
                return;
            }

            Debug.Log("TrashBin: Bowl is empty, nothing to discard.");
            return;
        }

        Debug.Log("TrashBin: Nothing to discard.");
    }
}
