using UnityEngine;

public interface IInteractable
{
    Transform GetPromptPoint();
    float GetInteractRadius();
    void Interact();
}
