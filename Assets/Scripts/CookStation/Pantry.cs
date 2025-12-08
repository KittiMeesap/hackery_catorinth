using UnityEngine;

public class Pantry : InteractStation
{
    public PantryUI pantryUI;
    public Animator animator;

    private static readonly int Open = Animator.StringToHash("Open");
    private static readonly int Close = Animator.StringToHash("Close");

    public override void Interact(PlayerInventory player)
    {
        if (!player.HasBowl())
        {
            Debug.Log("Need a bowl.");
            return;
        }

        LockInteraction();

        player.GetComponent<PlayerController>().RefreshCarryAnimation();

        if (animator != null)
            animator.SetTrigger(Open);

        var items = PantryDatabase.Instance.GetAllIngredients();
        pantryUI.Open(items.ToArray(), player, this);
    }

    public void OnPantryClosed()
    {
        if (animator != null)
            animator.SetTrigger(Close);

        UnlockInteraction();
    }
}
