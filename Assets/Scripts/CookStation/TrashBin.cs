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

        // DISCARD SERVE BOX
        if (player.HasServeBox())
        {
            player.serveBox = null;
            player.OnInventoryChanged?.Invoke();

            pc?.RefreshCarryAnimation();
            pc?.ForceIdle();
            return;
        }

        // DISCARD INGREDIENTS IN BOWL
        if (player.HasBowl())
        {
            if (player.bowl.contents.Count > 0)
            {
                player.bowl.Clear();
                player.OnInventoryChanged?.Invoke();

                pc?.RefreshCarryAnimation();
                pc?.ForceIdle();
                return;
            }

            NotificationUI.Instance?.Show(
                "The bowl is already empty",
                NotifyType.Info
            );

            pc?.ForceIdle();
            return;
        }

        // NOTHING TO DISCARD
        NotificationUI.Instance?.Show(
            "Nothing to discard",
            NotifyType.Info
        );

        pc?.ForceIdle();
    }
}
