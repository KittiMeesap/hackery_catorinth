using UnityEngine;

public class Counter : InteractStation
{
    [Header("Item Slot")]
    public ContainerData bowlOnCounter;

    [Header("Animation")]
    public Animator animator;
    private static readonly int AnimPlace = Animator.StringToHash("Place");
    private static readonly int AnimTake = Animator.StringToHash("Take");

    [Header("UI Icon")]
    public CounterIconUI iconUI;

    public override void Interact(PlayerInventory player)
    {
        PlayerController pc = player.GetComponent<PlayerController>();

        // --- PLAYER PLACES BOWL ---
        if (player.HasBowl() && bowlOnCounter == null)
        {
            bowlOnCounter = player.TakeBowl();

            if (animator != null)
                animator.SetTrigger(AnimPlace);

            iconUI?.Refresh(bowlOnCounter);
            pc?.RefreshCarryAnimation();
            return;
        }

        // --- PLAYER TAKES BOWL BACK ---
        if (!player.HasBowl() && bowlOnCounter != null)
        {
            player.GiveBowl(bowlOnCounter);
            bowlOnCounter = null;

            if (animator != null)
                animator.SetTrigger(AnimTake);

            iconUI?.Clear();
            pc?.RefreshCarryAnimation();
            return;
        }
    }
}
