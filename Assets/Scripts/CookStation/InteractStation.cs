using UnityEngine;
using UnityEngine.InputSystem;

public abstract class InteractStation : MonoBehaviour
{
    [Header("UI Prompt & Highlight")]
    public GameObject promptUI;
    public GameObject highlightObj;

    protected bool isPlayerInside;
    protected PlayerInventory playerInv;

    public static bool interactionLocked;

    protected virtual void Awake()
    {
        interactionLocked = false;
    }

    protected virtual void Start()
    {
        if (promptUI) promptUI.SetActive(false);
        if (highlightObj) highlightObj.SetActive(false);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInv = other.GetComponent<PlayerInventory>();
        isPlayerInside = true;

        RefreshPrompt();
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInside = false;
        playerInv = null;

        if (promptUI) promptUI.SetActive(false);
        if (highlightObj) highlightObj.SetActive(false);
    }

    private void Update()
    {
        if (!isPlayerInside) return;
        if (interactionLocked) return;
        if (GameInput.Instance == null) return;

        var interactAction = GameInput.Instance.InteractAction;
        if (interactAction == null) return;

        if (interactAction.WasPerformedThisFrame())
        {
            HidePrompt();
            Interact(playerInv);
        }
    }

    protected void RefreshPrompt()
    {
        if (!isPlayerInside || interactionLocked) return;

        if (promptUI) promptUI.SetActive(true);
        if (highlightObj) highlightObj.SetActive(true);
    }

    protected void HidePrompt()
    {
        if (promptUI) promptUI.SetActive(false);
        if (highlightObj) highlightObj.SetActive(false);
    }

    public void LockInteraction()
    {
        interactionLocked = true;
        HidePrompt();
    }

    public void UnlockInteraction()
    {
        interactionLocked = false;
        RefreshPrompt();
    }

    public abstract void Interact(PlayerInventory player);
}
