using UnityEngine;
using UnityEngine.InputSystem;

public abstract class InteractStation : MonoBehaviour
{
    [Header("UI Prompt & Highlight")]
    public GameObject promptUI;
    public GameObject highlightObj;

    protected bool isPlayerInside = false;
    protected PlayerInventory playerInv;

    public static bool interactionLocked = false;

    private InputAction interactAction;

    protected virtual void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
        if (highlightObj != null) highlightObj.SetActive(false);

        if (QTEManager.Instance != null)
        {
            interactAction = QTEManager.Instance.input.Player.Interact;
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInv = other.GetComponent<PlayerInventory>();
        isPlayerInside = true;

        if (!interactionLocked)
        {
            if (promptUI != null) promptUI.SetActive(true);
            if (highlightObj != null) highlightObj.SetActive(true);
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInside = false;

        if (promptUI != null) promptUI.SetActive(false);
        if (highlightObj != null) highlightObj.SetActive(false);

        playerInv = null;
    }

    private void Update()
    {
        if (!isPlayerInside) return;
        if (interactionLocked) return;
        if (interactAction == null) return;

        // Correct Input System way
        if (interactAction.WasPerformedThisFrame())
        {
            if (promptUI != null) promptUI.SetActive(false);
            if (highlightObj != null) highlightObj.SetActive(false);

            TryInteract();
        }
    }

    private void TryInteract()
    {
        if (playerInv != null)
            Interact(playerInv);
    }

    public void LockInteraction()
    {
        interactionLocked = true;

        if (promptUI != null) promptUI.SetActive(false);
        if (highlightObj != null) highlightObj.SetActive(false);
    }

    public void UnlockInteraction()
    {
        interactionLocked = false;

        if (isPlayerInside)
        {
            if (promptUI != null) promptUI.SetActive(true);
            if (highlightObj != null) highlightObj.SetActive(true);
        }
    }

    public abstract void Interact(PlayerInventory player);
}
