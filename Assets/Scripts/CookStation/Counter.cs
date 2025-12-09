using UnityEngine;

public class Counter : InteractStation
{
    [Header("Item Slot")]
    public ContainerData bowlOnCounter;

    [Header("Animation")]
    public Animator animator;
    private static readonly int AnimPlace = Animator.StringToHash("Place");
    private static readonly int AnimTake = Animator.StringToHash("Take");
    private static readonly int HasBowl = Animator.StringToHash("HasBowl");

    [Header("UI Icon")]
    public CounterIconUI iconUI;

    public override void Interact(PlayerInventory player)
    {
        PlayerController pc = player.GetComponent<PlayerController>();

        // === PLAYER PLACES BOWL ===
        if (player.HasBowl() && bowlOnCounter == null)
        {
            bowlOnCounter = player.TakeBowl();
            player.lastCounterPlaced = this;

            animator?.SetTrigger(AnimPlace);
            animator?.SetBool(HasBowl, true);

            iconUI?.Refresh(bowlOnCounter);
            pc?.RefreshCarryAnimation();
            return;
        }

        // === PLAYER TRIES TO TAKE BOWL WHILE HOLDING SERVE BOX ===
        if (player.HasServeBox())
        {
            Debug.Log("Counter: Cannot take bowl while holding a serve box!");
            return;
        }

        // === PLAYER TAKES BOWL BACK ===
        if (!player.HasBowl() && bowlOnCounter != null)
        {
            player.GiveBowl(bowlOnCounter);
            bowlOnCounter = null;

            animator?.SetTrigger(AnimTake);
            animator?.SetBool(HasBowl, false);

            iconUI?.Clear();
            pc?.RefreshCarryAnimation();
            return;
        }
    }

    public void ReceiveReturnedBowl(ContainerData data)
    {
        bowlOnCounter = data;

        if (animator)
        {
            animator.SetBool(HasBowl, true);
            animator.SetTrigger(AnimPlace);
        }

        iconUI?.Refresh(bowlOnCounter);

        Debug.Log("Counter: Received returned bowl.");
    }
}
