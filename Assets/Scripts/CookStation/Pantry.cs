using UnityEngine;

public class Pantry : InteractStation
{
    public PantryUI pantryUI;
    public Animator animator;

    private static readonly int AnimOpen = Animator.StringToHash("Open");
    private static readonly int AnimClose = Animator.StringToHash("Close");

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);

        if (other.CompareTag("Player"))
        {
            animator?.SetTrigger(AnimOpen);
        }
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);

        if (other.CompareTag("Player"))
        {
            animator?.SetTrigger(AnimClose);
        }
    }

    public override void Interact(PlayerInventory player)
    {
        if (!player.HasBowl())
        {
            NotificationUI.Instance.Show(
                "You need a bowl first!",
                NotifyType.Warning
            );
            return;
        }

        LockInteraction();

        player.GetComponent<PlayerController>()?.RefreshCarryAnimation();

        var items = PantryDatabase.Instance.GetAllIngredients();
        pantryUI.Open(items.ToArray(), player, this);
    }


    public void OnPantryClosed()
    {
        animator?.SetTrigger(AnimClose);

        UnlockInteraction();
    }
}
