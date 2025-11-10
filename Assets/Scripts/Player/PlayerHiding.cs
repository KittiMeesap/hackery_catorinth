using UnityEngine;

public class PlayerHiding : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private PlayerController playerController;
    private HidingSpot currentSpot;

    public bool IsHidingInContainer { get; private set; } = false;

    private int smokeStack = 0;
    public bool IsHiddenBySmoke => smokeStack > 0;
    public bool IsHidden => IsHidingInContainer || IsHiddenBySmoke;

    public static PlayerHiding Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
    }

    public void SetHidingSpot(HidingSpot spot) => currentSpot = spot;
    public void ClearHidingSpot(HidingSpot spot) { if (currentSpot == spot) currentSpot = null; }

    public void EnterHiding(HidingSpot spot)
    {
        if (IsHidingInContainer) return;
        IsHidingInContainer = true;

        if (spriteRenderer)
            spriteRenderer.enabled = false;

        if (playerController)
        {
            if (spot.isMovableContainer)
            {
                playerController.SetFrozen(false);
                playerController.SetControlLocked(false);
            }
            else
            {
                playerController.SetFrozen(true);
                playerController.SetControlLocked(true);
            }

            playerController.SetPhoneOut(false);
        }

        transform.position = spot.GetHidingPosition();
        currentSpot = spot;

        GetComponent<PlayerHacking>()?.SetHackingDisabled(true);
    }


    public void ExitHiding(HidingSpot spot)
    {
        if (!IsHidingInContainer) return;
        if (currentSpot != spot) return;

        IsHidingInContainer = false;

        if (spriteRenderer)
            spriteRenderer.enabled = true;

        if (playerController)
        {
            playerController.SetFrozen(false);
            playerController.SetControlLocked(false);
        }

        transform.position = spot.GetExitPosition();
        currentSpot = null;

        GetComponent<PlayerHacking>()?.SetHackingDisabled(false);
    }

    public void EnterSmoke() => smokeStack++;
    public void ExitSmoke() => smokeStack = Mathf.Max(0, smokeStack - 1);
}
