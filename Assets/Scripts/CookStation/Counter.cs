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

    [Header("SFX Keys")]
    public string sfxPlace = "SFX_Counter_Place";
    public string sfxTake = "SFX_Counter_Take";

    public override void Interact(PlayerInventory player)
    {
        PlayerController pc = player.GetComponent<PlayerController>();

        // =========================
        // PLAYER PLACES BOWL
        // =========================
        if (player.HasBowl() && bowlOnCounter == null)
        {
            bowlOnCounter = player.TakeBowl();
            player.lastCounterPlaced = this;

            animator?.SetTrigger(AnimPlace);
            animator?.SetBool(HasBowl, true);

            // ?? PLACE SOUND
            PlaySFX(sfxPlace);

            iconUI?.Refresh(bowlOnCounter);
            pc?.RefreshCarryAnimation();
            return;
        }

        // =========================
        // BLOCK: HOLDING SERVE BOX
        // =========================
        if (player.HasServeBox())
        {
            NotificationUI.Instance?.Show(
                "Put the serve box down first!",
                NotifyType.Warning
            );
            return;
        }

        // =========================
        // PLAYER TAKES BOWL
        // =========================
        if (!player.HasBowl() && bowlOnCounter != null)
        {
            player.GiveBowl(bowlOnCounter);
            bowlOnCounter = null;

            animator?.SetTrigger(AnimTake);
            animator?.SetBool(HasBowl, false);

            // ?? TAKE SOUND
            PlaySFX(sfxTake);

            iconUI?.Clear();
            pc?.RefreshCarryAnimation();
            return;
        }
    }

    // =========================
    // RETURN BOWL (AUTO)
    // =========================
    public void ReceiveReturnedBowl(ContainerData data)
    {
        bowlOnCounter = data;

        if (animator)
        {
            animator.SetBool(HasBowl, true);
            animator.SetTrigger(AnimPlace);
        }

        // ?? PLACE SOUND
        PlaySFX(sfxPlace);

        iconUI?.Refresh(bowlOnCounter);
    }

    // =========================
    // ?? SFX HELPER
    // =========================
    private void PlaySFX(string key)
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.PlaySFXAt(
            key,
            transform.position,
            true
        );
    }
}
